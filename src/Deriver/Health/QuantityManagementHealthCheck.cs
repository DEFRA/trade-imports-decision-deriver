using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TradeImportsQuantityMgmt.Client.Clients;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Health;

[ExcludeFromCodeCoverage]
public class QuantityManagementHealthCheck(IQuantityManagementClient quantityManagementClient) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await quantityManagementClient.HealthCheck(cancellationToken);

            if (response.StatusCode is not HttpStatusCode.OK)
                throw new InvalidOperationException($"Unexpected HTTP status code: {response.StatusCode}");

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                exception: new Exception($"Failed to connect to Traces Gateway", ex)
            );
        }
    }
}
