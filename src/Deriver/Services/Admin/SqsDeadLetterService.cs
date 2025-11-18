using System.Net;
using Amazon.SQS;
using Amazon.SQS.Model;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Services.Admin;

public interface ISqsDeadLetterService
{
    Task<bool> Redrive(string sourceQueueName, string destinationQueueName, CancellationToken cancellationToken);

    Task<string> Remove(string messageId, string queueName, CancellationToken cancellationToken);

    Task<bool> Drain(string queueName, CancellationToken cancellationToken);
}

public class SqsDeadLetterService(IAmazonSQS amazonSqs, ILogger<SqsDeadLetterService> logger) : ISqsDeadLetterService
{
    public async Task<bool> Redrive(
        string sourceQueueName,
        string destinationQueueName,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var sourceQueueUrl = await GetQueueUrl(sourceQueueName, cancellationToken);
            var destinationQueueUrl = await GetQueueUrl(destinationQueueName, cancellationToken);

            var sourceAttributes = await amazonSqs.GetQueueAttributesAsync(
                new GetQueueAttributesRequest
                {
                    QueueUrl = sourceQueueUrl,
                    AttributeNames = new List<string> { "QueueArn" },
                },
                cancellationToken
            );

            var destinationeAttributes = await amazonSqs.GetQueueAttributesAsync(
                new GetQueueAttributesRequest
                {
                    QueueUrl = destinationQueueUrl,
                    AttributeNames = new List<string> { "QueueArn" },
                },
                cancellationToken
            );
            var request = new StartMessageMoveTaskRequest
            {
                SourceArn = sourceAttributes.QueueARN,
                DestinationArn = destinationeAttributes.QueueARN,
            };

            var response = await amazonSqs.StartMessageMoveTaskAsync(request, cancellationToken);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                logger.LogInformation(
                    "Redrive message move task started with TaskHandle: {TaskHandle}",
                    response.TaskHandle
                );

                return true;
            }

            logger.LogError("Redrive failed with response: {TaskResponse}", response.ToStringExtended());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initiate start message move task during redrive request");
        }

        return false;
    }

    public async Task<string> Remove(string messageId, string queueName, CancellationToken cancellationToken)
    {
        try
        {
            var queueUrl = await GetQueueUrl(queueName, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 0,
                    VisibilityTimeout = 60,
                };

                var response = await amazonSqs.ReceiveMessageAsync(request, cancellationToken);
                if (response.Messages.Count == 0)
                {
                    return $"No messages found (visibility timeout used was {request.VisibilityTimeout} seconds, therefore wait before retrying)";
                }

                var message = response.Messages.FirstOrDefault(x =>
                    x.MessageId.Equals(messageId, StringComparison.OrdinalIgnoreCase)
                );

                if (message is not null)
                {
                    var result = await amazonSqs.DeleteMessageAsync(
                        new DeleteMessageRequest { QueueUrl = queueUrl, ReceiptHandle = message.ReceiptHandle },
                        cancellationToken
                    );

                    if (result.HttpStatusCode == HttpStatusCode.OK)
                    {
                        logger.LogInformation("Removed message {MessageId} from dead letter queue", messageId);

                        return $"Found message {messageId} and removed";
                    }

                    return $"Found message {messageId} but delete was not successful ({result.HttpStatusCode})";
                }

                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
            }

            return "Request was cancelled";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove message from dead letter queue");

            return "Exception, check logs";
        }
    }

    public async Task<bool> Drain(string queueName, CancellationToken cancellationToken)
    {
        try
        {
            var queueUrl = await GetQueueUrl(queueName, cancellationToken);
            var removed = 0;

            var request = new PurgeQueueRequest { QueueUrl = queueUrl };

            var response = await amazonSqs.PurgeQueueAsync(request, cancellationToken);
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                logger.LogInformation("Dead letter queue is empty, {Removed} message(s) removed", removed);

                return true;
            }

            logger.LogInformation("Drain operation cancelled, total messages removed so far {Removed}", removed);

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to drain dead letter queue");

            return false;
        }
    }

    private async Task<string> GetQueueUrl(string queueName, CancellationToken cancellationToken)
    {
        var queueUrlResponse = await amazonSqs.GetQueueUrlAsync(
            new GetQueueUrlRequest { QueueName = queueName },
            cancellationToken
        );

        return queueUrlResponse.QueueUrl;
    }
}
