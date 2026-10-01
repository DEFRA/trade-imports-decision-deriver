using System.Net;
using FluentAssertions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Endpoints.DecisionRules;

public class GetTests
{
    private static HttpClient CreateHttpClient() => new() { BaseAddress = new Uri("http://localhost:8080") };

    [Fact]
    public async Task Get_ShouldReturnCamelCaseDecisionRulesOptions()
    {
        var client = CreateHttpClient();

        var response = await client.GetAsync(Testing.Endpoints.DecisionRules.Options());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await VerifyJson(await response.Content.ReadAsStringAsync());
    }
}
