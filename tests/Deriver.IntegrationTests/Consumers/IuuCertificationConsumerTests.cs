using System.Net;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Clients;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using WireMock.Admin.Mappings;
using WireMock.Client;
using WireMock.Client.Extensions;
using Xunit.Abstractions;
using Assert = Xunit.Assert;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Consumers;

[Collection("UsesWireMockClient")]
public class IuuCertificationConsumerTests(ITestOutputHelper output, WireMockClient wireMockClient)
    : SqsTestBase(output)
{
    private const string IuuCertificationQueueUrl =
        "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/trade_imports_iuucertification_upserted_decision_deriver";

    private readonly IWireMockAdminApi _wireMockAdminApi = wireMockClient.WireMockAdminApi;

    [Fact]
    public async Task WhenIuuNotificationSent_ThenRelatedClearanceRequestsAreProcessedAndSentToTheDataApi()
    {
        await PurgeQueue(IuuCertificationQueueUrl);

        var mrn = $"iuu{Guid.NewGuid():N}"[..18];
        var iuuNotification = IuuNotificationFixtures.IuuNotificationCreatedFixture();

        var customsDeclarationResponse = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseFixture(mrn);
        customsDeclarationResponse = customsDeclarationResponse with { Finalisation = null };

        foreach (var commodity in customsDeclarationResponse.ClearanceRequest?.Commodities!)
        {
            foreach (var check in commodity.Checks!)
            {
                check.CheckCode = "H219";
            }
        }

        var createPath = $"/customs-declarations/{mrn}";
        var mappingBuilder = _wireMockAdminApi.GetMappingBuilder();

        mappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingGet().WithPath("/related-import-declarations"))
                .WithResponse(rsp =>
                    rsp.WithBody(
                            JsonSerializer.Serialize(
                                IuuNotificationFixtures.RelatedImportDeclarationsResponseFixture(
                                    customsDeclarationResponse
                                )
                            )
                        )
                        .WithStatusCode(HttpStatusCode.OK)
                )
        );

        mappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingGet().WithPath(createPath))
                .WithResponse(rsp =>
                    rsp.WithBody(JsonSerializer.Serialize(customsDeclarationResponse)).WithStatusCode(HttpStatusCode.OK)
                )
        );

        mappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingGet().WithPath($"/customs-declarations/{mrn}/import-pre-notifications"))
                .WithResponse(rsp =>
                    rsp.WithBody(JsonSerializer.Serialize(new ImportPreNotificationsResponse([])))
                        .WithStatusCode(HttpStatusCode.OK)
                )
        );

        mappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingGet().WithPath($"/customs-declarations/{mrn}/traces-cheds"))
                .WithResponse(rsp =>
                    rsp.WithBody(JsonSerializer.Serialize(new TracesChedsResponse([])))
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
            iuuNotification.ResourceId,
            JsonSerializer.Serialize(iuuNotification),
            IuuCertificationQueueUrl,
            WithResourceEventAttributes<ResourceEvent<IuuNotificationEvent>>(
                ResourceEventResourceTypes.IuuNotification,
                null,
                iuuNotification.ResourceId
            ),
            usesFifo: false
        );

        Assert.True(
            await AsyncWaiter.WaitForAsync(async () =>
            {
                var requestModel = new RequestModel { Methods = ["PUT"], Path = createPath };
                var requests = await _wireMockAdminApi.FindRequestsAsync(requestModel);

                return requests.Count == 1;
            })
        );
    }
}
