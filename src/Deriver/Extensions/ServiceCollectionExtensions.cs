using System.Diagnostics.CodeAnalysis;
using Amazon;
using Amazon.SQS;
using Defra.TradeImports.SMB.CompressedSerializer;
using Defra.TradeImports.SMB.Metrics;
using Defra.TradeImports.SMB.SQSSNS;
using Defra.TradeImports.SMB.Tracing;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Consumers;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.DecisionEngine.DecisionRules;
using Defra.TradeImportsDecisionDeriver.Deriver.Decisions.Processors;
using Defra.TradeImportsDecisionDeriver.Deriver.Metrics;
using Defra.TradeImportsDecisionDeriver.Deriver.Services.Admin;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.CorrelationId;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
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
        services.AddSingleton<ICorrelationIdGenerator, CorrelationIdGenerator>();
        services.AddSingleton<IDecisionService, DecisionService>();
        services.AddSingleton<ICheckProcessor, CheckProcessor>();
        services.AddSingleton<IDecisionRulesEngineFactory, DecisionRulesEngineFactory>();
        services.AddSingleton<IClearanceDecisionBuilder, ClearanceDecisionBuilder>();

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
        services.AddSingleton<UnknownCheckCodeDecisionRule>();

        // Order of interceptors is important here
        services.AddTraceContextInterceptor();
        services.AddConsumerMetrics(MetricNames.MeterName);

        services.AddOptions<AwsSqsOptions>().Bind(configuration).ValidateDataAnnotations();

        var autoStartConsumers = configuration.GetValue<bool>("AUTO_START_CONSUMERS");

        if (autoStartConsumers)
        {
            services.AddSlimMessageBus(mbb =>
            {
                var queueName = configuration.GetValue<string>("DATA_EVENTS_QUEUE_NAME");
                var consumersPerHost = configuration.GetValue<int>("CONSUMERS_PER_HOST");

                mbb.AddStringSerializer()
                    .AddServicesFromAssemblyContaining<ConsumerMediator>()
                    .WithProviderAmazonSQS(cfg =>
                    {
                        cfg.UseLocalOrAmbientCredentials(configuration);
                        cfg.TopologyProvisioning.Enabled = false;
                    })
                    .AutoStartConsumersEnabled(autoStartConsumers)
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
