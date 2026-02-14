using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Essential.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using Serilog;

namespace Essential.OpenTelemetry.Performance;

/// <summary>
/// Benchmarks for comparing logging performance across different implementations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
public class LoggingBenchmarks : BenchmarkBase
{
    private IHost? _standardConsoleHost;
    private IHost? _openTelemetryConsoleHost;
    private IHost? _coloredConsoleHost;
    private IHost? _serilogConsoleHost;
    private IHost? _disabledLoggingHost;
    private ILogger<LoggingBenchmarks>? _standardLogger;
    private ILogger<LoggingBenchmarks>? _openTelemetryLogger;
    private ILogger<LoggingBenchmarks>? _coloredLogger;
    private ILogger<LoggingBenchmarks>? _serilogLogger;
    private ILogger<LoggingBenchmarks>? _disabledLogger;
    private LoggerProvider? _openTelemetryLoggerProvider;
    private LoggerProvider? _coloredLoggerProvider;

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
        _openTelemetryLoggerProvider =
            _openTelemetryConsoleHost.Services.GetRequiredService<LoggerProvider>();

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
        _coloredLoggerProvider = _coloredConsoleHost.Services.GetRequiredService<LoggerProvider>();

        // Serilog Console Logger
        var serilogBuilder = Host.CreateApplicationBuilder();
        serilogBuilder.Logging.ClearProviders();
        serilogBuilder.Services.AddSerilog(
            new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().CreateLogger()
        );
        _serilogConsoleHost = serilogBuilder.Build();
        _serilogLogger = _serilogConsoleHost.Services.GetRequiredService<
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
        _openTelemetryLoggerProvider?.Dispose();
        _coloredLoggerProvider?.Dispose();
        _standardConsoleHost?.Dispose();
        _openTelemetryConsoleHost?.Dispose();
        _coloredConsoleHost?.Dispose();
        _serilogConsoleHost?.Dispose();
        _disabledLoggingHost?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void StandardConsoleLogger()
    {
        for (int i = 0; i < Configuration.LoggingIterations; i++)
        {
            _standardLogger!.LogInformation("Benchmark test message {Value}", 42);
        }
    }

    [Benchmark]
    public void OpenTelemetryConsoleExporter()
    {
        for (int i = 0; i < Configuration.LoggingIterations; i++)
        {
            _openTelemetryLogger!.LogInformation("Benchmark test message {Value}", 42);
        }
        _openTelemetryLoggerProvider!.ForceFlush();
    }

    [Benchmark]
    public void ColoredConsoleExporter()
    {
        for (int i = 0; i < Configuration.LoggingIterations; i++)
        {
            _coloredLogger!.LogInformation("Benchmark test message {Value}", 42);
        }
        _coloredLoggerProvider!.ForceFlush();
    }

    [Benchmark]
    public void SerilogConsoleLogger()
    {
        for (int i = 0; i < Configuration.LoggingIterations; i++)
        {
            _serilogLogger!.LogInformation("Benchmark test message {Value}", 42);
        }
    }

    [Benchmark]
    public void DisabledLogging()
    {
        for (int i = 0; i < Configuration.LoggingIterations; i++)
        {
            _disabledLogger!.LogInformation("Benchmark test message {Value}", 42);
        }
    }
}
