using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.Deriver.Metrics;
using Defra.TradeImportsDecisionDeriver.Deriver.Serializers;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using SlimMessageBus.Host;
using SlimMessageBus.Host.AmazonSQS;
using SlimMessageBus.Host.Interceptor;
using SlimMessageBus.Host.Serialization;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataApiHttpClient(this IServiceCollection services)
    {
        services
            .AddTradeImportsDataApiClient()
            .ConfigureHttpClient((sp, c) => sp.GetRequiredService<IOptions<DataApiOptions>>().Value.Configure(c))
            .AddHeaderPropagation()
            .AddStandardResilienceHandler(o =>
            {
                o.Retry.DisableForUnsafeHttpMethods();
            });

        return services;
    }

    public static IServiceCollection AddConsumers(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDecisionService, DecisionService>();
        services.AddScoped<IMatchingService, MatchingService>();

        services.AddScoped<IDecisionFinder, ChedADecisionFinder>();
        services.AddScoped<IDecisionFinder, ChedDDecisionFinder>();
        services.AddScoped<IDecisionFinder, ChedPDecisionFinder>();
        services.AddScoped<IDecisionFinder, ChedPPDecisionFinder>();
        services.AddScoped<IDecisionFinder, IuuDecisionFinder>();

        // Order of interceptors is important here
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(TraceContextInterceptor<>));
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(LoggingInterceptor<>));
        services.AddSingleton<ConsumerMetrics>();
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(MetricsInterceptor<>));

        services.AddSlimMessageBus(mbb =>
        {
            var queueName = configuration.GetValue<string>("DATA_EVENTS_QUEUE_NAME");
            var consumersPerHost = configuration.GetValue<int>("CONSUMERS_PER_HOST");
            if (consumersPerHost <= 0)
                consumersPerHost = 20;

            mbb.RegisterSerializer<ToStringSerializer>(s =>
            {
                s.TryAddSingleton(_ => new ToStringSerializer());
                s.TryAddSingleton<IMessageSerializer<string>>(svp => svp.GetRequiredService<ToStringSerializer>());
            });
            mbb.AddServicesFromAssemblyContaining<ConsumerMediator>();
            mbb.WithProviderAmazonSQS(cfg =>
            {
                cfg.TopologyProvisioning.Enabled = false;
                cfg.ClientProviderFactory = _ => new CdpCredentialsSqsClientProvider(
                    cfg.SqsClientConfig,
                    configuration
                );
            });
            mbb.Consume<string>(x => x.WithConsumer<ConsumerMediator>().Queue(queueName).Instances(consumersPerHost));
        });

        return services;
    }

    public static IServiceCollection AddProcessorConfiguration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddOptions<DataApiOptions>().BindConfiguration(DataApiOptions.SectionName).ValidateDataAnnotations();

        return services;
    }
}
