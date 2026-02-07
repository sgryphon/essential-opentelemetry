// Simple OpenTelemetry example
using System.Diagnostics;
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

const string ServiceName = "SimpleConsole";
var activitySource = new ActivitySource(ServiceName);

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
    });
var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var tracerProvider = host.Services.GetRequiredService<TracerProvider>();

// Create a simple activity (span)
using (var activity = activitySource.StartActivity("SampleOperation"))
{
    logger.LogInformation("Hello world");
}

// Force flush to ensure activity is exported before exit
tracerProvider.ForceFlush();
