using System.Diagnostics.CodeAnalysis;
using Defra.TradeImports.Tracing;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Elastic.Serilog.Enrichers.Web;
using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

namespace Defra.TradeImportsDecisionDeriver.Deriver.Utils.Logging;

[ExcludeFromCodeCoverage]
public static class WebApplicationBuilderExtensions
{
    public static void ConfigureLoggingAndTracing(this WebApplicationBuilder builder, bool integrationTest = false)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTraceContextAccessor(builder.Configuration);

        if (!integrationTest)
        {
            // Configuring Serilog below wipes out the framework logging
            // so we don't execute the following when the host is running
            // within an integration test
            builder.Host.UseSerilog(ConfigureLogging);
        }
    }

    private static void ConfigureLogging(
        HostBuilderContext hostBuilderContext,
        IServiceProvider services,
        LoggerConfiguration config
    )
    {
        var httpAccessor = services.GetRequiredService<IHttpContextAccessor>();
        var serviceVersion = Environment.GetEnvironmentVariable("SERVICE_VERSION") ?? "";

        config
            .ReadFrom.Configuration(hostBuilderContext.Configuration)
            .Enrich.WithEcsHttpContext(httpAccessor)
            .Enrich.FromLogContext()
            .Enrich.With(new TraceContextEnricher())
            .Filter.ByExcluding(x =>
                x.Level == LogEventLevel.Information
                && x.Properties.TryGetValue("RequestPath", out var path)
                && path.ToString().Contains("/health")
                && !x.MessageTemplate.Text.StartsWith("Request finished")
            )
            .Filter.ByExcluding(x =>
                x.Level == LogEventLevel.Error
                && x.Properties.TryGetValue("SourceContext", out var sourceContext)
                && sourceContext.ToString().Contains("SlimMessageBus.Host.AmazonSQS.SqsQueueConsumer")
                && x.MessageTemplate.Text.StartsWith("Message processing error")
            );

        if (!string.IsNullOrWhiteSpace(serviceVersion))
            config.Enrich.WithProperty("service.version", serviceVersion);
    }
}
