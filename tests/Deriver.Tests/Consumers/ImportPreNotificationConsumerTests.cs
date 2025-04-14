using Btms.Business.Services.Decisions;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Consumers;

public class ImportPreNotificationConsumerTests
{
    [Fact]
    public void OnHandle_ReturnsTaskCompleted()
    {
        var apiClient = NSubstitute.Substitute.For<ITradeImportsDataApiClient>();
        var decisionService = NSubstitute.Substitute.For<IDecisionService>();
        var consumer = new ImportPreNotificationConsumer(
            NullLogger<ImportPreNotificationConsumer>.Instance,
            decisionService, apiClient);

        var importNotification = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var result = consumer.OnHandle(importNotification, CancellationToken.None);
        Assert.Equal(Task.CompletedTask, result);
    }
}
