# Essential.OpenTelemetry.Exporter.ColoredConsole

Enhanced console exporter for OpenTelemetry .NET with color-coded formatting for logs, traces, and metrics.

This exporter is part of the [Essential .NET OpenTelemetry](https://github.com/sgryphon/essential-opentelemetry) project, which provides guidance, additional exporters, and extensions for .NET OpenTelemetry implementations.

## Installation

Install the NuGet package:

```bash
dotnet add package Essential.OpenTelemetry.Exporter.ColoredConsole
```

Or via Package Manager:

```powershell
Install-Package Essential.OpenTelemetry.Exporter.ColoredConsole
```

## Basic usage (logging)

Add the following using statements:

```csharp
using Essential.OpenTelemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
```

Configure OpenTelemetry with the colored console exporter for logging:

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Clear default logging providers to use only OpenTelemetry
builder.Logging.ClearProviders();

builder.Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddColoredConsoleExporter();
    });

var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Hello from OpenTelemetry!");
```

## Usage (full telemetry)

For full telemetry support including traces and metrics, add these using statements:

```csharp
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
```

Register the colored console exporter for logs, traces, and metrics:

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Clear default logging providers to use only OpenTelemetry
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

var host = builder.Build();
```

### Using the Logger

Once configured, use the standard `ILogger` interface:

```csharp
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Hello from OpenTelemetry with colored console output!");
```

## Features

- **Color-coded output**: Different colors for different log levels, trace events, and metrics
- **Structured logging support**: Displays structured log data in a readable format
- **OpenTelemetry standard compliance**: Works seamlessly with the OpenTelemetry SDK
- **Multi-framework support**: Compatible with .NET 10.0, 9.0, and 8.0

## Copyright

Copyright (C) 2026 Gryphon Technology Pty Ltd

This library is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License and GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License and GNU General Public License along with this library. If not, see <https://www.gnu.org/licenses/>.
