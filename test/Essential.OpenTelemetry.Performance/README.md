# Essential.OpenTelemetry Performance Tests

This project contains performance benchmarks for comparing different OpenTelemetry exporters and logging implementations.

## Prerequisites

- .NET 10.0 SDK

## Running the Benchmarks

To run all benchmarks (`*` is a wildcard for all benchmarks):

```bash
dotnet run --project test/Essential.OpenTelemetry.Performance -c Release -- --filter *
```

To run a specific benchmark (Logging, Tracing, or Metrics), use a wildcard pattern:

```bash
# Run only logging benchmarks
dotnet run --project test/Essential.OpenTelemetry.Performance -c Release --filter *Logging*
```

To get a list of the individual benchmarks:

```bash
dotnet run --project test/Essential.OpenTelemetry.Performance -c Release -- --list flat
```

## Configuration

Benchmark parameters can be configured in `appsettings.json`:

```json
{
  "BenchmarkConfiguration": {
    "LoggingIterations": 1000,
    "TracingIterations": 1000,
    "MetricsCounterCount": 1000,
    "MetricsIncrementsPerCounter": 10
  }
}
```

You can also override these settings via command-line arguments:

```bash
dotnet run --project test/Essential.OpenTelemetry.Performance -c Release --filter *Logging* -- BenchmarkConfiguration:LoggingIterations=10
```

## Results

Results are saved in the `BenchmarkDotNet.Artifacts` directory after running the benchmarks.

For detailed performance analysis and comparison, see [docs/Performance.md](../../docs/Performance.md).

## Notes

- All benchmarks are run in Release mode for accurate performance measurements
- BenchmarkDotNet automatically runs warmup iterations before actual measurements
- Memory diagnostics are enabled to track memory allocations
- The benchmarks target .NET 10.0 and use .NET 9.0 runtime moniker (the latest stable version supported by BenchmarkDotNet)
