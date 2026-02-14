# Essential OpenTelemetry JSONL Console Exporter

JSONL (JSON Lines) console exporter for OpenTelemetry .NET that outputs OpenTelemetry log signals to console in JSONL format compatible with the OpenTelemetry Collector File Exporter and OTLP JSON File Receiver.

## Overview

This exporter outputs telemetry data in JSON Lines format to the console, with each line being a valid JSON object representing an OpenTelemetry log signal in OTLP protobuf JSON format. This format is compatible with:

- [OpenTelemetry Collector File Exporter](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/exporter/fileexporter)
- [OTLP JSON File Receiver](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/receiver/otlpjsonfilereceiver)

## Features

- Outputs logs in OTLP protobuf JSON format (one JSON object per line)
- Compatible with OpenTelemetry Collector file exporter/receiver
- Supports structured logging with semantic attributes
- Includes trace context (trace ID and span ID) when available
- Maps log levels to OpenTelemetry severity numbers and text
- Thread-safe output

## Installation

```bash
dotnet add package Essential.OpenTelemetry.Exporter.JsonlConsole
```

## Usage

### Basic usage (logging)

```csharp
using Essential.OpenTelemetry;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

// Configure logging with JSONL console exporter
using var loggerFactory = LoggerFactory.Create(logging =>
    logging.AddOpenTelemetry(options =>
    {
        options.AddJsonlConsoleExporter();
    })
);

var logger = loggerFactory.CreateLogger<Program>();
logger.LogInformation("Hello from {Name}", "Alice");
```

### With Host Builder

```csharp
using Essential.OpenTelemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder
    .Services.AddOpenTelemetry()
    .WithLogging(logging =>
    {
        logging.AddJsonlConsoleExporter();
    });

var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application started");
```

### Output Format

The exporter outputs one JSON object per line in OTLP protobuf JSON format:

```json
{"resourceLogs":[{"resource":{"attributes":[]},"scopeLogs":[{"scope":{"name":"Program"},"logRecords":[{"timeUnixNano":"1771035371041000000","observedTimeUnixNano":"1771035371041000000","severityNumber":9,"severityText":"Info","body":{"stringValue":"Hello from Alice"},"attributes":[{"key":"Name","value":{"stringValue":"Alice"}}],"droppedAttributesCount":0}]}]}]}
```

### Using with OpenTelemetry Collector

You can pipe the output to a file and use it with the OTLP JSON File Receiver:

```bash
dotnet run > logs.jsonl
```

Then configure the OpenTelemetry Collector:

```yaml
receivers:
  otlpjsonfile:
    include:
      - ./logs.jsonl

exporters:
  # Your exporter configuration

service:
  pipelines:
    logs:
      receivers: [otlpjsonfile]
      exporters: [your-exporter]
```

## Advanced Usage

### Structured Logging

The exporter supports structured logging with semantic attributes:

```csharp
logger.LogInformation(
    "User {UserName} logged in from {IpAddress}",
    "Alice",
    "192.168.1.100"
);
```

Output includes both the formatted message and structured attributes.

### Trace Context

When logging within an Activity (span), the trace ID and span ID are automatically included:

```csharp
using var activity = activitySource.StartActivity("MyOperation");
logger.LogInformation("Processing request");
```

The output will include `traceId` and `spanId` fields.

## References

- [OpenTelemetry Collector File Exporter](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/exporter/fileexporter)
- [OTLP JSON File Receiver](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/receiver/otlpjsonfilereceiver)
- [OpenTelemetry Protocol File Exporter Specification](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/file-exporter.md)

## License

LGPL v3 - Copyright (C) 2026 Gryphon Technology Pty Ltd
