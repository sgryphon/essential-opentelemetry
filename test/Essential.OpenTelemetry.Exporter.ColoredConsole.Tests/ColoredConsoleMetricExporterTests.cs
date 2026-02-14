using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Xunit;

namespace Essential.OpenTelemetry.Exporter.ColoredConsole.Tests;

[Collection("ColoredConsoleTests")]
public class ColoredConsoleMetricExporterTests
{
    [Fact]
    public async Task BasicMetricOutputTest()
    {
        // Arrange
        var mockConsole = new MockConsole();

        using var meter = new Meter("TestMeter");

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(r => r.AddService("myservice"))
            .AddMeter(meter.Name)
            .AddColoredConsoleExporter(
                configure =>
                {
                    configure.TimestampFormat = string.Empty;
                    configure.Console = mockConsole;
                },
                exportIntervalMilliseconds: 100
            )
            .Build();

        // Act
        var counter = meter.CreateCounter<int>("counter", "things", "A count of things");

        counter?.Add(10);
        await Task.Delay(150);

        // Assert
        var output = mockConsole.GetOutput();
        Console.WriteLine("Metric: {0}", output);

        Assert.Matches(@"^METRIC \[counter\]", output);

        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        var values0 = lines[0].Split(' ');
        Assert.Contains("0s", values0[2], StringComparison.InvariantCulture);
        Assert.Contains("unit=things", values0[3], StringComparison.InvariantCulture);
        Assert.Contains("sum=10", values0[4], StringComparison.InvariantCulture);
    }

    [Fact]
    public async Task TaggedMetricOutputTest()
    {
        // Arrange
        var mockConsole = new MockConsole();

        using var meter = new Meter("TestMeter");

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(r => r.AddService("myservice"))
            .AddMeter(meter.Name)
            .AddColoredConsoleExporter(
                configure =>
                {
                    configure.TimestampFormat = string.Empty;
                    configure.Console = mockConsole;
                },
                exportIntervalMilliseconds: 100
            )
            .Build();

        // Act
        var counter = meter.CreateCounter<int>("counter", "things", "A count of things");

        counter?.Add(100, new KeyValuePair<string, object?>("tag1", "value1"));
        await Task.Delay(150);

        // Assert
        var output = mockConsole.GetOutput();
        Console.WriteLine("Metric: {0}", output);

        Assert.Matches(@"^METRIC \[counter\]", output);

        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        var values0 = lines[0].Split(' ');
        Assert.Contains("0s", values0[2], StringComparison.InvariantCulture);
        Assert.Contains("unit=things", values0[3], StringComparison.InvariantCulture);
        Assert.Contains("tag1=value1", values0[4], StringComparison.InvariantCulture);
        Assert.Contains("sum=100", values0[5], StringComparison.InvariantCulture);
    }

    [Fact]
    public async Task CumulativeMeterTest()
    {
        // Arrange
        var mockConsole = new MockConsole();

        using var meter = new Meter("TestMeter");

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(r => r.AddService("myservice"))
            .AddMeter(meter.Name)
            .AddColoredConsoleExporter(
                configure =>
                {
                    configure.Console = mockConsole;
                },
                exportIntervalMilliseconds: 1000
            )
            .Build();

        // Act
        var counter = meter.CreateCounter<int>("counter", "things", "A count of things");

        await Task.Delay(600);

        counter?.Add(100, new KeyValuePair<string, object?>("tag1", "value1"));
        await Task.Delay(50);

        counter?.Add(100, new KeyValuePair<string, object?>("tag1", "value1"));
        await Task.Delay(350);

        await Task.Delay(600);
        counter?.Add(100, new KeyValuePair<string, object?>("tag1", "value1"));
        await Task.Delay(50);

        counter?.Add(100, new KeyValuePair<string, object?>("tag1", "value1"));
        await Task.Delay(350);

        await Task.Delay(600);

        // Assert
        var output = mockConsole.GetOutput();
        Console.WriteLine("Metric: {0}", output);

        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        var line200 = lines.FirstOrDefault(x =>
            x.Contains("sum=200", StringComparison.InvariantCulture)
        );
        Assert.NotNull(line200);
        var values200 = line200.Split(' ');
        Assert.Contains("1s", values200[3], StringComparison.InvariantCulture);

        var line400 = lines.FirstOrDefault(x =>
            x.Contains("sum=400", StringComparison.InvariantCulture)
        );
        Assert.NotNull(line400);
        var values400 = line400.Split(' ');
        Assert.Contains("2s", values400[3], StringComparison.InvariantCulture);
    }

