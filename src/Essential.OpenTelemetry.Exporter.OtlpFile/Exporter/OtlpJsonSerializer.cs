using System.Globalization;
using System.Text.Json;
using Google.Protobuf;
using ProtoCommon = OpenTelemetry.Proto.Common.V1;
using ProtoLogs = OpenTelemetry.Proto.Logs.V1;
using ProtoResource = OpenTelemetry.Proto.Resource.V1;

namespace Essential.OpenTelemetry.Exporter;

/// <summary>
/// Custom serializer using <see cref="Utf8JsonWriter"/> to produce OTLP JSON Protobuf Encoding
/// compatible with the OpenTelemetry Collector File Exporter and OTLP JSON File Receiver.
/// </summary>
/// <remarks>
/// <para>
/// This serializer is used instead of Google.Protobuf's <c>JsonFormatter</c> because the OTLP spec
/// requires hex encoding for trace_id/span_id fields (not base64) and integer values for enum
/// fields (not string names).
/// </para>
/// <para>
/// Encoding rules applied:
/// <list type="bullet">
///   <item><description>Enum fields are serialized as integers (not string names)</description></item>
///   <item><description>bytes fields (trace_id, span_id) are serialized as lowercase hex (not base64)</description></item>
///   <item><description>int64/uint64/fixed64 fields are serialized as strings</description></item>
///   <item><description>Field names use camelCase</description></item>
/// </list>
/// </para>
/// <para>
/// See <see href="https://opentelemetry.io/docs/specs/otlp/#json-protobuf-encoding"/>.
/// </para>
/// </remarks>
internal static class OtlpJsonSerializer
{
    private static readonly byte[] NewLineBytes = new byte[] { (byte)'\n' };

    /// <summary>
    /// Serializes the given <see cref="ProtoLogs.LogsData"/> to the specified output stream
    /// in OTLP JSON Protobuf Encoding format, followed by a newline.
    /// </summary>
    /// <param name="logsData">The logs data to serialize.</param>
    /// <param name="output">The stream to write the JSON output to.</param>
    internal static void SerializeLogsData(ProtoLogs.LogsData logsData, Stream output)
    {
        using (var writer = new Utf8JsonWriter(output))
        {
            WriteLogsData(writer, logsData);
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

    private static void WriteResource(Utf8JsonWriter writer, ProtoResource.Resource resource)
    {
        writer.WriteStartObject();
        if (resource.Attributes.Count > 0)
        {
            writer.WriteStartArray("attributes");
            foreach (var attr in resource.Attributes)
            {
                WriteKeyValue(writer, attr);
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

    private static void WriteInstrumentationScope(
        Utf8JsonWriter writer,
        ProtoCommon.InstrumentationScope scope
    )
    {
        writer.WriteStartObject();
        if (!string.IsNullOrEmpty(scope.Name))
        {
            writer.WriteString("name", scope.Name);
        }

        if (!string.IsNullOrEmpty(scope.Version))
        {
            writer.WriteString("version", scope.Version);
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

    private static void WriteKeyValue(Utf8JsonWriter writer, ProtoCommon.KeyValue kv)
    {
        writer.WriteStartObject();
        writer.WriteString("key", kv.Key);
        if (kv.Value != null)
        {
            writer.WritePropertyName("value");
            WriteAnyValue(writer, kv.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteAnyValue(Utf8JsonWriter writer, ProtoCommon.AnyValue anyValue)
    {
        writer.WriteStartObject();
        switch (anyValue.ValueCase)
        {
            case ProtoCommon.AnyValue.ValueOneofCase.StringValue:
                writer.WriteString("stringValue", anyValue.StringValue);
                break;
            case ProtoCommon.AnyValue.ValueOneofCase.BoolValue:
                writer.WriteBoolean("boolValue", anyValue.BoolValue);
                break;
            case ProtoCommon.AnyValue.ValueOneofCase.IntValue:
                // int64 → string in JSON per protobuf convention
                writer.WriteString(
                    "intValue",
                    anyValue.IntValue.ToString(CultureInfo.InvariantCulture)
                );
                break;
            case ProtoCommon.AnyValue.ValueOneofCase.DoubleValue:
                writer.WriteNumber("doubleValue", anyValue.DoubleValue);
                break;
            case ProtoCommon.AnyValue.ValueOneofCase.ArrayValue:
                writer.WritePropertyName("arrayValue");
                writer.WriteStartObject();
                if (anyValue.ArrayValue.Values.Count > 0)
                {
                    writer.WriteStartArray("values");
                    foreach (var item in anyValue.ArrayValue.Values)
                    {
                        WriteAnyValue(writer, item);
                    }

                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
                break;
            case ProtoCommon.AnyValue.ValueOneofCase.KvlistValue:
                writer.WritePropertyName("kvlistValue");
                writer.WriteStartObject();
                if (anyValue.KvlistValue.Values.Count > 0)
                {
                    writer.WriteStartArray("values");
                    foreach (var kv in anyValue.KvlistValue.Values)
                    {
                        WriteKeyValue(writer, kv);
                    }

                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
                break;
            case ProtoCommon.AnyValue.ValueOneofCase.BytesValue:
                // General bytes fields use base64 per standard protobuf JSON mapping
                writer.WriteString(
                    "bytesValue",
                    Convert.ToBase64String(anyValue.BytesValue.ToByteArray())
                );
                break;
        }

        writer.WriteEndObject();
    }

    private static string ByteStringToHexLower(ByteString bytes)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        var byteArray = bytes.ToByteArray();
        var hex = new char[byteArray.Length * 2];
        for (var i = 0; i < byteArray.Length; i++)
        {
            var b = byteArray[i];
            hex[i * 2] = "0123456789abcdef"[b >> 4];
            hex[i * 2 + 1] = "0123456789abcdef"[b & 0xF];
        }

        return new string(hex);
    }
}
