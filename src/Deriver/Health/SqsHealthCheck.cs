using System.Diagnostics.CodeAnalysis;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Health;

[ExcludeFromCodeCoverage]
public class SqsHealthCheck(IConfiguration configuration, string queueName) : IHealthCheck
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

            _ = await client.GetQueueUrlAsync(queueName, cancellationToken).ConfigureAwait(false);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                exception: new Exception($"Failed to connect to AWS queue: {queueName}", ex)
            );
        }
    }

    private AmazonSQSClient CreateSqsClient()
    {
        var clientId = configuration.GetValue<string>("AWS_ACCESS_KEY_ID");
        var clientSecret = configuration.GetValue<string>("AWS_SECRET_ACCESS_KEY");

        if (!string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(clientId))
        {
            var region = configuration.GetValue<string>("AWS_REGION") ?? "eu-west-2";
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);

            return new AmazonSQSClient(
                new BasicAWSCredentials(clientId, clientSecret),
                new AmazonSQSConfig
                {
                    // https://github.com/aws/aws-sdk-net/issues/1781
                    AuthenticationRegion = region,
                    RegionEndpoint = regionEndpoint,
                    ServiceURL = configuration.GetValue<string>("SQS_Endpoint"),
                }
            );
        }

        return new AmazonSQSClient();
    }
}
