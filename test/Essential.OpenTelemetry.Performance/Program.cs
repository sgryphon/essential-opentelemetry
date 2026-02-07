using BenchmarkDotNet.Running;
using Essential.OpenTelemetry.Performance;

// Run all benchmarks in the assembly
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
