using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using SlimMessageBus.Host.AmazonSQS;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

public class CdpCredentialsSqsClientProvider : ISqsClientProvider, IDisposable
{
    private bool _disposedValue;

    private readonly AmazonSQSClient _client;

    public CdpCredentialsSqsClientProvider(AmazonSQSConfig sqsConfig, IConfiguration configuration)
    {
        var clientId = configuration.GetValue<string>("AWS_ACCESS_KEY_ID");
        var clientSecret = configuration.GetValue<string>("AWS_SECRET_ACCESS_KEY");

        if (!string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(clientId))
        {
            _client = new AmazonSQSClient(
                new BasicAWSCredentials(clientId, clientSecret),
                new AmazonSQSConfig
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(configuration.GetValue<string>("AWS_REGION")),
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

    public AmazonSQSClient Client => _client;

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
