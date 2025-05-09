using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Health;

[ExcludeFromCodeCoverage]
public static class DataApiHealthCheckBuilderExtensions
{
    public static IHealthChecksBuilder AddDataApi(
        this IHealthChecksBuilder builder,
        Func<IServiceProvider, DataApiOptions> optionsFunc,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null
    )
    {
        builder.Add(
            new HealthCheckRegistration(
                "Data API",
                sp => new DataApiHealthCheck(optionsFunc(sp)),
                HealthStatus.Unhealthy,
                tags,
                timeout
            )
        );

        return builder;
    }
}
