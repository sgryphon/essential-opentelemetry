using System;
using Essential.OpenTelemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Clear default logging
builder.Logging.ClearProviders();

// Add OpenTelemetry with Colored Console exporter
builder
    .Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddColoredConsoleExporter();
    })
    .WithTracing(tracing =>
    {
        // ASP.NET Core instrumentation automatically creates spans for HTTP requests
        tracing.AddAspNetCoreInstrumentation().AddColoredConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        // Collect metrics from ASP.NET Core
        metrics
            .AddAspNetCoreInstrumentation()
            // Keep instruments that start with "http.server.request", and drop others
            .AddView(instrument =>
                instrument.Name.StartsWith("http.server.request", StringComparison.Ordinal)
                    ? null
                    : MetricStreamConfiguration.Drop
            )
            .AddColoredConsoleExporter(options => { }, exportIntervalMilliseconds: 60_000);
    });

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
