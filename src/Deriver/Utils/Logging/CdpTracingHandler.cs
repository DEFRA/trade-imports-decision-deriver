using SlimMessageBus;
using SlimMessageBus.Host.Consumer;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Utils.Logging;

public class CdpTracingHandler(IMessageScopeAccessor? messageScopeAccessor, IConfiguration configuration)
    : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (messageScopeAccessor is null)
        {
            return base.SendAsync(request, cancellationToken);
        }

        var consumerContext = messageScopeAccessor.Current.GetService<IConsumerContext>();

        if (consumerContext is null)
        {
            return base.SendAsync(request, cancellationToken);
        }

        var traceIdHeader = configuration.GetValue<string>("TraceHeader");

        if (string.IsNullOrEmpty(traceIdHeader))
        {
            return base.SendAsync(request, cancellationToken);
        }

        consumerContext.Headers.TryGetValue(traceIdHeader, out object? headerValue);
        var requestHeader = headerValue?.ToString();

        var correlationId = !string.IsNullOrWhiteSpace(requestHeader) ? requestHeader : null;

        if (!string.IsNullOrEmpty(correlationId))
        {
            request.Headers.TryAddWithoutValidation(traceIdHeader, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
