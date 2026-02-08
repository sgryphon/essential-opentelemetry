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

# Getting Started with Essential OpenTelemetry

Welcome to Essential OpenTelemetry! This guide will help you get started with OpenTelemetry logging, tracing, and metrics in .NET applications using the Essential OpenTelemetry colored console exporter.

## What is OpenTelemetry?

OpenTelemetry is an open-source observability framework for instrumenting, generating, collecting, and exporting telemetry data such as traces, metrics, and logs. It provides a unified way to collect observability data from your applications.

## What is Essential OpenTelemetry?

Essential OpenTelemetry provides guidance, additional exporters, and extensions for .NET OpenTelemetry implementations. The primary component is a colored console exporter that makes it easy to see your telemetry data during development and debugging.

## Prerequisites

- [.NET SDK 8.0 or later](https://dotnet.microsoft.com/download) installed on your computer
- Basic familiarity with C# and .NET console or web applications

## Tutorial Series

This getting started guide consists of a series of hands-on tutorials that progressively introduce OpenTelemetry concepts:

### Console Application Tutorials

1. **[Hello World - Console Logging](./HelloWorld1-Console.md)**  
   Create your first console application with OpenTelemetry logging using the colored console exporter.

2. **[Adding Traces](./HelloWorld2-Traces.md)**  
   Learn how to add distributed tracing to your console application.

### ASP.NET Core Tutorials

3. **[Hello World - ASP.NET Core](./HelloWorld4-AspNetCore.md)**  
   Build an ASP.NET Core web application with OpenTelemetry, and see how traces are automatically created for HTTP requests.

4. **[Viewing Metrics](./HelloWorld5-Metrics.md)**  
   See how ASP.NET Core automatically generates metrics and how to view them.

## What You'll Learn

By the end of these tutorials, you'll understand:

- How to set up OpenTelemetry in both console and web applications
- The three pillars of observability: logs, traces, and metrics
- How to use the Essential OpenTelemetry colored console exporter
- How OpenTelemetry integrates with .NET's built-in logging and diagnostic features

## Next Steps

Ready to begin? Start with **[Hello World - Console Logging](./HelloWorld1-Console.md)** to create your first OpenTelemetry-enabled application!

## Additional Resources

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/languages/net/)
- [Essential OpenTelemetry GitHub Repository](https://github.com/sgryphon/essential-opentelemetry)
- [Logging Levels](./Logging-Levels.md)
- [Event IDs](./Event-Ids.md)
- [Correlation IDs](./Correlation-Ids.md)
