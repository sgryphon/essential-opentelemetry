# Performance Testing

Performance testing results and analysis for the Essential.OpenTelemetry project.

Performance testing is conducted using [BenchmarkDotNet](https://benchmarkdotnet.org/), the industry standard for .NET performance testing. The benchmarks compare:

- **Standard .NET Console Logger** - The default Microsoft.Extensions.Logging console logger (for reference)
- **Essential.OpenTelemetry Colored Console Exporter** - This project's colored console exporter
- **OpenTelemetry Console Exporter** - The out-of-the-box OpenTelemetry console exporter
- **Disabled/Overhead** - Measurements with OpenTelemetry logging/tracing/metrics disabled to understand the overhead

The focus is on comparing the console exporters, specifically the performance difference between the standard OpenTelemetry console exporter and the Essential.OpenTelemetry colored console exporter.

## Logging Performance

The logging benchmarks measure the performance of logging 100 messages with a simple template (`"Benchmark test message {Value}"`).

| Exporter                                  |     Mean | Allocated |
| ----------------------------------------- | -------: | --------: |
| ![](images/ex.png) ColoredConsoleExporter |   116 ms |     16 kB |
| StandardConsoleLogger                     |    88 ms |     34 kB |
| OpenTelemetryConsoleExporter              |   588 ms |    194 kB |
| DisabledLogging                           | 0.006 ms |      5 kB |

Run with BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7705/25H2/2025Update/HudsonValley2) on an Intel Core Ultra 7 155H 1.40GHz, 1 CPU, 22 logical and 16 physical cores and .NET SDK 10.0.102. Results will vary by machine.

The colored console exporter is comparable to the standard Microsoft.Extensions.Logger console.

It is significantly faster than, and uses less memory than, then default OpenTelemetry console logger.

## Tracing Performance

The tracing benchmarks measure the performance of creating and exporting 100 activities (spans) with a simple tag.

| Exporter                                  |   Mean | Allocated |
| ----------------------------------------- | -----: | --------: |
| ![](images/ex.png) ColoredConsoleExporter | 693 ms |    268 kB |
| OpenTelemetryConsoleExporter              | 743 ms |    267 kB |
| DisabledTracing                           | 774 ms |    267 kB |

All three results are comparable, with error ranges overlapping.

For spans disabling exporting makes no difference, indicating the majority of time may be in tracking the span, not exporting it.

## Metrics Performance

The metrics benchmarks test the performance of exporting 100 counters, each incremented 10 times, with an export interval of 100ms.

| Exporter                                  |    Mean | Allocated |
| ----------------------------------------- | ------: | --------: |
| ![](images/ex.png) ColoredConsoleExporter |  300 ms |    394 kB |
| OpenTelemetryConsoleExporter              |  368 ms |    380 kB |
| DisabledMetrics                           | 0.06 ms |    0.2 kB |

Metrics results are comparable to the default OpenTelemetry console exporter.

## Running the Benchmarks

See the [Essential.OpenTelemetry.Performance](../test/Essential.OpenTelemetry.Performance/README.md`) project for instructions on how to configure and run the benchmarks.

## Methodology Notes

- All benchmarks use BenchmarkDotNet with ShortRun configuration (3 warmup iterations, 3 measurement iterations)
- Measurements are taken in Release mode with optimizations enabled
- Memory diagnostics track managed memory allocations only
- Results may vary based on hardware, OS, and .NET runtime version
- Benchmarks simulate realistic workloads (message logging, span creation, metric recording)
- All benchmark methods call `ForceFlush()` to ensure exporters have completed their work

## Contributing Performance Improvements

If you identify performance improvements:

1. Create a benchmark demonstrating the issue
2. Implement your optimization
3. Run benchmarks to verify improvement
4. Submit a PR with before/after benchmark results

For questions or suggestions about performance, please [open an issue](https://github.com/sgryphon/essential-opentelemetry/issues).

[Home](../README.md) | [Getting Started](./Getting-Started.md) | [Logging Levels](./Logging-Levels.md) | [Event IDs](./Event-Ids.md) | Performance Testing
