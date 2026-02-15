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

From the `examples/OtlpFileCollector` directory, run the application and redirect output to a file:

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
podman compose down
```

## Reference output

Configuring the standard OTLP exporter to send to the OpenTelemetry Collector, and then have the Collector export to a file output, gets file contents similar to the following.

- Multiple log records in one line
- Resource details included
- severityNumber is numeric (not string enum)
- Body does not have attributes inserted
- Exception includes stack trace

```json
{
  "resourceLogs": [
    {
      "resource": {
        "attributes": [
          { "key": "host.name", "value": { "stringValue": "TAR-VALON" } },
          {
            "key": "deployment.environment.name",
            "value": { "stringValue": "production" }
          },
          {
            "key": "service.name",
            "value": { "stringValue": "Example.OtlpFile" }
          },
          {
            "key": "service.version",
            "value": {
              "stringValue": "1.0.0+a39edcd73d16166f5105ff3e08aae50d9a30f736"
            }
          },
          {
            "key": "service.instance.id",
            "value": { "stringValue": "38700bd8-eff0-4695-8d6e-6288aea65d46" }
          },
          {
            "key": "telemetry.sdk.name",
            "value": { "stringValue": "opentelemetry" }
          },
          {
            "key": "telemetry.sdk.language",
            "value": { "stringValue": "dotnet" }
          },
          {
            "key": "telemetry.sdk.version",
            "value": { "stringValue": "1.15.0" }
          }
        ]
      },
      "scopeLogs": [
        {
          "scope": { "name": "Program" },
          "logRecords": [
            {
              "timeUnixNano": "1771125740586979900",
              "observedTimeUnixNano": "1771125740586979900",
              "severityNumber": 5,
              "severityText": "Debug",
              "body": {
                "stringValue": "This is a debug message for debugging"
              },
              "eventName": "DebugMessage"
            },
            {
              "timeUnixNano": "1771125740663833200",
              "observedTimeUnixNano": "1771125740663833200",
              "severityNumber": 21,
              "severityText": "Critical",
              "body": {
                "stringValue": "Critical system failure in {Component}"
              },
              "attributes": [
                {
                  "key": "Component",
                  "value": { "stringValue": "payment-service" }
                }
              ],
              "eventName": "CriticalSystemFailure"
            },
            {
              "timeUnixNano": "1771125740702455200",
              "observedTimeUnixNano": "1771125740702455200",
              "severityNumber": 9,
              "severityText": "Information",
              "body": {
                "stringValue": "User {UserName} logged in from {IpAddress} at {Timestamp}"
              },
              "attributes": [
                { "key": "UserName", "value": { "stringValue": "Alice" } },
                {
                  "key": "IpAddress",
                  "value": { "stringValue": "fdbe:4f24:c288:3b1b::4" }
                },
                {
                  "key": "Timestamp",
                  "value": { "stringValue": "02/15/2026 03:22:20 +00:00" }
                }
              ],
              "eventName": "UserLoggedIn"
            },
            {
              "timeUnixNano": "1771125740728441800",
              "observedTimeUnixNano": "1771125740728441800",
              "severityNumber": 9,
              "severityText": "Information",
              "body": {
                "stringValue": "Processing order {OrderId} for {Amount:C}"
              },
              "attributes": [
                { "key": "OrderId", "value": { "stringValue": "ORD-789" } },
                { "key": "Amount", "value": { "stringValue": "150.00" } }
              ],
              "eventName": "ProcessingOrder"
            },
            {
              "timeUnixNano": "1771125740732785100",
              "observedTimeUnixNano": "1771125740732785100",
              "severityNumber": 13,
              "severityText": "Warning",
              "body": { "stringValue": "{Resource} is running low" },
              "attributes": [
                { "key": "Resource", "value": { "stringValue": "disk space" } }
              ],
              "eventName": "ResourceRunningLow"
            },
            {
              "timeUnixNano": "1771125740795465800",
              "observedTimeUnixNano": "1771125740795465800",
              "severityNumber": 17,
              "severityText": "Error",
              "body": {
                "stringValue": "Exception caught while processing {Operation}"
              },
              "attributes": [
                {
                  "key": "exception.type",
                  "value": { "stringValue": "InvalidOperationException" }
                },
                {
                  "key": "exception.message",
                  "value": { "stringValue": "Simulated exception for testing" }
                },
                {
                  "key": "exception.stacktrace",
                  "value": {
                    "stringValue": "System.InvalidOperationException: Simulated exception for testing\r\n   at Program.<Main>$(String[] args) in C:\\Code\\essential-opentelemetry\\examples\\OtlpFileCollector\\Example.OtlpFile\\Program.cs:line 87"
                  }
                },
                {
                  "key": "Operation",
                  "value": { "stringValue": "test-operation" }
                }
              ],
              "eventName": "OperationError"
            }
          ]
        }
      ]
    }
  ]
}
```

## References

- [OpenTelemetry Protocol File Exporter Specification](https://opentelemetry.io/docs/specs/otel/protocol/file-exporter/)
- [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/)
- [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard)
