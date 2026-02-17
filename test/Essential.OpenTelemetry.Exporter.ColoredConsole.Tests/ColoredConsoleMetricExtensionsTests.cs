using System.Diagnostics.Metrics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Xunit;

namespace Essential.OpenTelemetry.Exporter.ColoredConsole.Tests;

[Collection("ColoredConsoleTests")]
public class ColoredConsoleMetricExtensionsTests(ITestContextAccessor tc)
{
    [Fact]
    public async Task BasicMetricConfigureOptionsTest()
    {
        // Arrange
        var services = new ServiceCollection();

        services
            .AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter("TestMeter")
                    .AddColoredConsoleExporter(configure => { }, exportIntervalMilliseconds: 5000);
            });

        // Act
        using var sp = services.BuildServiceProvider();

        // Assert - verify the options were configured correctly
        var readerOptions = sp.GetRequiredService<
            IOptionsMonitor<PeriodicExportingMetricReaderOptions>
        >()
            .Get(Options.DefaultName);
        Assert.Equal(5000, readerOptions.ExportIntervalMilliseconds);
    }

    [Fact]
    public async Task BasicMetricSettingsCheckViaReflectionTest()
    {
        // Arrange
        var mockConsole = new MockConsole();

        using var meter = new Meter("TestMeter");

        var meterBuilder = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(r => r.AddService("myservice"))
            .AddMeter(meter.Name)
            .AddColoredConsoleExporter();

        // Act
        using var meterProvider = meterBuilder.Build();

        // Assert - use reflection to get the internal Reader property
        var readerProperty = meterProvider
            .GetType()
            .GetProperty("Reader", BindingFlags.NonPublic | BindingFlags.Instance);
        var reader = readerProperty?.GetValue(meterProvider) as PeriodicExportingMetricReader;

        Assert.NotNull(reader);

        // Access the export interval via reflection (it's also not publicly exposed as a getter)
        var intervalField = typeof(PeriodicExportingMetricReader).GetField(
            "ExportIntervalMilliseconds",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        var interval = (int?)intervalField?.GetValue(reader);
        Assert.Equal(60_000, interval);
    }

    [Fact]
    public async Task ConfiguredMetricSettingsCheckViaReflectionTest()
    {
        // Arrange
        var mockConsole = new MockConsole();

        using var meter = new Meter("TestMeter");

        var meterBuilder = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(r => r.AddService("myservice"))
            .AddMeter(meter.Name)
            .AddColoredConsoleExporter(
                name: null,
                configureExporter: null,
                periodicReaderOptions =>
                {
                    periodicReaderOptions.ExportIntervalMilliseconds = 2000;
                    periodicReaderOptions.ExportTimeoutMilliseconds = 3000;
                }
            );

        // Act
        using var meterProvider = meterBuilder.Build();

        // Assert - use reflection to get the internal Reader property
        var readerProperty = meterProvider
            .GetType()
            .GetProperty("Reader", BindingFlags.NonPublic | BindingFlags.Instance);
        var reader = readerProperty?.GetValue(meterProvider) as PeriodicExportingMetricReader;

        Assert.NotNull(reader);

        // Access the export interval via reflection (it's also not publicly exposed as a getter)
        var intervalField = typeof(PeriodicExportingMetricReader).GetField(
            "ExportIntervalMilliseconds",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        var interval = (int?)intervalField?.GetValue(reader);
        Assert.Equal(2000, interval);

        var timeoutField = typeof(PeriodicExportingMetricReader).GetField(
            "ExportTimeoutMilliseconds",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        var timeout = (int?)timeoutField?.GetValue(reader);
        Assert.Equal(3000, timeout);
    }
}
