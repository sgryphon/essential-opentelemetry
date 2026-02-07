using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;

namespace Essential.OpenTelemetry.Performance;

/// <summary>
/// Benchmarks for comparing tracing performance across different implementations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class TracingBenchmarks
{
    private const string ServiceName = "BenchmarkService";
    private ActivitySource? _activitySource;
    private IHost? _openTelemetryConsoleHost;
    private IHost? _coloredConsoleHost;
    private IHost? _disabledTracingHost;
    private TracerProvider? _openTelemetryTracerProvider;
    private TracerProvider? _coloredTracerProvider;
    private TracerProvider? _disabledTracerProvider;

    [GlobalSetup]
    public void Setup()
    {
        _activitySource = new ActivitySource(ServiceName);

        // OpenTelemetry Console Exporter (out of the box)
        var otelBuilder = Host.CreateApplicationBuilder();
        otelBuilder.Services.AddOpenTelemetry().WithTracing(tracing =>
        {
            tracing.AddSource(ServiceName).AddConsoleExporter();
        });
        _openTelemetryConsoleHost = otelBuilder.Build();
        _openTelemetryTracerProvider = _openTelemetryConsoleHost.Services.GetRequiredService<TracerProvider>();

        // Essential.OpenTelemetry Colored Console Exporter
        var coloredBuilder = Host.CreateApplicationBuilder();
        coloredBuilder.Services.AddOpenTelemetry().WithTracing(tracing =>
        {
            tracing.AddSource(ServiceName).AddColoredConsoleExporter();
        });
        _coloredConsoleHost = coloredBuilder.Build();
        _coloredTracerProvider = _coloredConsoleHost.Services.GetRequiredService<TracerProvider>();

        // Disabled tracing (no exporters)
        var disabledBuilder = Host.CreateApplicationBuilder();
        disabledBuilder.Services.AddOpenTelemetry().WithTracing(tracing =>
        {
            tracing.AddSource(ServiceName);
        });
        _disabledTracingHost = disabledBuilder.Build();
        _disabledTracerProvider = _disabledTracingHost.Services.GetRequiredService<TracerProvider>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _activitySource?.Dispose();
        _openTelemetryTracerProvider?.Dispose();
        _coloredTracerProvider?.Dispose();
        _disabledTracerProvider?.Dispose();
        _openTelemetryConsoleHost?.Dispose();
        _coloredConsoleHost?.Dispose();
        _disabledTracingHost?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void OpenTelemetryConsoleExporter()
    {
        for (int i = 0; i < 100; i++)
        {
            using var activity = _activitySource!.StartActivity("BenchmarkActivity");
            activity?.SetTag("benchmark", "test");
        }
    }

    [Benchmark]
    public void ColoredConsoleExporter()
    {
        for (int i = 0; i < 100; i++)
        {
            using var activity = _activitySource!.StartActivity("BenchmarkActivity");
            activity?.SetTag("benchmark", "test");
        }
    }

    [Benchmark]
    public void DisabledTracing()
    {
        for (int i = 0; i < 100; i++)
        {
            using var activity = _activitySource!.StartActivity("BenchmarkActivity");
            activity?.SetTag("benchmark", "test");
        }
    }
}
