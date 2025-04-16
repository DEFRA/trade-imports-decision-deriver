using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using SlimMessageBus;
using SlimMessageBus.Host.Interceptor;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Interceptors;

[ExcludeFromCodeCoverage]
public class LoggingInterceptor<TMessage>(ILogger<LoggingInterceptor<TMessage>> logger) : IConsumerInterceptor<TMessage>
{
    public async Task<object> OnHandle(TMessage message, Func<Task<object>> next, IConsumerContext context)
    {
        var messageId = context.GetMessageId();
        logger.LogInformation("Started Processing MessageId {MessageId}", messageId);

        try
        {
            return await next();
        }
#pragma warning disable S2139
        catch (Exception exception)
#pragma warning restore S2139
        {
            logger.LogError(exception, "Error Processing MessageId {MessageId}", messageId);
            throw;
        }
        finally
        {
            logger.LogInformation("Finished Processing MessageId {MessageId}", messageId);
        }
    }
}
