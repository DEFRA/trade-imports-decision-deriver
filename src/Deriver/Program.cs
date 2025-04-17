using System.Net;
using System.Net.Http.Headers;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDecisionDeriver.Deriver.Configuration;
using Defra.TradeImportsDecisionDeriver.Deriver.Extensions;
using Defra.TradeImportsDecisionDeriver.Deriver.Health;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils;
using Defra.TradeImportsDecisionDeriver.Deriver.Utils.Logging;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var app = CreateWebApplication(args);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    await Log.CloseAndFlushAsync();
}

return;

static WebApplication CreateWebApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    ConfigureWebApplication(builder, args);

    return BuildWebApplication(builder);
}

static void ConfigureWebApplication(WebApplicationBuilder builder, string[] args)
{
    var integrationTest = args.Contains("--integrationTest=true");

    builder.Configuration.AddJsonFile(
        $"appsettings.cdp.{Environment.GetEnvironmentVariable("ENVIRONMENT")?.ToLower()}.json",
        integrationTest
    );
    builder.Configuration.AddEnvironmentVariables();

    // Load certificates into Trust Store - Note must happen before Mongo and Http client connections
    builder.Services.AddCustomTrustStore();

    builder.ConfigureLoggingAndTracing();
    builder.Services.AddProblemDetails();
    builder.Services.AddHealth(builder.Configuration);
    builder.Services.AddProcessorConfiguration(builder.Configuration);
    builder.Services.AddDataApiHttpClient();

    builder.Services.AddHeaderPropagation(options =>
    {
        var traceHeader = builder.Configuration.GetValue<string>("TraceHeader");
        if (!string.IsNullOrWhiteSpace(traceHeader))
            options.Headers.Add(traceHeader);
    });

    builder.Services.AddConsumers(builder.Configuration);
    builder
        .Services.AddOptions<DataApiOptions>()
        .BindConfiguration(DataApiOptions.SectionName)
        .ValidateDataAnnotations();
}

static WebApplication BuildWebApplication(WebApplicationBuilder builder)
{
    var app = builder.Build();

    app.UseHeaderPropagation();
    app.MapHealth();

    app.UseStatusCodePages();
    app.UseExceptionHandler(
        new ExceptionHandlerOptions
        {
            AllowStatusCode404Response = true,
            ExceptionHandler = async context =>
            {
                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                var error = exceptionHandlerFeature?.Error;
                string? detail = null;

                if (error is BadHttpRequestException badHttpRequestException)
                {
                    context.Response.StatusCode = badHttpRequestException.StatusCode;
                    detail = badHttpRequestException.Message;
                }

                await context
                    .RequestServices.GetRequiredService<IProblemDetailsService>()
                    .WriteAsync(
                        new ProblemDetailsContext
                        {
                            HttpContext = context,
                            AdditionalMetadata = exceptionHandlerFeature?.Endpoint?.Metadata,
                            ProblemDetails = { Status = context.Response.StatusCode, Detail = detail },
                        }
                    );
            },
        }
    );

    return app;
}

#pragma warning disable S2094
namespace Defra.TradeImportsDecisionDeriver.Deriver
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Program;
}
#pragma warning restore S2094
