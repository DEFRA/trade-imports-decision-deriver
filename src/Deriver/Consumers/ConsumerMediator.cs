using System.Text.Json;
using Btms.Business.Services.Decisions;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
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
            case ResourceTypes.ClearanceRequest:
            {
                var consumer = new ClearanceRequestConsumer(
                    loggerFactory.CreateLogger<ClearanceRequestConsumer>(),
                    decisionService,
                    apiClient
                )
                {
                    Context = Context,
                };
                var @event = message.Deserialize<ResourceEvent<ClearanceRequest>>();
                return consumer.OnHandle(@event!, cancellationToken);
            }
            case ResourceTypes.ImportNotification:
            {
                var consumer = new ImportPreNotificationConsumer(
                    loggerFactory.CreateLogger<ImportPreNotificationConsumer>(),
                    decisionService,
                    apiClient
                )
                {
                    Context = Context,
                };
                var @event = message.Deserialize<ResourceEvent<ImportPreNotification>>();
                return consumer.OnHandle(@event!, cancellationToken);
            }
        }

        _logger.LogWarning("No Consumer for Resource Type: {ResourceType}", Context.GetResourceType());
        return Task.CompletedTask;
    }

    public IConsumerContext Context { get; set; } = null!;
}
