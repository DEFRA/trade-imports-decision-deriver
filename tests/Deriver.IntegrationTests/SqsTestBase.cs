using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using RestEase;
using WireMock.Admin.Mappings;
using WireMock.Client;
using WireMock.Client.Extensions;
using Xunit.Abstractions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests;

public class CustomsDeclarationsConsumerTests(ITestOutputHelper output) : SqsTestBase(output)
{
    private readonly IWireMockAdminApi _wireMockAdminApi = RestClient.For<IWireMockAdminApi>("http://localhost:9090");

    private static Dictionary<string, MessageAttributeValue> WithInboundHmrcMessageType(string messageType)
    {
        return new Dictionary<string, MessageAttributeValue>
        {
            {
                "InboundHmrcMessageType",
                new MessageAttributeValue { DataType = "String", StringValue = messageType }
            },
        };
    }

    [Fact]
    public async Task WhenClearanceRequestSent_ThenClearanceRequestIsProcessedAndSentToTheDataApi()
    {
        var importNotification = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();

        ////var mrn = GenerateMrn();
        ////var clearanceRequest = ClearanceRequestFixture(mrn).Create();

        await _wireMockAdminApi.ResetMappingsAsync();
        await _wireMockAdminApi.ResetRequestsAsync();

        var createPath = $"/customs-declarations/{customsDeclaration.MovementReferenceNumber}";
        var mappingBuilder = _wireMockAdminApi.GetMappingBuilder();

        mappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingGet().WithPath(createPath))
                .WithResponse(rsp => rsp.WithBodyAsJson(customsDeclaration).WithStatusCode(HttpStatusCode.OK))
        );

        mappingBuilder.Given(m =>
            m.WithRequest(req =>
                    req.UsingGet()
                        .WithPath(
                            $"/customs-declarations/{customsDeclaration.MovementReferenceNumber}/import-pre-notifications"
                        )
                )
                .WithResponse(rsp => rsp.WithBodyAsJson(new[] { importNotification }).WithStatusCode(HttpStatusCode.OK))
        );

        mappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPut().WithPath(createPath))
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.Created))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status);

        await SendMessage(
            customsDeclaration.MovementReferenceNumber,
            JsonSerializer.Serialize(customsDeclaration),
            WithInboundHmrcMessageType(ResourceEventResourceTypes.CustomsDeclaration)
        );

        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
            {
                var requestsModel = new RequestModel { Methods = ["PUT"], Path = createPath };
                var requests = await _wireMockAdminApi.FindRequestsAsync(requestsModel);
                return requests.Count == 1;
            })
        );
    }
}

public class SqsTestBase(ITestOutputHelper output)
{
    private const string QueueUrl =
        "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/trade_imports_data_import_declaration_upserts";

    private readonly AmazonSQSClient _sqsClient = new(
        new BasicAWSCredentials("test", "test"),
        new AmazonSQSConfig { AuthenticationRegion = "eu-west-2", ServiceURL = "http://localhost:4566" }
    );

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

    protected async Task SendMessage(
        string messageGroupId,
        string body,
        Dictionary<string, MessageAttributeValue>? messageAttributes = null
    )
    {
        var request = new SendMessageRequest
        {
            MessageAttributes = messageAttributes,
            MessageBody = body,
            ////MessageDeduplicationId = RandomNumberGenerator.GetString("abcdefg", 20),
            ////MessageGroupId = messageGroupId,
            QueueUrl = QueueUrl,
        };
        var result = await _sqsClient.SendMessageAsync(request, CancellationToken.None);
        output.WriteLine("Sent {0} to {1}", result.MessageId, QueueUrl);
    }
}
