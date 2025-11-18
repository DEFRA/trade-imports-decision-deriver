using System.Net;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;
using Defra.TradeImportsDecisionDeriver.Deriver.Services.Admin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Services.Admin;

public class SqsDeadLetterServiceTests
{
    private readonly IAmazonSQS _amazonSqs = Substitute.For<IAmazonSQS>();
    private readonly ILogger<SqsDeadLetterService> _logger = NullLogger<SqsDeadLetterService>.Instance;

    private readonly SqsDeadLetterService _resourceEventsDeadLetterService;

    private const string QueueName = "outbound_queue";
    private const string QueueNameDeadLetter = "outbound_queue-deadletter";
    private const string QueueUrl = "queueUrl";
    private const string DeadLetterQueueUrl = "deadLetterQueueUrl";

    public SqsDeadLetterServiceTests()
    {
        _resourceEventsDeadLetterService = new SqsDeadLetterService(_amazonSqs, _logger);

        _amazonSqs
            .GetQueueUrlAsync(Arg.Is<GetQueueUrlRequest>(x => x.QueueName == QueueName))
            .Returns(Task.FromResult(new GetQueueUrlResponse { QueueUrl = QueueUrl }));

        _amazonSqs
            .GetQueueUrlAsync(Arg.Is<GetQueueUrlRequest>(x => x.QueueName == QueueNameDeadLetter))
            .Returns(Task.FromResult(new GetQueueUrlResponse { QueueUrl = DeadLetterQueueUrl }));
    }

    [Fact]
    public async Task When_redrive_successful_Then_return_true()
    {
        _amazonSqs
            .StartMessageMoveTaskAsync(Arg.Any<StartMessageMoveTaskRequest>())
            .Returns(Task.FromResult(new StartMessageMoveTaskResponse { HttpStatusCode = HttpStatusCode.OK }));
        _amazonSqs
            .GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>())
            .Returns(
                Task.FromResult(
                    new GetQueueAttributesResponse
                    {
                        Attributes = new Dictionary<string, string>()
                        {
                            { SQSConstants.ATTRIBUTE_QUEUE_ARN, "queueArn" },
                        },
                    }
                )
            );

        var result = await _resourceEventsDeadLetterService.Redrive(
            QueueNameDeadLetter,
            QueueName,
            CancellationToken.None
        );

