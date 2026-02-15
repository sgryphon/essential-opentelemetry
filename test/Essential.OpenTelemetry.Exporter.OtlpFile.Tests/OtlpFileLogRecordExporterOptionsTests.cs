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

        // Example file output from OpenTelemetry Collector:
        //   "body": { "stringValue": "Processing order ORD-789 for ¤150.00" },
        //   "attributes": [
        //     { "key": "OrderId", "value": { "stringValue": "ORD-789" } },
        //     { "key": "Amount", "value": { "stringValue": "150.00" } },
        //     {
        //       "key": "{OriginalFormat}",
        //       "value": {
        //         "stringValue": "Processing order {OrderId} for {Amount:C}"
        //       }
        //     },

        // When formatted message option is on,
        // Body should contain the formatted message
        var body = logRecord.GetProperty("body").GetProperty("stringValue").GetString();
        Assert.Equal("Hello Alice, you are 30 years old", body);

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

        // OriginalFormat should also be in the attributes
        Assert.Equal(
            "Hello {Name}, you are {Age} years old",
            attributes["{OriginalFormat}"]
                .GetProperty("value")
                .GetProperty("stringValue")
                .GetString()
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
        using (logger.BeginScope("OuterScope {OuterId}", 123))
        {
            using (logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = "REQ-123" }))
            {
#pragma warning disable CA1848
                logger.LogInformation("Scoped message {MessageValue}", "Message");
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

        // Example file output from OpenTelemetry Collector:
        //   "attributes": [
        //     { "key": "OrderId", "value": { "stringValue": "ORD-789" } },
        //     { "key": "Amount", "value": { "stringValue": "150.00" } },
        //     { "key": "RequestId", "value": { "stringValue": "REQ-123" } }

        // Structured parameters should be in attributes
        var attributes = logRecord
            .GetProperty("attributes")
            .EnumerateArray()
            .ToDictionary(x => x.GetProperty("key").ToString());

        Assert.Equal(
            "Message",
            attributes["MessageValue"].GetProperty("value").GetProperty("stringValue").GetString()
        );

        // Scope values should be in attributes
        Assert.Equal(
            "123",
            attributes["OuterId"].GetProperty("value").GetProperty("intValue").GetString()
        );
        Assert.Equal(
            "REQ-123",
            attributes["Requestion"].GetProperty("value").GetProperty("stringValue").GetString()
        );
    }
}
