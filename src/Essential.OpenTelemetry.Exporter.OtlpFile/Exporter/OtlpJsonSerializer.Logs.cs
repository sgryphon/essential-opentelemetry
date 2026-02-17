using System.Text.Json;
using OpenTelemetry.Proto.Logs.V1;
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
        // SerializeToStream(output, writer => WriteLogsData(writer, logsData));
        SerializeToStream(
            output,
            writer =>
                WriteOtlpData<LogsData, ResourceLogs, ScopeLogs, LogRecord>(
                    writer,
                    logsData,
                    "resourceLogs",
                    data => data.ResourceLogs,
                    resourceBlock => resourceBlock.Resource,
                    resourceBlock => resourceBlock.SchemaUrl,
                    "scopeLogs",
                    resourceBlock => resourceBlock.ScopeLogs,
                    scopeBlock => scopeBlock.Scope,
                    scopeBlock => scopeBlock.SchemaUrl,
                    "logRecords",
                    scopeBlock => scopeBlock.LogRecords,
                    WriteLogRecord
                )
        );
    }

    private static void WriteLogRecord(Utf8JsonWriter writer, ProtoLogs.LogRecord logRecord)
    {
        writer.WriteStartObject();

        // Proto field order: time_unix_nano(1), severity_number(2), severity_text(3),
        // body(5), attributes(6), dropped_attributes_count(7), flags(8),
        // trace_id(9), span_id(10), observed_time_unix_nano(11), event_name(12)
        WriteTimestamp(writer, "timeUnixNano", logRecord.TimeUnixNano);

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

        WriteAttributes(writer, logRecord.Attributes, logRecord.DroppedAttributesCount);

        if (logRecord.Flags != 0)
        {
            // fixed32 → number in JSON
            writer.WriteNumber("flags", logRecord.Flags);
        }

        WriteHexBytesField(writer, "traceId", logRecord.TraceId);
        WriteHexBytesField(writer, "spanId", logRecord.SpanId);
        WriteTimestamp(writer, "observedTimeUnixNano", logRecord.ObservedTimeUnixNano);

        if (!string.IsNullOrEmpty(logRecord.EventName))
        {
            writer.WriteString("eventName", logRecord.EventName);
        }

        writer.WriteEndObject();
    }
}
