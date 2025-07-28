using System.Text.Json;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Entities;
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
    ICorrelationIdGenerator correlationIdGenerator
) : IConsumer<string>, IConsumerWithContext
{
    private readonly ILogger<ConsumerMediator> _logger = loggerFactory.CreateLogger<ConsumerMediator>();

    public Task OnHandle(string received, CancellationToken cancellationToken)
    {
        var message = MessageDeserializer.Deserialize<JsonElement>(received, Context.Headers.GetContentEncoding());

        switch (Context.GetResourceType())
        {
            case ResourceEventResourceTypes.CustomsDeclaration:
            {
                return HandleCustomsDeclaration(message, cancellationToken);
            }
            case ResourceEventResourceTypes.ImportPreNotification:
            {
                return HandleNotification(message, cancellationToken);
            }
        }

        _logger.LogWarning("No consumer for resource type {ResourceType}", Context.GetResourceType());

        return Task.CompletedTask;
    }

    private Task HandleNotification(JsonElement message, CancellationToken cancellationToken)
    {
        var consumer = new ImportPreNotificationConsumer(
            loggerFactory.CreateLogger<ImportPreNotificationConsumer>(),
            decisionService,
            apiClient,
            correlationIdGenerator
        )
        {
            Context = Context,
        };

        var @event = message.Deserialize<ResourceEvent<ImportPreNotificationEntity>>();

        return consumer.OnHandle(@event!, cancellationToken);
    }

    private Task HandleCustomsDeclaration(JsonElement message, CancellationToken cancellationToken)
    {
        var consumer = new ClearanceRequestConsumer(
            loggerFactory.CreateLogger<ClearanceRequestConsumer>(),
            decisionService,
            apiClient,
            correlationIdGenerator
        )
        {
            Context = Context,
        };

        var @event = message.Deserialize<ResourceEvent<CustomsDeclarationEntity>>();

        return consumer.OnHandle(@event!, cancellationToken);
    }

    public IConsumerContext Context { get; set; } = null!;
}
