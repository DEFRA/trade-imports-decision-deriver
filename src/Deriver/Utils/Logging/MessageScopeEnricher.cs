using Serilog.Core;
using Serilog.Events;
using SlimMessageBus;
using SlimMessageBus.Host.Consumer;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Utils.Logging;

public class MessageScopeEnricher(string headerKey) : ILogEventEnricher
{
    private const string CorrelationIdItemKey = "Serilog_CorrelationId";
    private const string PropertyName = "CorrelationId";

    /// <inheritdoc/>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var consumeContext = MessageScope.Current?.GetService<IConsumerContext>();

        if (consumeContext == null)
            return;

        if (
            consumeContext.Properties.TryGetValue(CorrelationIdItemKey, out var value)
            && value is LogEventProperty logEventProperty
        )
        {
            logEvent.AddPropertyIfAbsent(logEventProperty);
            return;
        }

        consumeContext.Headers.TryGetValue(headerKey, out object? headerValue);
        var requestHeader = headerValue?.ToString();

        var correlationId = !string.IsNullOrWhiteSpace(requestHeader) ? requestHeader : null;

        var correlationIdProperty = new LogEventProperty(PropertyName, new ScalarValue(correlationId));
        logEvent.AddOrUpdateProperty(correlationIdProperty);

        consumeContext.Properties.Add(CorrelationIdItemKey, correlationIdProperty);
    }
}
