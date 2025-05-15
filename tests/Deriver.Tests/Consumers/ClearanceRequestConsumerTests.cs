using Defra.TradeImportsDataApi.Api.Client;
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
        var apiClient = NSubstitute.Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = NSubstitute.Substitute.For<IDecisionService>();
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            decisionService,
            apiClient
        );

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        apiClient
            .GetCustomsDeclaration(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient.GetImportPreNotificationsByMrn(createdEvent.ResourceId, Arg.Any<CancellationToken>()).Returns([]);

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
        var apiClient = NSubstitute.Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = NSubstitute.Substitute.For<IDecisionService>();
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            decisionService,
            apiClient
        );

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        apiClient
            .GetCustomsDeclaration(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient.GetImportPreNotificationsByMrn(createdEvent.ResourceId, Arg.Any<CancellationToken>()).Returns([]);

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
        var apiClient = NSubstitute.Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = NSubstitute.Substitute.For<IDecisionService>();
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            decisionService,
            apiClient
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
        var apiClient = NSubstitute.Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = NSubstitute.Substitute.For<IDecisionService>();
        var consumer = new ClearanceRequestConsumer(
            NullLogger<ClearanceRequestConsumer>.Instance,
            decisionService,
            apiClient
        );

        var createdEvent = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        apiClient
            .GetCustomsDeclaration(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient.GetImportPreNotificationsByMrn(createdEvent.ResourceId, Arg.Any<CancellationToken>()).Returns([]);

        var decisionResult = new DecisionResult();
        decisionResult.AddDecision("mrn", 1, "docref", "checkCode", DecisionCode.C03);
        decisionService.Process(Arg.Any<DecisionContext>(), Arg.Any<CancellationToken>()).Returns(decisionResult);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(3);
    }
}
