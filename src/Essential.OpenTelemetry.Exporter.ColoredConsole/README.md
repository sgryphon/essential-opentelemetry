# Essential .NET OpenTelemetry Colored Console Exporter

Enhanced console exporter for OpenTelemetry .NET with color-coded formatting for logs, traces, and metrics.

This exporter is part of the [Essential .NET OpenTelemetry](https://github.com/sgryphon/essential-opentelemetry) project, which provides guidance, additional exporters, and extensions for .NET OpenTelemetry implementations.

## Features

- **Color-coded output**: Different colors for different log levels, trace events, and metrics
- **Structured logging support**: Displays structured log data in a readable format
- **OpenTelemetry standard compliance**: Works seamlessly with the OpenTelemetry SDK
- **Multi-framework support**: Compatible with currently supported .NET versions

## Installation

Install the required NuGet packages via `dotnet` or another package manager:

```powershell
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package Essential.OpenTelemetry.Exporter.ColoredConsole
```

## Getting started with basic logging

For a standard .NET host application, add the following using statements:

```csharp
using Essential.OpenTelemetry;
using Microsoft.Extensions.Hosting;
```

Then clear the default loggers, and configure OpenTelemetry with the colored console exporter:

```csharp
builder.Logging.ClearProviders();
builder.Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddColoredConsoleExporter();
    });
```

## Automatic instrumentation - ASP.NET

Automatic instrumentation is available for various components, e.g. for an ASP.NET application, following on from the basic setup, above:

```powershell
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
```

Then add the additional using statements:

```csharp
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
```

And configure the colored console exporter along with the automatic instrumentation for traces and metrics:

```csharp
builder.Logging.ClearProviders();
builder.Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddColoredConsoleExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation().AddColoredConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddView(instrument =>
                instrument.Name.StartsWith("http.server.request", StringComparison.Ordinal)
                    ? null
                    : MetricStreamConfiguration.Drop
            )
            .AddColoredConsoleExporter(options => { }, exportIntervalMilliseconds: 60_000);
    });
```

See the full project for a working example.

## Copyright

Essential.OpenTelemetry ColoredConsole Exporter - Color-coded formatting for OpenTelemetry logs, traces, and metrics.
Copyright (C) 2026 Gryphon Technology Pty Ltd

This library is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License and GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License and GNU General Public License along with this library. If not, see <https://www.gnu.org/licenses/>.
