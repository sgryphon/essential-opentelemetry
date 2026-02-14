using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Essential.OpenTelemetry.Exporter;

/// <summary>
/// JSONL console exporter for OpenTelemetry logs.
/// Outputs log records in OTLP protobuf JSON format compatible with the
/// OpenTelemetry Collector File Exporter and OTLP JSON File Receiver.
/// </summary>
public class JsonlConsoleLogRecordExporter : BaseExporter<LogRecord>
{
    private static readonly PropertyInfo? SeverityProperty = typeof(LogRecord).GetProperty(
        "Severity",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
    );

    private readonly JsonlConsoleOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonlConsoleLogRecordExporter"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the exporter.</param>
    public JsonlConsoleLogRecordExporter(JsonlConsoleOptions options)
    {
        this.options = options ?? new JsonlConsoleOptions();
    }

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        var output = this.options.Output;

        // Group log records by scope (category)
        var scopeGroups = new Dictionary<string, List<LogRecord>>();

        foreach (var logRecord in batch)
        {
            var categoryName = logRecord.CategoryName ?? string.Empty;
            if (!scopeGroups.ContainsKey(categoryName))
            {
                scopeGroups[categoryName] = new List<LogRecord>();
            }
            scopeGroups[categoryName].Add(logRecord);
        }

