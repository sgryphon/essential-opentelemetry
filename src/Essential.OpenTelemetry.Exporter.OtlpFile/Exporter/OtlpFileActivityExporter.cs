using System.Diagnostics;
using Google.Protobuf;
using OpenTelemetry;
using ProtoCommon = OpenTelemetry.Proto.Common.V1;
using ProtoResource = OpenTelemetry.Proto.Resource.V1;
using ProtoTrace = OpenTelemetry.Proto.Trace.V1;

namespace Essential.OpenTelemetry.Exporter;

/// <summary>
/// OTLP file exporter for OpenTelemetry traces.
/// Outputs activity spans in OTLP JSON Protobuf Encoding format compatible with the
/// OpenTelemetry Collector File Exporter and OTLP JSON File Receiver.
/// See <see cref="OtlpJsonSerializer"/> for serialization details.
/// </summary>
public class OtlpFileActivityExporter : BaseExporter<Activity>
{
#if NETSTANDARD2_1_OR_GREATER
    private static readonly long UnixEpochTicks = DateTimeOffset.UnixEpoch.Ticks;
#else
    private static readonly long UnixEpochTicks = new DateTimeOffset(
        1970,
        1,
        1,
        0,
        0,
        0,
        TimeSpan.Zero
    ).Ticks;
#endif

    private readonly OtlpFileOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpFileActivityExporter"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the exporter.</param>
    public OtlpFileActivityExporter(OtlpFileOptions options)
    {
        this.options = options ?? new OtlpFileOptions();
    }

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<Activity> batch)
    {
        var console = this.options.Console;

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

        lock (console.SyncRoot)
        {
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
                        Version =
                            scopeGroup.Value.FirstOrDefault()?.Source?.Version ?? string.Empty,
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

            // Serialize directly to output stream in OTLP JSON Protobuf Encoding
            var stream = console.OpenStandardOutput();
            OtlpJsonSerializer.SerializeTracesData(tracesData, stream);
        }

        return ExportResult.Success;
    }

    private ProtoResource.Resource CreateProtoResource()
    {
        var protoResource = new ProtoResource.Resource();
        var resource = this.ParentProvider?.GetResource();
        if (resource != null)
        {
            foreach (var attribute in resource.Attributes)
            {
                protoResource.Attributes.Add(CreateKeyValue(attribute.Key, attribute.Value));
            }
        }

        return protoResource;
    }

    private static ProtoTrace.Span ConvertToOtlpSpan(Activity activity)
    {
        // Convert timestamps to Unix nanoseconds
        var startTimeUnixNano = DateTimeToUnixNano(activity.StartTimeUtc);
        var endTimeUnixNano = DateTimeToUnixNano(activity.StartTimeUtc + activity.Duration);

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
                TimeUnixNano = DateTimeToUnixNano(activityEvent.Timestamp.UtcDateTime),
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

    private static ProtoCommon.KeyValue CreateKeyValue(string key, object? value)
    {
        var keyValue = new ProtoCommon.KeyValue { Key = key, Value = new ProtoCommon.AnyValue() };

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
            case float f:
                keyValue.Value.DoubleValue = f;
                break;
            case double d:
                keyValue.Value.DoubleValue = d;
                break;
            default:
                // For other types, convert to string
                keyValue.Value.StringValue = value.ToString() ?? string.Empty;
                break;
        }

        return keyValue;
    }

    private static ulong DateTimeToUnixNano(DateTime dateTime)
    {
        var unixTicks = dateTime.ToUniversalTime().Ticks - UnixEpochTicks;
        // Convert ticks to nanoseconds: 1 tick = 100 nanoseconds
        return (ulong)unixTicks * 100;
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
