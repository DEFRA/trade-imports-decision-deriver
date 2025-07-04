using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
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
        var decisionService = Substitute.For<IDecisionService>();
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId")
        );

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        apiClient
            .GetCustomsDeclaration(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient
            .GetImportPreNotificationsByMrn(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new ImportPreNotificationsResponse([]));

        var decisionResult = new DecisionResult();
        decisionResult.AddDecision("mrn", 1, "docref", "checkCode", DecisionCode.C03);
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
        customsDeclaration.ClearanceDecision!.SourceVersion =
            $"CR-VERSION-{customsDeclaration.ClearanceRequest?.ExternalVersion}";
        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = Substitute.For<IDecisionService>();
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId")
        );

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        apiClient
            .GetCustomsDeclaration(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient
            .GetImportPreNotificationsByMrn(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new ImportPreNotificationsResponse([]));

        var decisionResult = new DecisionResult();
        decisionResult.AddDecision("mrn", 1, "docref", "checkCode", DecisionCode.C03);
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
            new TestCorrelationIdGenerator("CorrelationId")
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
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId")
        );

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        apiClient
            .GetCustomsDeclaration(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient
            .GetImportPreNotificationsByMrn(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(new ImportPreNotificationsResponse([]));

        var decisionResult = new DecisionResult();
        decisionResult.AddDecision("mrn", 1, "docref", "checkCode", DecisionCode.C03);
        decisionService.Process(Arg.Any<DecisionContext>(), Arg.Any<CancellationToken>()).Returns(decisionResult);

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
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            decisionService,
            apiClient,
            new TestCorrelationIdGenerator("CorrelationId")
        );

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        apiClient
            .GetCustomsDeclaration(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient
            .GetImportPreNotificationsByMrn(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(
                new ImportPreNotificationsResponse(
                    [
                        new ImportPreNotificationResponse(
                            new ImportPreNotification { ReferenceNumber = "chedRef" },
                            DateTime.UtcNow,
                            DateTime.UtcNow
                        ),
                    ]
                )
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
