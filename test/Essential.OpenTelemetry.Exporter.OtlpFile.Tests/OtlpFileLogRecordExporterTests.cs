using System.Diagnostics;
using System.Text.Json;
using Essential.OpenTelemetry;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using Xunit;

namespace Essential.OpenTelemetry.Exporter.OtlpFile.Tests;

[Collection("OtlpFileTests")]
public class OtlpFileLogRecordExporterTests
{
    [Fact]
    public void BasicLogExportTest()
    {
        // Arrange
        var mockOutput = new MockOutput();
        using var loggerFactory = LoggerFactory.Create(logging =>
            logging.AddOpenTelemetry(options =>
            {
                options.AddOtlpFileExporter(configure =>
                {
                    configure.Output = mockOutput;
                });
            })
        );

        // Act
        var logger = loggerFactory.CreateLogger<OtlpFileLogRecordExporterTests>();
#pragma warning disable CA1848
        logger.LogInformation("Test log message");
#pragma warning restore CA1848

        // Assert
        Assert.Single(mockOutput.Lines);
        var jsonLine = mockOutput.Lines[0];

        // Validate it's valid JSON
        var doc = JsonDocument.Parse(jsonLine);
        var root = doc.RootElement;

        // Check structure
        var resourceLogs = root.GetProperty("resourceLogs");
        Assert.Equal(1, resourceLogs.GetArrayLength());

        var scopeLogs = resourceLogs[0].GetProperty("scopeLogs");
        Assert.Equal(1, scopeLogs.GetArrayLength());

        var logRecords = scopeLogs[0].GetProperty("logRecords");
        Assert.Equal(1, logRecords.GetArrayLength());

        var logRecord = logRecords[0];
        Assert.Equal(
            "Test log message",
            logRecord.GetProperty("body").GetProperty("stringValue").GetString()
        );
    }

    [Fact]
    public void LogWithSeverityTest()
    {
        // Arrange
        var mockOutput = new MockOutput();
        using var loggerFactory = LoggerFactory.Create(logging =>
            logging.AddOpenTelemetry(options =>
            {
                options.AddOtlpFileExporter(configure =>
                {
                    configure.Output = mockOutput;
                });
            })
        );

        // Act
        var logger = loggerFactory.CreateLogger<OtlpFileLogRecordExporterTests>();
#pragma warning disable CA1848
        logger.LogWarning("Warning message");
#pragma warning restore CA1848

        // Assert
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var logRecord = doc
            .RootElement.GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs")[0]
            .GetProperty("logRecords")[0];

        Assert.Equal(13, logRecord.GetProperty("severityNumber").GetInt32());
        Assert.Equal("Warn", logRecord.GetProperty("severityText").GetString());
    }

    [Fact]
    public void LogWithEventIdTest()
    {
        // Arrange
        var mockOutput = new MockOutput();
        using var loggerFactory = LoggerFactory.Create(logging =>
            logging.AddOpenTelemetry(options =>
            {
                options.AddOtlpFileExporter(configure =>
                {
                    configure.Output = mockOutput;
                });
            })
        );

        // Act
        var logger = loggerFactory.CreateLogger<OtlpFileLogRecordExporterTests>();
#pragma warning disable CA1848
        logger.Log(LogLevel.Information, new EventId(42, "TestEvent"), "Event message");
#pragma warning restore CA1848

        // Assert
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var logRecord = doc
            .RootElement.GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs")[0]
            .GetProperty("logRecords")[0];

        Console.WriteLine("OUTPUT: {0}", mockOutput.Lines[0]);

        // Event Name
        Assert.Equal("TestEvent", logRecord.GetProperty("eventName").GetString());

        // Event ID should be in attributes
        var attributes = logRecord
            .GetProperty("attributes")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("key").ToString());
        attributes.TryGetValue("event.id", out var eventIdAttribute);
        // intValue is int64 in protobuf, serialized as string in JSON per OTLP spec
        Assert.Equal(JsonValueKind.Object, eventIdAttribute.ValueKind);
        Assert.Equal(
            "42",
            eventIdAttribute.GetProperty("value").GetProperty("intValue").GetString()
        );
    }

    [Fact]
    public void LogWithStructuredDataTest()
    {
        // Arrange
        var mockOutput = new MockOutput();
        using var loggerFactory = LoggerFactory.Create(logging =>
            logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.AddOtlpFileExporter(configure =>
                {
                    configure.Output = mockOutput;
                });
            })
        );

        // Act
        var logger = loggerFactory.CreateLogger<OtlpFileLogRecordExporterTests>();
        var userName = "Alice";
        var userId = 123;
