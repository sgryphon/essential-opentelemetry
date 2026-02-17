// OtlpFile Exporter example for OpenTelemetry Collector verification
// This example outputs logs, traces, and metrics in OTLP file format that can be consumed by the OTel Collector
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
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
var meter = new Meter(serviceName, serviceVersion);

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
            .AddService(serviceName, serviceVersion: serviceVersion)
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
        // logging.AddConsoleExporter();
        logging.AddOtlpFileExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddSource(serviceName);
        tracing.AddOtlpFileExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter(serviceName);
        metrics.AddOtlpFileExporter();
    });

// Configure logging options
builder.Services.Configure<OpenTelemetryLoggerOptions>(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
});

var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Create metrics
var requestCounter = meter.CreateCounter<long>("requests", "count", "Number of requests");
var requestDuration = meter.CreateHistogram<double>("request.duration", "ms", "Request duration");
var activeConnections = meter.CreateObservableGauge<int>(
    "active.connections",
    () => Random.Shared.Next(10, 100),
    "count",
    "Number of active connections"
);

logger.DebugMessage();
logger.CriticalSystemFailure("payment-service");

// Structured logging with multiple attributes
logger.UserLoggedIn("Alice", IPAddress.Parse("fdbe:4f24:c288:3b1b::4"), DateTimeOffset.UtcNow);

// Record some metrics
requestCounter.Add(10, new KeyValuePair<string, object?>("endpoint", "/api/users"));
requestCounter.Add(5, new KeyValuePair<string, object?>("endpoint", "/api/orders"));
requestDuration.Record(123.45, new KeyValuePair<string, object?>("endpoint", "/api/users"));
requestDuration.Record(67.89, new KeyValuePair<string, object?>("endpoint", "/api/orders"));

// Log within an activity to include trace context
using (var activity = activitySource.StartActivity("OrderProcessing"))
{
    activity?.SetTag("order.id", "ORD-789");
    activity?.SetTag("order.amount", 150.00);

    using (logger.BeginScope("Request {RequestId}", "REQ-123"))
    {
        using (logger.BeginScope("Inner Scope"))
        {
            logger.ProcessingOrder("ORD-789", 150);
        }
    }
    logger.ResourceRunningLow("disk space");

    // More metrics during processing
    requestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "/api/process"));
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

// Flush providers
host.Services.GetRequiredService<LoggerProvider>().ForceFlush();
host.Services.GetRequiredService<MeterProvider>().ForceFlush();
