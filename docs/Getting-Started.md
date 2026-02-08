# Getting started

## Basic usage (logging)

Add the following using statements:

```csharp
using Essential.OpenTelemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
```

Configure OpenTelemetry with the colored console exporter for logging, and clear the default loggers:

```csharp
builder.Logging.ClearProviders();
builder.Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddColoredConsoleExporter();
    });
```

## Usage (full telemetry)

For full telemetry support including traces and metrics, also add these using statements:

```csharp
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
```

Then configure the colored console exporter along with your other configuration for logs, traces, and metrics:

```csharp
builder.Logging.ClearProviders();
builder.Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddColoredConsoleExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddSource("YourServiceName")
               .AddColoredConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("YourServiceName")
               .AddColoredConsoleExporter();
    });
```

See the [examples](../examples/SimpleConsole) for a working example.
