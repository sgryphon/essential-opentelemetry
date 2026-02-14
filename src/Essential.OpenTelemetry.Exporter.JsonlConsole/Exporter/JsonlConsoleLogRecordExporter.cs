using System.Globalization;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Proto.Common.V1;

namespace Essential.OpenTelemetry.Exporter;

/// <summary>
/// JSONL console exporter for OpenTelemetry logs.
/// Outputs log records in OTLP protobuf JSON format compatible with the
/// OpenTelemetry Collector File Exporter and OTLP JSON File Receiver.
/// </summary>
public class JsonlConsoleLogRecordExporter : BaseExporter<LogRecord>
{
    private static readonly JsonFormatter JsonFormatter = new(
        JsonFormatter.Settings.Default.WithPreserveProtoFieldNames(false)
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
                scopeGroups[categoryName] = new List<SdkLogRecord>();
            }
            scopeGroups[categoryName].Add(logRecord);
        }

        lock (output.SyncRoot)
        {
            // Create OTLP LogsData message
            var logsData = new global::OpenTelemetry.Proto.Logs.V1.LogsData();
            var resourceLogs = new global::OpenTelemetry.Proto.Logs.V1.ResourceLogs
            {
                Resource = new global::OpenTelemetry.Proto.Resource.V1.Resource()
            };

            // Add scope logs for each category
            foreach (var scopeGroup in scopeGroups)
            {
                var scopeLogs = new global::OpenTelemetry.Proto.Logs.V1.ScopeLogs
                {
                    Scope = new InstrumentationScope { Name = scopeGroup.Key }
                };

                // Convert each LogRecord to OTLP LogRecord
                foreach (var logRecord in scopeGroup.Value)
                {
                    var otlpLogRecord = ConvertToOtlpLogRecord(logRecord);
                    scopeLogs.LogRecords.Add(otlpLogRecord);
                }

                resourceLogs.ScopeLogs.Add(scopeLogs);
            }

            logsData.ResourceLogs.Add(resourceLogs);

            // Serialize to JSON using Google.Protobuf JsonFormatter
            var jsonLine = JsonFormatter.Format(logsData);
            output.WriteLine(jsonLine);
        }

        return ExportResult.Success;
    }

    private static global::OpenTelemetry.Proto.Logs.V1.LogRecord ConvertToOtlpLogRecord(global::OpenTelemetry.Logs.LogRecord logRecord)
    {
        // Convert DateTime to Unix nanoseconds
        var timestampUnixNano =
            (ulong)((DateTimeOffset)logRecord.Timestamp).ToUnixTimeMilliseconds() * 1_000_000;

        var otlpLogRecord = new global::OpenTelemetry.Proto.Logs.V1.LogRecord
        {
            TimeUnixNano = timestampUnixNano,
            ObservedTimeUnixNano = timestampUnixNano
        };

        // Set severity
        if (logRecord.Severity.HasValue)
        {
            otlpLogRecord.SeverityNumber = (global::OpenTelemetry.Proto.Logs.V1.SeverityNumber)(int)logRecord.Severity.Value;
            otlpLogRecord.SeverityText = GetSeverityText((int)logRecord.Severity.Value);
        }

        // Set body
        var body = GetBody(logRecord);
        if (!string.IsNullOrEmpty(body))
        {
            otlpLogRecord.Body = new AnyValue { StringValue = body };
        }

        // Add event ID as attributes if present
        if (logRecord.EventId.Id != 0)
        {
            otlpLogRecord.Attributes.Add(CreateKeyValue("event.id", logRecord.EventId.Id));
        }

        if (!string.IsNullOrEmpty(logRecord.EventId.Name))
        {
            otlpLogRecord.Attributes.Add(CreateKeyValue("event.name", logRecord.EventId.Name));
        }

        // Add attributes from the log record
        if (logRecord.Attributes != null)
        {
            foreach (var attribute in logRecord.Attributes)
            {
                otlpLogRecord.Attributes.Add(CreateKeyValue(attribute.Key, attribute.Value));
            }
        }

        // Set trace context
        if (logRecord.TraceId != default)
        {
            // Convert ActivityTraceId to byte array
            Span<byte> traceIdBytes = stackalloc byte[16];
            logRecord.TraceId.CopyTo(traceIdBytes);
            otlpLogRecord.TraceId = ByteString.CopyFrom(traceIdBytes);
        }

        if (logRecord.SpanId != default)
        {
            // Convert ActivitySpanId to byte array
            Span<byte> spanIdBytes = stackalloc byte[8];
            logRecord.SpanId.CopyTo(spanIdBytes);
            otlpLogRecord.SpanId = ByteString.CopyFrom(spanIdBytes);
        }

        if (logRecord.TraceFlags != default)
        {
            otlpLogRecord.Flags = (uint)logRecord.TraceFlags;
        }

        // Set event name if available
        if (!string.IsNullOrEmpty(logRecord.EventId.Name))
        {
            otlpLogRecord.EventName = logRecord.EventId.Name;
        }

        return otlpLogRecord;
    }

    private static KeyValue CreateKeyValue(string key, object? value)
    {
        var keyValue = new KeyValue { Key = key, Value = new AnyValue() };

        switch (value)
        {
            case null:
                keyValue.Value.StringValue = string.Empty;
                break;
            case string s:
                keyValue.Value.StringValue = s;
                break;
            case bool b:
                keyValue.Value.BoolValue = b;
                break;
            case byte by:
                keyValue.Value.IntValue = by;
                break;
            case sbyte sb:
                keyValue.Value.IntValue = sb;
                break;
            case short sh:
                keyValue.Value.IntValue = sh;
                break;
            case ushort us:
                keyValue.Value.IntValue = us;
                break;
            case int i:
                keyValue.Value.IntValue = i;
                break;
            case uint ui:
                keyValue.Value.IntValue = (long)ui;
                break;
            case long l:
                keyValue.Value.IntValue = l;
                break;
            case ulong ul:
                // Use string for ulong to avoid overflow
                keyValue.Value.StringValue = ul.ToString(CultureInfo.InvariantCulture);
                break;
            case float f:
                keyValue.Value.DoubleValue = f;
                break;
            case double d:
                keyValue.Value.DoubleValue = d;
                break;
            case decimal dec:
                // Use string for decimal to preserve precision
                keyValue.Value.StringValue = dec.ToString(CultureInfo.InvariantCulture);
                break;
            default:
                // For other types, convert to string
                keyValue.Value.StringValue = value.ToString() ?? string.Empty;
                break;
        }

        return keyValue;
    }

    private static string GetBody(global::OpenTelemetry.Logs.LogRecord logRecord)
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
