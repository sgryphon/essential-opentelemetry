using System.Diagnostics;
using Google.Protobuf;
using OpenTelemetry;
using ProtoCommon = OpenTelemetry.Proto.Common.V1;
using ProtoTrace = OpenTelemetry.Proto.Trace.V1;

namespace Essential.OpenTelemetry.Exporter;

/// <summary>
/// OTLP file exporter for OpenTelemetry traces.
/// Outputs activity spans in OTLP JSON Protobuf Encoding format compatible with the
/// OpenTelemetry Collector File Exporter and OTLP JSON File Receiver.
/// See <see cref="OtlpJsonSerializer"/> for serialization details.
/// </summary>
public class OtlpFileActivityExporter : OtlpFileExporter<Activity>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpFileActivityExporter"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the exporter.</param>
    public OtlpFileActivityExporter(OtlpFileOptions options)
        : base(options) { }

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<Activity> batch)
    {
        // Group activities by instrumentation scope
        var scopeGroups = new Dictionary<string, List<Activity>>();

        foreach (var activity in batch)
        {
            var scopeName = activity.Source?.Name ?? string.Empty;
            if (!scopeGroups.ContainsKey(scopeName))
            {
                scopeGroups[scopeName] = new List<Activity>();
            }
            scopeGroups[scopeName].Add(activity);
        }

        // Create OTLP TracesData message
        var tracesData = new ProtoTrace.TracesData();
        var resourceSpans = new ProtoTrace.ResourceSpans { Resource = CreateProtoResource() };

        // Add scope spans for each instrumentation scope
        foreach (var scopeGroup in scopeGroups)
        {
            var scopeSpans = new ProtoTrace.ScopeSpans
            {
                Scope = new ProtoCommon.InstrumentationScope
                {
                    Name = scopeGroup.Key,
                    Version = scopeGroup.Value.FirstOrDefault()?.Source?.Version ?? string.Empty,
                },
            };

            // Convert each Activity to OTLP proto Span
            foreach (var activity in scopeGroup.Value)
            {
                var protoSpan = ConvertToOtlpSpan(activity);
                scopeSpans.Spans.Add(protoSpan);
            }

            resourceSpans.ScopeSpans.Add(scopeSpans);
        }

        tracesData.ResourceSpans.Add(resourceSpans);

        var console = this.Options.Console;
        lock (console.SyncRoot)
        {
            // Serialize directly to output stream in OTLP JSON Protobuf Encoding
            var stream = console.OpenStandardOutput();
            OtlpJsonSerializer.SerializeTracesData(tracesData, stream);
        }

        return ExportResult.Success;
    }

    private static ProtoTrace.Span ConvertToOtlpSpan(Activity activity)
    {
        // Convert timestamps to Unix nanoseconds
        var startTimeUnixNano = DateTimeOffsetToUnixNano(activity.StartTimeUtc);
        var endTimeUnixNano = DateTimeOffsetToUnixNano(activity.StartTimeUtc + activity.Duration);

        var protoSpan = new ProtoTrace.Span
        {
            Name = activity.DisplayName ?? activity.OperationName,
            StartTimeUnixNano = startTimeUnixNano,
            EndTimeUnixNano = endTimeUnixNano,
            Kind = ConvertActivityKindToSpanKind(activity.Kind),
        };

        // Set trace ID
        if (activity.TraceId != default)
        {
            Span<byte> traceIdBytes = stackalloc byte[16];
            activity.TraceId.CopyTo(traceIdBytes);
            protoSpan.TraceId = ByteString.CopyFrom(traceIdBytes);
        }

        // Set span ID
        if (activity.SpanId != default)
        {
            Span<byte> spanIdBytes = stackalloc byte[8];
            activity.SpanId.CopyTo(spanIdBytes);
            protoSpan.SpanId = ByteString.CopyFrom(spanIdBytes);
        }

        // Set parent span ID
        if (activity.ParentSpanId != default)
        {
            Span<byte> parentSpanIdBytes = stackalloc byte[8];
            activity.ParentSpanId.CopyTo(parentSpanIdBytes);
            protoSpan.ParentSpanId = ByteString.CopyFrom(parentSpanIdBytes);
        }

        // Set trace flags
        if (activity.ActivityTraceFlags != ActivityTraceFlags.None)
        {
            protoSpan.Flags = (uint)activity.ActivityTraceFlags;
        }

        // Set trace state
        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            protoSpan.TraceState = activity.TraceStateString;
        }

        // Add attributes
        foreach (var tag in activity.TagObjects)
        {
            protoSpan.Attributes.Add(CreateKeyValue(tag.Key, tag.Value));
        }

        // Add events
        foreach (var activityEvent in activity.Events)
        {
            var protoEvent = new ProtoTrace.Span.Types.Event
            {
                Name = activityEvent.Name,
                TimeUnixNano = DateTimeOffsetToUnixNano(activityEvent.Timestamp.UtcDateTime),
            };

            foreach (var tag in activityEvent.Tags)
            {
                protoEvent.Attributes.Add(CreateKeyValue(tag.Key, tag.Value));
            }

            protoSpan.Events.Add(protoEvent);
        }

        // Add links
        Span<byte> linkTraceIdBytes = stackalloc byte[16];
        Span<byte> linkSpanIdBytes = stackalloc byte[8];
        foreach (var activityLink in activity.Links)
        {
            var protoLink = new ProtoTrace.Span.Types.Link();

            if (activityLink.Context.TraceId != default)
            {
                activityLink.Context.TraceId.CopyTo(linkTraceIdBytes);
                protoLink.TraceId = ByteString.CopyFrom(linkTraceIdBytes);
            }

            if (activityLink.Context.SpanId != default)
            {
                activityLink.Context.SpanId.CopyTo(linkSpanIdBytes);
                protoLink.SpanId = ByteString.CopyFrom(linkSpanIdBytes);
            }

            if (activityLink.Context.TraceFlags != ActivityTraceFlags.None)
            {
                protoLink.Flags = (uint)activityLink.Context.TraceFlags;
            }

            if (!string.IsNullOrEmpty(activityLink.Context.TraceState))
            {
                protoLink.TraceState = activityLink.Context.TraceState;
            }

            if (activityLink.Tags != null)
            {
                foreach (var tag in activityLink.Tags)
                {
                    protoLink.Attributes.Add(CreateKeyValue(tag.Key, tag.Value));
                }
            }

            protoSpan.Links.Add(protoLink);
        }

        // Set status
        if (activity.Status != ActivityStatusCode.Unset)
        {
            protoSpan.Status = new ProtoTrace.Status
            {
                Code = ConvertActivityStatusToStatusCode(activity.Status),
                Message = activity.StatusDescription ?? string.Empty,
            };
        }

        return protoSpan;
    }

    private static ProtoTrace.Span.Types.SpanKind ConvertActivityKindToSpanKind(ActivityKind kind)
    {
        return kind switch
        {
            ActivityKind.Internal => ProtoTrace.Span.Types.SpanKind.Internal,
            ActivityKind.Server => ProtoTrace.Span.Types.SpanKind.Server,
            ActivityKind.Client => ProtoTrace.Span.Types.SpanKind.Client,
            ActivityKind.Producer => ProtoTrace.Span.Types.SpanKind.Producer,
            ActivityKind.Consumer => ProtoTrace.Span.Types.SpanKind.Consumer,
            _ => ProtoTrace.Span.Types.SpanKind.Unspecified,
        };
    }

    private static ProtoTrace.Status.Types.StatusCode ConvertActivityStatusToStatusCode(
        ActivityStatusCode statusCode
    )
    {
        return statusCode switch
        {
            ActivityStatusCode.Ok => ProtoTrace.Status.Types.StatusCode.Ok,
            ActivityStatusCode.Error => ProtoTrace.Status.Types.StatusCode.Error,
            _ => ProtoTrace.Status.Types.StatusCode.Unset,
        };
    }
}
