// Simple OpenTelemetry example
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

const string ServiceName = "SimpleConsole";
var activitySource = new ActivitySource(ServiceName);
var meter = new Meter(ServiceName);

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder
    .Services.AddOpenTelemetry()
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
var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var tracerProvider = host.Services.GetRequiredService<TracerProvider>();
var meterProvider = host.Services.GetRequiredService<MeterProvider>();

// Create a simple counter metric
var requestCounter = meter.CreateCounter<int>("requests", "count", "Number of requests");

// Create a simple activity (span)
using (var activity = activitySource.StartActivity("SampleOperation"))
{
    logger.LogInformation("Hello world");
    requestCounter.Add(1);
}

// Force flush to ensure all telemetry is exported before exit
tracerProvider.ForceFlush();
meterProvider.ForceFlush();
