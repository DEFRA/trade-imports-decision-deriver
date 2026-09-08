using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TradeImportsQuantityMgmt.Client.Clients;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Health;

[ExcludeFromCodeCoverage]
public static class QuantityManagementHealthCheckBuilderExtensions
{
    public static IHealthChecksBuilder AddQuantityManagement(
        this IHealthChecksBuilder builder,
        Func<IServiceProvider, IQuantityManagementClient> clientFunc,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null
    )
    {
        builder.Add(
            new HealthCheckRegistration(
                "Quantity Management",
                sp => new QuantityManagementHealthCheck(clientFunc(sp)),
                HealthStatus.Unhealthy,
                tags,
                timeout
            )
        );

        return builder;
    }
}
