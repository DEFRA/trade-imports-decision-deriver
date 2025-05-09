using System.Net;
using Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Clients;
using FluentAssertions;
using WireMock.Client;
using WireMock.Client.Extensions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Health;

[Collection("UsesWireMockClient")]
public class HealthTests(WireMockClient wireMockClient)
{
    private readonly IWireMockAdminApi _wireMockAdminApi = wireMockClient.WireMockAdminApi;

    [Fact]
    public async Task HealthAll_ShouldBeOk()
    {
        const string createPath = "/health/authorized";
        var getMappingBuilder = _wireMockAdminApi.GetMappingBuilder();
        getMappingBuilder.Given(m =>
            m.WithRequest(req =>
                    req.UsingGet()
                        .WithPath(createPath)
                        .WithHeaders(x =>
                            x.Add(authHeader =>
                                authHeader
                                    .WithName("Authorization")
                                    .WithMatchers(authHeaderMatcher =>
                                        authHeaderMatcher.Add(authHeaderMatcherValue =>
                                            authHeaderMatcherValue
                                                .WithName("WildcardMatcher")
                                                .WithPattern(
                                                    $"Basic {Convert.ToBase64String("TradeImportsDecisionDeriver:secret"u8.ToArray())}"
                                                )
                                        )
                                    )
                            )
                        )
                )
                .WithResponse(rsp =>
                {
                    rsp.WithStatusCode(HttpStatusCode.OK);
                })
        );
        var getMappingBuilderResult = await getMappingBuilder.BuildAndPostAsync();
        Assert.Null(getMappingBuilderResult.Error);

        var client = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };

        var response = await client.GetAsync("/health/all");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
