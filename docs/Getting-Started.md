# Getting started

Welcome to Essential OpenTelemetry! This guide will help you get started with OpenTelemetry logging, tracing, and metrics in .NET applications using the Essential OpenTelemetry colored console exporter.

## What is OpenTelemetry?

OpenTelemetry is an open-source observability framework for instrumenting, generating, collecting, and exporting telemetry data such as traces, metrics, and logs. It provides a unified way to collect observability data from your applications.

## What is Essential OpenTelemetry?

Essential OpenTelemetry provides guidance, additional exporters, and extensions for .NET OpenTelemetry implementations. The primary component is a colored console exporter that makes it easy to see your telemetry data during development and debugging.

## Prerequisites

- [.NET SDK 8.0 or later](https://dotnet.microsoft.com/download) installed on your computer
- Basic familiarity with C# and .NET console or web applications

## Walk through

1. [Hello World - Console Logging](./HelloWorld1-Console.md) -
   Create your first console application with OpenTelemetry logging using the colored console exporter.

2. [Adding Traces](./HelloWorld2-Traces.md) -
   Learn how to add distributed tracing to your console application.

3. [Hello World - ASP.NET Core](HelloWorld3-AspNetCore.md) -
   Build an ASP.NET Core web application with OpenTelemetry, and see how the ASP.NET instrumentation traces are created for HTTP requests.

4. [Viewing Metrics](HelloWorld4-Metrics.md) - See how ASP.NET instrumentation generates metrics and how to view them.

See the [SimpleConsole example](../examples/SimpleConsole) for a working example.

## Next steps

1. Add OpenTelemetry and the `ColoredConsoleExporter` to your project, and start your OpenTelemetry journey today.
2. Expand from console logging to a local system such as [Grafana Loki](https://grafana.com/oss/loki/), [Jaeger](https://www.jaegertracing.io/), [Aspire Dashboard](https://aspire.dev/dashboard/overview/), or [Seq](https://datalust.co/).
3. Deploy with standard OpenTelemetry Protocol (OTLP), or a custom protocol, to one of dozens of supported logging platforms.

---

**Next:** [Hello World - Console Logging](./HelloWorld1-Console.md)

[Home](../README.md) | Getting Started | [Logging Levels](./Logging-Levels.md) | [Event IDs](./Event-Ids.md) | [Performance Testing](docs/Performance.md)