    [Fact]
    public async Task HistogramOutputTest()
    {
        // Arrange
        var mockConsole = new MockConsole();

        using var meter = new Meter("TestMeter");

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(r => r.AddService("myservice"))
            .AddMeter(meter.Name)
            .AddColoredConsoleExporter(
                configure =>
                {
                    configure.TimestampFormat = string.Empty;
                    configure.Console = mockConsole;
                },
                exportIntervalMilliseconds: 100
            )
            .Build();

        // Act
        var histogram = meter.CreateHistogram<int>("histogram"); // No Unit

        histogram?.Record(50, new KeyValuePair<string, object?>("tag1", "value1"));
        histogram?.Record(100, new KeyValuePair<string, object?>("tag1", "value1"));
        histogram?.Record(100, new KeyValuePair<string, object?>("tag1", "value1"));
        histogram?.Record(150, new KeyValuePair<string, object?>("tag1", "value1"));

        await Task.Delay(150);

        // Assert
        var output = mockConsole.GetOutput();
        Console.WriteLine("Metric: {0}", output);

        Assert.Matches(@"^METRIC \[histogram\]", output);

        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        var values0 = lines[0].Split(' ');
        Assert.Contains("0s", values0[2], StringComparison.InvariantCulture);
        Assert.Contains("count=4", values0[4], StringComparison.InvariantCulture);
        Assert.Contains("min=50", values0[5], StringComparison.InvariantCulture);
        Assert.Contains("max=150", values0[6], StringComparison.InvariantCulture);
        Assert.Contains("sum=400", values0[7], StringComparison.InvariantCulture);
    }

    [Fact]
    public async Task TimestampOutputTest()
    {
        // Arrange
        var mockConsole = new MockConsole();
        var timestampFormat = "HH:mm:ss ";

        using var meter = new Meter("TestMeter");

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(r => r.AddService("myservice"))
            .AddMeter(meter.Name)
            .AddColoredConsoleExporter(
                configure =>
                {
                    configure.TimestampFormat = timestampFormat;
                    configure.UseUtcTimestamp = false;
                    configure.Console = mockConsole;
                },
                exportIntervalMilliseconds: 100
            )
            .Build();

        // Act
        var counter = meter.CreateCounter<int>("counter", "things", "A count of things");

        counter?.Add(10);
        await Task.Delay(150);

        // Assert
        var output = mockConsole.GetOutput();
        Console.WriteLine("Metric: {0}", output);

        // Should start with a timestamp
        Assert.Matches(@"^\d\d:\d\d:\d\d METRIC", output);
    }

    [Fact]
    public async Task ColorChangeTest()
    {
        // Arrange
        var mockConsole = new MockConsole();

        using var meter = new Meter("TestMeter");

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(r => r.AddService("myservice"))
            .AddMeter(meter.Name)
            .AddColoredConsoleExporter(
                configure =>
                {
                    configure.TimestampFormat = string.Empty;
                    configure.Console = mockConsole;
                },
                exportIntervalMilliseconds: 100
            )
            .Build();

        // Act
        var counter = meter.CreateCounter<int>("counter");

        counter?.Add(10);
        await Task.Delay(150);

        // Assert
        var output = mockConsole.GetOutput();

        // Verify color changes: fg and bg for metric text, then restore both
        Assert.True(mockConsole.ColorChanges.Count >= 4);
        Assert.Equal(("Foreground", ConsoleColor.DarkBlue), mockConsole.ColorChanges[0]); // Metric fg
        Assert.Equal(("Background", ConsoleColor.Black), mockConsole.ColorChanges[1]); // Metric bg
        Assert.Equal(("Foreground", MockConsole.DefaultForeground), mockConsole.ColorChanges[2]); // Restore fg
        Assert.Equal(("Background", MockConsole.DefaultBackground), mockConsole.ColorChanges[3]); // Restore bg
    }
}
