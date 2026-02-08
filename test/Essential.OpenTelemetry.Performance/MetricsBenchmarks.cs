using System.Diagnostics.Metrics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;

namespace Essential.OpenTelemetry.Performance;

/// <summary>
/// Benchmarks for comparing metrics performance across different implementations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
public class MetricsBenchmarks : BenchmarkBase
{
    private const string ServiceName = "BenchmarkService";
    private Meter? _meter;
    private Counter<int>[]? _counters;
    private IHost? _openTelemetryConsoleHost;
    private IHost? _coloredConsoleHost;
    private IHost? _disabledMetricsHost;
    private MeterProvider? _openTelemetryMeterProvider;
    private MeterProvider? _coloredMeterProvider;
    private MeterProvider? _disabledMeterProvider;

    [GlobalSetup]
    public void Setup()
    {
        _meter = new Meter(ServiceName);

        // Create multiple counters
        _counters = new Counter<int>[Configuration.MetricsCounterCount];
        for (int index = 0; index < Configuration.MetricsCounterCount; index++)
        {
            _counters[index] = _meter.CreateCounter<int>(
                $"benchmark_counter_{index}",
                "count",
                $"Benchmark counter {index}"
            );
        }

        // OpenTelemetry Console Exporter (out of the box)
        var otelBuilder = Host.CreateApplicationBuilder();
        otelBuilder
            .Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(ServiceName)
                    .AddConsoleExporter(
                        (configExporter, configMetrics) =>
                        {
                            configMetrics
                                .PeriodicExportingMetricReaderOptions
                                .ExportIntervalMilliseconds =
                                Configuration.MetricsExportIntervalMilliseconds;
                        }
                    );
            });
        _openTelemetryConsoleHost = otelBuilder.Build();
        _openTelemetryMeterProvider =
            _openTelemetryConsoleHost.Services.GetRequiredService<MeterProvider>();

        // Essential.OpenTelemetry Colored Console Exporter
        var coloredBuilder = Host.CreateApplicationBuilder();
        coloredBuilder
            .Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(ServiceName)
                    .AddColoredConsoleExporter(
                        configure => { },
                        Configuration.MetricsExportIntervalMilliseconds
                    );
            });
        _coloredConsoleHost = coloredBuilder.Build();
        _coloredMeterProvider = _coloredConsoleHost.Services.GetRequiredService<MeterProvider>();

        // Disabled metrics (no exporters)
        var disabledBuilder = Host.CreateApplicationBuilder();
        disabledBuilder
            .Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(ServiceName);
            });
        _disabledMetricsHost = disabledBuilder.Build();
        _disabledMeterProvider = _disabledMetricsHost.Services.GetRequiredService<MeterProvider>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _meter?.Dispose();
        _openTelemetryMeterProvider?.Dispose();
        _coloredMeterProvider?.Dispose();
        _disabledMeterProvider?.Dispose();
        _openTelemetryConsoleHost?.Dispose();
        _coloredConsoleHost?.Dispose();
        _disabledMetricsHost?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void OpenTelemetryConsoleExporter()
    {
        // Increment each counter multiple times
        for (int increment = 0; increment < Configuration.MetricsIncrementsPerCounter; increment++)
        {
            for (int index = 0; index < Configuration.MetricsCounterCount; index++)
            {
                _counters![index].Add(1);
            }
        }
        _openTelemetryMeterProvider!.ForceFlush();
    }

    [Benchmark]
    public void ColoredConsoleExporter()
    {
        // Increment each counter multiple times
        for (int i = 0; i < Configuration.MetricsCounterCount; i++)
        {
            for (int j = 0; j < Configuration.MetricsIncrementsPerCounter; j++)
            {
                _counters![i].Add(1);
            }
        }
        _coloredMeterProvider!.ForceFlush();
    }

    [Benchmark]
    public void DisabledMetrics()
    {
        // Increment each counter multiple times
        for (int i = 0; i < Configuration.MetricsCounterCount; i++)
        {
            for (int j = 0; j < Configuration.MetricsIncrementsPerCounter; j++)
            {
                _counters![i].Add(1);
            }
        }
        _disabledMeterProvider!.ForceFlush();
    }
}
