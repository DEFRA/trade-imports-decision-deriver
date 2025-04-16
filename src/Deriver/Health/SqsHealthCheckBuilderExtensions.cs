using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Health;

[ExcludeFromCodeCoverage]
public static class SqsHealthCheckBuilderExtensions
{
    private const string Name = "aws sqs";

    public static IHealthChecksBuilder AddSqs(
        this IHealthChecksBuilder builder,
        IConfiguration configuration,
        IEnumerable<string>? tags = default,
        TimeSpan? timeout = default
    )
    {
        builder.Add(
            new HealthCheckRegistration(
                Name,
                sp => new SqsHealthCheck(configuration),
                HealthStatus.Unhealthy,
                tags,
                timeout
            )
        );

        return builder;
    }
}
