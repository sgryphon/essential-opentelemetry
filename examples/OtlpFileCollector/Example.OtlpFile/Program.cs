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
using OpenTelemetry.Trace;

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
        // Enable OTLP export for comparison, to either 14317 (collector) or 18889 (Aspire)
        // logging.AddOtlpExporter(otlpOptions =>
        // {
        //     otlpOptions.Endpoint = new Uri("http://127.0.0.1:14317");
        // });
        // tracing.AddConsoleExporter();
        tracing.AddOtlpFileExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter(serviceName);
        // Enable OTLP export for comparison, to either 14317 (collector) or 18889 (Aspire)
        // logging.AddOtlpExporter(otlpOptions =>
        // {
        //     otlpOptions.Endpoint = new Uri("http://127.0.0.1:14317");
        // });
        // metrics.AddConsoleExporter();
        metrics.AddOtlpFileExporter(configure => { }, 3000);
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
    activity?.AddBaggage("Baggage1", "One");

    using (logger.BeginScope("Request {RequestId}", "REQ-123"))
    {
        using (logger.BeginScope("Inner Scope"))
        {
            logger.ProcessingOrder("ORD-789", 150);

            var eventTags = new ActivityTagsCollection() { { "ActivityTag", 150 } };
            activity?.AddEvent(new ActivityEvent("ActivityEvent", tags: eventTags));
        }
    }
    using (var innerActivity = activitySource.StartActivity("InnerActivity"))
    {
        var linkTags = new ActivityTagsCollection() { { "LinkTag", "LINK" } };
        innerActivity?.AddLink(new ActivityLink(activity!.Context, linkTags));

        logger.ResourceRunningLow("disk space");

        try
        {
            throw new InvalidOperationException("Simulated activity exception for testing");
        }
        catch (Exception ex)
        {
            var exceptionTags = new TagList() { { "LinkTag", "LINK" } };
            innerActivity?.AddException(ex, tags: exceptionTags);
        }

        innerActivity?.SetStatus(ActivityStatusCode.Error, "Status error");
    }

    // More metrics during processing
    requestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "/api/process"));
}

// Exception logging example
try
{
    throw new InvalidOperationException("Simulated log exception for testing");
}
catch (Exception ex)
{
    logger.OperationError(ex, "test-operation");
}

// Flush providers
await Task.Delay(4000);
host.Services.GetRequiredService<LoggerProvider>().ForceFlush();
host.Services.GetRequiredService<TracerProvider>().ForceFlush();
host.Services.GetRequiredService<MeterProvider>().ForceFlush();
