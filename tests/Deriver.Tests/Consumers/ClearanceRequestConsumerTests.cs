using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Processors;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

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
        var decisionServicev2 = Substitute.For<IDecisionService>();
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            apiClient,
            decisionServicev2
        );

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        apiClient
            .GetCustomsDeclaration(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient
            .GetImportPreNotificationsByMrn(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new ImportPreNotificationsResponse([]));

        decisionServicev2
            .Process(Arg.Any<DecisionContext>())
            .Returns([new ValueTuple<string, ClearanceDecision>("mrn", new ClearanceDecision() { Items = [] })]);

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
        var decisionServicev2 = Substitute.For<IDecisionService>();
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            apiClient,
            decisionServicev2
        );

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        apiClient
            .GetCustomsDeclaration(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient
            .GetImportPreNotificationsByMrn(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new ImportPreNotificationsResponse([]));

        decisionServicev2
            .Process(Arg.Any<DecisionContext>())
            .Returns([new ValueTuple<string, ClearanceDecision>("mrn", customsDeclaration.ClearanceDecision)]);

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
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            apiClient,
            new DecisionService(
                new ClearanceDecisionBuilder(new CorrelationIdGenerator()),
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
    }

    [Fact]
    public async Task GivenACreatedEvent_AndNotImportPreNotificationsExist_AndCustomsDeclarationAlreadyFinalised_ButClearanceRequestTimeBeforeFinalisation_ThenDecisionShouldBeSent()
    {
        // ARRANGE
        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration.ClearanceRequest!.MessageSentAt = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1));
        customsDeclaration.Finalisation!.MessageSentAt = DateTime.UtcNow;
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionServicev2 = Substitute.For<IDecisionService>();
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            apiClient,
            decisionServicev2
        );

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        apiClient
            .GetCustomsDeclaration(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient
            .GetImportPreNotificationsByMrn(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new ImportPreNotificationsResponse([]));

        decisionServicev2
            .Process(Arg.Any<DecisionContext>())
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
}
