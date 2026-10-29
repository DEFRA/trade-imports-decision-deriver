using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Processors;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SlimMessageBus.Host;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Consumers;

public class IuuCertificationConsumerTests
{
    private readonly ITradeImportsDataApiClient _apiClient = Substitute.For<ITradeImportsDataApiClient>();
    private readonly IDecisionService _decisionService = Substitute.For<IDecisionService>();

    private IuuCertificationConsumer CreateConsumer()
    {
        return new IuuCertificationConsumer(
            NullLogger<IuuCertificationConsumer>.Instance,
            _apiClient,
            new ClearanceRequestConsumer(NullLogger<ClearanceRequestConsumer>.Instance, _apiClient, _decisionService)
        )
        {
            Context = new ConsumerContext(),
        };
    }

    private void SetupClearanceRequest(CustomsDeclarationResponse customsDeclaration)
    {
        _apiClient
            .GetCustomsDeclaration(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(customsDeclaration);

        _apiClient
            .GetImportPreNotificationsByMrn(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(new ImportPreNotificationsResponse([]));

        _apiClient
            .GetTracesChedsByMrn(customsDeclaration.MovementReferenceNumber, Arg.Any<CancellationToken>())
            .Returns(new TracesChedsResponse([]));
    }

    [Fact]
    public async Task GivenACreatedEvent_ThenRelatedImportDeclarationsShouldBeRequestedByIuuReference()
    {
        // ARRANGE
        var consumer = CreateConsumer();

        var createdEvent = IuuNotificationFixtures.IuuNotificationCreatedFixture();

        RelatedImportDeclarationsRequest? request = null;
        _apiClient
            .RelatedImportDeclarations(
                Arg.Do<RelatedImportDeclarationsRequest>(x => request = x),
                Arg.Any<CancellationToken>()
            )
            .Returns(IuuNotificationFixtures.RelatedImportDeclarationsResponseFixture());

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        request.Should().NotBeNull();
        request!.Iuu.Should().Be(createdEvent.ResourceId);
    }

    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationsNotExists_ThenDecisionShouldNotBeCreated()
    {
        // ARRANGE
        var consumer = CreateConsumer();

        var createdEvent = IuuNotificationFixtures.IuuNotificationCreatedFixture();

        _apiClient
            .RelatedImportDeclarations(Arg.Any<RelatedImportDeclarationsRequest>(), Arg.Any<CancellationToken>())
            .Returns(IuuNotificationFixtures.RelatedImportDeclarationsResponseFixture());

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        _apiClient.ReceivedCalls().Count().Should().Be(1);
        _decisionService.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationsExistWithoutClearanceRequest_ThenDecisionShouldNotBeCreated()
    {
        // ARRANGE
        var consumer = CreateConsumer();

        var createdEvent = IuuNotificationFixtures.IuuNotificationCreatedFixture();

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture() with
        {
            ClearanceRequest = null,
        };

        _apiClient
            .RelatedImportDeclarations(Arg.Any<RelatedImportDeclarationsRequest>(), Arg.Any<CancellationToken>())
            .Returns(IuuNotificationFixtures.RelatedImportDeclarationsResponseFixture(customsDeclaration));

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        _apiClient.ReceivedCalls().Count().Should().Be(1);
        _decisionService.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationExists_ThenDecisionShouldBeCreated()
    {
        // ARRANGE
        var consumer = CreateConsumer();

        var createdEvent = IuuNotificationFixtures.IuuNotificationCreatedFixture();

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture() with
        {
            Finalisation = null,
        };

        _apiClient
            .RelatedImportDeclarations(Arg.Any<RelatedImportDeclarationsRequest>(), Arg.Any<CancellationToken>())
            .Returns(IuuNotificationFixtures.RelatedImportDeclarationsResponseFixture(customsDeclaration));

        SetupClearanceRequest(customsDeclaration);

        _decisionService
            .Process(Arg.Any<DecisionContext>())
            .Returns([
                new ValueTuple<string, ClearanceDecision>(
                    customsDeclaration.MovementReferenceNumber,
                    new ClearanceDecision() { Items = [] }
                ),
            ]);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        _apiClient.ReceivedCalls().Count().Should().Be(5);
        await _apiClient
            .Received(1)
            .PutCustomsDeclaration(
                customsDeclaration.MovementReferenceNumber,
                Arg.Any<CustomsDeclaration>(),
                customsDeclaration.ETag,
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task GivenACreatedEvent_AndMultipleCustomsDeclarationsExist_ThenDecisionShouldBeCreatedForEach()
    {
        // ARRANGE
        var consumer = CreateConsumer();

        var createdEvent = IuuNotificationFixtures.IuuNotificationCreatedFixture();

        var customsDeclaration1 = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture("mrn1") with
        {
            Finalisation = null,
        };
        var customsDeclaration2 = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture("mrn2") with
        {
            Finalisation = null,
        };

        _apiClient
            .RelatedImportDeclarations(Arg.Any<RelatedImportDeclarationsRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                IuuNotificationFixtures.RelatedImportDeclarationsResponseFixture(
                    customsDeclaration1,
                    customsDeclaration2
                )
            );

        SetupClearanceRequest(customsDeclaration1);
        SetupClearanceRequest(customsDeclaration2);

        _decisionService
            .Process(Arg.Any<DecisionContext>())
            .Returns([new ValueTuple<string, ClearanceDecision>("mrn", new ClearanceDecision() { Items = [] })]);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        _apiClient.ReceivedCalls().Count().Should().Be(9);
        await _apiClient
            .Received(1)
            .PutCustomsDeclaration(
                customsDeclaration1.MovementReferenceNumber,
                Arg.Any<CustomsDeclaration>(),
                customsDeclaration1.ETag,
                Arg.Any<CancellationToken>()
            );
        await _apiClient
            .Received(1)
            .PutCustomsDeclaration(
                customsDeclaration2.MovementReferenceNumber,
                Arg.Any<CustomsDeclaration>(),
                customsDeclaration2.ETag,
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task GivenACreatedEvent_AndCustomsDeclarationAlreadyFinalised_ThenDecisionShouldNotBeSent()
    {
        // ARRANGE
        var consumer = CreateConsumer();

        var createdEvent = IuuNotificationFixtures.IuuNotificationCreatedFixture();

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();
        customsDeclaration.Finalisation!.MessageSentAt = customsDeclaration.ClearanceRequest!.MessageSentAt.AddDays(-1);

        _apiClient
            .RelatedImportDeclarations(Arg.Any<RelatedImportDeclarationsRequest>(), Arg.Any<CancellationToken>())
            .Returns(IuuNotificationFixtures.RelatedImportDeclarationsResponseFixture(customsDeclaration));

        SetupClearanceRequest(customsDeclaration);

        // ACT
        await consumer.OnHandle(createdEvent, CancellationToken.None);

        // ASSERT
        _apiClient.ReceivedCalls().Count().Should().Be(2);
        await _apiClient.DidNotReceiveWithAnyArgs().PutCustomsDeclaration(default!, default!, default, default);
    }
}
