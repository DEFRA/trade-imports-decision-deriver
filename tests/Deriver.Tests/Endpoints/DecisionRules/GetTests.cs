using System.Net;
using System.Text.Json;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Tests.Endpoints.DecisionRules;

public class GetTests(ApiWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestBase(factory, outputHelper)
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        services.Configure<DecisionRulesOptions>(c =>
        {
            c.Ipaffs.Level2Mode = RuleMode.Live;
            c.Ipaffs.Cheds["CHEDA"] = new DecisionRulesPerChedOptions
            {
                DisabledRules = ["CommodityCodeDecisionRule"],
                DisabledForEu = ["CommodityQuantityCheckDecisionRule"],
                DisabledForRoW = [],
            };
            c.Traces.Level3Mode = RuleMode.Live;
        });
    }

    protected override void ConfigureHostConfiguration(IConfigurationBuilder config)
    {
        base.ConfigureHostConfiguration(config);

        config.AddInMemoryCollection([new KeyValuePair<string, string?>("AUTO_START_CONSUMERS", "false")]);
    }

    [Fact]
    public async Task Get_ShouldReturnContent()
    {
        var client = CreateClient();

        var response = await client.GetAsync(Testing.Endpoints.DecisionRules.Options());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await VerifyJson(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_ShouldReturnCamelCasePropertiesAndStringEnums()
    {
        var client = CreateClient();

        var response = await client.GetAsync(Testing.Endpoints.DecisionRules.Options());

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var ipaffs = json.RootElement.GetProperty("ipaffs");

        json.RootElement.TryGetProperty("Ipaffs", out _).Should().BeFalse();
        json.RootElement.TryGetProperty("traces", out _).Should().BeTrue();
        ipaffs.GetProperty("level2Mode").GetString().Should().Be(nameof(RuleMode.Live));
        ipaffs.GetProperty("level3Mode").GetString().Should().Be(nameof(RuleMode.DryRun));
        ipaffs
            .GetProperty("commodityQuantityCheckDecisionRule")
            .GetProperty("scoring")
            .GetProperty("commodityWeight")
            .GetInt32()
            .Should()
            .Be(100);
        // Dictionary keys are data, not property names, so they must not be camelCased
        ipaffs.GetProperty("cheds").TryGetProperty("CHEDA", out var cheda).Should().BeTrue();
        cheda.GetProperty("disabledRules")[0].GetString().Should().Be("CommodityCodeDecisionRule");
    }
}
