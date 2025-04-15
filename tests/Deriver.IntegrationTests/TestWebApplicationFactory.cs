using MartinCostello.Logging.XUnit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Defra.TradeImportsDecisionDeriver.Deriver.IntegrationTests;

public class TestWebApplicationFactory<T> : WebApplicationFactory<T>, ITestOutputHelperAccessor
    where T : class
{
    public Action<IConfigurationBuilder> ConfigureHostConfiguration { get; set; } = _ => { };

    public Action<IServiceCollection> ConfigureTestServices { get; set; } = _ => { };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(config => config.AddXUnit(this));
        builder.UseSetting("integrationTest", "true");
        builder.UseEnvironment("IntegrationTests");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            ConfigureHostConfiguration(config);
        });
        builder.ConfigureServices(ConfigureTestServices);
        return base.CreateHost(builder);
    }

    public ITestOutputHelper? OutputHelper { get; set; }
}
