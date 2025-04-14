using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Consumers;

public class ImportPreNotificationConsumerTests
{
    [Fact]
    public void OnHandle_ReturnsTaskCompleted()
    {
        var consumer = new ImportPreNotificationConsumer(
            NullLogger<ImportPreNotificationConsumer>.Instance,
            null!,
            null!
        );

        var importNotification = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var result = consumer.OnHandle(importNotification, CancellationToken.None);
        Assert.Equal(Task.CompletedTask, result);
    }
}