        Assert.True(result);
    }

    [Fact]
    public async Task When_redrive_failed_Then_return_false()
    {
        _amazonSqs
            .StartMessageMoveTaskAsync(Arg.Any<StartMessageMoveTaskRequest>())
            .Returns(
                Task.FromResult(
                    new StartMessageMoveTaskResponse
                    {
                        HttpStatusCode = HttpStatusCode.InternalServerError,
                        ResponseMetadata = new ResponseMetadata() { Metadata = { } },
                    }
                )
            );

        _amazonSqs
            .GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>())
            .Returns(
                Task.FromResult(
                    new GetQueueAttributesResponse
                    {
                        Attributes = new Dictionary<string, string>()
                        {
                            { SQSConstants.ATTRIBUTE_QUEUE_ARN, "queueArn" },
                        },
                    }
                )
            );

        var result = await _resourceEventsDeadLetterService.Redrive(
            QueueNameDeadLetter,
            QueueName,
            CancellationToken.None
        );

        Assert.False(result);
    }

    [Fact]
    public async Task When_redrive_throws_exception_Then_return_false()
    {
        _amazonSqs.StartMessageMoveTaskAsync(Arg.Any<StartMessageMoveTaskRequest>()).ThrowsAsync(new Exception("Test"));

        var result = await _resourceEventsDeadLetterService.Redrive(
            QueueNameDeadLetter,
            QueueName,
            CancellationToken.None
        );

        Assert.False(result);
    }

    [Fact]
    public async Task When_remove_message_successful_Then_return_as_expected()
    {
        const string messageId = "messageId";
        const string receiptHandle = "receiptHandle";

        _amazonSqs
            .ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == DeadLetterQueueUrl))
            .Returns(
                Task.FromResult(
                    new ReceiveMessageResponse
                    {
                        Messages = [new Message { MessageId = messageId, ReceiptHandle = receiptHandle }],
                    }
                )
            );
        _amazonSqs
            .DeleteMessageAsync(Arg.Is<DeleteMessageRequest>(x => x.ReceiptHandle == receiptHandle))
            .Returns(Task.FromResult(new DeleteMessageResponse { HttpStatusCode = HttpStatusCode.OK }));

        var result = await _resourceEventsDeadLetterService.Remove(
            messageId,
            QueueNameDeadLetter,
            CancellationToken.None
        );

        result.Should().Be($"Found message {messageId} and removed");
    }

    [Fact]
    public async Task When_remove_message_unsuccessful_Then_return_as_expected()
    {
        const string messageId = "messageId";
        const string receiptHandle = "receiptHandle";

        _amazonSqs
            .ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == DeadLetterQueueUrl))
            .Returns(
                Task.FromResult(
                    new ReceiveMessageResponse
                    {
                        Messages = [new Message { MessageId = messageId, ReceiptHandle = receiptHandle }],
                    }
                )
            );
        _amazonSqs
            .DeleteMessageAsync(Arg.Is<DeleteMessageRequest>(x => x.ReceiptHandle == receiptHandle))
            .Returns(
                Task.FromResult(new DeleteMessageResponse { HttpStatusCode = HttpStatusCode.InternalServerError })
            );

        var result = await _resourceEventsDeadLetterService.Remove(
            messageId,
            QueueNameDeadLetter,
            CancellationToken.None
        );

        result.Should().Be($"Found message {messageId} but delete was not successful (InternalServerError)");
    }

    [Fact]
    public async Task When_remove_message_and_no_messages_on_dlq_Then_return_as_expected()
    {
        const string messageId = "messageId";

        _amazonSqs
            .ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == DeadLetterQueueUrl))
            .Returns(Task.FromResult(new ReceiveMessageResponse { Messages = [] }));

        var result = await _resourceEventsDeadLetterService.Remove(
            messageId,
            QueueNameDeadLetter,
            CancellationToken.None
        );

        result
            .Should()
            .Be("No messages found (visibility timeout used was 60 seconds, therefore wait before retrying)");
    }

    [Fact]
    public async Task When_remove_message_and_multiple_receive_calls_return_Then_return_as_expected()
    {
        const string messageId = "messageId";

        _amazonSqs
            .ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == DeadLetterQueueUrl))
            .Returns(
                Task.FromResult(new ReceiveMessageResponse { Messages = [new Message { MessageId = "unknown1" }] }),
                Task.FromResult(new ReceiveMessageResponse { Messages = [new Message { MessageId = "unknown2" }] }),
                Task.FromResult(new ReceiveMessageResponse { Messages = [] })
            );

        var result = await _resourceEventsDeadLetterService.Remove(
            messageId,
            QueueNameDeadLetter,
            CancellationToken.None
        );

        result
            .Should()
            .Be("No messages found (visibility timeout used was 60 seconds, therefore wait before retrying)");
    }

    [Fact]
    public async Task When_remove_message_and_exception_Then_return_as_expected()
    {
        const string messageId = "messageId";
        _amazonSqs
            .GetQueueUrlAsync(Arg.Is<GetQueueUrlRequest>(x => x.QueueName == QueueNameDeadLetter))
            .Throws(new Exception());

        var result = await _resourceEventsDeadLetterService.Remove(
            messageId,
            QueueNameDeadLetter,
            CancellationToken.None
        );

        result.Should().Be("Exception, check logs");
    }

    [Fact]
    public async Task When_drain_successful_Then_return_as_expected()
    {
        _amazonSqs
            .PurgeQueueAsync(Arg.Is<PurgeQueueRequest>(x => x.QueueUrl == DeadLetterQueueUrl))
            .Returns(Task.FromResult(new PurgeQueueResponse() { HttpStatusCode = HttpStatusCode.OK }));

        var result = await _resourceEventsDeadLetterService.Drain(QueueNameDeadLetter, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task When_drain_unsuccessful_Then_return_as_expected(bool statusCode)
    {
        const string messageId = "messageId";
        const string receiptHandle = "receiptHandle";

        _amazonSqs
            .ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == QueueUrl))
            .Returns(
                Task.FromResult(
                    new ReceiveMessageResponse
                    {
                        Messages = [new Message { MessageId = messageId, ReceiptHandle = receiptHandle }],
                    }
                ),
                Task.FromResult(new ReceiveMessageResponse { Messages = [] })
            );
        _amazonSqs
            .DeleteMessageBatchAsync(
                Arg.Is<DeleteMessageBatchRequest>(x =>
                    x.QueueUrl == QueueUrl
                    && x.Entries.Count == 1
                    && x.Entries[0].Id == "0"
                    && x.Entries[0].ReceiptHandle == receiptHandle
                )
            )
            .Returns(
                Task.FromResult(
                    statusCode
                        ? new DeleteMessageBatchResponse { HttpStatusCode = HttpStatusCode.InternalServerError }
                        : new DeleteMessageBatchResponse
                        {
                            HttpStatusCode = HttpStatusCode.OK,
                            Failed = [new BatchResultErrorEntry()],
                        }
                )
            );

        var result = await _resourceEventsDeadLetterService.Drain(QueueNameDeadLetter, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task When_drain_and_exception_Then_return_as_expected()
    {
        _amazonSqs
            .GetQueueUrlAsync(Arg.Is<GetQueueUrlRequest>(x => x.QueueName == QueueNameDeadLetter))
            .Throws(new Exception());

        var result = await _resourceEventsDeadLetterService.Drain(QueueNameDeadLetter, CancellationToken.None);

        result.Should().BeFalse();
    }
}
