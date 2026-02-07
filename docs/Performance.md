# Performance Testing

This document provides performance testing results and analysis for the Essential.OpenTelemetry project, comparing different logging, tracing, and metrics implementations.

## Overview

Performance testing is conducted using [BenchmarkDotNet](https://benchmarkdotnet.org/), the industry standard for .NET performance testing. The benchmarks compare:

- **Standard .NET Console Logger** - The default Microsoft.Extensions.Logging console logger
- **OpenTelemetry Console Exporter** - The out-of-the-box OpenTelemetry console exporter
- **Essential.OpenTelemetry Colored Console Exporter** - This project's colored console exporter
- **Disabled/Overhead** - Measurements with logging/tracing/metrics disabled to understand the overhead

## Running the Benchmarks

To run the performance benchmarks:

```bash
cd test/Essential.OpenTelemetry.Performance
dotnet run -c Release
```

For specific benchmark categories:

```bash
# Run only logging benchmarks
dotnet run -c Release -- --filter *LoggingBenchmarks*

# Run only tracing benchmarks
dotnet run -c Release -- --filter *TracingBenchmarks*

# Run only metrics benchmarks
dotnet run -c Release -- --filter *MetricsBenchmarks*
```

Results are saved in the `BenchmarkDotNet.Artifacts` directory.

## Test Environment

- **OS**: Ubuntu 24.04.3 LTS (Noble Numbat)
- **CPU**: AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
- **Runtime**: .NET 10.0.2 (10.0.225.61305), X64 RyuJIT AVX2
- **BenchmarkDotNet**: v0.14.0

## Logging Performance

The logging benchmarks measure the performance of logging 100 messages with a simple template (`"Benchmark test message {Value}"`).

### Results Summary

| Method                       | Mean         | Ratio | Allocated | Alloc Ratio |
|------------------------------|-------------:|------:|----------:|------------:|
| StandardConsoleLogger        |   512.421 us |  1.00 |  33.59 KB |        1.00 |
| ColoredConsoleExporter       |   902.390 us |  1.77 |  16.41 KB |        0.49 |
| OpenTelemetryConsoleExporter | 3,412.384 us |  6.68 | 189.10 KB |        5.63 |
| DisabledLogging              |     3.776 us |  0.01 |   5.47 KB |        0.16 |

### Analysis

**Key Findings:**

1. **Essential.OpenTelemetry Colored Console Exporter** is significantly faster than the standard OpenTelemetry Console Exporter:
   - **3.8x faster** (902 µs vs 3,412 µs)
   - **11.5x less memory allocation** (16.41 KB vs 189.10 KB)

2. **Memory efficiency**: The Colored Console Exporter uses **less than half the memory** of the standard .NET console logger (16.41 KB vs 33.59 KB)

3. **Overhead**: When logging is disabled, the overhead is minimal at 3.776 µs (0.7% of the fastest logger)

4. **Standard .NET Console Logger** remains the fastest single logger implementation for simple console output, but the Essential.OpenTelemetry exporter provides significantly better performance than the standard OpenTelemetry implementation while adding color formatting capabilities.

### Performance Recommendations

- For production OpenTelemetry scenarios where console output is needed, the Essential.OpenTelemetry Colored Console Exporter provides the best balance of performance and functionality
- The low overhead of disabled logging (0.7%) means there's minimal performance impact from having logging statements in your code
- Memory-constrained environments will benefit from the lower allocation rate of the Colored Console Exporter

## Tracing Performance

The tracing benchmarks measure the performance of creating and exporting 100 activities (spans) with a simple tag.

### Results Summary

| Method                       | Mean     | Ratio | Allocated | Alloc Ratio |
|------------------------------|--------:|------:|----------:|------------:|
| OpenTelemetryConsoleExporter | 4.454 ms |  1.00 | 266.44 KB |        1.00 |
| ColoredConsoleExporter       | 4.284 ms |  0.96 | 266.44 KB |        1.00 |
| DisabledTracing              | 4.013 ms |  0.90 | 266.44 KB |        1.00 |

### Analysis

**Key Findings:**

1. **Essential.OpenTelemetry Colored Console Exporter** performs **equivalently** to the standard OpenTelemetry Console Exporter:
   - Similar execution time (4.284 ms vs 4.454 ms)
   - Identical memory allocation (266.44 KB)

2. **Disabled tracing overhead**: With tracing disabled (no exporters), there's only a 10% performance improvement (4.013 ms vs 4.454 ms), indicating that most of the cost is in activity creation, not export

3. **Activity creation dominates**: The similar performance across all scenarios shows that activity creation and management is the primary cost, not the export process

### Performance Recommendations

- The Essential.OpenTelemetry Colored Console Exporter adds color formatting with **no performance penalty** compared to the standard exporter
- Tracing has inherent overhead even when disabled; careful use of sampling and filtering is recommended
- The export process is relatively inexpensive compared to activity creation

## Metrics Performance

The metrics benchmarks measure the performance of incrementing a counter 1,000 times.

### Results Summary

| Method                       | Mean     | Ratio |
|------------------------------|--------:|------:|
| OpenTelemetryConsoleExporter | 22.58 us |  1.00 |
| ColoredConsoleExporter       | 22.02 us |  0.98 |
| DisabledMetrics              | 21.68 us |  0.96 |

### Analysis

**Key Findings:**

1. **Essential.OpenTelemetry Colored Console Exporter** performs **identically** to the standard OpenTelemetry Console Exporter:
   - Virtually identical execution time (22.02 µs vs 22.58 µs)
   - No measurable memory allocation difference

2. **Very low overhead**: Metrics collection has minimal overhead with only 4% difference between enabled and disabled metrics (21.68 µs vs 22.58 µs)

3. **Efficient implementation**: Metrics are highly optimized in OpenTelemetry with minimal per-operation cost

### Performance Recommendations

- Use metrics freely; the overhead is negligible (approximately 22 nanoseconds per increment)
- The Essential.OpenTelemetry Colored Console Exporter is a drop-in replacement for the standard exporter with no performance impact
- Metrics provide excellent observability with minimal performance cost

## Overall Conclusions

### Essential.OpenTelemetry Colored Console Exporter Performance Profile

1. **Logging**: **3.8x faster** and **11.5x more memory efficient** than standard OpenTelemetry Console Exporter
2. **Tracing**: **Equivalent performance** to standard OpenTelemetry Console Exporter
3. **Metrics**: **Equivalent performance** to standard OpenTelemetry Console Exporter

### When to Use Essential.OpenTelemetry

The Essential.OpenTelemetry Colored Console Exporter is an excellent choice for:

- **Development environments** where colored console output improves readability
- **Production scenarios** where console logging is required and performance matters
- **Memory-constrained environments** where allocation rates are important
- **Any OpenTelemetry console output** - it's a superior drop-in replacement

### Performance Best Practices

1. **Logging**: Use appropriate log levels and filtering to minimize unnecessary logging
2. **Tracing**: Use sampling to control the volume of traces in production
3. **Metrics**: Metrics have negligible overhead; use them liberally for observability
4. **Console Output**: For production, consider structured exporters (OTLP, etc.) for better performance than console output

## Comparison with Legacy Systems

This performance testing approach is similar to the [Essential.Diagnostics project](https://github.com/sgryphon/essential-diagnostics/blob/develop/docs/Comparison.md), which provided detailed performance comparisons for System.Diagnostics listeners. The modern OpenTelemetry approach with Essential.OpenTelemetry continues this tradition of providing high-performance, efficient telemetry solutions.

## Methodology Notes

- All benchmarks use BenchmarkDotNet with ShortRun configuration (3 warmup iterations, 3 measurement iterations)
- Measurements are taken in Release mode with optimizations enabled
- Memory diagnostics track managed memory allocations only
- Results may vary based on hardware, OS, and .NET runtime version
- Benchmarks simulate realistic workloads (message logging, span creation, metric recording)

## Contributing Performance Improvements

If you identify performance improvements:

1. Create a benchmark demonstrating the issue
2. Implement your optimization
3. Run benchmarks to verify improvement
4. Submit a PR with before/after benchmark results

For questions or suggestions about performance, please [open an issue](https://github.com/sgryphon/essential-opentelemetry/issues).
