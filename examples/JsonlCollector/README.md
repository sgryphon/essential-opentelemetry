# JSONL Console Exporter - OpenTelemetry Collector Verification Example

This example demonstrates how to use the Essential OpenTelemetry JSONL Console Exporter with the OpenTelemetry Collector and verify the output using the Aspire Dashboard.

## Overview

This example includes:
- A simple console application that outputs logs in JSONL format (OTLP JSON)
- Docker/Podman Compose configuration for running the OpenTelemetry Collector
- Aspire Dashboard for visualizing the collected telemetry
- Configuration for the OTel Collector to read JSONL files and forward to Aspire

## Prerequisites

- .NET SDK 8.0, 9.0, or 10.0
- Docker or Podman with compose support
- Basic understanding of OpenTelemetry concepts

## Architecture

```
Console App → JSONL File → OTel Collector → Aspire Dashboard
                 ↓              ↓                  ↓
            logs.jsonl    (file receiver)    (visualization)
```

## Quick Start

### 1. Start the Observability Stack

Navigate to the compose directory and start the containers:

```bash
cd compose
podman-compose up -d
# or with Docker:
# docker-compose up -d
```

This will start:
- **Aspire Dashboard** on http://localhost:18888
- **OpenTelemetry Collector** configured to read JSONL files

### 2. Check Aspire Dashboard Access

View the logs to get any authentication details (if needed):

```bash
podman logs jsonl-aspire-dashboard
# or with Docker:
# docker logs jsonl-aspire-dashboard
```

The dashboard is configured for anonymous access, so you can navigate directly to:
- **Dashboard UI**: http://localhost:18888

### 3. Run the Sample Application

From the example directory, run the application and redirect output to a file:

```bash
cd ..
dotnet run --framework net10.0 > logs.jsonl
```

You can also specify a different framework:
```bash
dotnet run --framework net9.0 > logs.jsonl
dotnet run --framework net8.0 > logs.jsonl
```

### 4. Copy Logs to Collector Input

Create the input directory if it doesn't exist and copy the logs:

```bash
mkdir -p compose/data/input
cp logs.jsonl compose/data/input/
```

The OTel Collector is configured to watch the `compose/data/input/` directory for `*.jsonl` files.

### 5. View Logs in Aspire Dashboard

1. Open the Aspire Dashboard at http://localhost:18888
2. Navigate to the "Structured Logs" or "Traces" section
3. You should see the logs from the sample application

The logs include:
- Different severity levels (Trace, Debug, Info, Warning, Error, Critical)
- Structured logging with attributes
- Trace context (when logging within an Activity)
- Exception information

## Example Output

The application generates JSONL output like:

```json
{"resourceLogs":[{"resource":{"attributes":[...]},"scopeLogs":[{"scope":{"name":"Program"},"logRecords":[{"timeUnixNano":"1771035371041000000","observedTimeUnixNano":"1771035371041000000","severityNumber":9,"severityText":"Info","body":{"stringValue":"Application starting on .NET 10.0"},"attributes":[{"key":"Framework","value":{"stringValue":".NET 10.0"}}],"droppedAttributesCount":0}]}]}]}
```

Each line is a complete JSON object representing one or more log records in OTLP protobuf JSON format.

## Troubleshooting

### Checking Collector Logs

To verify the collector is working correctly and see debug output:

```bash
podman logs jsonl-otel-collector
# or with Docker:
# docker logs jsonl-otel-collector
```

The collector is configured with debug logging, so you should see:
- Information about the file receiver reading your logs
- Debug output showing the log records being processed
- Any errors or warnings

Look for lines like:
```
LogsExporter {"#logs": 10}
```

### Common Issues

1. **Logs not appearing in Aspire Dashboard**
   - Check the collector logs for errors
   - Verify the file is in `compose/data/input/` directory
   - Ensure the file has `.jsonl` extension
   - Check that the JSON format is valid

2. **Cannot access Aspire Dashboard**
   - Verify the container is running: `podman ps` or `docker ps`
   - Check that port 18888 is not already in use
   - Review the Aspire Dashboard logs for errors

3. **Collector not starting**
   - Verify the config file syntax: `podman-compose config`
   - Check for port conflicts (especially 18889)
   - Ensure the data directory is properly mounted

4. **File not being read by collector**
   - The collector reads files on startup and doesn't watch for new files by default
   - Restart the collector after adding new files: `podman-compose restart otel-collector`
   - Or configure file polling in the receiver configuration

### Restarting Components

To restart just the collector:
```bash
cd compose
podman-compose restart otel-collector
```

To restart everything:
```bash
cd compose
podman-compose down
podman-compose up -d
```

## Configuration Files

### Collector Configuration (`compose/otel-collector-config.yml`)

The collector is configured with:
- **Receiver**: `otlpjsonfile` - reads JSONL files from `/data/input/*.jsonl`
- **Processor**: `batch` - batches logs for efficiency
- **Exporters**:
  - `debug` - outputs logs to console for troubleshooting
  - `otlp` - forwards to Aspire Dashboard

### Compose Configuration (`compose/compose.yml`)

The compose file defines:
- **Aspire Dashboard**: Visualization and OTLP receiver
- **OTel Collector**: File receiver and OTLP forwarder
- **Volumes**: 
  - `./otel-collector-config.yml` → collector config
  - `./data` → shared directory for log files

## Cleaning Up

To stop and remove the containers:

```bash
cd compose
podman-compose down
# or with Docker:
# docker-compose down
```

To also remove the data directory:
```bash
rm -rf compose/data
```

## References

- [Essential OpenTelemetry JSONL Console Exporter](../../src/Essential.OpenTelemetry.Exporter.JsonlConsole/README.md)
- [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/)
- [OTLP JSON File Receiver](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/receiver/otlpjsonfilereceiver)
- [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard)
- [OpenTelemetry Protocol Specification](https://opentelemetry.io/docs/specs/otlp/)

## License

LGPL v3 - Copyright (C) 2026 Gryphon Technology Pty Ltd
