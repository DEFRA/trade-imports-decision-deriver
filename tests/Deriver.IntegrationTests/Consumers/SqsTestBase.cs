using System.Security.Cryptography;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using SlimMessageBus.Host;
using Xunit.Abstractions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Consumers;

public class SqsTestBase(ITestOutputHelper output)
{
    protected const string QueueUrl =
        "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/trade_imports_data_upserted_decision_deriver";
    protected const string DeadLetterQueueUrl =
        "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/trade_imports_data_upserted_decision_deriver-deadletter";

    private readonly AmazonSQSClient _sqsClient = new(
        new BasicAWSCredentials("test", "test"),
        new AmazonSQSConfig { AuthenticationRegion = "eu-west-2", ServiceURL = "http://localhost:4566" }
    );

    protected Task PurgeQueue()
    {
        return _sqsClient.PurgeQueueAsync(QueueUrl, CancellationToken.None);
    }

    protected Task PurgeQueue(string queueUrl)
    {
        return _sqsClient.PurgeQueueAsync(queueUrl, CancellationToken.None);
    }

    protected Task<ReceiveMessageResponse> ReceiveMessage()
    {
        return _sqsClient.ReceiveMessageAsync(QueueUrl, CancellationToken.None);
    }

    protected Task<GetQueueAttributesResponse> GetQueueAttributes()
    {
        return _sqsClient.GetQueueAttributesAsync(
            new GetQueueAttributesRequest { AttributeNames = ["ApproximateNumberOfMessages"], QueueUrl = QueueUrl },
            CancellationToken.None
        );
    }

    protected Task<GetQueueAttributesResponse> GetQueueAttributes(string queueUrl)
    {
        return _sqsClient.GetQueueAttributesAsync(
            new GetQueueAttributesRequest { AttributeNames = ["ApproximateNumberOfMessages"], QueueUrl = queueUrl },
            CancellationToken.None
        );
    }

    protected async Task SendMessage(string body, Dictionary<string, MessageAttributeValue>? messageAttributes = null)
    {
        var request = new SendMessageRequest
        {
            MessageAttributes = messageAttributes,
            MessageBody = body,
            QueueUrl = QueueUrl,
        };
        var result = await _sqsClient.SendMessageAsync(request, CancellationToken.None);
        output.WriteLine("Sent {0} to {1}", result.MessageId, QueueUrl);
    }

    protected async Task<string> SendMessage(
        string messageGroupId,
        string body,
        string queueUrl,
        Dictionary<string, MessageAttributeValue>? messageAttributes = null,
        bool usesFifo = true
    )
    {
        var request = new SendMessageRequest
        {
            MessageAttributes = messageAttributes,
            MessageBody = body,
            MessageDeduplicationId = usesFifo ? RandomNumberGenerator.GetString("abcdefg", 20) : null,
            MessageGroupId = usesFifo ? messageGroupId : null,
            QueueUrl = queueUrl,
        };

        var result = await _sqsClient.SendMessageAsync(request, CancellationToken.None);

        output.WriteLine("Sent {0} to {1}", result.MessageId, queueUrl);

        return result.MessageId;
    }

    protected static Dictionary<string, MessageAttributeValue> WithResourceEventAttributes<T>(
        string resourceType,
        string? subResourceType,
        string resourceId
    )
    {
        var messageAttributes = new Dictionary<string, MessageAttributeValue>
        {
            {
                "MessageType",
                new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = new AssemblyQualifiedNameMessageTypeResolver().ToName(typeof(T)),
                }
            },
            {
                MessageBusHeaders.ResourceType,
                new MessageAttributeValue { DataType = "String", StringValue = resourceType }
            },
            {
                MessageBusHeaders.ResourceId,
                new MessageAttributeValue { DataType = "String", StringValue = resourceId }
            },
        };

        if (subResourceType != null)
        {
            messageAttributes.Add(
                MessageBusHeaders.SubResourceType,
                new MessageAttributeValue { DataType = "String", StringValue = subResourceType }
            );
        }

        return messageAttributes;
    }
}
