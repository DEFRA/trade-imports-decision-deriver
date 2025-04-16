using System.Diagnostics.CodeAnalysis;
using Serilog.Context;
using SlimMessageBus;
using SlimMessageBus.Host.Interceptor;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Interceptors;

[ExcludeFromCodeCoverage]
public class TracingInterceptor<TMessage>(IConfiguration configuration, ILogger<TracingInterceptor<TMessage>> logger)
    : IConsumerInterceptor<TMessage>
{
    public async Task<object> OnHandle(TMessage message, Func<Task<object>> next, IConsumerContext context)
    {
        var traceIdHeader = configuration.GetValue<string>("TraceHeader");

        if (traceIdHeader == null)
        {
            return await next();
        }

        context.Headers.TryGetValue(traceIdHeader, out object? headerValue);
        var requestHeader = headerValue?.ToString()?.Replace("-", "");

        if (!string.IsNullOrWhiteSpace(requestHeader))
        {
            using (LogContext.PushProperty("CorrelationId", requestHeader))
            {
                return await next();
            }
        }

        logger.LogInformation(
            "No CorrelationId found on Message Header to {Headers}",
            string.Join(Environment.NewLine, context.Headers)
        );

        return await next();
    }
}
