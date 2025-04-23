using System.Net;
using Amazon.SQS.Model;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using RestEase;
using WireMock.Admin.Mappings;
using WireMock.Client;
using WireMock.Client.Extensions;
using Xunit.Abstractions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Consumers;

public class CustomsDeclarationsConsumerTests(ITestOutputHelper output) : SqsTestBase(output)
{
    private readonly IWireMockAdminApi _wireMockAdminApi = RestClient.For<IWireMockAdminApi>("http://localhost:9090");

    private static Dictionary<string, MessageAttributeValue> WithInboundHmrcMessageType(string messageType)
    {
        return new Dictionary<string, MessageAttributeValue>
        {
            {
                "resourceType",
                new MessageAttributeValue { DataType = "String", StringValue = messageType }
            },
        };
    }

    [Fact]
    public async Task WhenClearanceRequestSent_ThenClearanceRequestIsProcessedAndSentToTheDataApi()
    {
        await PurgeQueue();
        var importNotification = ImportPreNotificationFixtures.ImportPreNotificationResponseFixture();

        var customsDeclaration = ClearanceRequestFixtures.ClearanceRequestCreatedFixture();

        await _wireMockAdminApi.ResetMappingsAsync();
        await _wireMockAdminApi.ResetRequestsAsync();

        var createPath = $"/customs-declarations/{customsDeclaration.ResourceId}";
        var mappingBuilder = _wireMockAdminApi.GetMappingBuilder();

        mappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingGet().WithPath(createPath))
                .WithResponse(rsp =>
                    rsp.WithBody(
                            JsonSerializer.Serialize(
                                CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture(
                                    customsDeclaration.ResourceId
                                )
                            )
                        )
                        .WithStatusCode(HttpStatusCode.OK)
                )
        );

        mappingBuilder.Given(m =>
            m.WithRequest(req =>
                    req.UsingGet()
                        .WithPath($"/customs-declarations/{customsDeclaration.ResourceId}/import-pre-notifications")
                )
                .WithResponse(rsp =>
                    rsp.WithBody(JsonSerializer.Serialize(new[] { importNotification }))
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
