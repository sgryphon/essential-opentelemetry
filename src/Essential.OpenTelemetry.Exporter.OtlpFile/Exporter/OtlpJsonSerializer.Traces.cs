using System.Text.Json;
using OpenTelemetry.Proto.Trace.V1;
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
        // SerializeToStream(output, writer => WriteTracesData(writer, tracesData));
        SerializeToStream(
            output,
            writer =>
                WriteOtlpData<TracesData, ResourceSpans, ScopeSpans, Span>(
                    writer,
                    tracesData,
                    "resourceSpans",
                    data => data.ResourceSpans,
                    resourceBlock => resourceBlock.Resource,
                    resourceBlock => resourceBlock.SchemaUrl,
                    "scopeSpans",
                    resourceBlock => resourceBlock.ScopeSpans,
                    scopeBlock => scopeBlock.Scope,
                    scopeBlock => scopeBlock.SchemaUrl,
                    "spans",
                    scopeBlock => scopeBlock.Spans,
                    WriteSpan
                )
        );
    }

    private static void WriteSpan(Utf8JsonWriter writer, ProtoTrace.Span span)
    {
        writer.WriteStartObject();

        // Proto field order: trace_id(1), span_id(2), trace_state(3), parent_span_id(4),
        // flags(5), name(6), kind(7), start_time_unix_nano(8), end_time_unix_nano(9),
        // attributes(10), dropped_attributes_count(11), events(12), dropped_events_count(13),
        // links(14), dropped_links_count(15), status(16)

        WriteHexBytesField(writer, "traceId", span.TraceId);
        WriteHexBytesField(writer, "spanId", span.SpanId);

        if (!string.IsNullOrEmpty(span.TraceState))
        {
            writer.WriteString("traceState", span.TraceState);
        }

        WriteHexBytesField(writer, "parentSpanId", span.ParentSpanId);

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

        WriteTimestamp(writer, "startTimeUnixNano", span.StartTimeUnixNano);
        WriteTimestamp(writer, "endTimeUnixNano", span.EndTimeUnixNano);

        WriteAttributes(writer, span.Attributes, span.DroppedAttributesCount);

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

        WriteTimestamp(writer, "timeUnixNano", evt.TimeUnixNano);

        if (!string.IsNullOrEmpty(evt.Name))
        {
            writer.WriteString("name", evt.Name);
        }

        WriteAttributes(writer, evt.Attributes, evt.DroppedAttributesCount);

        writer.WriteEndObject();
    }

    private static void WriteSpanLink(Utf8JsonWriter writer, ProtoTrace.Span.Types.Link link)
    {
        writer.WriteStartObject();

        WriteHexBytesField(writer, "traceId", link.TraceId);
        WriteHexBytesField(writer, "spanId", link.SpanId);

        if (!string.IsNullOrEmpty(link.TraceState))
        {
            writer.WriteString("traceState", link.TraceState);
        }

        WriteAttributes(writer, link.Attributes, link.DroppedAttributesCount);

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
