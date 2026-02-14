# Essential OpenTelemetry JSONL Console Exporter

JSONL (JSON Lines) console exporter for OpenTelemetry .NET that outputs OpenTelemetry signals (traces, metrics, logs) to console in JSONL format compatible with the OpenTelemetry Collector File Exporter and OTLP JSON File Receiver.

## Overview

This exporter outputs telemetry data in JSON Lines format to the console, with each line being a valid JSON object representing an OpenTelemetry signal. This format is compatible with:

- [OpenTelemetry Collector File Exporter](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/exporter/fileexporter)
- [OTLP JSON File Receiver](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/receiver/otlpjsonfilereceiver)

## Installation

```bash
dotnet add package Essential.OpenTelemetry.Exporter.JsonlConsole
```

## Usage

*Usage instructions will be added when implementation is complete.*

## References

- [OpenTelemetry Collector File Exporter](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/exporter/fileexporter)
- [OTLP JSON File Receiver](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/receiver/otlpjsonfilereceiver)

## License

LGPL v3 - Copyright (C) 2026 Gryphon Technology Pty Ltd
