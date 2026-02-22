using System.Diagnostics.Metrics;
using System.Text.Json;
using Essential.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Xunit;

namespace Essential.OpenTelemetry.Exporter.OtlpFile.Tests;

[Collection("OtlpFileTests")]
public class OtlpFileMetricExporterTests(ITestContextAccessor tc)
{
    [Fact]
    public void BasicMetricExportTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var meterName = $"TestMeter.{nameof(BasicMetricExportTest)}";
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
        foreach (var (line, index) in mockOutput.Lines.Select((l, i) => (l, i)))
        {
            Console.WriteLine("OUTPUT {0}: {1}", index, line);
        }

        var jsonLine = mockOutput.Lines.Last();

        // Validate it's valid JSON
        var doc = JsonDocument.Parse(jsonLine);
        var root = doc.RootElement;

        // Check structure
        var resourceMetrics = root.GetProperty("resourceMetrics");
        Assert.Equal(1, resourceMetrics.GetArrayLength());

        var scopeMetrics = resourceMetrics[0].GetProperty("scopeMetrics");
        Assert.Equal(1, scopeMetrics.GetArrayLength());

        var metric = scopeMetrics[0]
            .GetProperty("metrics")
            .EnumerateArray()
            .SingleOrDefault(metric => metric.GetProperty("name").GetString() == "test.counter");
        Assert.Equal("test.counter", metric.GetProperty("name").GetString());
    }

    [Fact]
    public void CounterMetricExportTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var meterName = $"TestMeter.{nameof(CounterMetricExportTest)}";
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
            .GetProperty("metrics")
            .EnumerateArray()
            .SingleOrDefault(metric => metric.GetProperty("name").GetString() == "test.counter");

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
        var meterName = $"TestMeter.{nameof(GaugeMetricExportTest)}";
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
            .GetProperty("metrics")
            .EnumerateArray()
            .SingleOrDefault(metric => metric.GetProperty("name").GetString() == "test.gauge");

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
        var meterName = $"TestMeter.{nameof(HistogramMetricExportTest)}";
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
            .GetProperty("metrics")
            .EnumerateArray()
            .SingleOrDefault(metric => metric.GetProperty("name").GetString() == "test.histogram");

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
        var meterName = $"TestMeter.{nameof(MetricWithAttributesTest)}";
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
        var meterName = $"TestMeter.{nameof(MetricWithResourceAttributesTest)}";
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
        var meterName = $"TestMeter.{nameof(MetricTimestampsTest)}";
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
        var meter1 = new Meter($"Meter1.{nameof(MultipleMetersTest)}");
        var meter2 = new Meter($"Meter2.{nameof(MultipleMetersTest)}");
        var counter1 = meter1.CreateCounter<long>("counter1");
        var counter2 = meter2.CreateCounter<long>("counter2");

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter($"Meter1.{nameof(MultipleMetersTest)}")
            .AddMeter($"Meter2.{nameof(MultipleMetersTest)}")
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        // Act
        counter1.Add(10);
        counter2.Add(20);
        meterProvider?.ForceFlush();

        // Assert — one JSONL line per metric (not batched)
        Assert.Equal(2, mockOutput.Lines.Count);

        var scopes = mockOutput
            .Lines.Select(line =>
                JsonDocument
                    .Parse(line)
                    .RootElement.GetProperty("resourceMetrics")[0]
                    .GetProperty("scopeMetrics")[0]
                    .GetProperty("scope")
                    .GetProperty("name")
                    .GetString()
            )
            .ToList();

        Assert.Contains($"Meter1.{nameof(MultipleMetersTest)}", scopes);
        Assert.Contains($"Meter2.{nameof(MultipleMetersTest)}", scopes);
    }

    [Fact]
    public async Task CheckMetricExporterAgainstCollector()
    {
        // Arrange
        var mockOutput = new MockConsole();
        var meterName = "Test.OtlpFile.Meter";
        var meter = new Meter(meterName);
        var counter = meter.CreateCounter<long>("test.counter");

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(
                ResourceBuilder
                    .CreateDefault()
                    .AddService("test-service-name", serviceVersion: "v1.2.3-test")
                    .AddAttributes(
                        new KeyValuePair<string, object>[]
                        {
                            new("host.name", "test-host"),
                            new("deployment.environment.name", "test"),
                        }
                    )
            )
            .AddMeter(meterName)
            .AddOtlpFileExporter(configure =>
            {
                configure.Console = mockOutput;
            })
            .Build();

        var requestCounter = meter.CreateCounter<long>("requests", "count", "Number of requests");
        var requestDuration = meter.CreateHistogram<double>(
            "request.duration",
            "ms",
            "Request duration"
        );
        var currentObservedValue = 1;
        var activeConnections = meter.CreateObservableGauge<int>(
            "active.connections",
            ObserveValue,
            "count",
            "Number of active connections"
        );

        // Act
        int ObserveValue() // values for the gauge above
        {
            return ++currentObservedValue;
        }

        requestCounter.Add(10, new KeyValuePair<string, object?>("endpoint", "/api/counter1"));
        await Task.Delay(100, tc.Current.CancellationToken);
        requestCounter.Add(5, new KeyValuePair<string, object?>("endpoint", "/api/counter2"));
        await Task.Delay(100, tc.Current.CancellationToken);
        requestDuration.Record(
            123.45,
            new KeyValuePair<string, object?>("endpoint", "/api/duration1"),
            new KeyValuePair<string, object?>("result", "success")
        );
        await Task.Delay(100, tc.Current.CancellationToken);
        requestDuration.Record(
            67.89,
            new KeyValuePair<string, object?>("endpoint", "/api/duration1"),
            new KeyValuePair<string, object?>("result", "success")
        );
        await Task.Delay(100, tc.Current.CancellationToken);

        requestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "/api/counter1"));

        meterProvider?.ForceFlush();

        // Assert

        // ---------------------------------------------------------------
        // Validate resource and scope in the first line
        var firstLine = mockOutput.Lines[0];

        Console.WriteLine("OUTPUT 0: " + firstLine);

        // Validate it's valid JSON
        var firstLineDoc = JsonDocument.Parse(firstLine);

        // {
        //   "resourceMetrics": [
        var firstResourceMetrics = firstLineDoc.RootElement.GetProperty("resourceMetrics");
        Assert.Equal(1, firstResourceMetrics.GetArrayLength());

        //     {
        //       "resource": {
        //         "attributes": [
        var firstResourceAttributes = firstResourceMetrics[0]
            .GetProperty("resource")
            .GetProperty("attributes")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("key").ToString());

        //           { "key": "host.name", "value": { "stringValue": "TAR-VALON" } },
        Assert.Equal(
            "test-host",
            firstResourceAttributes["host.name"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //           {
        //             "key": "deployment.environment.name",
        //             "value": { "stringValue": "production" }
        //           },
        //           {
        //             "key": "service.name",
        //             "value": { "stringValue": "Example.OtlpFile" }
        //           },
        Assert.Equal(
            "test-service-name",
            firstResourceAttributes["service.name"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //           {
        //             "key": "service.version",
        //             "value": {
        //               "stringValue": "1.0.0+d40ee8c350d543a91f3bfabebe40e61b30c7508d"
        //             }
        //           },
        //           {
        //             "key": "service.instance.id",
        //             "value": { "stringValue": "6eda2ce7-dabf-43a7-8610-af22295d530f" }
        //           },
        //           {
        //             "key": "telemetry.sdk.name",
        //             "value": { "stringValue": "opentelemetry" }
        //           },
        //           {
        //             "key": "telemetry.sdk.language",
        //             "value": { "stringValue": "dotnet" }
        //           },
        //           {
        //             "key": "telemetry.sdk.version",
        //             "value": { "stringValue": "1.15.0" }
        //           }
        //         ]
        //       },

        //       "scopeMetrics": [
        var firstScopeMetrics = firstResourceMetrics[0].GetProperty("scopeMetrics");
        Assert.Equal(1, firstScopeMetrics.GetArrayLength());

        //         {
        //           "scope": { "name": "Example.OtlpFile" },
        Assert.Equal(
            "Test.OtlpFile.Meter",
            firstScopeMetrics[0].GetProperty("scope").GetProperty("name").GetString()
        );

        // ---------------------------------------------------------------
        // We output one JSONL line per metric (not batched), so each line should only have one;
        // group them by metric name.

        var rootElementsByMetricName = mockOutput
            .Lines.Select(line => JsonDocument.Parse(line).RootElement)
            .GroupBy(element =>
                element
                    .GetProperty("resourceMetrics")[0]
                    .GetProperty("scopeMetrics")[0]
                    .GetProperty("metrics")[0]
                    .GetProperty("name")
                    .GetString()
            )
            .Where(x => !string.IsNullOrEmpty(x.Key))
            .ToDictionary(x => x.Key!, x => x);

        foreach (var group in rootElementsByMetricName)
        {
            Console.WriteLine("METRIC {0}: count={1}", group.Key, group.Value.Count());
        }

        #region Initial collector output
        // ---------------------------------------------------------------
        // Initial line of Collector output
        // We ignore the first batch of outputs, and just check the last batch,
        // that totals are correct.
        // See end of this test for the checks.

        //           "metrics": [
        //             {
        //               "name": "requests",
        //               "description": "Number of requests",
        //               "unit": "count",
        //               "sum": {
        //                 "dataPoints": [
        //                   {
        //                     "attributes": [
        //                       {
        //                         "key": "endpoint",
        //                         "value": { "stringValue": "/api/users" }
        //                       }
        //                     ],
        //                     "startTimeUnixNano": "1771654168794218700",
        //                     "timeUnixNano": "1771654169144782300",
        //                     "asInt": "10"
        //                   },
        //                   {
        //                     "attributes": [
        //                       {
        //                         "key": "endpoint",
        //                         "value": { "stringValue": "/api/orders" }
        //                       }
        //                     ],
        //                     "startTimeUnixNano": "1771654168794218700",
        //                     "timeUnixNano": "1771654169144782300",
        //                     "asInt": "5"
        //                   },
        //                   {
        //                     "attributes": [
        //                       {
        //                         "key": "endpoint",
        //                         "value": { "stringValue": "/api/process" }
        //                       }
        //                     ],
        //                     "startTimeUnixNano": "1771654168794218700",
        //                     "timeUnixNano": "1771654169144782300",
        //                     "asInt": "1"
        //                   }
        //                 ],
        //                 "aggregationTemporality": 2,
        //                 "isMonotonic": true
        //               }
        //             },
        //             {
        //               "name": "request.duration",
        //               "description": "Request duration",
        //               "unit": "ms",
        //               "histogram": {
        //                 "dataPoints": [
        //                   {
        //                     "attributes": [
        //                       {
        //                         "key": "endpoint",
        //                         "value": { "stringValue": "/api/users" }
        //                       }
        //                     ],
        //                     "startTimeUnixNano": "1771654168802047200",
        //                     "timeUnixNano": "1771654169145271000",
        //                     "count": "1",
        //                     "sum": 123.45,
        //                     "bucketCounts": [
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "1",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0"
        //                     ],
        //                     "explicitBounds": [
        //                       0, 5, 10, 25, 50, 75, 100, 250, 500, 750, 1000, 2500,
        //                       5000, 7500, 10000
        //                     ],
        //                     "min": 123.45,
        //                     "max": 123.45
        //                   },
        //                   {
        //                     "attributes": [
        //                       {
        //                         "key": "endpoint",
        //                         "value": { "stringValue": "/api/orders" }
        //                       }
        //                     ],
        //                     "startTimeUnixNano": "1771654168802047200",
        //                     "timeUnixNano": "1771654169145271000",
        //                     "count": "1",
        //                     "sum": 67.89,
        //                     "bucketCounts": [
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "1",

        // ------------------------------------------------------------------------
        // Second line of Collector output

        // {
        //   "resourceMetrics": [
        //     {
        //       "resource": {
        //         "attributes": [
        //           { "key": "host.name", "value": { "stringValue": "TAR-VALON" } },
        //           {
        //             "key": "deployment.environment.name",
        //             "value": { "stringValue": "production" }
        //           },
        //           {
        //             "key": "service.name",
        //             "value": { "stringValue": "Example.OtlpFile" }
        //           },
        //           {
        //             "key": "service.version",
        //             "value": {
        //               "stringValue": "1.0.0+d40ee8c350d543a91f3bfabebe40e61b30c7508d"
        //             }
        //           },
        //           {
        //             "key": "service.instance.id",
        //             "value": { "stringValue": "e68381dc-e6fe-4ced-aa7c-f980d6a50f93" }
        //           },
        //           {
        //             "key": "telemetry.sdk.name",
        //             "value": { "stringValue": "opentelemetry" }
        //           },
        //           {
        //             "key": "telemetry.sdk.language",
        //             "value": { "stringValue": "dotnet" }
        //           },
        //           {
        //             "key": "telemetry.sdk.version",
        //             "value": { "stringValue": "1.15.0" }
        //           }
        //         ]
        //       },
        //       "scopeMetrics": [
        //         {
        //           "scope": { "name": "Example.OtlpFile.Meter" },
        //           "metrics": [
        //             {
        //               "name": "requests",
        //               "description": "Number of requests",
        //               "unit": "count",
        //               "sum": {
        //                 "dataPoints": [
        //                   {
        //                     "attributes": [
        //                       {
        //                         "key": "endpoint",
        //                         "value": { "stringValue": "/api/users" }
        //                       }
        //                     ],
        //                     "startTimeUnixNano": "1771717690455267200",
        //                     "timeUnixNano": "1771717690768636800",
        //                     "asInt": "10"
        //                   },
        //                   {
        //                     "attributes": [
        //                       {
        //                         "key": "endpoint",
        //                         "value": { "stringValue": "/api/orders" }
        //                       }
        //                     ],
        //                     "startTimeUnixNano": "1771717690455267200",
        //                     "timeUnixNano": "1771717690768636800",
        //                     "asInt": "5"
        //                   },
        //                   {
        //                     "attributes": [
        //                       {
        //                         "key": "endpoint",
        //                         "value": { "stringValue": "/api/process" }
        //                       }
        //                     ],
        //                     "startTimeUnixNano": "1771717690455267200",
        //                     "timeUnixNano": "1771717690768636800",
        //                     "asInt": "1"
        //                   }
        //                 ],
        //                 "aggregationTemporality": 2,
        //                 "isMonotonic": true
        //               }
        //             },
        //             {
        //               "name": "request.duration",
        //               "description": "Request duration",
        //               "unit": "ms",
        //               "histogram": {
        //                 "dataPoints": [
        //                   {
        //                     "attributes": [
        //                       {
        //                         "key": "endpoint",
        //                         "value": { "stringValue": "/api/users" }
        //                       }
        //                     ],
        //                     "startTimeUnixNano": "1771717690461528000",
        //                     "timeUnixNano": "1771717690769129700",
        //                     "count": "1",
        //                     "sum": 123.45,
        //                     "bucketCounts": [
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "1",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0"
        //                     ],
        //                     "explicitBounds": [
        //                       0, 5, 10, 25, 50, 75, 100, 250, 500, 750, 1000, 2500,
        //                       5000, 7500, 10000
        //                     ],
        //                     "min": 123.45,
        //                     "max": 123.45
        //                   },
        //                   {
        //                     "attributes": [
        //                       {
        //                         "key": "endpoint",
        //                         "value": { "stringValue": "/api/orders" }
        //                       }
        //                     ],
        //                     "startTimeUnixNano": "1771717690461528000",
        //                     "timeUnixNano": "1771717690769129700",
        //                     "count": "1",
        //                     "sum": 67.89,
        //                     "bucketCounts": [
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "1",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0"
        //                     ],
        //                     "explicitBounds": [
        //                       0, 5, 10, 25, 50, 75, 100, 250, 500, 750, 1000, 2500,
        //                       5000, 7500, 10000
        //                     ],
        //                     "min": 67.89,
        //                     "max": 67.89
        //                   }
        //                 ],
        //                 "aggregationTemporality": 2
        //               }
        //             },
        //             {
        //               "name": "active.connections",
        //               "description": "Number of active connections",
        //               "unit": "count",
        //               "gauge": {
        //                 "dataPoints": [
        //                   {
        //                     "startTimeUnixNano": "1771717690462259100",
        //                     "timeUnixNano": "1771717690769133100",
        //                     "asInt": "28"
        //                   }
        //                 ]
        //               }
        //             }
        //           ]
        //         }
        //       ]
        //     },
        #endregion

        // ------------------------------------------------------------------------
        // Last section of output
        // Check these values, e.g. that totals are correct.

        //     {
        //       "resource": {
        //         "attributes": [
        //           { "key": "host.name", "value": { "stringValue": "TAR-VALON" } },
        //           {
        //             "key": "deployment.environment.name",
        //             "value": { "stringValue": "production" }
        //           },
        //           {
        //             "key": "service.name",
        //             "value": { "stringValue": "Example.OtlpFile" }
        //           },
        //           {
        //             "key": "service.version",
        //             "value": {
        //               "stringValue": "1.0.0+d40ee8c350d543a91f3bfabebe40e61b30c7508d"
        //             }
        //           },
        //           {
        //             "key": "service.instance.id",
        //             "value": { "stringValue": "e68381dc-e6fe-4ced-aa7c-f980d6a50f93" }
        //           },
        //           {
        //             "key": "telemetry.sdk.name",
        //             "value": { "stringValue": "opentelemetry" }
        //           },
        //           {
        //             "key": "telemetry.sdk.language",
        //             "value": { "stringValue": "dotnet" }
        //           },
        //           {
        //             "key": "telemetry.sdk.version",
        //             "value": { "stringValue": "1.15.0" }
        //           }
        //         ]
        //       },
        //       "scopeMetrics": [
        //         {
        //           "scope": { "name": "Example.OtlpFile.Meter" },
        //           "metrics": [
        //             {

        // ------------------------------------------------------------------------
        // Counter metric

        //               "name": "requests",
        var requestMetric = rootElementsByMetricName["requests"]
            .Last()
            .GetProperty("resourceMetrics")[0]
            .GetProperty("scopeMetrics")[0]
            .GetProperty("metrics")[0];

        //               "description": "Number of requests",
        Assert.Equal("Number of requests", requestMetric.GetProperty("description").GetString());

        //               "unit": "count",
        Assert.Equal("count", requestMetric.GetProperty("unit").GetString());

        //               "sum": {
        //                 "dataPoints": [
        var requestDataPoints = requestMetric.GetProperty("sum").GetProperty("dataPoints");

        //                   {
        //                     "attributes": [
        //                       {
        //                         "key": "endpoint",
        //                         "value": { "stringValue": "/api/users" }
        //                       }
        Assert.Equal(
            "endpoint",
            requestDataPoints[0].GetProperty("attributes")[0].GetProperty("key").GetString()
        );

        // Output all the values
        foreach (
            var (dataPoint, index) in requestDataPoints.EnumerateArray().Select((x, i) => (x, i))
        )
        {
            Console.WriteLine(
                "request data point {0} {1}: int={2}",
                index,
                dataPoint
                    .GetProperty("attributes")[0]
                    .GetProperty("value")
                    .GetProperty("stringValue")
                    .GetString(),
                dataPoint.GetProperty("asInt").GetString()
            );
        }

        //                     ],
        //                     "startTimeUnixNano": "1771717690455267200",
        //                     "timeUnixNano": "1771717691164720600",
        //                     "asInt": "10"
        var requestSum = requestDataPoints
            .EnumerateArray()
            .GroupBy(dataPoint =>
                dataPoint
                    .GetProperty("attributes")[0]
                    .GetProperty("value")
                    .GetProperty("stringValue")
                    .GetString()
            )
            .ToDictionary(
                group => group.Key!,
                group =>
                    group.Sum(dataPoint => int.Parse(dataPoint.GetProperty("asInt").GetString()!))
            );
        Assert.Equal(11, requestSum["/api/counter1"]);
        Assert.Equal(5, requestSum["/api/counter2"]);

        //                   },
        //                   {
        //                     "attributes": [
        //                       {
        //                         "key": "endpoint",
        //                         "value": { "stringValue": "/api/orders" }
        //                       }
        //                     ],
        //                     "startTimeUnixNano": "1771717690455267200",
        //                     "timeUnixNano": "1771717691164720600",
        //                     "asInt": "5"
        //                   },
        //                   {
        //                     "attributes": [
        //                       {
        //                         "key": "endpoint",
        //                         "value": { "stringValue": "/api/process" }
        //                       }
        //                     ],
        //                     "startTimeUnixNano": "1771717690455267200",
        //                     "timeUnixNano": "1771717691164720600",
        //                     "asInt": "1"
        //                   }
        //                 ],
        //                 "aggregationTemporality": 2,
        Assert.Equal(
            2,
            requestMetric.GetProperty("sum").GetProperty("aggregationTemporality").GetInt32()
        );

        //                 "isMonotonic": true
        Assert.True(requestMetric.GetProperty("sum").GetProperty("isMonotonic").GetBoolean());

        // ------------------------------------------------------------------------
        // Histogram metric

        //               }
        //             },
        //             {
        //               "name": "request.duration",
        var durationMetric = rootElementsByMetricName["request.duration"]
            .Last()
            .GetProperty("resourceMetrics")[0]
            .GetProperty("scopeMetrics")[0]
            .GetProperty("metrics")[0];

        //               "description": "Request duration",
        Assert.Equal("Request duration", durationMetric.GetProperty("description").GetString());

        //               "unit": "ms",
        Assert.Equal("ms", durationMetric.GetProperty("unit").GetString());

        //               "histogram": {
        //                 "dataPoints": [
        var durationDataPoints = durationMetric.GetProperty("histogram").GetProperty("dataPoints");

        foreach (
            var (dataPoint, index) in durationDataPoints.EnumerateArray().Select((x, i) => (x, i))
        )
        {
            Console.WriteLine(
                "duration data point {0}: count={1} sum={2}",
                index,
                dataPoint.GetProperty("count").GetString(),
                dataPoint.GetProperty("sum").GetDouble()
            );
        }

        //                   {
        //                     "attributes": [
        //                       {
        //                         "key": "endpoint",
        //                         "value": { "stringValue": "/api/users" }
        var histogramAttributes = durationDataPoints[0]
            .GetProperty("attributes")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("key").ToString());

        Assert.Equal(
            "/api/duration1",
            histogramAttributes["endpoint"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        Assert.Equal(
            "success",
            histogramAttributes["result"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //                       }
        //                     ],
        //                     "startTimeUnixNano": "1771717690461528000",
        //                     "timeUnixNano": "1771717691164723900",
        //                     "count": "1",
        //                     "sum": 123.45,
        //                     "bucketCounts": [
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "1",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0"
        //                     ],
        //                     "explicitBounds": [
        //                       0, 5, 10, 25, 50, 75, 100, 250, 500, 750, 1000, 2500,
        //                       5000, 7500, 10000
        //                     ],
        Assert.Equal(15, durationDataPoints[0].GetProperty("explicitBounds").GetArrayLength());

        Assert.Equal(0, durationDataPoints[0].GetProperty("explicitBounds")[0].GetInt32());
        Assert.Equal(5, durationDataPoints[0].GetProperty("explicitBounds")[1].GetInt32());
        Assert.Equal(10000, durationDataPoints[0].GetProperty("explicitBounds")[14].GetInt32());

        //                     "min": 123.45,
        //                     "max": 123.45
        //                   },
        //                   {
        //                     "attributes": [
        //                       {
        //                         "key": "endpoint",
        //                         "value": { "stringValue": "/api/orders" }
        //                       }
        //                     ],
        //                     "startTimeUnixNano": "1771717690461528000",
        //                     "timeUnixNano": "1771717691164723900",
        //                     "count": "1",
        var histogramCount = durationDataPoints
            .EnumerateArray()
            .Sum(dataPoint => int.Parse(dataPoint.GetProperty("count").GetString()!));
        Assert.Equal(2, histogramCount);

        //                     "sum": 67.89,
        var histogramSum = durationDataPoints
            .EnumerateArray()
            .Sum(dataPoint => dataPoint.GetProperty("sum").GetDouble()!);
        Assert.Equal(123.45 + 67.89, histogramSum);

        //                     "bucketCounts": [
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "1",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0",
        //                       "0"
        //                     ],
        var bucket5Count = durationDataPoints
            .EnumerateArray()
            .Sum(dataPoint => int.Parse(dataPoint.GetProperty("bucketCounts")[5].GetString()!));
        Assert.Equal(1, bucket5Count);
        var bucket7Count = durationDataPoints
            .EnumerateArray()
            .Sum(dataPoint => int.Parse(dataPoint.GetProperty("bucketCounts")[7].GetString()!));
        Assert.Equal(1, bucket5Count);

        //                     "explicitBounds": [
        //                       0, 5, 10, 25, 50, 75, 100, 250, 500, 750, 1000, 2500,
        //                       5000, 7500, 10000
        //                     ],
        //                     "min": 67.89,
        //                     "max": 67.89
        var histogramMin = durationDataPoints
            .EnumerateArray()
            .Min(dataPoint => dataPoint.GetProperty("min").GetDouble()!);
        Assert.Equal(67.89, histogramMin);
        var histogramMax = durationDataPoints
            .EnumerateArray()
            .Max(dataPoint => dataPoint.GetProperty("max").GetDouble()!);
        Assert.Equal(123.45, histogramMax);

        //                   }
        //                 ],
        //                 "aggregationTemporality": 2
        Assert.Equal(
            2,
            durationMetric.GetProperty("histogram").GetProperty("aggregationTemporality").GetInt32()
        );

        //               }
        //             },

        // ------------------------------------------------------------------------
        // Observable Gauge metric

        //             {
        //               "name": "active.connections",
        var connectionsMetric = rootElementsByMetricName["active.connections"]
            .Last()
            .GetProperty("resourceMetrics")[0]
            .GetProperty("scopeMetrics")[0]
            .GetProperty("metrics")[0];

        //               "description": "Number of active connections",
        Assert.Equal(
            "Number of active connections",
            connectionsMetric.GetProperty("description").GetString()
        );

        //               "unit": "count",
        Assert.Equal("count", connectionsMetric.GetProperty("unit").GetString());

        //               "gauge": {
        //                 "dataPoints": [
        //                   {
        //                     "startTimeUnixNano": "1771717690462259100",
        //                     "timeUnixNano": "1771717691164724400",
        //                     "asInt": "38"
        Assert.True(
            int.TryParse(
                connectionsMetric
                    .GetProperty("gauge")
                    .GetProperty("dataPoints")[0]
                    .GetProperty("asInt")
                    .GetString(),
                out var gaugeValue
            ),
            "asInt should be a valid integer string"
        );
        Assert.True(gaugeValue > 0, $"Gauge value should be positive, was {gaugeValue}");
        //                   }
        //                 ]
        //               }
        //             }
        //           ]
        //         }
        //       ]
        //     }
        //   ]
        // }
    }
}
