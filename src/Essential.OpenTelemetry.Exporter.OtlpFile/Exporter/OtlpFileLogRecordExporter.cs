using System.Globalization;
using System.Reflection;
using Google.Protobuf;
using OpenTelemetry;
using OpenTelemetry.Resources;
using ProtoCommon = OpenTelemetry.Proto.Common.V1;
using ProtoLogs = OpenTelemetry.Proto.Logs.V1;
using ProtoResource = OpenTelemetry.Proto.Resource.V1;
using SdkLogs = OpenTelemetry.Logs;

namespace Essential.OpenTelemetry.Exporter;

/// <summary>
/// OTLP file exporter for OpenTelemetry logs.
/// Outputs log records in OTLP JSON Protobuf Encoding format compatible with the
/// OpenTelemetry Collector File Exporter and OTLP JSON File Receiver.
/// See <see cref="OtlpJsonSerializer"/> for serialization details.
/// </summary>
public class OtlpFileLogRecordExporter : BaseExporter<SdkLogs.LogRecord>
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

    private static readonly PropertyInfo? SeverityProperty = typeof(SdkLogs.LogRecord).GetProperty(
        "Severity",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
    );

    private readonly OtlpFileOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpFileLogRecordExporter"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the exporter.</param>
    public OtlpFileLogRecordExporter(OtlpFileOptions options)
    {
        this.options = options ?? new OtlpFileOptions();
    }

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<SdkLogs.LogRecord> batch)
    {
        var output = this.options.Output;

        // Group log records by scope (category)
        var scopeGroups = new Dictionary<string, List<SdkLogs.LogRecord>>();

        foreach (var sdkLogRecord in batch)
        {
            var categoryName = sdkLogRecord.CategoryName ?? string.Empty;
            if (!scopeGroups.ContainsKey(categoryName))
            {
                scopeGroups[categoryName] = new List<SdkLogs.LogRecord>();
            }
            scopeGroups[categoryName].Add(sdkLogRecord);
        }

        lock (output.SyncRoot)
        {
            // Create OTLP LogsData message
            var logsData = new ProtoLogs.LogsData();
            var resourceLogs = new ProtoLogs.ResourceLogs { Resource = CreateProtoResource() };

            // Add scope logs for each category
            foreach (var scopeGroup in scopeGroups)
            {
                var scopeLogs = new ProtoLogs.ScopeLogs
                {
                    Scope = new ProtoCommon.InstrumentationScope { Name = scopeGroup.Key },
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

            // Serialize to OTLP JSON Protobuf Encoding using custom Utf8JsonWriter
            var jsonLine = OtlpJsonSerializer.SerializeLogsData(logsData);
            output.WriteLine(jsonLine);
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

    private static ProtoLogs.LogRecord ConvertToOtlpLogRecord(SdkLogs.LogRecord sdkLogRecord)
    {
        // Convert DateTimeOffset to Unix nanoseconds
        var unixTicks =
            ((DateTimeOffset)sdkLogRecord.Timestamp).ToUniversalTime().Ticks - UnixEpochTicks;
        var timestampUnixNano = (ulong)unixTicks * (1_000_000 / TimeSpan.TicksPerMillisecond);

        var protoLogRecord = new ProtoLogs.LogRecord
        {
            TimeUnixNano = timestampUnixNano,
            ObservedTimeUnixNano = timestampUnixNano,
        };

        // Set severity
        var severityValue = SeverityProperty?.GetValue(sdkLogRecord);
        if (severityValue != null)
        {
            var severityInt = (int)severityValue;
            protoLogRecord.SeverityNumber = (ProtoLogs.SeverityNumber)severityInt;
            protoLogRecord.SeverityText = GetSeverityText(severityInt);
        }

        // Set body
        var body = GetBody(sdkLogRecord);
        if (!string.IsNullOrEmpty(body))
        {
            protoLogRecord.Body = new ProtoCommon.AnyValue { StringValue = body };
        }

        // Set event name if available
        if (!string.IsNullOrEmpty(sdkLogRecord.EventId.Name))
        {
            protoLogRecord.EventName = sdkLogRecord.EventId.Name;
        }

        // Add event ID as attribute if present
        if (sdkLogRecord.EventId.Id != 0)
        {
            protoLogRecord.Attributes.Add(CreateKeyValue("event.id", sdkLogRecord.EventId.Id));
        }

        // Add attributes from the log record
        if (sdkLogRecord.Attributes != null)
        {
            foreach (var attribute in sdkLogRecord.Attributes)
            {
                protoLogRecord.Attributes.Add(CreateKeyValue(attribute.Key, attribute.Value));
            }
        }

        // Add exception attributes if present
        if (sdkLogRecord.Exception != null)
        {
            protoLogRecord.Attributes.Add(
                CreateKeyValue("exception.type", sdkLogRecord.Exception.GetType().FullName)
            );
            protoLogRecord.Attributes.Add(
                CreateKeyValue("exception.message", sdkLogRecord.Exception.Message)
            );
            protoLogRecord.Attributes.Add(
                CreateKeyValue("exception.stacktrace", sdkLogRecord.Exception.ToString())
            );
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

        return protoLogRecord;
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

    private static string GetBody(SdkLogs.LogRecord sdkLogRecord)
    {
        // For OTLP format, prefer the Body property which contains the original
        // message template/format string. Parameter values are in attributes.
        var message = sdkLogRecord.Body;

        // Fall back to FormattedMessage if Body is not available
        if (string.IsNullOrEmpty(message))
        {
            message = sdkLogRecord.FormattedMessage;
        }

        // Fall back to State.ToString() as last resort
        if (string.IsNullOrEmpty(message))
        {
#pragma warning disable CS0618 // Type or member is obsolete
            message = sdkLogRecord.State?.ToString();
#pragma warning restore CS0618 // Type or member is obsolete
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
