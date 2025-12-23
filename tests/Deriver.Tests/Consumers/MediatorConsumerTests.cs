using System.Text.Json;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.Processors;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SlimMessageBus.Host;
using ClearanceDecisionBuilder = Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.ClearanceDecisionBuilder;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Consumers;

public class MediatorConsumerTests
{
    [Fact]
    public async Task GivenACreatedEvent_WhenAUnknownResouceType_ThenShouldBeSkipped()
    {
        // ARRANGE
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = Substitute.For<IDecisionService>();
        var consumer = new ConsumerMediator(
            NullLoggerFactory.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId"),
            new DecisionServiceV2(
                new Deriver.Decisions.V2.ClearanceDecisionBuilder(new CorrelationIdGenerator()),
                new CheckProcessor(new TestDecisionRulesEngineFactory())
            )
        )
        {
            Context = new ConsumerContext
            {
                Headers = new Dictionary<string, object> { { MessageBusHeaders.ResourceType, "Unknown" } },
            },
        };

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        // ACT

        await consumer.OnHandle(JsonSerializer.Serialize(createdEvent), CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(0);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndNotImportPreNotificationsExist_ThenDecisionShouldBeCreated()
    {
        // ARRANGE
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = Substitute.For<IDecisionService>();
        var decisionServicev2 = Substitute.For<IDecisionServiceV2>();
        var consumer = new ConsumerMediator(
            NullLoggerFactory.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId"),
            decisionServicev2
        )
        {
            Context = new ConsumerContext
            {
                Headers = new Dictionary<string, object>
                {
                    { MessageBusHeaders.ResourceType, ResourceEventResourceTypes.CustomsDeclaration },
                },
            },
        };

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();
        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration = customsDeclaration with { Finalisation = null };
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

        await consumer.OnHandle(JsonSerializer.Serialize(createdEvent), CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(3);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationsExist_ThenDecisionShouldBeCreated()
    {
        // ARRANGE
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = Substitute.For<IDecisionService>();
        var decisionServicev2 = Substitute.For<IDecisionServiceV2>();
        var consumer = new ConsumerMediator(
            NullLoggerFactory.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId"),
            decisionServicev2
        )
        {
            Context = new ConsumerContext
            {
                Headers = new Dictionary<string, object>
                {
                    { MessageBusHeaders.ResourceType, ResourceEventResourceTypes.ImportPreNotification },
                },
            },
        };

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

        var decisionResult = new DecisionResult();
        decisionResult.AddDecision("mrn", 1, "docref", "docCode", "checkCode", DecisionCode.C03);
        decisionService.Process(Arg.Any<DecisionContext>(), Arg.Any<CancellationToken>()).Returns(decisionResult);

        // ACT
        await consumer.OnHandle(JsonSerializer.Serialize(createdEvent), CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(4);
    }
}
