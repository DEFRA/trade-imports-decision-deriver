using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Microsoft.Extensions.Options;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Health;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHealth(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddSqs(
                configuration,
                "Data events SQS queue",
                _ =>
                    configuration.GetValue<string>("DATA_EVENTS_QUEUE_NAME")
                    ?? throw new InvalidOperationException("Missing DATA_EVENTS_QUEUE_NAME"),
                timeout: TimeSpan.FromSeconds(10),
                tags: [WebApplicationExtensions.Extended]
            )
            .AddDataApi(
                sp => sp.GetRequiredService<IOptions<DataApiOptions>>().Value,
                tags: [WebApplicationExtensions.Extended],
                timeout: TimeSpan.FromSeconds(10)
            );

        return services;
    }
}
