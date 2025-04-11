using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Health;

public class SqsHealthCheck(IConfiguration configuration) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            using var client = CreateSqsClient();
            _ = await client
                .GetQueueUrlAsync(configuration.GetValue<string>("DATA_EVENTS_QUEUE_NAME"), cancellationToken)
                .ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }

    private IAmazonSQS CreateSqsClient()
    {
        var clientId = configuration.GetValue<string>("AWS_ACCESS_KEY_ID");
        var clientSecret = configuration.GetValue<string>("AWS_SECRET_ACCESS_KEY");
        var region = configuration.GetValue<string>("AWS_REGION");
        var serverUrl = configuration.GetValue<string>("SQS_Endpoint");

        if (!string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(clientId))
        {
            return new AmazonSQSClient(
                new BasicAWSCredentials(clientId, clientSecret),
                new AmazonSQSConfig { RegionEndpoint = RegionEndpoint.GetBySystemName(region), ServiceURL = serverUrl }
            );
        }

        return new AmazonSQSClient();
    }
}
