using System.Text.Json;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using SlimMessageBus;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class ConsumerMediator(
    ILoggerFactory loggerFactory,
    IDecisionService decisionService,
    ITradeImportsDataApiClient apiClient
) : IConsumer<JsonElement>, IConsumerWithContext
{
    private readonly ILogger<ConsumerMediator> _logger = loggerFactory.CreateLogger<ConsumerMediator>();

    public Task OnHandle(JsonElement message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received notification: {Message}", JsonSerializer.Serialize(message));

        switch (Context.GetResourceType())
        {
            case ResourceEventResourceTypes.CustomsDeclaration:
            {
                var consumer = new ClearanceRequestConsumer(
                    loggerFactory.CreateLogger<ClearanceRequestConsumer>(),
                    decisionService,
                    apiClient
                )
                {
                    Context = Context,
                };
                var @event = message.Deserialize<ResourceEvent<object>>();
                return consumer.OnHandle(@event!, cancellationToken);
            }
            case ResourceEventResourceTypes.ImportPreNotification:
            {
                var consumer = new ImportPreNotificationConsumer(
                    loggerFactory.CreateLogger<ImportPreNotificationConsumer>(),
                    decisionService,
                    apiClient
                )
                {
                    Context = Context,
                };
                var @event = message.Deserialize<ResourceEvent<object>>();
                return consumer.OnHandle(@event!, cancellationToken);
            }
        }

        _logger.LogWarning("No Consumer for Resource Type: {ResourceType}", Context.GetResourceType());
        return Task.CompletedTask;
    }

    public IConsumerContext Context { get; set; } = null!;
}
