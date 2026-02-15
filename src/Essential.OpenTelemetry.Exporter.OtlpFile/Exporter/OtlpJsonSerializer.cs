using System.Globalization;
using System.Text.Json;
using Google.Protobuf;
using ProtoCommon = OpenTelemetry.Proto.Common.V1;
using ProtoLogs = OpenTelemetry.Proto.Logs.V1;
using ProtoResource = OpenTelemetry.Proto.Resource.V1;
using ProtoTrace = OpenTelemetry.Proto.Trace.V1;

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

    /// <summary>
    /// Serializes the given <see cref="ProtoTrace.TracesData"/> to the specified output stream
    /// in OTLP JSON Protobuf Encoding format, followed by a newline.
    /// </summary>
    /// <param name="tracesData">The traces data to serialize.</param>
    /// <param name="output">The stream to write the JSON output to.</param>
    internal static void SerializeTracesData(ProtoTrace.TracesData tracesData, Stream output)
    {
        using (var writer = new Utf8JsonWriter(output, options: default))
        {
            WriteTracesData(writer, tracesData);
            writer.Flush();
        }

        output.Write(NewLineBytes, 0, NewLineBytes.Length);
        output.Flush();
    }

    private static void WriteTracesData(Utf8JsonWriter writer, ProtoTrace.TracesData tracesData)
    {
        writer.WriteStartObject();
        if (tracesData.ResourceSpans.Count > 0)
        {
            writer.WriteStartArray("resourceSpans");
            foreach (var rs in tracesData.ResourceSpans)
            {
                WriteResourceSpans(writer, rs);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteResourceSpans(
        Utf8JsonWriter writer,
        ProtoTrace.ResourceSpans resourceSpans
    )
    {
        writer.WriteStartObject();
        if (resourceSpans.Resource != null)
        {
            writer.WritePropertyName("resource");
            WriteResource(writer, resourceSpans.Resource);
        }

        if (resourceSpans.ScopeSpans.Count > 0)
        {
            writer.WriteStartArray("scopeSpans");
            foreach (var ss in resourceSpans.ScopeSpans)
            {
                WriteScopeSpans(writer, ss);
            }

            writer.WriteEndArray();
        }

        if (!string.IsNullOrEmpty(resourceSpans.SchemaUrl))
        {
            writer.WriteString("schemaUrl", resourceSpans.SchemaUrl);
        }

        writer.WriteEndObject();
    }

    private static void WriteScopeSpans(Utf8JsonWriter writer, ProtoTrace.ScopeSpans scopeSpans)
    {
        writer.WriteStartObject();
        if (scopeSpans.Scope != null)
        {
            writer.WritePropertyName("scope");
            WriteInstrumentationScope(writer, scopeSpans.Scope);
        }

        if (scopeSpans.Spans.Count > 0)
        {
            writer.WriteStartArray("spans");
            foreach (var span in scopeSpans.Spans)
            {
                WriteSpan(writer, span);
            }

            writer.WriteEndArray();
        }

        if (!string.IsNullOrEmpty(scopeSpans.SchemaUrl))
        {
            writer.WriteString("schemaUrl", scopeSpans.SchemaUrl);
        }

        writer.WriteEndObject();
    }

    private static void WriteSpan(Utf8JsonWriter writer, ProtoTrace.Span span)
    {
        writer.WriteStartObject();

        // Proto field order: trace_id(1), span_id(2), trace_state(3), parent_span_id(4),
        // flags(5), name(6), kind(7), start_time_unix_nano(8), end_time_unix_nano(9),
        // attributes(10), dropped_attributes_count(11), events(12), dropped_events_count(13),
        // links(14), dropped_links_count(15), status(16)

        if (!span.TraceId.IsEmpty)
        {
            // bytes → lowercase hex per OTLP JSON Protobuf Encoding
            writer.WriteString("traceId", ByteStringToHexLower(span.TraceId));
        }

        if (!span.SpanId.IsEmpty)
        {
            // bytes → lowercase hex per OTLP JSON Protobuf Encoding
            writer.WriteString("spanId", ByteStringToHexLower(span.SpanId));
        }

        if (!string.IsNullOrEmpty(span.TraceState))
        {
            writer.WriteString("traceState", span.TraceState);
        }

        if (!span.ParentSpanId.IsEmpty)
        {
            // bytes → lowercase hex per OTLP JSON Protobuf Encoding
            writer.WriteString("parentSpanId", ByteStringToHexLower(span.ParentSpanId));
        }

        if (span.Flags != 0)
        {
            // fixed32 → number in JSON
            writer.WriteNumber("flags", span.Flags);
        }

        if (!string.IsNullOrEmpty(span.Name))
        {
            writer.WriteString("name", span.Name);
        }

        if (span.Kind != ProtoTrace.Span.Types.SpanKind.Unspecified)
        {
            // Enum → integer per OTLP JSON Protobuf Encoding
            writer.WriteNumber("kind", (int)span.Kind);
        }

        if (span.StartTimeUnixNano != 0)
        {
            // fixed64 → string in JSON per protobuf convention
            writer.WriteString(
                "startTimeUnixNano",
                span.StartTimeUnixNano.ToString(CultureInfo.InvariantCulture)
            );
        }

        if (span.EndTimeUnixNano != 0)
        {
            // fixed64 → string in JSON per protobuf convention
            writer.WriteString(
                "endTimeUnixNano",
                span.EndTimeUnixNano.ToString(CultureInfo.InvariantCulture)
            );
        }

        if (span.Attributes.Count > 0)
        {
            writer.WriteStartArray("attributes");
            foreach (var attr in span.Attributes)
            {
                WriteKeyValue(writer, attr);
            }

            writer.WriteEndArray();
        }

        if (span.DroppedAttributesCount != 0)
        {
            writer.WriteNumber("droppedAttributesCount", span.DroppedAttributesCount);
        }

        if (span.Events.Count > 0)
        {
            writer.WriteStartArray("events");
            foreach (var evt in span.Events)
            {
                WriteSpanEvent(writer, evt);
            }

            writer.WriteEndArray();
        }

        if (span.DroppedEventsCount != 0)
        {
            writer.WriteNumber("droppedEventsCount", span.DroppedEventsCount);
        }

        if (span.Links.Count > 0)
        {
            writer.WriteStartArray("links");
            foreach (var link in span.Links)
            {
                WriteSpanLink(writer, link);
            }

            writer.WriteEndArray();
        }

        if (span.DroppedLinksCount != 0)
        {
            writer.WriteNumber("droppedLinksCount", span.DroppedLinksCount);
        }

        if (span.Status != null)
        {
            writer.WritePropertyName("status");
            WriteSpanStatus(writer, span.Status);
        }

        writer.WriteEndObject();
    }

    private static void WriteSpanEvent(Utf8JsonWriter writer, ProtoTrace.Span.Types.Event evt)
    {
        writer.WriteStartObject();

        if (evt.TimeUnixNano != 0)
        {
            // fixed64 → string in JSON per protobuf convention
            writer.WriteString(
                "timeUnixNano",
                evt.TimeUnixNano.ToString(CultureInfo.InvariantCulture)
            );
        }

        if (!string.IsNullOrEmpty(evt.Name))
        {
            writer.WriteString("name", evt.Name);
        }

        if (evt.Attributes.Count > 0)
        {
            writer.WriteStartArray("attributes");
            foreach (var attr in evt.Attributes)
            {
                WriteKeyValue(writer, attr);
            }

            writer.WriteEndArray();
        }

        if (evt.DroppedAttributesCount != 0)
        {
            writer.WriteNumber("droppedAttributesCount", evt.DroppedAttributesCount);
        }

        writer.WriteEndObject();
    }

    private static void WriteSpanLink(Utf8JsonWriter writer, ProtoTrace.Span.Types.Link link)
    {
        writer.WriteStartObject();

        if (!link.TraceId.IsEmpty)
        {
            // bytes → lowercase hex per OTLP JSON Protobuf Encoding
            writer.WriteString("traceId", ByteStringToHexLower(link.TraceId));
        }

        if (!link.SpanId.IsEmpty)
        {
            // bytes → lowercase hex per OTLP JSON Protobuf Encoding
            writer.WriteString("spanId", ByteStringToHexLower(link.SpanId));
        }

        if (!string.IsNullOrEmpty(link.TraceState))
        {
            writer.WriteString("traceState", link.TraceState);
        }

        if (link.Attributes.Count > 0)
        {
            writer.WriteStartArray("attributes");
            foreach (var attr in link.Attributes)
            {
                WriteKeyValue(writer, attr);
            }

            writer.WriteEndArray();
        }

        if (link.DroppedAttributesCount != 0)
        {
            writer.WriteNumber("droppedAttributesCount", link.DroppedAttributesCount);
        }

        if (link.Flags != 0)
        {
            // fixed32 → number in JSON
            writer.WriteNumber("flags", link.Flags);
        }

        writer.WriteEndObject();
    }

    private static void WriteSpanStatus(Utf8JsonWriter writer, ProtoTrace.Status status)
    {
        writer.WriteStartObject();

        if (!string.IsNullOrEmpty(status.Message))
        {
            writer.WriteString("message", status.Message);
        }

        if (status.Code != ProtoTrace.Status.Types.StatusCode.Unset)
        {
            // Enum → integer per OTLP JSON Protobuf Encoding
            writer.WriteNumber("code", (int)status.Code);
        }

        writer.WriteEndObject();
    }
}
