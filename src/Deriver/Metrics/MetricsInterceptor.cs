using System.Diagnostics.CodeAnalysis;
using System.Net;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using SlimMessageBus;
using SlimMessageBus.Host.Interceptor;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Metrics;

[ExcludeFromCodeCoverage]
public class MetricsInterceptor<TMessage>(ConsumerMetrics consumerMetrics) : IConsumerInterceptor<TMessage>
    where TMessage : notnull
{
    public async Task<object> OnHandle(TMessage message, Func<Task<object>> next, IConsumerContext context)
    {
        var startingTimestamp = TimeProvider.System.GetTimestamp();
        var consumerName = context.Consumer.GetType().Name;
        var resourceType = context.GetResourceType();
        var subResourceType = context.GetSubResourceType();

        try
        {
            consumerMetrics.Start(context.Path, consumerName, resourceType, subResourceType);

            return await next();
        }
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode == HttpStatusCode.Conflict)
        {
            consumerMetrics.Warn(context.Path, consumerName, resourceType, subResourceType, httpRequestException);

            throw;
        }
        catch (Exception exception)
        {
            consumerMetrics.Faulted(context.Path, consumerName, resourceType, subResourceType, exception);

            throw;
        }
        finally
        {
            consumerMetrics.Complete(
                context.Path,
                consumerName,
                TimeProvider.System.GetElapsedTime(startingTimestamp).TotalMilliseconds,
                resourceType,
                subResourceType
            );
        }
    }
}
