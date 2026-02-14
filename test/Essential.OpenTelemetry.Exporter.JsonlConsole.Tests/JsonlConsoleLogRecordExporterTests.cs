using System.Text.Json;
using Essential.OpenTelemetry;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;
using OpenTelemetry.Logs;
using Xunit;

namespace Essential.OpenTelemetry.Exporter.JsonlConsole.Tests;

[Collection("JsonlConsoleTests")]
public class JsonlConsoleLogRecordExporterTests
{
    [Fact]
    public void BasicLogExportTest()
    {
        // Arrange
        var mockOutput = new MockOutput();
        using var loggerFactory = LoggerFactory.Create(logging =>
            logging.AddOpenTelemetry(options =>
            {
                options.AddJsonlConsoleExporter(configure =>
                {
                    configure.Output = mockOutput;
                });
            })
        );

        // Act
        var logger = loggerFactory.CreateLogger<JsonlConsoleLogRecordExporterTests>();
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
                options.AddJsonlConsoleExporter(configure =>
                {
                    configure.Output = mockOutput;
                });
            })
        );

        // Act
        var logger = loggerFactory.CreateLogger<JsonlConsoleLogRecordExporterTests>();
#pragma warning disable CA1848
        logger.LogWarning("Warning message");
#pragma warning restore CA1848

        // Assert
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var logRecord = doc
            .RootElement.GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs")[0]
            .GetProperty("logRecords")[0];

        // SeverityNumber is an enum - per OTLP spec, enums are serialized as strings in JSON
        Assert.Equal("SEVERITY_NUMBER_WARN", logRecord.GetProperty("severityNumber").GetString());
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
                options.AddJsonlConsoleExporter(configure =>
                {
                    configure.Output = mockOutput;
                });
            })
        );

        // Act
        var logger = loggerFactory.CreateLogger<JsonlConsoleLogRecordExporterTests>();
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
        Assert.Equal("TestEvent", logRecord.GetProperty("eventName").ToString());

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
                options.AddJsonlConsoleExporter(configure =>
                {
                    configure.Output = mockOutput;
                });
            })
        );

        // Act
        var logger = loggerFactory.CreateLogger<JsonlConsoleLogRecordExporterTests>();
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

        // Check formatted message in body
        var body = logRecord.GetProperty("body").GetProperty("stringValue").GetString();
        Assert.Contains("Alice", body);
        Assert.Contains("123", body);

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
                options.AddJsonlConsoleExporter(configure =>
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
                options.AddJsonlConsoleExporter(configure =>
                {
                    configure.Output = mockOutput;
                });
            })
        );

        // Act
        var logger = loggerFactory.CreateLogger<JsonlConsoleLogRecordExporterTests>();
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
                    options.AddJsonlConsoleExporter(configure =>
                    {
                        configure.Output = mockOutput;
                    });
                })
        );

        // Act
        var logger = loggerFactory.CreateLogger<JsonlConsoleLogRecordExporterTests>();
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
}
