// OtlpFile Exporter example for OpenTelemetry Collector verification
// This example outputs logs in OTLP file format that can be consumed by the OTel Collector
using System.Diagnostics;
using System.Net;
using System.Reflection;
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

var assembly = Assembly.GetExecutingAssembly();
var assemblyName = assembly!.GetName();
var versionAttribute = assembly
    .GetCustomAttributes(false)
    .OfType<AssemblyInformationalVersionAttribute>()
    .FirstOrDefault();
var serviceName = assemblyName.Name!;
var serviceVersion =
    versionAttribute?.InformationalVersion ?? assemblyName.Version?.ToString() ?? string.Empty;

var activitySource = new ActivitySource(serviceName);

var builder = Host.CreateApplicationBuilder(
    new HostApplicationBuilderSettings { Args = args, ContentRootPath = AppContext.BaseDirectory }
);

// If you are using the OTLP exporter, you can enable this to debug connection issues
// var otelDebugListener = new OpenTelemetryDebugEventListener(
//     new OpenTelemetryDebugOptions() { DebugOtlp = true }
// );

builder.Logging.ClearProviders();
builder
    .Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource
            .AddService(serviceName, serviceVersion)
            .AddAttributes(
                new KeyValuePair<string, object>[]
                {
                    new("host.name", Environment.MachineName),
                    new(
                        "deployment.environment.name",
                        builder.Environment.EnvironmentName.ToLowerInvariant()
                    ),
                }
            )
    )
    .WithLogging(logging =>
    {
        // Enable OTLP export for comparison, to either 14317 (collector) or 18889 (Aspire)
        // logging.AddOtlpExporter(otlpOptions =>
        // {
        //     otlpOptions.Endpoint = new Uri("http://127.0.0.1:14317");
        // });
        logging.AddOtlpFileExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddSource(serviceName);
    });

var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.DebugMessage();
logger.CriticalSystemFailure("payment-service");

// Structured logging with multiple attributes
logger.UserLoggedIn("Alice", IPAddress.Parse("fdbe:4f24:c288:3b1b::4"), DateTimeOffset.UtcNow);

// Log within an activity to include trace context
using (var activity = activitySource.StartActivity("OrderProcessing"))
{
    activity?.SetTag("order.id", "ORD-789");
    activity?.SetTag("order.amount", 150.00);

    logger.ProcessingOrder("ORD-789", 150.00m);
    logger.ResourceRunningLow("disk space");
}

// Exception logging example
try
{
    throw new InvalidOperationException("Simulated exception for testing");
}
catch (Exception ex)
{
    logger.OperationError(ex, "test-operation");
}

// Flush provider
host.Services.GetRequiredService<LoggerProvider>().ForceFlush();
