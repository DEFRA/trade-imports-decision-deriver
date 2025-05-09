using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;
using Defra.TradeImportsDecisionDeriver.Deriver.Interceptors;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.Deriver.Metrics;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.Logging;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using SlimMessageBus.Host;
using SlimMessageBus.Host.AmazonSQS;
using SlimMessageBus.Host.Interceptor;
using SlimMessageBus.Host.Serialization.SystemTextJson;

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
        services.AddSingleton<ConsumerMetrics>();
        services.AddScoped<IDecisionService, DecisionService>();
        services.AddScoped<IMatchingService, MatchingService>();

        services.AddScoped<IDecisionFinder, ChedADecisionFinder>();
        services.AddScoped<IDecisionFinder, ChedDDecisionFinder>();
        services.AddScoped<IDecisionFinder, ChedPDecisionFinder>();
        services.AddScoped<IDecisionFinder, ChedPPDecisionFinder>();
        services.AddScoped<IDecisionFinder, IuuDecisionFinder>();

        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(MetricsInterceptor<>));
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
                cfg.ClientProviderFactory = _ => new CdpCredentialsSqsClientProvider(
                    cfg.SqsClientConfig,
                    configuration
                );
            });

            mbb.Consume<JsonElement>(x => x.WithConsumer<ConsumerMediator>().Queue(queueName).Instances(20));

            mbb.AddJsonSerializer();
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

    public static IServiceCollection AddTracingForConsumers(this IServiceCollection services)
    {
        services.AddScoped(typeof(IConsumerInterceptor<>), typeof(TraceContextInterceptor<>));
        services.AddSingleton(typeof(ISqsConsumerErrorHandler<>), typeof(SerilogTraceErrorHandler<>));

        return services;
    }
}
