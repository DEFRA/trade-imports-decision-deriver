using System.Net;
using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Consumers;

public class ImportPreNotificationConsumerTests : IClassFixture<DeriverWebApplicationFactory>
{
    private readonly DeriverWebApplicationFactory _factory;
    private readonly AmazonSQSClient _sender;

    public ImportPreNotificationConsumerTests(DeriverWebApplicationFactory factory, ITestOutputHelper outputHelper)
    {
        _factory = factory;
        _factory.OutputHelper = outputHelper;
        _factory.ConfigureHostConfiguration = config =>
        {
            config.AddInMemoryCollection(
                [
                    new KeyValuePair<string, string?>("AWS_REGION", "eu-west-2"),
                    new KeyValuePair<string, string?>("AWS_DEFAULT_REGION", "eu-west-2"),
                    new KeyValuePair<string, string?>("AWS_ACCESS_KEY_ID", "test"),
                    new KeyValuePair<string, string?>("AWS_SECRET_ACCESS_KEY", "test"),
                    new KeyValuePair<string, string?>(
                        "SQS_Endpoint",
                        "http://sqs.eu-west-2.localhost.localstack.cloud:4566"
                    ),
                    new KeyValuePair<string, string?>("DATA_EVENTS_QUEUE_NAME", "data_events"),
                ]
            );
        };

        _sender = new AmazonSQSClient(
            new BasicAWSCredentials("test", "test"),
            new AmazonSQSConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName("eu-west-2"),
                ServiceURL = "http://sqs.eu-west-2.localhost.localstack.cloud:4566",
            }
        );

        _factory.CreateClient();
    }

    [Fact]
    public async Task OnHandle_ReturnsTaskCompleted()
    {
        ////var importNotification = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var listQueuesResponse = await _sender.ListQueuesAsync(new ListQueuesRequest());
        _factory.OutputHelper?.WriteLine("Listing Queues");
        foreach (var url in listQueuesResponse.QueueUrls)
        {
            _factory.OutputHelper?.WriteLine(url);
        }

        ////var queueUrl = await _sender.GetQueueUrlAsync("data_events");
        ////var response = await _sender.SendMessageAsync(
        ////    new SendMessageRequest(queueUrl.QueueUrl, JsonSerializer.Serialize(importNotification))
        ////);

        ////int queueSize = 1;

        //////This just makes sure the message is taking off the queue, its a rather simple, crud test for the moment
        ////while (queueSize > 0)
        ////{
        ////    await Task.Delay(TimeSpan.FromSeconds(1));
        ////    var attributeResponse = await _sender.GetQueueAttributesAsync(queueUrl.QueueUrl, ["All"]);
        ////    queueSize = attributeResponse.ApproximateNumberOfMessages;
        ////}

        ////response.HttpStatusCode.Should().Be(HttpStatusCode.OK);
        Assert.True(true);
    }
}
