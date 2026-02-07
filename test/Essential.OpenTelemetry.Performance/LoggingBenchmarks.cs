using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

namespace Essential.OpenTelemetry.Performance;

/// <summary>
/// Benchmarks for comparing logging performance across different implementations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class LoggingBenchmarks
{
    private IHost? _standardConsoleHost;
    private IHost? _openTelemetryConsoleHost;
    private IHost? _coloredConsoleHost;
    private IHost? _disabledLoggingHost;
    private ILogger<LoggingBenchmarks>? _standardLogger;
    private ILogger<LoggingBenchmarks>? _openTelemetryLogger;
    private ILogger<LoggingBenchmarks>? _coloredLogger;
    private ILogger<LoggingBenchmarks>? _disabledLogger;

    [GlobalSetup]
    public void Setup()
    {
        // Standard .NET Console Logger
        var standardBuilder = Host.CreateApplicationBuilder();
        standardBuilder.Logging.ClearProviders();
        standardBuilder.Logging.AddConsole();
        standardBuilder.Logging.SetMinimumLevel(LogLevel.Information);
        _standardConsoleHost = standardBuilder.Build();
        _standardLogger = _standardConsoleHost.Services.GetRequiredService<
            ILogger<LoggingBenchmarks>
        >();

        // OpenTelemetry Console Exporter (out of the box)
        var otelBuilder = Host.CreateApplicationBuilder();
        otelBuilder.Logging.ClearProviders();
        otelBuilder
            .Services.AddOpenTelemetry()
            .WithLogging(logging =>
            {
                logging.AddConsoleExporter();
            });
        _openTelemetryConsoleHost = otelBuilder.Build();
        _openTelemetryLogger = _openTelemetryConsoleHost.Services.GetRequiredService<
            ILogger<LoggingBenchmarks>
        >();

        // Essential.OpenTelemetry Colored Console Exporter
        var coloredBuilder = Host.CreateApplicationBuilder();
        coloredBuilder.Logging.ClearProviders();
        coloredBuilder
            .Services.AddOpenTelemetry()
            .WithLogging(logging =>
            {
                logging.AddColoredConsoleExporter();
            });
        _coloredConsoleHost = coloredBuilder.Build();
        _coloredLogger = _coloredConsoleHost.Services.GetRequiredService<
            ILogger<LoggingBenchmarks>
        >();

        // Disabled logging (to measure overhead)
        var disabledBuilder = Host.CreateApplicationBuilder();
        disabledBuilder.Logging.ClearProviders();
        disabledBuilder.Logging.SetMinimumLevel(LogLevel.None);
        _disabledLoggingHost = disabledBuilder.Build();
        _disabledLogger = _disabledLoggingHost.Services.GetRequiredService<
            ILogger<LoggingBenchmarks>
        >();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _standardConsoleHost?.Dispose();
        _openTelemetryConsoleHost?.Dispose();
        _coloredConsoleHost?.Dispose();
        _disabledLoggingHost?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void StandardConsoleLogger()
    {
        for (int i = 0; i < 100; i++)
        {
            _standardLogger!.LogInformation("Benchmark test message {Value}", 42);
        }
    }

    [Benchmark]
    public void OpenTelemetryConsoleExporter()
    {
        for (int i = 0; i < 100; i++)
        {
            _openTelemetryLogger!.LogInformation("Benchmark test message {Value}", 42);
        }
    }

    [Benchmark]
    public void ColoredConsoleExporter()
    {
        for (int i = 0; i < 100; i++)
        {
            _coloredLogger!.LogInformation("Benchmark test message {Value}", 42);
        }
    }

    [Benchmark]
    public void DisabledLogging()
    {
        for (int i = 0; i < 100; i++)
        {
            _disabledLogger!.LogInformation("Benchmark test message {Value}", 42);
        }
    }
}
