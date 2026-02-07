# Getting Started Examples

This directory contains working example projects for the [Getting Started Guide](../../docs/Getting-Started.md) tutorial series.

## Console Application Examples

### HelloWorld1-Console

Basic console logging with OpenTelemetry using the colored console exporter.

**Tutorial:** [Hello World - Console Logging](../../docs/tutorials/HelloWorld1-Console.md)

```bash
cd HelloWorld1-Console
dotnet run
```

### HelloWorld2-Traces

Console application demonstrating distributed tracing with OpenTelemetry.

**Tutorial:** [Adding Traces](../../docs/tutorials/HelloWorld2-Traces.md)

```bash
cd HelloWorld2-Traces
dotnet run
```

### HelloWorld3-Spans

Console application showing nested spans and attributes for detailed tracing.

**Tutorial:** [Working with Spans](../../docs/tutorials/HelloWorld3-Spans.md)

```bash
cd HelloWorld3-Spans
dotnet run
```

## ASP.NET Core Examples

### HelloWorld4-AspNetCore

Web application with OpenTelemetry logging and automatic HTTP request tracing.

**Tutorial:** [Hello World - ASP.NET Core](../../docs/tutorials/HelloWorld4-AspNetCore.md)

```bash
cd HelloWorld4-AspNetCore
dotnet run
# In another terminal:
curl http://localhost:5000
curl http://localhost:5000/greet/World
```

### HelloWorld5-Metrics

Web application with OpenTelemetry logging, tracing, and custom metrics.

**Tutorial:** [Adding Metrics](../../docs/tutorials/HelloWorld5-Metrics.md)

```bash
cd HelloWorld5-Metrics
dotnet run
# In another terminal, make several requests to generate metrics:
curl http://localhost:5000
curl http://localhost:5000/greet/Alice
curl http://localhost:5000/slow
```

Metrics will be exported to the console every 5 seconds.

## Building All Examples

To build all the getting started examples:

```bash
cd HelloWorld1-Console && dotnet build && cd ..
cd HelloWorld2-Traces && dotnet build && cd ..
cd HelloWorld3-Spans && dotnet build && cd ..
cd HelloWorld4-AspNetCore && dotnet build && cd ..
cd HelloWorld5-Metrics && dotnet build && cd ..
```

## Prerequisites

- .NET SDK 10.0 or later
- All examples use project references to the Essential.OpenTelemetry.Exporter.ColoredConsole library

## Learn More

For detailed explanations and additional context, see the full tutorial series starting with the [Getting Started Guide](../../docs/Getting-Started.md).
