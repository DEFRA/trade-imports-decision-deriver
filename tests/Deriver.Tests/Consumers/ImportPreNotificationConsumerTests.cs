using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.Processors;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SlimMessageBus.Host;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Consumers;

public class ImportPreNotificationConsumerTests
{
    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationsNotExists_ThenDecisionShouldNotBeCreated()
    {
        // ARRANGE
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var consumer = new ImportPreNotificationConsumer(
            NullLogger<ImportPreNotificationConsumer>.Instance,
            apiClient,
            new DecisionServiceV2(
                new Deriver.Decisions.V2.ClearanceDecisionBuilder(new CorrelationIdGenerator()),
                new CheckProcessor(new TestDecisionRulesEngineFactory())
            )
        )
        {
            Context = new ConsumerContext(),
        };

        var createdEvent = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();
        apiClient
            .GetCustomsDeclarationsByChedId(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new CustomsDeclarationsResponse([]));

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(1);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationsExist_AndVersionsAreTheSame_ThenDecisionShouldBeCreated()
    {
        // ARRANGE
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionServicev2 = Substitute.For<IDecisionServiceV2>();
        var consumer = new ImportPreNotificationConsumer(
            NullLogger<ImportPreNotificationConsumer>.Instance,
            apiClient,
            decisionServicev2
        );

        var createdEvent = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration = customsDeclaration with { Finalisation = null };
        apiClient
            .GetCustomsDeclarationsByChedId(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new CustomsDeclarationsResponse([customsDeclaration]));

        apiClient
            .GetImportPreNotificationsByMrn(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(
                new ImportPreNotificationsResponse([
                    new ImportPreNotificationResponse(
                        ImportPreNotificationFixtures.ImportPreNotificationFixture("test")!,
                        DateTime.Now,
                        DateTime.Now
                    ),
                ])
            );

        decisionServicev2
            .Process(Arg.Any<DecisionContextV2>())
            .Returns([new ValueTuple<string, ClearanceDecision>("mrn", customsDeclaration.ClearanceDecision!)]);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(4);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationsExist_ThenDecisionShouldBeCreated()
    {
        // ARRANGE
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionServicev2 = Substitute.For<IDecisionServiceV2>();
        var consumer = new ImportPreNotificationConsumer(
            NullLogger<ImportPreNotificationConsumer>.Instance,
            apiClient,
            decisionServicev2
        );

        var createdEvent = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration = customsDeclaration with { Finalisation = null };
        apiClient
            .GetCustomsDeclarationsByChedId(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new CustomsDeclarationsResponse([customsDeclaration]));

        apiClient
            .GetImportPreNotificationsByMrn(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(
                new ImportPreNotificationsResponse([
                    new ImportPreNotificationResponse(
                        ImportPreNotificationFixtures.ImportPreNotificationFixture(createdEvent.ResourceId)!,
                        DateTime.Now,
                        DateTime.Now
                    ),
                ])
            );

        decisionServicev2
            .Process(Arg.Any<DecisionContextV2>())
            .Returns([new ValueTuple<string, ClearanceDecision>("mrn", customsDeclaration.ClearanceDecision!)]);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(4);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationsExist_AndDecisionAlreadyExists_ThenDecisionShouldNotBeSent()
    {
        // ARRANGE
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionServicev2 = Substitute.For<IDecisionServiceV2>();
        var consumer = new ImportPreNotificationConsumer(
            NullLogger<ImportPreNotificationConsumer>.Instance,
            apiClient,
            decisionServicev2
        );

        var createdEvent = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var notification = ImportPreNotificationFixtures.ImportPreNotificationFixture("test");

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration.ClearanceRequest!.ExternalCorrelationId = "correlationId";
        customsDeclaration.ClearanceDecision!.CorrelationId = "correlationId";
        customsDeclaration.ClearanceRequest!.ExternalVersion = 22;
        customsDeclaration = customsDeclaration with
        {
            ClearanceDecision = new ClearanceDecision()
            {
                DecisionNumber = 4,
                ExternalVersionNumber = 22,
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

        customsDeclaration = customsDeclaration with { Finalisation = null };

        apiClient
            .GetCustomsDeclaration(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient
            .GetCustomsDeclarationsByChedId(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new CustomsDeclarationsResponse([customsDeclaration]));

        apiClient
            .GetImportPreNotificationsByMrn(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(
                new ImportPreNotificationsResponse([
                    new ImportPreNotificationResponse(notification, DateTime.Now, DateTime.Now),
                ])
            );

        decisionServicev2
            .Process(Arg.Any<DecisionContextV2>())
            .Returns([
                new ValueTuple<string, ClearanceDecision>(
                    customsDeclaration.MovementReferenceNumber,
                    customsDeclaration.ClearanceDecision!
                ),
            ]);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(3);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationsExist_AndAlreadyFinalised_ThenDecisionShouldNotBeSent()
    {
        // ARRANGE
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionServicev2 = Substitute.For<IDecisionServiceV2>();
        var consumer = new ImportPreNotificationConsumer(
            NullLogger<ImportPreNotificationConsumer>.Instance,
            apiClient,
            decisionServicev2
        );

        var createdEvent = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();

        apiClient
            .GetCustomsDeclarationsByChedId(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new CustomsDeclarationsResponse([customsDeclaration]));

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(1);
    }
}
