using System.Text.Json;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration.ClearanceRequest;
using Defra.TradeImportsDataApi.Domain.Events;
using SlimMessageBus;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class ClearanceRequestConsumer(ILogger<ClearanceRequestConsumer> logger) : IConsumer<ResourceEvent<ClearanceRequest>>, IConsumerWithContext
{
    public Task OnHandle(ResourceEvent<ClearanceRequest> message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received notification: {Message}", JsonSerializer.Serialize(message));

        return Task.CompletedTask;
    }

    public IConsumerContext Context { get; set; } = null!;
}