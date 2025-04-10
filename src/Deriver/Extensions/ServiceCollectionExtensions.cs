using System.Text.Json;
using System.Text.Json.Nodes;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.Deriver.Interceptors;
using SlimMessageBus.Host;
using SlimMessageBus.Host.AmazonSQS;
using SlimMessageBus.Host.Interceptor;
using SlimMessageBus.Host.Serialization.SystemTextJson;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsumers(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(TracingInterceptor<>));
        services.AddSlimMessageBus(mbb =>
        {
            var queueName = configuration.GetValue<string>("DATA_EVENTS_QUEUE_NAME");

            mbb.AddServicesFromAssemblyContaining<ConsumerMediator>(
                consumerLifetime: ServiceLifetime.Scoped).PerMessageScopeEnabled();

            mbb.WithProviderAmazonSQS(cfg =>
            {
                cfg.TopologyProvisioning.Enabled = false;
                cfg.ClientProviderFactory = (provider => new CdpCredentialsSqsClientProvider(cfg.SqsClientConfig, configuration));
            });
            
            mbb.Consume<JsonElement>(x => x
                .WithConsumer<ConsumerMediator>()
                .Queue(queueName));

            mbb.AddJsonSerializer();


        });

        return services;
    }
}