#pragma warning disable CA1848
        logger.LogInformation("User {UserName} with ID {UserId} logged in", userName, userId);
#pragma warning restore CA1848

        // Assert
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var logRecord = doc
            .RootElement.GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs")[0]
            .GetProperty("logRecords")[0];

        // Check body contains the original template (not formatted message)
        // per OTLP format, parameter values are in attributes
        var body = logRecord.GetProperty("body").GetProperty("stringValue").GetString();
        Assert.Equal("User {UserName} with ID {UserId} logged in", body);

        // Check attributes contain the structured data
        var attributes = logRecord
            .GetProperty("attributes")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("key").ToString());

        attributes.TryGetValue("UserName", out var userNameAttribute);
        Assert.Equal(
            "Alice",
            userNameAttribute.GetProperty("value").GetProperty("stringValue").GetString()
        );

        attributes.TryGetValue("UserId", out var userIdAttribute);
        Assert.Equal(
            "123",
            userIdAttribute.GetProperty("value").GetProperty("intValue").GetString()
        );
    }

    [Fact]
    public void MultipleScopesTest()
    {
        // Arrange
        var mockOutput = new MockOutput();
        using var loggerFactory = LoggerFactory.Create(logging =>
            logging.AddOpenTelemetry(options =>
            {
                options.AddOtlpFileExporter(configure =>
                {
                    configure.Output = mockOutput;
                });
            })
        );

        // Act
        var logger1 = loggerFactory.CreateLogger("Scope1");
        var logger2 = loggerFactory.CreateLogger("Scope2");

#pragma warning disable CA1848
        logger1.LogInformation("Message from scope 1");
        logger2.LogInformation("Message from scope 2");
#pragma warning restore CA1848

        // Assert - with SimpleLogRecordExportProcessor, each log is a separate line
        Assert.Equal(2, mockOutput.Lines.Count);

        // Check first line
        var doc1 = JsonDocument.Parse(mockOutput.Lines[0]);
        var scopeLogs1 = doc1.RootElement.GetProperty("resourceLogs")[0].GetProperty("scopeLogs");
        Assert.Single(scopeLogs1.EnumerateArray());
        var scope1Name = scopeLogs1[0].GetProperty("scope").GetProperty("name").GetString();
        Assert.Equal("Scope1", scope1Name);

        // Check second line
        var doc2 = JsonDocument.Parse(mockOutput.Lines[1]);
        var scopeLogs2 = doc2.RootElement.GetProperty("resourceLogs")[0].GetProperty("scopeLogs");
        Assert.Single(scopeLogs2.EnumerateArray());
        var scope2Name = scopeLogs2[0].GetProperty("scope").GetProperty("name").GetString();
        Assert.Equal("Scope2", scope2Name);
    }

    [Fact]
    public void JsonlFormatTest()
    {
        // Arrange
        var mockOutput = new MockOutput();
        using var loggerFactory = LoggerFactory.Create(logging =>
            logging.AddOpenTelemetry(options =>
            {
                options.AddOtlpFileExporter(configure =>
                {
                    configure.Output = mockOutput;
                });
            })
        );

        // Act
        var logger = loggerFactory.CreateLogger<OtlpFileLogRecordExporterTests>();
#pragma warning disable CA1848
        logger.LogInformation("Message 1");
        logger.LogInformation("Message 2");
#pragma warning restore CA1848

        // Assert - Each log is exported separately with SimpleLogRecordExportProcessor
        Assert.Equal(2, mockOutput.Lines.Count);

        // Each line should be valid JSON without internal newlines
        foreach (var jsonLine in mockOutput.Lines)
        {
            Assert.DoesNotContain('\n', jsonLine);
            Assert.DoesNotContain('\r', jsonLine);

            // Should be valid JSON
            var doc = JsonDocument.Parse(jsonLine);
            Assert.NotNull(doc);
        }
    }

    [Theory]
    [InlineData(LogLevel.Trace, "Trace")]
    [InlineData(LogLevel.Debug, "Debug")]
    [InlineData(LogLevel.Information, "Info")]
    [InlineData(LogLevel.Warning, "Warn")]
    [InlineData(LogLevel.Error, "Error")]
    [InlineData(LogLevel.Critical, "Fatal")]
    public void SeverityLevelMappingTest(LogLevel logLevel, string expectedSeverityText)
    {
        // Arrange
        var mockOutput = new MockOutput();
        using var loggerFactory = LoggerFactory.Create(logging =>
            logging
                .SetMinimumLevel(LogLevel.Trace)
                .AddOpenTelemetry(options =>
                {
                    options.AddOtlpFileExporter(configure =>
                    {
                        configure.Output = mockOutput;
                    });
                })
        );

        // Act
        var logger = loggerFactory.CreateLogger<OtlpFileLogRecordExporterTests>();
#pragma warning disable CA1848
#pragma warning disable CA2254
        logger.Log(logLevel, "Test message");
#pragma warning restore CA2254
#pragma warning restore CA1848

        // Assert
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var logRecord = doc
            .RootElement.GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs")[0]
            .GetProperty("logRecords")[0];

        Assert.True(logRecord.TryGetProperty("severityText", out var severityText));
        Assert.Equal(expectedSeverityText, severityText.GetString());
    }

    [Fact]
    public void CheckExportAgainstCollector()
    {
        // Arrange
        var mockOutput = new MockOutput();
        using var loggerFactory = LoggerFactory.Create(logging =>
            logging.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(
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
                );
                options.AddOtlpFileExporter(configure =>
                {
                    configure.Output = mockOutput;
                });
            })
        );

        // Act - create an Activity to provide trace context
        using var activitySource = new ActivitySource("TestSource");
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(activityListener);

        var logger = loggerFactory.CreateLogger<OtlpFileLogRecordExporterTests>();

        using var activity = activitySource.StartActivity("TestOperation");

        try
        {
            throw new InvalidOperationException("Simulated exception for testing");
        }
        catch (Exception ex)
        {
            logger.OperationError(ex, "ORD-789", 150.00m);
        }

        // Assert
        Assert.Single(mockOutput.Lines);
        var jsonLine = mockOutput.Lines[0];

        Console.WriteLine("OUTPUT: " + jsonLine);

        // Validate it's valid JSON
        var doc = JsonDocument.Parse(jsonLine);
        var root = doc.RootElement;

        // Check against sample output from OpenTelemetry Collector
        // This was generated by configuring the Collector with an OTLP receiver and File exporter,
        // sending using the OtlpExporter, and then checking the file output

        // {
        //   "resourceLogs": [
        var resourceLogs = root.GetProperty("resourceLogs");
        Assert.Equal(1, resourceLogs.GetArrayLength());

        //     {
        //       "resource": {
        //         "attributes": [
        var resourceAttributes = resourceLogs[0]
            .GetProperty("resource")
            .GetProperty("attributes")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("key").ToString());

        //           { "key": "host.name", "value": { "stringValue": "TAR-VALON" } },
        Assert.Equal(
            "test-host",
            resourceAttributes["host.name"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //           {
        //             "key": "deployment.environment.name",
        //             "value": { "stringValue": "production" }
        //           },
        Assert.Equal(
            "test",
            resourceAttributes["deployment.environment.name"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //           {
        //             "key": "service.name",
        //             "value": { "stringValue": "Example.OtlpFile" }
        //           },
        Assert.Equal(
            "test-service-name",
            resourceAttributes["service.name"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //           {
        //             "key": "service.version",
        //             "value": {
        //               "stringValue": "1.0.0+a39edcd73d16166f5105ff3e08aae50d9a30f736"
        //             }
        //           },
        Assert.Equal(
            "v1.2.3-test",
            resourceAttributes["service.version"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //           {
        //             "key": "service.instance.id",
        //             "value": { "stringValue": "38700bd8-eff0-4695-8d6e-6288aea65d46" }
        //           },
        //           {
        //             "key": "telemetry.sdk.name",
        //             "value": { "stringValue": "opentelemetry" }
        //           },
        Assert.Equal(
            "opentelemetry",
            resourceAttributes["telemetry.sdk.name"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //           {
        //             "key": "telemetry.sdk.language",
        //             "value": { "stringValue": "dotnet" }
        //           },
        Assert.Equal(
            "dotnet",
            resourceAttributes["telemetry.sdk.language"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //           {
        //             "key": "telemetry.sdk.version",
        //             "value": { "stringValue": "1.15.0" }
        //           }
        //         ]
        //       },

        //       "scopeLogs": [
        var scopeLogs = resourceLogs[0].GetProperty("scopeLogs");
        Assert.Equal(1, scopeLogs.GetArrayLength());

        //         {
        //           "scope": { "name": "Program" },
        Assert.EndsWith(
            "OtlpFileLogRecordExporterTests",
            scopeLogs[0].GetProperty("scope").GetProperty("name").GetString()
        );

        //           "logRecords": [
        var logRecords = scopeLogs[0].GetProperty("logRecords");
        Assert.Equal(1, logRecords.GetArrayLength());

        var logRecord = logRecords[0];

        //             {
        //               "timeUnixNano": "1771125740795465800",
        //               "observedTimeUnixNano": "1771125740795465800",
        //               "traceId": "hex-encoded-trace-id",
        //               Per OTLP JSON Protobuf Encoding, trace_id is lowercase hex, 32 chars (16 bytes)
        Assert.True(
            logRecord.TryGetProperty("traceId", out var traceIdElement),
            "traceId should be present when Activity is active"
        );
        Assert.Matches("^[0-9a-f]{32}$", traceIdElement.GetString());

        //               "spanId": "hex-encoded-span-id",
        //               Per OTLP JSON Protobuf Encoding, span_id is lowercase hex, 16 chars (8 bytes)
        Assert.True(
            logRecord.TryGetProperty("spanId", out var spanIdElement),
            "spanId should be present when Activity is active"
        );
        Assert.Matches("^[0-9a-f]{16}$", spanIdElement.GetString());

        //               "flags": 1,
        Assert.Equal(1, logRecord.GetProperty("flags").GetInt32());

        //               "severityNumber": 17,
        Assert.Equal(17, logRecord.GetProperty("severityNumber").GetInt32());

        //               "severityText": "Error",
        Assert.Equal("Error", logRecord.GetProperty("severityText").GetString());

        //               "body": {
        //                 "stringValue": "Exception caught while processing order {OrderId} for {Amount:C}"
        //               },
        Assert.Equal(
            "Exception caught while processing order {OrderId} for {Amount:C}",
            logRecord.GetProperty("body").GetProperty("stringValue").GetString()
        );

        //               "attributes": [
        var attributes = logRecord
            .GetProperty("attributes")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("key").ToString());

        //                 {
        //                   "key": "exception.type",
        //                   "value": { "stringValue": "InvalidOperationException" }
        //                 },
        Assert.Equal(
            "System.InvalidOperationException",
            attributes["exception.type"].GetProperty("value").GetProperty("stringValue").GetString()
        );

        //                 {
        //                   "key": "exception.message",
        //                   "value": { "stringValue": "Simulated exception for testing" }
        //                 },
        Assert.Equal(
            "Simulated exception for testing",
            attributes["exception.message"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //                 {
        //                   "key": "exception.stacktrace",
        //                   "value": {
        //                     "stringValue": "System.InvalidOperationException: Simulated exception for testing\r\n   at Program.<Main>$(String[] args) in C:\\Code\\essential-opentelemetry\\examples\\OtlpFileCollector\\Example.OtlpFile\\Program.cs:line 87"
        //                   }
        //                 },
        Assert.StartsWith(
            "System.InvalidOperationException: Simulated exception for testing",
            attributes["exception.stacktrace"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
        );

        //                 { "key": "OrderId", "value": { "stringValue": "ORD-789" } },
        Assert.Equal(
            "ORD-789",
            attributes["OrderId"].GetProperty("value").GetProperty("stringValue").GetString()
        );

        //                 { "key": "Amount", "value": { "stringValue": "150.00" } }
        Assert.Equal(
            "150.00",
            attributes["Amount"].GetProperty("value").GetProperty("stringValue").GetString()
        );

        //               ],
        //               "eventName": "OperationError"
        Assert.Equal("OperationError", logRecord.GetProperty("eventName").GetString());

        //             }
        //           ]
    }
}
