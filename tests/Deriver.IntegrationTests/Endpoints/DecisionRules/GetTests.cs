using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using FluentAssertions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests.Endpoints.DecisionRules;

public class GetTests
{
    private static HttpClient CreateHttpClient(bool withAuthentication = true)
    {
        var client = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };

        if (withAuthentication)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                // See compose.yml for username, password and scope configuration
                Convert.ToBase64String("IntegrationTests:integration-tests-pwd"u8.ToArray())
            );
        }

        return client;
    }

    [Fact]
    public async Task Get_ShouldReturnCamelCaseDecisionRulesOptions()
    {
        var client = CreateHttpClient();

        var response = await client.GetAsync(Testing.Endpoints.DecisionRules.Options());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        json.RootElement.TryGetProperty("Ipaffs", out _).Should().BeFalse();

        foreach (var source in new[] { "ipaffs", "traces" })
        {
            var options = json.RootElement.GetProperty(source);

            options.GetProperty("cheds").ValueKind.Should().Be(JsonValueKind.Object);
            options
                .GetProperty("level2Mode")
                .GetString()
                .Should()
                .BeOneOf(nameof(RuleMode.DryRun), nameof(RuleMode.Live));
            options
                .GetProperty("commodityQuantityCheckDecisionRule")
                .GetProperty("scoring")
                .GetProperty("commodityWeight")
                .ValueKind.Should()
                .Be(JsonValueKind.Number);
        }
    }

    [Fact]
    public async Task Get_ShouldDeserializeToDecisionRulesOptions()
    {
        var client = CreateHttpClient();

        var response = await client.GetAsync(Testing.Endpoints.DecisionRules.Options());

        var options = JsonSerializer.Deserialize<DecisionRulesOptions>(
            await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
            }
        );

        options.Should().NotBeNull();
        options.Ipaffs.CommodityQuantityCheckDecisionRule.ComparisonEntries.Should().NotBeEmpty();
        options.Traces.CommodityQuantityCheckDecisionRule.ComparisonEntries.Should().NotBeEmpty();
    }
}
