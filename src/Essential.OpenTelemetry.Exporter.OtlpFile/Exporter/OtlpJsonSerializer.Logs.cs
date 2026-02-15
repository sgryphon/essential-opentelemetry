using System.Globalization;
using System.Text.Json;
using ProtoLogs = OpenTelemetry.Proto.Logs.V1;

namespace Essential.OpenTelemetry.Exporter;

internal static partial class OtlpJsonSerializer
{
    /// <summary>
    /// Serializes the given <see cref="ProtoLogs.LogsData"/> to the specified output stream
    /// in OTLP JSON Protobuf Encoding format, followed by a newline.
    /// </summary>
    /// <param name="logsData">The logs data to serialize.</param>
    /// <param name="output">The stream to write the JSON output to.</param>
    internal static void SerializeLogsData(ProtoLogs.LogsData logsData, Stream output)
    {
        using (var writer = new Utf8JsonWriter(output, options: default))
        {
            WriteLogsData(writer, logsData);
            writer.Flush();
        }

        output.Write(NewLineBytes, 0, NewLineBytes.Length);
        output.Flush();
    }

    private static void WriteLogsData(Utf8JsonWriter writer, ProtoLogs.LogsData logsData)
    {
        writer.WriteStartObject();
        if (logsData.ResourceLogs.Count > 0)
        {
            writer.WriteStartArray("resourceLogs");
            foreach (var rl in logsData.ResourceLogs)
            {
                WriteResourceLogs(writer, rl);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteResourceLogs(
        Utf8JsonWriter writer,
        ProtoLogs.ResourceLogs resourceLogs
    )
    {
        writer.WriteStartObject();
        if (resourceLogs.Resource != null)
        {
            writer.WritePropertyName("resource");
            WriteResource(writer, resourceLogs.Resource);
        }

        if (resourceLogs.ScopeLogs.Count > 0)
        {
            writer.WriteStartArray("scopeLogs");
            foreach (var sl in resourceLogs.ScopeLogs)
            {
                WriteScopeLogs(writer, sl);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteScopeLogs(Utf8JsonWriter writer, ProtoLogs.ScopeLogs scopeLogs)
    {
        writer.WriteStartObject();
        if (scopeLogs.Scope != null)
        {
            writer.WritePropertyName("scope");
            WriteInstrumentationScope(writer, scopeLogs.Scope);
        }

        if (scopeLogs.LogRecords.Count > 0)
        {
            writer.WriteStartArray("logRecords");
            foreach (var lr in scopeLogs.LogRecords)
            {
                WriteLogRecord(writer, lr);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteLogRecord(Utf8JsonWriter writer, ProtoLogs.LogRecord logRecord)
    {
        writer.WriteStartObject();

        // Proto field order: time_unix_nano(1), severity_number(2), severity_text(3),
        // body(5), attributes(6), dropped_attributes_count(7), flags(8),
        // trace_id(9), span_id(10), observed_time_unix_nano(11), event_name(12)
        if (logRecord.TimeUnixNano != 0)
        {
            // fixed64 → string in JSON per protobuf convention
            writer.WriteString(
                "timeUnixNano",
                logRecord.TimeUnixNano.ToString(CultureInfo.InvariantCulture)
            );
        }

        if (logRecord.SeverityNumber != ProtoLogs.SeverityNumber.Unspecified)
        {
            // Enum → integer per OTLP JSON Protobuf Encoding
            writer.WriteNumber("severityNumber", (int)logRecord.SeverityNumber);
        }

        if (!string.IsNullOrEmpty(logRecord.SeverityText))
        {
            writer.WriteString("severityText", logRecord.SeverityText);
        }

        if (logRecord.Body != null)
        {
            writer.WritePropertyName("body");
            WriteAnyValue(writer, logRecord.Body);
        }

        if (logRecord.Attributes.Count > 0)
        {
            writer.WriteStartArray("attributes");
            foreach (var attr in logRecord.Attributes)
            {
                WriteKeyValue(writer, attr);
            }

            writer.WriteEndArray();
        }

        if (logRecord.DroppedAttributesCount != 0)
        {
            writer.WriteNumber("droppedAttributesCount", logRecord.DroppedAttributesCount);
        }

        if (logRecord.Flags != 0)
        {
            // fixed32 → number in JSON
            writer.WriteNumber("flags", logRecord.Flags);
        }

        if (!logRecord.TraceId.IsEmpty)
        {
            // bytes → lowercase hex per OTLP JSON Protobuf Encoding
            writer.WriteString("traceId", ByteStringToHexLower(logRecord.TraceId));
        }

        if (!logRecord.SpanId.IsEmpty)
        {
            // bytes → lowercase hex per OTLP JSON Protobuf Encoding
            writer.WriteString("spanId", ByteStringToHexLower(logRecord.SpanId));
        }

        if (logRecord.ObservedTimeUnixNano != 0)
        {
            // fixed64 → string in JSON per protobuf convention
            writer.WriteString(
                "observedTimeUnixNano",
                logRecord.ObservedTimeUnixNano.ToString(CultureInfo.InvariantCulture)
            );
        }

        if (!string.IsNullOrEmpty(logRecord.EventName))
        {
            writer.WriteString("eventName", logRecord.EventName);
        }

        writer.WriteEndObject();
    }
}