        lock (output.SyncRoot)
        {
            // Write one JSON line per batch
            using var stream = new MemoryStream();
            using (
                var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false })
            )
            {
                writer.WriteStartObject();
                writer.WriteStartArray("resourceLogs");

                writer.WriteStartObject();

                // Write empty resource for now (could be enhanced to read from LogRecord if available)
                WriteResource(writer);

                // Write scopeLogs
                writer.WriteStartArray("scopeLogs");

                foreach (var scopeGroup in scopeGroups)
                {
                    writer.WriteStartObject();

                    // Write scope
                    WriteScope(writer, scopeGroup.Key);

                    // Write logRecords
                    writer.WriteStartArray("logRecords");
                    foreach (var logRecord in scopeGroup.Value)
                    {
                        WriteLogRecord(writer, logRecord);
                    }
                    writer.WriteEndArray(); // logRecords

                    writer.WriteEndObject(); // scopeLogs item
                }

                writer.WriteEndArray(); // scopeLogs

                writer.WriteEndObject(); // resourceLogs item

                writer.WriteEndArray(); // resourceLogs
                writer.WriteEndObject(); // root
            }

            var jsonLine = Encoding.UTF8.GetString(stream.ToArray());
            output.WriteLine(jsonLine);
        }

        return ExportResult.Success;
    }

    private static void WriteResource(Utf8JsonWriter writer)
    {
        writer.WriteStartObject("resource");
        writer.WriteStartArray("attributes");
        // Empty attributes array for now
        writer.WriteEndArray(); // attributes
        writer.WriteEndObject(); // resource
    }

    private static void WriteScope(Utf8JsonWriter writer, string categoryName)
    {
        writer.WriteStartObject("scope");

        if (!string.IsNullOrEmpty(categoryName))
        {
            writer.WriteString("name", categoryName);
        }

        writer.WriteEndObject(); // scope
    }

    private static void WriteLogRecord(Utf8JsonWriter writer, LogRecord logRecord)
    {
        writer.WriteStartObject();

        // timeUnixNano - convert DateTime to Unix nanoseconds
        var timestampUnixNano =
            ((DateTimeOffset)logRecord.Timestamp).ToUnixTimeMilliseconds() * 1_000_000;
        writer.WriteString(
            "timeUnixNano",
            timestampUnixNano.ToString(CultureInfo.InvariantCulture)
        );

        // observedTimeUnixNano (optional - same as timestamp if not set)
        writer.WriteString(
            "observedTimeUnixNano",
            timestampUnixNano.ToString(CultureInfo.InvariantCulture)
        );

        // severityNumber
        var severity = GetSeverityNumber(logRecord);
        if (severity != 0)
        {
            writer.WriteNumber("severityNumber", severity);
        }

        // severityText
        var severityText = GetSeverityText(severity);
        if (!string.IsNullOrEmpty(severityText))
        {
            writer.WriteString("severityText", severityText);
        }

        // body
        var body = GetBody(logRecord);
        if (!string.IsNullOrEmpty(body))
        {
            writer.WriteStartObject("body");
            writer.WriteString("stringValue", body);
            writer.WriteEndObject();
        }

        // attributes
        writer.WriteStartArray("attributes");

        // Add event ID as attributes if present
        if (logRecord.EventId.Id != 0)
        {
            WriteAttribute(writer, "event.id", logRecord.EventId.Id);
        }

        if (!string.IsNullOrEmpty(logRecord.EventId.Name))
        {
            WriteAttribute(writer, "event.name", logRecord.EventId.Name);
        }

        // Add attributes from the log record
        if (logRecord.Attributes != null)
        {
            foreach (var attribute in logRecord.Attributes)
            {
                WriteAttribute(writer, attribute.Key, attribute.Value);
            }
        }

        writer.WriteEndArray(); // attributes

        // droppedAttributesCount (always 0 for now)
        writer.WriteNumber("droppedAttributesCount", 0);

        // traceId
        if (logRecord.TraceId != default)
        {
            writer.WriteString("traceId", logRecord.TraceId.ToHexString());
        }

        // spanId
        if (logRecord.SpanId != default)
        {
            writer.WriteString("spanId", logRecord.SpanId.ToHexString());
        }

        writer.WriteEndObject(); // logRecord
    }

    private static void WriteAttribute(Utf8JsonWriter writer, string key, object? value)
    {
        writer.WriteStartObject();
        writer.WriteString("key", key);

        writer.WriteStartObject("value");

        switch (value)
        {
            case null:
                writer.WriteNull("stringValue");
                break;
            case string s:
                writer.WriteString("stringValue", s);
                break;
            case bool b:
                writer.WriteBoolean("boolValue", b);
                break;
            case byte b:
                writer.WriteNumber("intValue", b);
                break;
            case sbyte sb:
                writer.WriteNumber("intValue", sb);
                break;
            case short s:
                writer.WriteNumber("intValue", s);
                break;
            case ushort us:
                writer.WriteNumber("intValue", us);
                break;
            case int i:
                writer.WriteNumber("intValue", i);
                break;
            case uint ui:
                writer.WriteNumber("intValue", ui);
                break;
            case long l:
                writer.WriteNumber("intValue", l);
                break;
            case ulong ul:
                writer.WriteNumber("intValue", (long)ul);
                break;
            case float f:
                writer.WriteNumber("doubleValue", f);
                break;
            case double d:
                writer.WriteNumber("doubleValue", d);
                break;
            case decimal dec:
                writer.WriteNumber("doubleValue", (double)dec);
                break;
            default:
                // For other types, convert to string
                writer.WriteString("stringValue", value.ToString());
                break;
        }

        writer.WriteEndObject(); // value

        writer.WriteEndObject(); // attribute
    }

    private static string GetBody(LogRecord logRecord)
    {
        // Use FormattedMessage if available
        var message = logRecord.FormattedMessage;

        // Otherwise try State
        if (string.IsNullOrEmpty(message))
        {
#pragma warning disable CS0618 // Type or member is obsolete
            message = logRecord.State?.ToString();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // Otherwise fall back to Body
        if (string.IsNullOrEmpty(message))
        {
            message = logRecord.Body?.ToString() ?? string.Empty;
        }

        return message ?? string.Empty;
    }

    private static int GetSeverityNumber(LogRecord logRecord)
    {
        // Get the internal Severity property via reflection
        var severityValue = SeverityProperty?.GetValue(logRecord);
        if (severityValue != null)
        {
            return (int)severityValue;
        }

        return 0;
    }

    private static string GetSeverityText(int severityNumber)
    {
        // Map OpenTelemetry severity numbers to text
        return severityNumber switch
        {
            >= 1 and <= 4 => "Trace",
            >= 5 and <= 8 => "Debug",
            >= 9 and <= 12 => "Info",
            >= 13 and <= 16 => "Warn",
            >= 17 and <= 20 => "Error",
            >= 21 and <= 24 => "Fatal",
            _ => string.Empty,
        };
    }
}
