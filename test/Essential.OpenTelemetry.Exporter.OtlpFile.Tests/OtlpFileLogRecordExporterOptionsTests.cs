using System.Text.Json;
using Essential.OpenTelemetry;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using Xunit;

namespace Essential.OpenTelemetry.Exporter.OtlpFile.Tests;

[Collection("OtlpFileTests")]
public class OtlpFileLogRecordExporterOptionsTests
{
    [Fact]
    public void IncludeFormattedMessageTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        using var loggerFactory = LoggerFactory.Create(logging =>
            logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.AddOtlpFileExporter(configure =>
                {
                    configure.Console = mockOutput;
                });
            })
        );

        // Act
        var logger = loggerFactory.CreateLogger<OtlpFileLogRecordExporterOptionsTests>();
#pragma warning disable CA1848
        logger.LogInformation("Hello {Name}, you are {Age} years old", "Alice", 30);
#pragma warning restore CA1848

        // Assert
        Assert.Single(mockOutput.Lines);
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var logRecord = doc
            .RootElement.GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs")[0]
            .GetProperty("logRecords")[0];

        // Body should contain the message template (exporter prefers template over formatted)
        var body = logRecord.GetProperty("body").GetProperty("stringValue").GetString();
        Assert.Equal("Hello {Name}, you are {Age} years old", body);

        // Structured parameters should be in attributes
        var attributes = logRecord
            .GetProperty("attributes")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("key").ToString());

        Assert.Equal(
            "Alice",
            attributes["Name"].GetProperty("value").GetProperty("stringValue").GetString()
        );
        Assert.Equal(
            "30",
            attributes["Age"].GetProperty("value").GetProperty("intValue").GetString()
        );
    }

    [Fact]
    public void IncludeScopesTest()
    {
        // Arrange
        var mockOutput = new MockConsole();
        using var loggerFactory = LoggerFactory.Create(logging =>
            logging.AddOpenTelemetry(options =>
            {
                options.IncludeScopes = true;
                options.AddOtlpFileExporter(configure =>
                {
                    configure.Console = mockOutput;
                });
            })
        );

        // Act
        var logger = loggerFactory.CreateLogger<OtlpFileLogRecordExporterOptionsTests>();
        using (logger.BeginScope("OuterScope"))
        {
            using (logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = "REQ-123" }))
            {
#pragma warning disable CA1848
                logger.LogInformation("Scoped message");
#pragma warning restore CA1848
            }
        }

        // Assert - should export successfully with nested scopes
        Assert.Single(mockOutput.Lines);
        var doc = JsonDocument.Parse(mockOutput.Lines[0]);
        var logRecord = doc
            .RootElement.GetProperty("resourceLogs")[0]
            .GetProperty("scopeLogs")[0]
            .GetProperty("logRecords")[0];

        var body = logRecord.GetProperty("body").GetProperty("stringValue").GetString();
        Assert.Equal("Scoped message", body);
    }
}
