using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
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
        var apiClient = NSubstitute.Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = NSubstitute.Substitute.For<IDecisionService>();
        var consumer = new ImportPreNotificationConsumer(
            NullLogger<ImportPreNotificationConsumer>.Instance,
            decisionService,
            apiClient
        )
        {
            Context = new ConsumerContext(),
        };

        var createdEvent = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var decisionResult = new DecisionResult();
        decisionResult.AddDecision("mrn", 1, "docref", "checkCode", DecisionCode.C03);
        decisionService.Process(Arg.Any<DecisionContext>(), Arg.Any<CancellationToken>()).Returns(decisionResult);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(1);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationsExist_ThenDecisionShouldBeCreated()
    {
        // ARRANGE
        var apiClient = NSubstitute.Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = NSubstitute.Substitute.For<IDecisionService>();
        var consumer = new ImportPreNotificationConsumer(
            NullLogger<ImportPreNotificationConsumer>.Instance,
            decisionService,
            apiClient
        );

        var createdEvent = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        apiClient
            .GetCustomsDeclarationsByChedId(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns([customsDeclaration]);

        apiClient
            .GetImportPreNotificationsByMrn(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(
                [
                    new ImportPreNotificationResponse(
                        ImportPreNotificationFixtures.ImportPreNotificationFixture("test")!,
                        DateTime.Now,
                        DateTime.Now
                    ),
                ]
            );

        var decisionResult = new DecisionResult();
        decisionResult.AddDecision("mrn", 1, "docref", "checkCode", DecisionCode.C03);
        decisionService.Process(Arg.Any<DecisionContext>(), Arg.Any<CancellationToken>()).Returns(decisionResult);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(4);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationsExist_AndDecisionAlreadyExists_ThenDecisionShouldNotBeSent()
    {
        // ARRANGE
        var apiClient = NSubstitute.Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = NSubstitute.Substitute.For<IDecisionService>();
        var consumer = new ImportPreNotificationConsumer(
            NullLogger<ImportPreNotificationConsumer>.Instance,
            decisionService,
            apiClient
        );

        var createdEvent = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var notification = ImportPreNotificationFixtures.ImportPreNotificationFixture("test");

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration.ClearanceDecision!.SourceVersion =
            $"CR-VERSION-{customsDeclaration.ClearanceRequest?.ExternalVersion}";

        apiClient
            .GetCustomsDeclaration(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        apiClient
            .GetCustomsDeclarationsByChedId(createdEvent.ResourceId, Arg.Any<CancellationToken>())
            .Returns([customsDeclaration]);

        apiClient
            .GetImportPreNotificationsByMrn(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns([new ImportPreNotificationResponse(notification, DateTime.Now, DateTime.Now)]);

        var decisionResult = new DecisionResult();
        decisionResult.AddDecision("mrn", 1, "docref", "checkCode", DecisionCode.C03);
        decisionService.Process(Arg.Any<DecisionContext>(), Arg.Any<CancellationToken>()).Returns(decisionResult);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        apiClient.ReceivedCalls().Count().Should().Be(3);
    }
}
