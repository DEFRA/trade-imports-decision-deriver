using System.IO.Compression;
using System.Net;
using System.Text;
using Amazon.SQS.Model;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Clients;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using WireMock.Admin.Mappings;
using WireMock.Client;
using WireMock.Client.Extensions;
using Xunit.Abstractions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Consumers;

[Collection("UsesWireMockClient")]
public class CustomsDeclarationsConsumerTests(ITestOutputHelper output, WireMockClient wireMockClient)
    : SqsTestBase(output)
{
    private readonly IWireMockAdminApi _wireMockAdminApi = wireMockClient.WireMockAdminApi;

    private static Dictionary<string, MessageAttributeValue> WithInboundHmrcMessageType(
        string resourceType,
        string subResourceType
    )
    {
        return new Dictionary<string, MessageAttributeValue>
        {
            {
                MessageBusHeaders.ResourceType,
                new MessageAttributeValue { DataType = "String", StringValue = resourceType }
            },
            {
                MessageBusHeaders.SubResourceType,
                new MessageAttributeValue { DataType = "String", StringValue = subResourceType }
            },
        };
    }

    [Fact]
    public async Task WhenClearanceRequestSent_ThenClearanceRequestIsProcessedAndSentToTheDataApi()
    {
        await PurgeQueue();
        var importNotification = ImportPreNotificationFixtures.ImportPreNotificationResponseFixture();

        var customsDeclaration = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        var customsDeclarationResponse = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture(
            customsDeclaration.ResourceId
        );
        customsDeclarationResponse = customsDeclarationResponse with { Finalisation = null };

        var createPath = $"/customs-declarations/{customsDeclaration.ResourceId}";
        var mappingBuilder = _wireMockAdminApi.GetMappingBuilder();

        mappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingGet().WithPath(createPath))
                .WithResponse(rsp =>
                    rsp.WithBody(JsonSerializer.Serialize(customsDeclarationResponse)).WithStatusCode(HttpStatusCode.OK)
                )
        );

        mappingBuilder.Given(m =>
            m.WithRequest(req =>
                    req.UsingGet()
                        .WithPath($"/customs-declarations/{customsDeclaration.ResourceId}/import-pre-notifications")
                )
                .WithResponse(rsp =>
                    rsp.WithBody(JsonSerializer.Serialize(new ImportPreNotificationsResponse([importNotification])))
                        .WithStatusCode(HttpStatusCode.OK)
                )
        );

        mappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPut().WithPath(createPath))
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.Created))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status);

        await SendMessage(
            JsonSerializer.Serialize(customsDeclaration),
            WithInboundHmrcMessageType(
                ResourceEventResourceTypes.CustomsDeclaration,
                ResourceEventSubResourceTypes.ClearanceDecision
            )
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

    [Fact]
    public async Task WhenCompressedClearanceRequestSent_ThenClearanceRequestIsProcessedAndSentToTheDataApi()
    {
        await PurgeQueue();
        var importNotification = ImportPreNotificationFixtures.ImportPreNotificationResponseFixture();

        var customsDeclaration = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        var customsDeclarationResponse = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture(
            customsDeclaration.ResourceId
        );
        customsDeclarationResponse = customsDeclarationResponse with { Finalisation = null };

        var createPath = $"/customs-declarations/{customsDeclaration.ResourceId}";
        var mappingBuilder = _wireMockAdminApi.GetMappingBuilder();

        mappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingGet().WithPath(createPath))
                .WithResponse(rsp =>
                    rsp.WithBody(JsonSerializer.Serialize(customsDeclarationResponse)).WithStatusCode(HttpStatusCode.OK)
                )
        );

        mappingBuilder.Given(m =>
            m.WithRequest(req =>
                    req.UsingGet()
                        .WithPath($"/customs-declarations/{customsDeclaration.ResourceId}/import-pre-notifications")
                )
                .WithResponse(rsp =>
                    rsp.WithBody(JsonSerializer.Serialize(new ImportPreNotificationsResponse([importNotification])))
                        .WithStatusCode(HttpStatusCode.OK)
                )
        );

        mappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingPut().WithPath(createPath))
                .WithResponse(rsp => rsp.WithStatusCode(HttpStatusCode.Created))
        );
        var status = await mappingBuilder.BuildAndPostAsync();
        Assert.NotNull(status);

        var buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(customsDeclaration));
        var memoryStream = new MemoryStream();
        await using var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal);
        await gzipStream.WriteAsync(buffer);
        await gzipStream.FlushAsync();
        var message = Convert.ToBase64String(memoryStream.ToArray());

        var headers = WithInboundHmrcMessageType(
            ResourceEventResourceTypes.CustomsDeclaration,
            ResourceEventSubResourceTypes.ClearanceDecision
        );
        headers.Add(
            MessageBusHeaders.ContentEncoding,
            new MessageAttributeValue { DataType = "String", StringValue = "gzip, base64" }
        );

        await SendMessage(message, headers);

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
