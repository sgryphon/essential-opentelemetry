using System.Globalization;
using System.Text.Json;
using ProtoTrace = OpenTelemetry.Proto.Trace.V1;

namespace Essential.OpenTelemetry.Exporter;

internal static partial class OtlpJsonSerializer
{
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
