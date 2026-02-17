using System.Diagnostics.Metrics;
using System.Text.Json;
using Essential.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Xunit;

namespace Essential.OpenTelemetry.Exporter.OtlpFile.Tests;

[Collection("OtlpFileTests")]
public class OtlpFileMetricExporterTests
{
    [Fact]
    public void BasicMetricExportTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var meterName = "TestMeter";
        var meter = new Meter(meterName);
        var counter = meter.CreateCounter<long>("test.counter");

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(meterName)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        counter.Add(10);
        meterProvider?.ForceFlush();

        // Assert
        Assert.Single(mockOutput.Lines);
        var jsonLine = mockOutput.Lines[0];

        Console.WriteLine("OUTPUT: " + jsonLine);

        // Validate it's valid JSON
        var doc = JsonDocument.Parse(jsonLine);
        var root = doc.RootElement;

        // Check structure
        var resourceMetrics = root.GetProperty("resourceMetrics");
        Assert.Equal(1, resourceMetrics.GetArrayLength());

        var scopeMetrics = resourceMetrics[0].GetProperty("scopeMetrics");
        Assert.Equal(1, scopeMetrics.GetArrayLength());

        var metrics = scopeMetrics[0].GetProperty("metrics");
        var metric = metrics[0];
        Assert.Equal("test.counter", metric.GetProperty("name").GetString());
    }

    [Fact]
    public void CounterMetricExportTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var meterName = "TestMeter";
        var meter = new Meter(meterName);
        var counter = meter.CreateCounter<long>("test.counter", "count", "Test counter metric");

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(meterName)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        counter.Add(10, new KeyValuePair<string, object?>("tag1", "value1"));
        counter.Add(5, new KeyValuePair<string, object?>("tag1", "value2"));
        meterProvider?.ForceFlush();

        // Assert
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var metric = doc
            .RootElement.GetProperty("resourceMetrics")[0]
            .GetProperty("scopeMetrics")[0]
            .GetProperty("metrics")[0];

        Assert.Equal("test.counter", metric.GetProperty("name").GetString());
        Assert.Equal("Test counter metric", metric.GetProperty("description").GetString());
        Assert.Equal("count", metric.GetProperty("unit").GetString());

        // Check sum data
        Assert.True(metric.TryGetProperty("sum", out var sum));
        var dataPoints = sum.GetProperty("dataPoints");
        Assert.Equal(2, dataPoints.GetArrayLength());

        // Verify aggregation temporality is set
        Assert.Equal(2, sum.GetProperty("aggregationTemporality").GetInt32()); // Cumulative = 2

        // Verify monotonic flag
        Assert.True(sum.GetProperty("isMonotonic").GetBoolean());
    }

    [Fact]
    public void GaugeMetricExportTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var meterName = "TestMeter";
        var meter = new Meter(meterName);
        var gauge = meter.CreateObservableGauge<double>(
            "test.gauge",
            () => 42.5,
            "bytes",
            "Test gauge metric"
        );

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(meterName)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        meterProvider?.ForceFlush();

        // Assert
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var metric = doc
            .RootElement.GetProperty("resourceMetrics")[0]
            .GetProperty("scopeMetrics")[0]
            .GetProperty("metrics")[0];

        Assert.Equal("test.gauge", metric.GetProperty("name").GetString());
        Assert.Equal("Test gauge metric", metric.GetProperty("description").GetString());
        Assert.Equal("bytes", metric.GetProperty("unit").GetString());

        // Check gauge data
        Assert.True(metric.TryGetProperty("gauge", out var gaugeData));
        var dataPoints = gaugeData.GetProperty("dataPoints");
        Assert.Equal(1, dataPoints.GetArrayLength());

        var dataPoint = dataPoints[0];
        Assert.Equal(42.5, dataPoint.GetProperty("asDouble").GetDouble());
    }

    [Fact]
    public void HistogramMetricExportTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var meterName = "TestMeter";
        var meter = new Meter(meterName);
        var histogram = meter.CreateHistogram<double>(
            "test.histogram",
            "ms",
            "Test histogram metric"
        );

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(meterName)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        histogram.Record(10.5);
        histogram.Record(20.3);
        histogram.Record(15.7);
        meterProvider?.ForceFlush();

        // Assert
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var metric = doc
            .RootElement.GetProperty("resourceMetrics")[0]
            .GetProperty("scopeMetrics")[0]
            .GetProperty("metrics")[0];

        Assert.Equal("test.histogram", metric.GetProperty("name").GetString());
        Assert.Equal("Test histogram metric", metric.GetProperty("description").GetString());
        Assert.Equal("ms", metric.GetProperty("unit").GetString());

        // Check histogram data
        Assert.True(metric.TryGetProperty("histogram", out var histogramData));
        var dataPoints = histogramData.GetProperty("dataPoints");
        Assert.Equal(1, dataPoints.GetArrayLength());

        var dataPoint = dataPoints[0];

        // Verify count
        Assert.Equal("3", dataPoint.GetProperty("count").GetString());

        // Verify sum
        Assert.True(
            dataPoint.TryGetProperty("sum", out var sumElement)
                && Math.Abs(sumElement.GetDouble() - 46.5) < 0.01
        );

        // Verify explicit bounds exist
        Assert.True(dataPoint.TryGetProperty("explicitBounds", out _));

        // Verify bucket counts exist
        Assert.True(dataPoint.TryGetProperty("bucketCounts", out _));
    }

    [Fact]
    public void MetricWithAttributesTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var meterName = "TestMeter";
        var meter = new Meter(meterName);
        var counter = meter.CreateCounter<long>("test.counter");

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(meterName)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        counter.Add(
            10,
            new KeyValuePair<string, object?>("string.tag", "value1"),
            new KeyValuePair<string, object?>("int.tag", 42),
            new KeyValuePair<string, object?>("bool.tag", true)
        );
        meterProvider?.ForceFlush();

        // Assert
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var dataPoint = doc
            .RootElement.GetProperty("resourceMetrics")[0]
            .GetProperty("scopeMetrics")[0]
            .GetProperty("metrics")[0]
            .GetProperty("sum")
            .GetProperty("dataPoints")[0];

        var attributes = dataPoint
            .GetProperty("attributes")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("key").GetString() ?? string.Empty);

        Assert.Equal(3, attributes.Count);

        Assert.True(attributes.ContainsKey("string.tag"));
        Assert.Equal(
            "value1",
            attributes["string.tag"].GetProperty("value").GetProperty("stringValue").GetString()
        );

        Assert.True(attributes.ContainsKey("int.tag"));
        Assert.Equal(
            "42",
            attributes["int.tag"].GetProperty("value").GetProperty("intValue").GetString()
        );

        Assert.True(attributes.ContainsKey("bool.tag"));
        Assert.True(
            attributes["bool.tag"].GetProperty("value").GetProperty("boolValue").GetBoolean()
        );
    }

    [Fact]
    public void MetricWithResourceAttributesTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var meterName = "TestMeter";
        var meter = new Meter(meterName);
        var counter = meter.CreateCounter<long>("test.counter");

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault().AddService("test-service", serviceVersion: "1.0.0")
            )
            .AddMeter(meterName)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        counter.Add(10);
        meterProvider?.ForceFlush();

        // Assert
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var resource = doc.RootElement.GetProperty("resourceMetrics")[0].GetProperty("resource");

        var attributes = resource
            .GetProperty("attributes")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("key").GetString() ?? string.Empty);

        Assert.True(attributes.ContainsKey("service.name"));
        Assert.Equal(
            "test-service",
            attributes["service.name"].GetProperty("value").GetProperty("stringValue").GetString()
        );

        Assert.True(attributes.ContainsKey("service.version"));
        Assert.Equal(
            "1.0.0",
            attributes["service.version"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );
    }

    [Fact]
    public void MetricTimestampsTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var meterName = "TestMeter";
        var meter = new Meter(meterName);
        var counter = meter.CreateCounter<long>("test.counter");

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter(meterName)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        counter.Add(10);
        meterProvider?.ForceFlush();

        // Assert
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var dataPoint = doc
            .RootElement.GetProperty("resourceMetrics")[0]
            .GetProperty("scopeMetrics")[0]
            .GetProperty("metrics")[0]
            .GetProperty("sum")
            .GetProperty("dataPoints")[0];

        // Verify timestamps are present and are strings (uint64 in JSON)
        Assert.True(dataPoint.TryGetProperty("startTimeUnixNano", out var startTime));
        Assert.Equal(JsonValueKind.String, startTime.ValueKind);
        Assert.NotEmpty(startTime.GetString() ?? string.Empty);

        Assert.True(dataPoint.TryGetProperty("timeUnixNano", out var endTime));
        Assert.Equal(JsonValueKind.String, endTime.ValueKind);
        Assert.NotEmpty(endTime.GetString() ?? string.Empty);
    }

    [Fact]
    public void MultipleMetersTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var meter1 = new Meter("Meter1");
        var meter2 = new Meter("Meter2");
        var counter1 = meter1.CreateCounter<long>("counter1");
        var counter2 = meter2.CreateCounter<long>("counter2");

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("Meter1")
            .AddMeter("Meter2")
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        counter1.Add(10);
        counter2.Add(20);
        meterProvider?.ForceFlush();

        // Assert
        Assert.Single(mockOutput.Lines);
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);

        var scopeMetrics = doc
            .RootElement.GetProperty("resourceMetrics")[0]
            .GetProperty("scopeMetrics");
        Assert.Equal(2, scopeMetrics.GetArrayLength());

        var scope1 = scopeMetrics[0].GetProperty("scope").GetProperty("name").GetString();
        var scope2 = scopeMetrics[1].GetProperty("scope").GetProperty("name").GetString();

        Assert.Contains("Meter1", new[] { scope1, scope2 });
        Assert.Contains("Meter2", new[] { scope1, scope2 });
    }
}
