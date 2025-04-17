using Microsoft.Extensions.Options;
using Serilog.Context;
using SlimMessageBus;
using SlimMessageBus.Host;
using SlimMessageBus.Host.AmazonSQS;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Utils.Logging;

public class SerilogTraceErrorHandler<TMessage>(
    IOptions<TraceHeader> traceHeader,
    ILogger<SerilogTraceErrorHandler<TMessage>> logger
) : SqsConsumerErrorHandler<TMessage>
{
    public override Task<ProcessResult> OnHandleError(
        TMessage message,
        IConsumerContext consumerContext,
        Exception exception,
        int attempts
    )
    {
        var traceId = consumerContext.Headers.GetTraceId(traceHeader.Value.Name);

        using (LogContext.PushProperty(TraceContextEnricher.PropertyName, traceId))
        {
            // This ensures any trace header value is included with the exception, but it
            // does mean the exception is logged twice - once here and once within SMB code.
            logger.LogError(exception, "Error processing message");
        }

        return Task.FromResult(Failure());
    }
}
