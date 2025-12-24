using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.Processors;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ClearanceDecisionBuilder = Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.ClearanceDecisionBuilder;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Consumers;

public class ClearanceRequestConsumerTests
{
    [Fact]
    public async Task GivenACreatedEvent_AndNotImportPreNotificationsExist_ThenDecisionShouldBeCreated()
    {
        // ARRANGE
        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration = customsDeclaration with { Finalisation = null };
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = Substitute.For<IDecisionService>();
        var decisionServicev2 = Substitute.For<IDecisionServiceV2>();
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId"),
            decisionServicev2
        );

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        apiClient
            .GetCustomsDeclaration(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient
            .GetImportPreNotificationsByMrn(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new ImportPreNotificationsResponse([]));

        var decisionResult = new DecisionResult();
        decisionResult.AddDecision("mrn", 1, "docref", "docCode", "checkCode", DecisionCode.C03);
        decisionService.Process(Arg.Any<DecisionContext>(), Arg.Any<CancellationToken>()).Returns(decisionResult);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(3);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndNotImportPreNotificationsExist_AndDecisionAlreadyExists_ThenDecisionShouldNotBeSent()
    {
        // ARRANGE
        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration = customsDeclaration with { Finalisation = null };
        customsDeclaration = customsDeclaration with
        {
            ClearanceDecision = new ClearanceDecision()
            {
                DecisionNumber = 4,
                ExternalVersionNumber = customsDeclaration.ClearanceRequest?.ExternalVersion,
                Items =
                [
                    new ClearanceDecisionItem()
                    {
                        ItemNumber = 1,
                        Checks = [new ClearanceDecisionCheck() { CheckCode = "9115", DecisionCode = "C03" }],
                    },
                    new ClearanceDecisionItem()
                    {
                        ItemNumber = 2,
                        Checks = [new ClearanceDecisionCheck() { CheckCode = "9115", DecisionCode = "C03" }],
                    },
                    new ClearanceDecisionItem()
                    {
                        ItemNumber = 3,
                        Checks = [new ClearanceDecisionCheck() { CheckCode = "9115", DecisionCode = "C03" }],
                    },
                ],
            },
        };

        var results = new List<ClearanceDecisionResult>();
        var itemNumber = 1;
        foreach (var commodity in customsDeclaration.ClearanceRequest!.Commodities!)
        {
            foreach (var document in commodity.Documents!)
            {
                results.Add(
                    new ClearanceDecisionResult()
                    {
                        ItemNumber = itemNumber,
                        DocumentReference = document.DocumentReference!.Value,
                        CheckCode = "9115",
                        DecisionCode = "C03",
                    }
                );
            }

            itemNumber++;
        }

        customsDeclaration.ClearanceDecision.Results = results.ToArray();

        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = Substitute.For<IDecisionService>();
        var decisionServicev2 = Substitute.For<IDecisionServiceV2>();
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId"),
            decisionServicev2
        );

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        apiClient
            .GetCustomsDeclaration(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient
            .GetImportPreNotificationsByMrn(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new ImportPreNotificationsResponse([]));

        var decisionResult = new DecisionResult();
        for (var i = 0; i < (customsDeclaration.ClearanceRequest?.Commodities!).Length; i++)
        {
            var commodity = (customsDeclaration.ClearanceRequest?.Commodities!)[i];
            commodity.ItemNumber = i + 1;
            commodity.Checks = commodity.Checks!.Take(1).ToArray();
            commodity.Checks[0].CheckCode = "9115";
            foreach (var document in commodity.Documents!)
            {
                document.DocumentCode = "9115";
                decisionResult.AddDecision(
                    customsDeclaration.MovementReferenceNumber,
                    commodity.ItemNumber!.Value!,
                    document.DocumentReference!.Value,
                    document.DocumentCode,
                    commodity.Checks[0].CheckCode,
                    DecisionCode.C03
                );
            }
        }
        decisionService.Process(Arg.Any<DecisionContext>(), Arg.Any<CancellationToken>()).Returns(decisionResult);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(2);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndNotImportPreNotificationsExist_AndCustomsDeclarationAlreadyFinalised_ThenDecisionShouldNotBeSent()
    {
        // ARRANGE
        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration.ClearanceRequest!.MessageSentAt = DateTime.UtcNow;
        customsDeclaration.Finalisation!.MessageSentAt = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1));
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = Substitute.For<IDecisionService>();
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId"),
            new DecisionServiceV2(
                new Deriver.Decisions.V2.ClearanceDecisionBuilder(new CorrelationIdGenerator()),
                new CheckProcessor(new TestDecisionRulesEngineFactory())
            )
        );

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        apiClient
            .GetCustomsDeclaration(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(1);
        decisionService.ReceivedCalls().Count().Should().Be(0);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndNotImportPreNotificationsExist_AndCustomsDeclarationAlreadyFinalised_ButClearanceRequestTimeBeforeFinalisation_ThenDecisionShouldBeSent()
    {
        // ARRANGE
        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration.ClearanceRequest!.MessageSentAt = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1));
        customsDeclaration.Finalisation!.MessageSentAt = DateTime.UtcNow;
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = Substitute.For<IDecisionService>();
        var decisionServicev2 = Substitute.For<IDecisionServiceV2>();
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId"),
            decisionServicev2
        );

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        apiClient
            .GetCustomsDeclaration(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient
            .GetImportPreNotificationsByMrn(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new ImportPreNotificationsResponse([]));

        var decisionResult = new DecisionResult();
        decisionResult.AddDecision("mrn", 1, "docref", "docCode", "checkCode", DecisionCode.C03);
        decisionService.Process(Arg.Any<DecisionContext>(), Arg.Any<CancellationToken>()).Returns(decisionResult);
        decisionServicev2
            .Process(Arg.Any<DecisionContextV2>())
            .Returns(
                new List<(string, ClearanceDecision)>()
                {
                    new ValueTuple<string, ClearanceDecision>("mrn", new ClearanceDecision() { Items = [] }),
                }
            );

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(3);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndImportPreNotificationsExist_AndNoDecisionResult_ThenNoDecisionShouldBeCreated()
    {
        // ARRANGE
        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration = customsDeclaration with { Finalisation = null };
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = Substitute.For<IDecisionService>();
        var decisionServicev2 = Substitute.For<IDecisionServiceV2>();
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId"),
            decisionServicev2
        );

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        apiClient
            .GetCustomsDeclaration(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient
            .GetImportPreNotificationsByMrn(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(
                new ImportPreNotificationsResponse([
                    new ImportPreNotificationResponse(
                        new ImportPreNotification { ReferenceNumber = "chedRef" },
                        DateTime.UtcNow,
                        DateTime.UtcNow
                    ),
                ])
            );

        var decisionResult = new DecisionResult();
        decisionService
            .Process(
                Arg.Is<DecisionContext>(x => x.Notifications.Count == 1 && x.Notifications[0].Id == "chedRef"),
                Arg.Any<CancellationToken>()
            )
            .Returns(decisionResult);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(2);
    }
}
