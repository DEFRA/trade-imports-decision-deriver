using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.Processors;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.Logging;
using SlimMessageBus;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Consumers;

public class ConsumerMediator(
    ILoggerFactory loggerFactory,
    IDecisionService decisionService,
    ITradeImportsDataApiClient apiClient,
    ICorrelationIdGenerator correlationIdGenerator,
    IDecisionServiceV2 decisionServiceV2
) : IConsumer<string>, IConsumerWithContext
{
    private readonly ILogger<ConsumerMediator> _logger = loggerFactory.CreateLogger<ConsumerMediator>();

    public Task OnHandle(string received, CancellationToken cancellationToken)
    {
        switch (Context.GetResourceType())
        {
            case ResourceEventResourceTypes.CustomsDeclaration:
            {
                return HandleCustomsDeclaration(received, cancellationToken);
            }
            case ResourceEventResourceTypes.ImportPreNotification:
            {
                return HandleNotification(received, cancellationToken);
            }
        }

        _logger.LogWarning("No consumer for resource type {ResourceType}", Context.GetResourceType());

        return Task.CompletedTask;
    }

    private Task HandleNotification(string message, CancellationToken cancellationToken)
    {
        var consumer = new ImportPreNotificationConsumer(
            loggerFactory.CreateLogger<ImportPreNotificationConsumer>(),
            decisionService,
            apiClient,
            correlationIdGenerator,
            decisionServiceV2
        )
        {
            Context = Context,
        };

        var @event = MessageDeserializer.Deserialize<ResourceEvent<ImportPreNotificationEvent>>(
            message,
            Context.Headers.GetContentEncoding()
        );

        return consumer.OnHandle(@event!, cancellationToken);
    }

    private Task HandleCustomsDeclaration(string message, CancellationToken cancellationToken)
    {
        var consumer = new ClearanceRequestConsumer(
            loggerFactory.CreateLogger<ClearanceRequestConsumer>(),
            decisionService,
            apiClient,
            correlationIdGenerator,
            decisionServiceV2
        )
        {
            Context = Context,
        };

        var @event = MessageDeserializer.Deserialize<ResourceEvent<CustomsDeclarationEvent>>(
            message,
            Context.Headers.GetContentEncoding()
        );

        return consumer.OnHandle(@event!, cancellationToken);
    }

    public IConsumerContext Context { get; set; } = null!;
}
