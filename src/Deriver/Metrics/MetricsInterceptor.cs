using System.Diagnostics.CodeAnalysis;
using System.Net;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using SlimMessageBus;
using SlimMessageBus.Host.Interceptor;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Metrics;

[ExcludeFromCodeCoverage]
public class MetricsInterceptor<TMessage>(ConsumerMetrics consumerMetrics, ILogger<MetricsInterceptor<TMessage>> logger)
    : IConsumerInterceptor<TMessage>
    where TMessage : notnull
{
    public async Task<object> OnHandle(TMessage message, Func<Task<object>> next, IConsumerContext context)
    {
        var startingTimestamp = TimeProvider.System.GetTimestamp();
        var consumerName = context.Consumer.GetType().Name;
        var resourceType = context.GetResourceType();
        var subResourceType = context.GetSubResourceType();
        var resourceId = context.GetResourceId();

        try
        {
            consumerMetrics.Start(context.Path, consumerName, resourceType, subResourceType);

            return await next();
        }
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.StatusCode == HttpStatusCode.Conflict)
        {
            consumerMetrics.Warn(context.Path, consumerName, resourceType, subResourceType, httpRequestException);

            LogForTriaging("Warn", consumerName, resourceId, resourceType, subResourceType);

            throw;
        }
        catch (Exception exception)
        {
            consumerMetrics.Faulted(context.Path, consumerName, resourceType, subResourceType, exception);

            LogForTriaging("Faulted", consumerName, resourceId, resourceType, subResourceType);

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

    /// <summary>
    /// Intentionally an information log as this supports triaging, not alerting.
    /// The logging interceptor will log for the benefit of alerting.
    /// </summary>
    /// <param name="level"></param>
    /// <param name="consumerName"></param>
    /// <param name="resourceId"></param>
    /// <param name="resourceType"></param>
    /// <param name="subResourceType"></param>
    private void LogForTriaging(
        string level,
        string consumerName,
        string resourceId,
        string resourceType,
        string subResourceType
    )
    {
        logger.LogInformation(
            "{Level} consumer {Consumer} for resource {Resource} of type {Type} {SubType}",
            level,
            consumerName,
            resourceId,
            resourceType,
            subResourceType
        );
    }
}
