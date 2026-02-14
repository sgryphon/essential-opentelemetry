using System.Globalization;
using Google.Protobuf;
using OpenTelemetry;
using OtelSdk = OpenTelemetry.Logs;
using OtelProto = OpenTelemetry.Proto;

namespace Essential.OpenTelemetry.Exporter;

/// <summary>
/// JSONL console exporter for OpenTelemetry logs.
/// Outputs log records in OTLP protobuf JSON format compatible with the
/// OpenTelemetry Collector File Exporter and OTLP JSON File Receiver.
/// </summary>
public class JsonlConsoleLogRecordExporter : BaseExporter<OtelSdk.LogRecord>
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
    public override ExportResult Export(in Batch<OtelSdk.LogRecord> batch)
    {
        var output = this.options.Output;

        // Group log records by scope (category)
        var scopeGroups = new Dictionary<string, List<OtelSdk.LogRecord>>();

        foreach (var sdkLogRecord in batch)
        {
            var categoryName = sdkLogRecord.CategoryName ?? string.Empty;
            if (!scopeGroups.ContainsKey(categoryName))
            {
                scopeGroups[categoryName] = new List<OtelSdk.LogRecord>();
            }
            scopeGroups[categoryName].Add(sdkLogRecord);
        }

        lock (output.SyncRoot)
        {
            // Create OTLP LogsData message
            var logsData = new OtelProto.Logs.V1.LogsData();
            var resourceLogs = new OtelProto.Logs.V1.ResourceLogs
            {
                Resource = new OtelProto.Resource.V1.Resource()
            };

            // Add scope logs for each category
            foreach (var scopeGroup in scopeGroups)
            {
                var scopeLogs = new OtelProto.Logs.V1.ScopeLogs
                {
                    Scope = new OtelProto.Common.V1.InstrumentationScope { Name = scopeGroup.Key }
                };

                // Convert each SDK LogRecord to OTLP proto LogRecord
                foreach (var sdkLogRecord in scopeGroup.Value)
                {
                    var protoLogRecord = ConvertToOtlpLogRecord(sdkLogRecord);
                    scopeLogs.LogRecords.Add(protoLogRecord);
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

    private static OtelProto.Logs.V1.LogRecord ConvertToOtlpLogRecord(OtelSdk.LogRecord sdkLogRecord)
    {
        // Convert DateTime to Unix nanoseconds
        var timestampUnixNano =
            (ulong)((DateTimeOffset)sdkLogRecord.Timestamp).ToUnixTimeMilliseconds() * 1_000_000;

        var protoLogRecord = new OtelProto.Logs.V1.LogRecord
        {
            TimeUnixNano = timestampUnixNano,
            ObservedTimeUnixNano = timestampUnixNano
        };

        // Set severity
        if (sdkLogRecord.Severity.HasValue)
        {
            protoLogRecord.SeverityNumber = (OtelProto.Logs.V1.SeverityNumber)(int)sdkLogRecord.Severity.Value;
            protoLogRecord.SeverityText = GetSeverityText((int)sdkLogRecord.Severity.Value);
        }

        // Set body
        var body = GetBody(sdkLogRecord);
        if (!string.IsNullOrEmpty(body))
        {
            protoLogRecord.Body = new OtelProto.Common.V1.AnyValue { StringValue = body };
        }

        // Add event ID as attributes if present
        if (sdkLogRecord.EventId.Id != 0)
        {
            protoLogRecord.Attributes.Add(CreateKeyValue("event.id", sdkLogRecord.EventId.Id));
        }

        if (!string.IsNullOrEmpty(sdkLogRecord.EventId.Name))
        {
            protoLogRecord.Attributes.Add(CreateKeyValue("event.name", sdkLogRecord.EventId.Name));
        }

        // Add attributes from the log record
        if (sdkLogRecord.Attributes != null)
        {
            foreach (var attribute in sdkLogRecord.Attributes)
            {
                protoLogRecord.Attributes.Add(CreateKeyValue(attribute.Key, attribute.Value));
            }
        }

        // Set trace context
        if (sdkLogRecord.TraceId != default)
        {
            // Convert ActivityTraceId to byte array
            Span<byte> traceIdBytes = stackalloc byte[16];
            sdkLogRecord.TraceId.CopyTo(traceIdBytes);
            protoLogRecord.TraceId = ByteString.CopyFrom(traceIdBytes);
        }

        if (sdkLogRecord.SpanId != default)
        {
            // Convert ActivitySpanId to byte array
            Span<byte> spanIdBytes = stackalloc byte[8];
            sdkLogRecord.SpanId.CopyTo(spanIdBytes);
            protoLogRecord.SpanId = ByteString.CopyFrom(spanIdBytes);
        }

        if (sdkLogRecord.TraceFlags != default)
        {
            protoLogRecord.Flags = (uint)sdkLogRecord.TraceFlags;
        }

        // Set event name if available
        if (!string.IsNullOrEmpty(sdkLogRecord.EventId.Name))
        {
            protoLogRecord.EventName = sdkLogRecord.EventId.Name;
        }

        return protoLogRecord;
    }

    private static OtelProto.Common.V1.KeyValue CreateKeyValue(string key, object? value)
    {
        var keyValue = new OtelProto.Common.V1.KeyValue
        {
            Key = key,
            Value = new OtelProto.Common.V1.AnyValue()
        };

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

    private static string GetBody(OtelSdk.LogRecord sdkLogRecord)
    {
        // Use FormattedMessage if available
        var message = sdkLogRecord.FormattedMessage;

        // Otherwise try State
        if (string.IsNullOrEmpty(message))
        {
#pragma warning disable CS0618 // Type or member is obsolete
            message = sdkLogRecord.State?.ToString();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // Otherwise fall back to Body
        if (string.IsNullOrEmpty(message))
        {
            message = sdkLogRecord.Body?.ToString() ?? string.Empty;
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
