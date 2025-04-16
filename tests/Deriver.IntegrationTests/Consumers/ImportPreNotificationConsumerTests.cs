using System.Net;
using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Consumers;

public class ImportPreNotificationConsumerTests : IClassFixture<DeriverWebApplicationFactory>
{
    private readonly DeriverWebApplicationFactory _factory;
    private readonly AmazonSQSClient _sender;
    private readonly TradeImportsDataApi.Api.Client.ITradeImportsDataApiClient _apiClient;

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
                    new KeyValuePair<string, string?>(
                        "DATA_EVENTS_QUEUE_NAME",
                        "trade_imports_data_import_declaration_upserts"
                    ),
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

        _apiClient = NSubstitute.Substitute.For<TradeImportsDataApi.Api.Client.ITradeImportsDataApiClient>();
        _factory.ConfigureTestServices = services =>
        {
            services.AddSingleton(_apiClient);
        };

        _factory.CreateClient();
    }

    [Fact]
    public async Task OnHandle_ReturnsTaskCompleted()
    {
        var importNotification = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        _apiClient.GetCustomsDeclarationsByChedId(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);

        var queueUrl = await _sender.GetQueueUrlAsync("trade_imports_data_import_declaration_upserts");
        await _sender.PurgeQueueAsync(queueUrl.QueueUrl, CancellationToken.None);
        var response = await _sender.SendMessageAsync(
            new SendMessageRequest(queueUrl.QueueUrl, JsonSerializer.Serialize(importNotification))
            {
                MessageAttributes = new Dictionary<string, MessageAttributeValue>()
                {
                    {
                        "x-cdp-request-id",
                        new MessageAttributeValue() { StringValue = Guid.NewGuid().ToString(), DataType = "String" }
                    },
                    {
                        MessageBusHeaders.ResourceType,
                        new MessageAttributeValue()
                        {
                            StringValue = ResourceEventResourceTypes.ImportPreNotification,
                            DataType = "String",
                        }
                    },
                },
            }
        );

        int queueSize = 1;

        //This just makes sure the message is taking off the queue, its a rather simple, crud test for the moment
        while (queueSize > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            var attributeResponse = await _sender.GetQueueAttributesAsync(queueUrl.QueueUrl, ["All"]);
            queueSize = attributeResponse.ApproximateNumberOfMessages;
        }

        response.HttpStatusCode.Should().Be(HttpStatusCode.OK);
        await _apiClient.Received(1).GetCustomsDeclarationsByChedId(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
