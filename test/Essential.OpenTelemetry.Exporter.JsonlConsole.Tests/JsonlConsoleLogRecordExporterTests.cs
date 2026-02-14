using System.Text.Json;
using Essential.OpenTelemetry;
using Microsoft.Extensions.Logging;
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
        Assert.True(root.TryGetProperty("resourceLogs", out var resourceLogs));
        Assert.Equal(JsonValueKind.Array, resourceLogs.ValueKind);
        Assert.True(resourceLogs.GetArrayLength() > 0);

        var resourceLog = resourceLogs[0];
        Assert.True(resourceLog.TryGetProperty("scopeLogs", out var scopeLogs));
        Assert.Equal(JsonValueKind.Array, scopeLogs.ValueKind);
        Assert.True(scopeLogs.GetArrayLength() > 0);

        var scopeLog = scopeLogs[0];
        Assert.True(scopeLog.TryGetProperty("logRecords", out var logRecords));
        Assert.Equal(JsonValueKind.Array, logRecords.ValueKind);
        Assert.Single(logRecords.EnumerateArray());

        var logRecord = logRecords[0];
        Assert.True(logRecord.TryGetProperty("body", out var body));
        Assert.True(body.TryGetProperty("stringValue", out var stringValue));
        Assert.Equal("Test log message", stringValue.GetString());
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
        var logRecord = doc.RootElement
            .GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs")[0]
            .GetProperty("logRecords")[0];

        Assert.True(logRecord.TryGetProperty("severityNumber", out var severityNumber));
        Assert.True(severityNumber.GetInt32() >= 13 && severityNumber.GetInt32() <= 16); // Warn range

        Assert.True(logRecord.TryGetProperty("severityText", out var severityText));
        Assert.Equal("Warn", severityText.GetString());
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
        var logRecord = doc.RootElement
            .GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs")[0]
            .GetProperty("logRecords")[0];

        // Event ID should be in attributes
        var attributes = logRecord.GetProperty("attributes");
        bool foundEventId = false;
        bool foundEventName = false;

        foreach (var attr in attributes.EnumerateArray())
        {
            var key = attr.GetProperty("key").GetString();
            if (key == "event.id")
            {
                foundEventId = true;
                Assert.Equal(42, attr.GetProperty("value").GetProperty("intValue").GetInt32());
            }
            else if (key == "event.name")
            {
                foundEventName = true;
                Assert.Equal("TestEvent", attr.GetProperty("value").GetProperty("stringValue").GetString());
            }
        }

        Assert.True(foundEventId);
        Assert.True(foundEventName);
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
        var logRecord = doc.RootElement
            .GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs")[0]
            .GetProperty("logRecords")[0];

        // Check formatted message in body
        var body = logRecord.GetProperty("body").GetProperty("stringValue").GetString();
        Assert.Contains("Alice", body);
        Assert.Contains("123", body);

        // Check attributes contain the structured data
        var attributes = logRecord.GetProperty("attributes");
        bool foundUserName = false;
        bool foundUserId = false;

        foreach (var attr in attributes.EnumerateArray())
        {
            var key = attr.GetProperty("key").GetString();
            if (key == "UserName")
            {
                foundUserName = true;
                Assert.Equal("Alice", attr.GetProperty("value").GetProperty("stringValue").GetString());
            }
            else if (key == "UserId")
            {
                foundUserId = true;
                Assert.Equal(123, attr.GetProperty("value").GetProperty("intValue").GetInt32());
            }
        }

        Assert.True(foundUserName);
        Assert.True(foundUserId);
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
        var scopeLogs1 = doc1.RootElement
            .GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs");
        Assert.Single(scopeLogs1.EnumerateArray());
        var scope1Name = scopeLogs1[0].GetProperty("scope").GetProperty("name").GetString();
        Assert.Equal("Scope1", scope1Name);

        // Check second line
        var doc2 = JsonDocument.Parse(mockOutput.Lines[1]);
        var scopeLogs2 = doc2.RootElement
            .GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs");
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
        var logRecord = doc.RootElement
            .GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs")[0]
            .GetProperty("logRecords")[0];

        Assert.True(logRecord.TryGetProperty("severityText", out var severityText));
        Assert.Equal(expectedSeverityText, severityText.GetString());
    }
}
