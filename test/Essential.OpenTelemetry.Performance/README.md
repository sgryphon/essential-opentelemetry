# Essential.OpenTelemetry Performance Tests

This project contains performance benchmarks for comparing different OpenTelemetry exporters and logging implementations.

## Prerequisites

- .NET 10.0 SDK
- BenchmarkDotNet 0.14.0

## Running the Benchmarks

### Using the provided scripts

The easiest way to run all benchmarks:

**Linux/macOS:**
```bash
./run-benchmarks.sh
```

**Windows:**
```powershell
.\run-benchmarks.ps1
```

### Manual execution

To run all benchmarks manually:

```bash
cd test/Essential.OpenTelemetry.Performance
dotnet run -c Release
```

To run a specific benchmark:

```bash
# Run only logging benchmarks
dotnet run -c Release --filter *LoggingBenchmarks*

# Run only tracing benchmarks
dotnet run -c Release --filter *TracingBenchmarks*

# Run only metrics benchmarks
dotnet run -c Release --filter *MetricsBenchmarks*
```

## Benchmark Categories

### Logging Benchmarks (`LoggingBenchmarks`)
Compares the performance of:
- Standard .NET Console Logger
- OpenTelemetry Console Exporter (out of the box)
- Essential.OpenTelemetry Colored Console Exporter
- Disabled Logging (to measure overhead)

### Tracing Benchmarks (`TracingBenchmarks`)
Compares the performance of:
- OpenTelemetry Console Exporter (out of the box)
- Essential.OpenTelemetry Colored Console Exporter
- Disabled Tracing (no exporters)

### Metrics Benchmarks (`MetricsBenchmarks`)
Compares the performance of:
- OpenTelemetry Console Exporter (out of the box)
- Essential.OpenTelemetry Colored Console Exporter
- Disabled Metrics (no exporters)

## Results

Results are saved in the `BenchmarkDotNet.Artifacts` directory after running the benchmarks.

For detailed performance analysis and comparison, see [docs/Performance.md](../../docs/Performance.md).

## Notes

- All benchmarks are run in Release mode for accurate performance measurements
- BenchmarkDotNet automatically runs warmup iterations before actual measurements
- Memory diagnostics are enabled to track memory allocations
- The benchmarks target .NET 9.0 runtime (the latest stable version supported by BenchmarkDotNet)
