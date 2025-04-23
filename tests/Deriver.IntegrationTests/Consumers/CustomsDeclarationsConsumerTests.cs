using System.Net;
using System.Text.Json;
using Amazon.SQS.Model;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using FluentAssertions;
using RestEase;
using WireMock.Admin.Mappings;
using WireMock.Client;
using WireMock.Client.Extensions;
using Xunit.Abstractions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Consumers;

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
        var response = await new HttpClient().GetAsync("http://localhost:8080/health/all");
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());

        var importNotification = ImportPreNotificationFixtures.ImportPreNotificationCreatedFixture();

        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture();

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