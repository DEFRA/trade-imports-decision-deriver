using System.Text.Json;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using SlimMessageBus;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class ImportPreNotificationConsumer(ILogger<ImportPreNotificationConsumer> logger) : IConsumer<ResourceEvent<ImportNotification>>, IConsumerWithContext
{
    public Task OnHandle(ResourceEvent<ImportNotification> message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received notification: {Message}", JsonSerializer.Serialize(message));

        return Task.CompletedTask;
    }

    public IConsumerContext Context { get; set; } = null!;
}