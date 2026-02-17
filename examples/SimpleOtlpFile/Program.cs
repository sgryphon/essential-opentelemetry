// Simple OpenTelemetry example (multi-targeting net10.0, net9.0, net8.0)
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

#if NET10_0
const string ServiceName = "SimpleOtlpFile-Net10";
const string FrameworkVersion = ".NET 10.0";
#elif NET9_0
const string ServiceName = "SimpleOtlpFile-Net9";
const string FrameworkVersion = ".NET 9.0";
#elif NET8_0
const string ServiceName = "SimpleOtlpFile-Net8";
const string FrameworkVersion = ".NET 8.0";
#else
const string ServiceName = "SimpleOtlpFile";
const string FrameworkVersion = "Unknown";
#endif

var activitySource = new ActivitySource(ServiceName);
var meter = new Meter(ServiceName);

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder
    .Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddOtlpFileExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddSource(ServiceName).AddOtlpFileExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter(ServiceName).AddOtlpFileExporter();
    });
var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var tracerProvider = host.Services.GetRequiredService<TracerProvider>();
var meterProvider = host.Services.GetRequiredService<MeterProvider>();

// Create a simple counter metric
var requestCounter = meter.CreateCounter<int>("requests", "count", "Number of requests");

Console.WriteLine($"Running on {FrameworkVersion}");

// Create a simple activity (span)
using (var activity = activitySource.StartActivity("SampleOperation"))
{
    logger.HelloWorld(FrameworkVersion);
    requestCounter.Add(1);
}

// Force flush to ensure all telemetry is exported before exit
meterProvider.ForceFlush();
tracerProvider.ForceFlush();

// Source-generated log methods
internal static partial class LoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Hello world from {FrameworkVersion}")]
    public static partial void HelloWorld(this ILogger logger, string frameworkVersion);
}
