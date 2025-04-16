using System.Text.Json;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;
using Defra.TradeImportsDecisionDeriver.Deriver.Interceptors;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using SlimMessageBus.Host;
using SlimMessageBus.Host.AmazonSQS;
using SlimMessageBus.Host.Interceptor;
using SlimMessageBus.Host.Serialization.SystemTextJson;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsumers(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDecisionService, DecisionService>();
        services.AddScoped<IMatchingService, MatchingService>();

        services.AddScoped<IDecisionFinder, ChedADecisionFinder>();
        services.AddScoped<IDecisionFinder, ChedDDecisionFinder>();
        services.AddScoped<IDecisionFinder, ChedPDecisionFinder>();
        services.AddScoped<IDecisionFinder, ChedPPDecisionFinder>();
        services.AddScoped<IDecisionFinder, IuuDecisionFinder>();

        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(TracingInterceptor<>));
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(LoggingInterceptor<>));
        services.AddSlimMessageBus(mbb =>
        {
            var queueName = configuration.GetValue<string>("DATA_EVENTS_QUEUE_NAME");

            mbb.AddServicesFromAssemblyContaining<ConsumerMediator>(consumerLifetime: ServiceLifetime.Scoped)
                .PerMessageScopeEnabled();

            mbb.WithProviderAmazonSQS(cfg =>
            {
                cfg.TopologyProvisioning.Enabled = false;
                cfg.ClientProviderFactory = (
                    provider => new CdpCredentialsSqsClientProvider(cfg.SqsClientConfig, configuration)
                );
            });

            mbb.Consume<JsonElement>(x => x.WithConsumer<ConsumerMediator>().Queue(queueName));

            mbb.AddJsonSerializer();
        });

        return services;
    }
}
