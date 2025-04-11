using System.Text.Json;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using SlimMessageBus;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class ImportPreNotificationConsumer(ILogger<ImportPreNotificationConsumer> logger)
    : IConsumer<ResourceEvent<ImportPreNotification>>,
        IConsumerWithContext
{
    public Task OnHandle(ResourceEvent<ImportPreNotification> message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received notification: {Message}", JsonSerializer.Serialize(message));

        return Task.CompletedTask;
    }

    public IConsumerContext Context { get; set; } = null!;
}
