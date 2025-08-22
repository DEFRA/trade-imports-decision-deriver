using System.Diagnostics.CodeAnalysis;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using SlimMessageBus.Host.AmazonSQS;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

[ExcludeFromCodeCoverage]
public class CdpCredentialsSqsClientProvider : ISqsClientProvider, IDisposable
{
    private const string DefaultRegion = "eu-west-2";
    private bool _disposedValue;

    private readonly AmazonSQSClient _client;

    public CdpCredentialsSqsClientProvider(AmazonSQSConfig sqsConfig, IConfiguration configuration)
    {
        var clientId = configuration.GetValue<string>("AWS_ACCESS_KEY_ID");
        var clientSecret = configuration.GetValue<string>("AWS_SECRET_ACCESS_KEY");

        if (!string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(clientId))
        {
            var region = configuration.GetValue<string>("AWS_REGION") ?? DefaultRegion;
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);

            _client = new AmazonSQSClient(
                new BasicAWSCredentials(clientId, clientSecret),
                new AmazonSQSConfig
                {
                    AuthenticationRegion = region,
                    RegionEndpoint = regionEndpoint,
                    ServiceURL = configuration.GetValue<string>("SQS_Endpoint"),
                }
            );
        }
        else
        {
            _client = new AmazonSQSClient(sqsConfig);
        }
    }

    #region ISqsClientProvider

    public IAmazonSQS Client => _client;

    public Task EnsureClientAuthenticated() => Task.CompletedTask;

    #endregion

    #region Dispose Pattern

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _client?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
