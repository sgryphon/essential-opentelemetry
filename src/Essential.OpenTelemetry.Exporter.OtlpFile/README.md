# Essential OpenTelemetry OTLP File Exporter

OTLP File exporter for OpenTelemetry .NET that outputs OpenTelemetry signals to stdout (console) in JSONL format compatible with the [OpenTelemetry Protocol File Exporter specification](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/file-exporter.md) and the OpenTelemetry Collector JSON File Receiver.

This exporter is part of the [Essential .NET OpenTelemetry](https://github.com/sgryphon/essential-opentelemetry) project, which provides guidance, additional exporters, and extensions for .NET OpenTelemetry implementations.

NOTE: This alpha version only supports console output. Future versions will include configuration for specific output files, along with rotation options based on the Collector format.

## Features

- Outputs logs, spans, and traces in OTLP protobuf JSON format (one JSON object per line)
- Compatible with OpenTelemetry Collector file exporter/receiver
- Supports structured logging with semantic attributes

## Installation

Install the required NuGet packages via `dotnet` or another package manager:

```powershell
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package Essential.OpenTelemetry.Exporter.OtlpFile
```

## Usage

The exporter can be configured as a standard OpenTelemetry exporter for a host based application.

- Add the required packages
- Reference the required namespaces
- Clear the default logging providers (so there is no other console output)
- Add the OtlpFileExporter

```csharp
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

// Add OpenTelemetry with OTLP File exporter
builder
    .Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddOtlpFileExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation().AddOtlpFileExporter();
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
            .AddOtlpFileExporter(options => { }, exportIntervalMilliseconds: 60_000);
    });

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
```

### Output Format

The exporter outputs one JSON object per line in OTLP protobuf JSON format. The example is here is formatted for readability, but is output as a single line (JSONL) by the exporter.

```json
{
  "resourceLogs": [
    {
      "resource": { "attributes": [] },
      "scopeLogs": [
        {
          "scope": { "name": "Program" },
          "logRecords": [
            {
              "timeUnixNano": "1771035371041000000",
              "observedTimeUnixNano": "1771035371041000000",
              "severityNumber": 9,
              "severityText": "Information",
              "body": { "stringValue": "Hello from Alice" },
              "attributes": [
                { "key": "Name", "value": { "stringValue": "Alice" } }
              ]
            }
          ]
        }
      ]
    }
  ]
}
```

### Using with OpenTelemetry Collector

You can pipe the output to a file and use it with the OTLP JSON File Receiver:

```bash
dotnet run > logs.jsonl
```

## License

Essential.OpenTelemetry OtlpFile Exporter - OTLP JSON file output for OpenTelemetry logs, traces, and metrics.
Copyright (C) 2026 Gryphon Technology Pty Ltd

This library is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License and GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License and GNU General Public License along with this library. If not, see <https://www.gnu.org/licenses/>.
