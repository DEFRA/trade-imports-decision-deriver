using System.Diagnostics.CodeAnalysis;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Finders;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.Processors;
using Defra.TradeImportsDecisionDeriver.Deriver.Matching;
using Defra.TradeImportsDecisionDeriver.Deriver.Metrics;
using Defra.TradeImportsDecisionDeriver.Deriver.Serializers;
using Defra.TradeImportsDecisionDeriver.Deriver.Services.Admin;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.Logging;
using Elastic.CommonSchema;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Scrutor;
using SlimMessageBus.Host;
using SlimMessageBus.Host.AmazonSQS;
using SlimMessageBus.Host.Interceptor;
using SlimMessageBus.Host.Serialization;
using ClearanceDecisionBuilder = Defra.TradeImportsDecisionDeriver.Deriver.Decisions.ClearanceDecisionBuilder;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataApiHttpClient(this IServiceCollection services)
    {
        var resilienceOptions = new HttpStandardResilienceOptions { Retry = { UseJitter = true } };
        resilienceOptions.Retry.DisableForUnsafeHttpMethods();

        services
            .AddTradeImportsDataApiClient()
            .ConfigureHttpClient(
                (sp, c) =>
                {
                    sp.GetRequiredService<IOptions<DataApiOptions>>().Value.Configure(c);

                    // Disable the HttpClient timeout to allow the resilient pipeline below
                    // to handle all timeouts
                    c.Timeout = Timeout.InfiniteTimeSpan;
                }
            )
            .AddHeaderPropagation()
            .AddResilienceHandler(
                "DataApi",
                builder =>
                {
                    builder
                        .AddTimeout(resilienceOptions.TotalRequestTimeout)
                        .AddRetry(resilienceOptions.Retry)
                        .AddTimeout(resilienceOptions.AttemptTimeout);
                }
            );

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

        services.Decorate<IDecisionFinder, CommodityCodeDecisionFinder>();
        services.Decorate<IDecisionFinder>(
            (inner, provider) =>
                new CommodityWeightOrQuantityDecisionFinder(
                    inner,
                    provider.GetRequiredService<ILogger<CommodityWeightOrQuantityDecisionFinder>>()
                )
        );

        //V2 of Decision Making
        services.AddSingleton<ICorrelationIdGenerator, CorrelationIdGenerator>();
        services.AddSingleton<IDecisionServiceV2, DecisionServiceV2>();
        services.AddSingleton<ICheckProcessor, CheckProcessor>();
        services.AddSingleton<IDecisionRulesEngineFactory, DecisionRulesEngineFactory>();
        services.AddSingleton<
            IClearanceDecisionBuilder,
            Defra.TradeImportsDecisionDeriver.Deriver.Decisions.V2.ClearanceDecisionBuilder
        >();

        // Register all IDecisionRule implementations
        services.AddSingleton<OrphanCheckCodeDecisionRule>();
        services.AddSingleton<UnlinkedNotificationDecisionRule>();
        services.AddSingleton<WrongChedTypeDecisionRule>();
        services.AddSingleton<MissingPartTwoDecisionRule>();
        services.AddSingleton<TerminalStatusDecisionRule>();
        services.AddSingleton<AmendDecisionRule>();
        services.AddSingleton<InspectionRequiredDecisionRule>();
        services.AddSingleton<CvedaDecisionRule>();
        services.AddSingleton<CvedpIuuCheckRule>();
        services.AddSingleton<CvedpDecisionRule>();
        services.AddSingleton<ChedppDecisionRule>();
        services.AddSingleton<CedDecisionRule>();
        services.AddSingleton<CommodityCodeValidationRule>();
        services.AddSingleton<CommodityWeightOrQuantityValidationRule>();

        // Order of interceptors is important here
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(TraceContextInterceptor<>));
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(LoggingInterceptor<>));
        services.AddSingleton<ConsumerMetrics>();
        services.AddSingleton(typeof(IConsumerInterceptor<>), typeof(MetricsInterceptor<>));

        services.AddOptions<AwsSqsOptions>().Bind(configuration).ValidateDataAnnotations();

        var autoStartConsumers = configuration.GetValue<bool>("AUTO_START_CONSUMERS");

        if (autoStartConsumers)
        {
            services.AddSlimMessageBus(mbb =>
            {
                var queueName = configuration.GetValue<string>("DATA_EVENTS_QUEUE_NAME");
                var consumersPerHost = configuration.GetValue<int>("CONSUMERS_PER_HOST");

                mbb.RegisterSerializer<ToStringSerializer>(s =>
                {
                    s.TryAddSingleton(_ => new ToStringSerializer());
                    s.TryAddSingleton<IMessageSerializer<string>>(svp => svp.GetRequiredService<ToStringSerializer>());
                });
                mbb.AddServicesFromAssemblyContaining<ConsumerMediator>();
                mbb.WithProviderAmazonSQS(cfg =>
                {
                    cfg.TopologyProvisioning.Enabled = false;
                    cfg.SqsClientProviderFactory = _ => new CdpCredentialsSqsClientProvider(
                        cfg.SqsClientConfig,
                        configuration
                    );
                });

                mbb.AutoStartConsumersEnabled(autoStartConsumers)
                    .Consume<string>(x =>
                        x.WithConsumer<ConsumerMediator>().Queue(queueName).Instances(consumersPerHost)
                    );
            });
        }

        return services;
    }

    public static IServiceCollection AddProcessorConfiguration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddOptions<DataApiOptions>().BindConfiguration(DataApiOptions.SectionName).ValidateDataAnnotations();
        services.AddSingleton<ISqsDeadLetterService, SqsDeadLetterService>();
        services.AddAWSService<IAmazonSQS>();
        return services;
    }
}
