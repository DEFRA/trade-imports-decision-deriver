using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Clients;
using Defra.TradeImportsDecisionDeriver.TestFixtures;
using FluentAssertions;
using WireMock.Client;
using WireMock.Client.Extensions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Endpoints.Decision;

[Collection("UsesWireMockClient")]
public class GetTests
{
    private readonly IWireMockAdminApi _wireMockAdminApi;
    private readonly VerifySettings _settings;

    public GetTests(WireMockClient wireMockClient)
    {
        _wireMockAdminApi = wireMockClient.WireMockAdminApi;

        _settings = new VerifySettings();
        _settings.ScrubMember("created");
        _settings.ScrubMember("traceId");
        _settings.ScrubMember("correlationId");
        _settings.DontScrubDateTimes();
        _settings.DontScrubGuids();
        _settings.DontIgnoreEmptyCollections();
    }

    [Fact]
    public async Task Get_WhenFound_ShouldReturnContent()
    {
        var customsDeclaration = CustomsDeclarationResponseFixtures.CustomsDeclarationResponseSimpleStaticFixture();
        var createPath = $"/customs-declarations/{customsDeclaration.MovementReferenceNumber}";
        var mappingBuilder = _wireMockAdminApi.GetMappingBuilder();

        mappingBuilder.Given(m =>
            m.WithRequest(req => req.UsingGet().WithPath(createPath))
                .WithResponse(rsp =>
                    rsp.WithBody(JsonSerializer.Serialize(customsDeclaration)).WithStatusCode(HttpStatusCode.OK)
                )
        );

        mappingBuilder.Given(m =>
            m.WithRequest(req =>
                    req.UsingGet()
                        .WithPath(
                            $"/customs-declarations/{customsDeclaration.MovementReferenceNumber}/import-pre-notifications"
                        )
                )
                .WithResponse(rsp =>
                    rsp.WithBody(JsonSerializer.Serialize(new ImportPreNotificationsResponse([])))
                        .WithStatusCode(HttpStatusCode.OK)
                )
        );

        var getMappingBuilderResult = await mappingBuilder.BuildAndPostAsync();
        Assert.Null(getMappingBuilderResult.Error);

        var client = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            // See compose.yml for username, password and scope configuration
            Convert.ToBase64String("IntegrationTests:integration-tests-pwd"u8.ToArray())
        );

        var response = await client.GetAsync(
            Testing.Endpoints.Decision.Get(customsDeclaration.MovementReferenceNumber)
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await VerifyJson(await response.Content.ReadAsStringAsync(), _settings);
    }
}
