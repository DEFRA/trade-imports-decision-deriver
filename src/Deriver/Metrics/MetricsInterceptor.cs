using System.Diagnostics.CodeAnalysis;
using System.Net;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using SlimMessageBus;
using SlimMessageBus.Host.Interceptor;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Metrics;

[ExcludeFromCodeCoverage]
public sealed class MetricsInterceptor<TMessage>(
    ConsumerMetrics consumerMetrics,
    ILogger<MetricsInterceptor<TMessage>> logger
) : IConsumerInterceptor<TMessage>
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

            MetricsInterceptorLogs.ConsumerWarn(logger, consumerName, resourceId, resourceType, subResourceType);

            throw;
        }
        catch (Exception exception)
        {
            consumerMetrics.Faulted(context.Path, consumerName, resourceType, subResourceType, exception);

            MetricsInterceptorLogs.ConsumerFaulted(logger, consumerName, resourceId, resourceType, subResourceType);

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

internal static partial class MetricsInterceptorLogs
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Warn consumer {Consumer} for resource {Resource} of type {Type} {SubType}"
    )]
    public static partial void ConsumerWarn(
        ILogger logger,
        string consumer,
        string resource,
        string type,
        string subType
    );

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Faulted consumer {Consumer} for resource {Resource} of type {Type} {SubType}"
    )]
    public static partial void ConsumerFaulted(
        ILogger logger,
        string consumer,
        string resource,
        string type,
        string subType
    );
}
