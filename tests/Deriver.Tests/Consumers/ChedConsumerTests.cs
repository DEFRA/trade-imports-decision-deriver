using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Processors;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Trade.Gateway.Api.Contract.Certificate;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Consumers;

public class ChedConsumerTests
{
    private const string ChedId = "CHEDP.GB.2025.1234567";

    [Fact]
    public async Task GivenMultipleMrnsForOneChed_ThenTheChedIsReturnedOnce()
    {
        var ched = TracesChedFixtures.TracesChedEntityFixture(ChedId).Ched;

        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        apiClient.GetTracesChedsByMrn(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => GetChedsAsync());

        var consumer = new TestChedConsumer(apiClient);
        var mrns = Enumerable.Range(0, 20).Select(i => $"MRN{i}").ToArray();

        for (var run = 0; run < 200; run++)
        {
            var cheds = await consumer.GetCheds(mrns);

            cheds.Should().HaveCount(1);
        }

        async Task<TracesChedsResponse> GetChedsAsync()
        {
            await Task.Delay(20);

            return new TracesChedsResponse([new TracesChedResponse(ched, DateTime.Now, DateTime.Now)]);
        }
    }

    [Fact]
    public async Task GivenATracesChedEventWithMultipleMrnsForOneNotification_ThenItReturnsUniqueNotifications()
    {
        var notification = ImportPreNotificationFixtures.ImportPreNotificationFixture(ChedId);

        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        apiClient
            .GetCustomsDeclarationsByTracesChedId(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(DeclarationsFor("mrn1", "mrn2", "mrn3"));
        apiClient
            .GetImportPreNotificationsByMrn(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(NotificationsFor(notification));
        apiClient
            .GetTracesChedsByMrn(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TracesChedsResponse([]));

        var decisionService = Substitute.For<IDecisionService>();
        DecisionContext? context = null;
        decisionService.Process(Arg.Do<DecisionContext>(x => context = x)).Returns([]);

        var consumer = new TracesChedConsumer(NullLogger<TracesChedConsumer>.Instance, apiClient, decisionService);

        await consumer.OnHandle(TracesChedFixtures.TracesChedCreatedFixture(), CancellationToken.None);

        context!.Notifications.Should().HaveCount(1);
    }

    [Fact]
    public async Task GivenAnImportPreNotificationEventWithMultipleMrnsForOneNotification_ThenItReturnsUniqueNotifications()
    {
        var notification = ImportPreNotificationFixtures.ImportPreNotificationFixture(ChedId);

        var apiClient = Substitute.For<ITradeImportsDataApiClient>();
        apiClient
            .GetCustomsDeclarationsByChedId(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(DeclarationsFor("mrn1", "mrn2", "mrn3"));
        apiClient
            .GetImportPreNotificationsByMrn(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(NotificationsFor(notification));
        apiClient
            .GetTracesChedsByMrn(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TracesChedsResponse([]));

        var decisionService = Substitute.For<IDecisionService>();
        DecisionContext? context = null;
        decisionService.Process(Arg.Do<DecisionContext>(x => context = x)).Returns([]);

        var consumer = new ImportPreNotificationConsumer(
            NullLogger<ImportPreNotificationConsumer>.Instance,
            apiClient,
            decisionService
        );

        await consumer.OnHandle(
            ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture(),
            CancellationToken.None
        );

        context!.Notifications.Should().HaveCount(1);
    }

    private static CustomsDeclarationsResponse DeclarationsFor(params string[] mrns)
    {
        return new CustomsDeclarationsResponse([
            .. mrns.Select(mrn =>
                CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture(mrn) with
                {
                    Finalisation = null,
                }
            ),
        ]);
    }

    private static ImportPreNotificationsResponse NotificationsFor(ImportPreNotification notification)
    {
        return new ImportPreNotificationsResponse([
            new ImportPreNotificationResponse(notification, DateTime.Now, DateTime.Now),
        ]);
    }

    private sealed class TestChedConsumer(ITradeImportsDataApiClient apiClient)
        : ChedConsumer<TracesChedEvent>(apiClient, NullLogger.Instance)
    {
        public Task<List<DefraUNVTDCHEDProfile>> GetCheds(string[] mrns) => GetTracesCheds(mrns);

        public override Task OnHandle(ResourceEvent<TracesChedEvent> message, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
