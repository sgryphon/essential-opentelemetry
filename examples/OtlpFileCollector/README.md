# JSONL Console Exporter - OpenTelemetry Collector Verification Example

This example demonstrates how to use the Essential OpenTelemetry JSONL Console Exporter with the OpenTelemetry Collector and verify the output using the Aspire Dashboard.

## Overview

This example includes:

- A simple console application that outputs logs in JSONL format (OTLP JSON)
- Podman/Docker Compose configuration for running the OpenTelemetry Collector
- Aspire Dashboard for visualizing the collected telemetry
- Configuration for the OTel Collector to read JSONL files and forward to Aspire

## Prerequisites

- .NET SDK 10.0
- Podman or Docker with compose support
- Basic understanding of OpenTelemetry concepts

## Architecture

```
Console App → JSONL File → OTel Collector → Aspire Dashboard
                 ↓              ↓                  ↓
            logs.jsonl    (file receiver)    (visualization)
```

## Quick Start

### 1. Start the Observability Stack

Navigate to the compose directory and start the containers using podman (or docker):

```powershell
cd examples/OtlpFileCollector
podman compose up -d
```

This will start:

- Aspire Dashboard on http://localhost:18888
- OpenTelemetry Collector configured to read JSONL files

### 2. Check Aspire Dashboard Access

The dashboard is configured for anonymous access, so you can navigate directly to:

- **Dashboard UI**: http://localhost:18888

### 3. Run the Sample Application

From the example directory, run the application and redirect output to a file:

```powershell
dotnet run --project Example.OtlpFile > logs.jsonl
```

### 4. Copy Logs to Collector Input

Create the input directory if it doesn't exist and copy the logs:

```powershell
mkdir -p data/input
cp logs.jsonl data/input/
```

The OTel Collector is configured to watch the `data/input/` directory for `*.jsonl` files.

### 5. View Logs in Aspire Dashboard

Check the Aspire Dashboard at http://localhost:18888

Once the file is processed you should see the structured log events appear.

The logs include:

- Different severity levels (Trace, Debug, Info, Warning, Error, Critical)
- Structured logging with attributes
- Trace context (when logging within an Activity)
- Exception information

## Troubleshooting

### Checking Collector Logs

To verify the collector is working correctly and see debug output:

```powershell
podman logs otlpfilecollector_otel-collector_1
```

The collector is configured with debug logging, so you should see:

- Information about the file receiver reading your logs
- Debug output showing the log records being processed
- Any errors or warnings

### Checking Aspire Logs

If the collector is working, you can check the Aspire logs

```powershell
podman logs otlpfilecollector_aspire-dashboard_1
```

## Cleaning Up

To stop and remove the containers:

```powershell
podman-compose down
```

## References

- [OpenTelemetry Protocol File Exporter Specification](https://opentelemetry.io/docs/specs/otel/protocol/file-exporter/)
- [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/)
- [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard)
