using Amazon.SQS.Model;
using SlimMessageBus;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

public static class MessageBusHeaders
{
    public const string ContentEncoding = "Content-Encoding";
    public const string ResourceType = nameof(ResourceType);
    public const string SubResourceType = nameof(SubResourceType);
    public const string SqsBusMessage = "Sqs_Message";
}

public static class ConsumerContextExtensions
{
    public static string GetMessageId(this IConsumerContext consumerContext)
    {
        if (consumerContext.Properties.TryGetValue(MessageBusHeaders.SqsBusMessage, out var sqsMessage))
        {
            return ((Message)sqsMessage).MessageId;
        }

        return string.Empty;
    }

    public static string GetResourceType(this IConsumerContext consumerContext)
    {
        if (consumerContext.Headers.TryGetValue(MessageBusHeaders.ResourceType, out var value))
        {
            return value.ToString()!;
        }

        return string.Empty;
    }

    public static string GetSubResourceType(this IConsumerContext consumerContext)
    {
        if (consumerContext.Headers.TryGetValue(MessageBusHeaders.SubResourceType, out var value))
        {
            return value.ToString()!;
        }

        return string.Empty;
    }
}
