// Simple OpenTelemetry example (net6.0)
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

const string ServiceName = "Net60Console-Net6";
const string FrameworkVersion = ".NET 6.0";

var activitySource = new ActivitySource(ServiceName);
var meter = new Meter(ServiceName);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging => logging.ClearProviders())
    .ConfigureServices(services =>
    {
        services
            .AddOpenTelemetry()
            .WithLogging(logging =>
            {
                logging.AddColoredConsoleExporter();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(ServiceName).AddColoredConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(ServiceName).AddColoredConsoleExporter();
            });
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var tracerProvider = host.Services.GetRequiredService<TracerProvider>();
var meterProvider = host.Services.GetRequiredService<MeterProvider>();

// Create a simple counter metric
var requestCounter = meter.CreateCounter<int>("requests", "count", "Number of requests");

Console.WriteLine($"Running on {FrameworkVersion}");

// Create a simple activity (span)
using (var activity = activitySource.StartActivity("SampleOperation"))
{
    logger.LogInformation($"Hello world from {FrameworkVersion}");
    requestCounter.Add(1);
}

// Force flush to ensure all telemetry is exported before exit
meterProvider.ForceFlush();
tracerProvider.ForceFlush();
