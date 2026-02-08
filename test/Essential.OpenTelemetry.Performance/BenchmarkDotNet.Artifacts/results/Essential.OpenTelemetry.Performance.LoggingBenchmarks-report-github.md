```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.102
  [Host] : .NET 10.0.2 (10.0.225.61305), X64 RyuJIT AVX2
  Dry    : .NET 10.0.2 (10.0.225.61305), X64 RyuJIT AVX2


```
| Method                       | Job      | Runtime   | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean      | Error | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------------- |--------- |---------- |--------------- |------------ |------------ |------------- |------------ |----------:|------:|------:|--------:|----------:|------------:|
| StandardConsoleLogger        | .NET 9.0 | .NET 9.0  | Default        | Default     | Default     | 16           | Default     |        NA |    NA |     ? |       ? |        NA |           ? |
| OpenTelemetryConsoleExporter | .NET 9.0 | .NET 9.0  | Default        | Default     | Default     | 16           | Default     |        NA |    NA |     ? |       ? |        NA |           ? |
| ColoredConsoleExporter       | .NET 9.0 | .NET 9.0  | Default        | Default     | Default     | 16           | Default     |        NA |    NA |     ? |       ? |        NA |           ? |
| DisabledLogging              | .NET 9.0 | .NET 9.0  | Default        | Default     | Default     | 16           | Default     |        NA |    NA |     ? |       ? |        NA |           ? |
|                              |          |           |                |             |             |              |             |           |       |       |         |           |             |
| StandardConsoleLogger        | Dry      | .NET 10.0 | 1              | 1           | ColdStart   | 1            | 1           |  9.754 ms |    NA |  1.00 |    0.00 |         - |          NA |
| OpenTelemetryConsoleExporter | Dry      | .NET 10.0 | 1              | 1           | ColdStart   | 1            | 1           | 16.506 ms |    NA |  1.69 |    0.00 |         - |          NA |
| ColoredConsoleExporter       | Dry      | .NET 10.0 | 1              | 1           | ColdStart   | 1            | 1           | 14.745 ms |    NA |  1.51 |    0.00 |         - |          NA |
| DisabledLogging              | Dry      | .NET 10.0 | 1              | 1           | ColdStart   | 1            | 1           |  5.328 ms |    NA |  0.55 |    0.00 |         - |          NA |

Benchmarks with issues:
  LoggingBenchmarks.StandardConsoleLogger: .NET 9.0(Runtime=.NET 9.0)
  LoggingBenchmarks.OpenTelemetryConsoleExporter: .NET 9.0(Runtime=.NET 9.0)
  LoggingBenchmarks.ColoredConsoleExporter: .NET 9.0(Runtime=.NET 9.0)
  LoggingBenchmarks.DisabledLogging: .NET 9.0(Runtime=.NET 9.0)
