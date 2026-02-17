using System.Reflection;
using Google.Protobuf;
using OpenTelemetry;
using ProtoCommon = OpenTelemetry.Proto.Common.V1;
using ProtoLogs = OpenTelemetry.Proto.Logs.V1;
using SdkLogs = OpenTelemetry.Logs;

namespace Essential.OpenTelemetry.Exporter;

/// <summary>
/// OTLP file exporter for OpenTelemetry logs.
/// Outputs log records in OTLP JSON Protobuf Encoding format compatible with the
/// OpenTelemetry Collector File Exporter and OTLP JSON File Receiver.
/// See <see cref="OtlpJsonSerializer"/> for serialization details.
/// </summary>
public class OtlpFileLogRecordExporter : OtlpFileExporter<SdkLogs.LogRecord>
{
    private static readonly PropertyInfo? SeverityProperty = typeof(SdkLogs.LogRecord).GetProperty(
        "Severity",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpFileLogRecordExporter"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the exporter.</param>
    public OtlpFileLogRecordExporter(OtlpFileOptions options)
        : base(options) { }

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<SdkLogs.LogRecord> batch)
    {
        var console = this.Options.Console;

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

        lock (console.SyncRoot)
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

            // Serialize directly to output stream in OTLP JSON Protobuf Encoding
            var stream = console.OpenStandardOutput();
            OtlpJsonSerializer.SerializeLogsData(logsData, stream);
        }

        return ExportResult.Success;
    }

    private static ProtoLogs.LogRecord ConvertToOtlpLogRecord(SdkLogs.LogRecord sdkLogRecord)
    {
        var timestampUnixNano = DateTimeOffsetToUnixNano(sdkLogRecord.Timestamp);

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
        }

        // Original string representation of the severity as it is known at the source
        protoLogRecord.SeverityText = sdkLogRecord.LogLevel.ToString();

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

        // Add scope attributes if present
        // When IncludeScopes is enabled, scope values are added as regular attributes,
        // matching the OpenTelemetry Collector file exporter behavior.
        sdkLogRecord.ForEachScope(ProcessScope, protoLogRecord);

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

    private static void ProcessScope(
        SdkLogs.LogRecordScope scope,
        ProtoLogs.LogRecord protoLogRecord
    )
    {
        foreach (var attribute in scope)
        {
            // Skip the format string scope entries (e.g. "{OriginalFormat}" from BeginScope)
            if (attribute.Key == "{OriginalFormat}")
            {
                continue;
            }

            protoLogRecord.Attributes.Add(CreateKeyValue(attribute.Key, attribute.Value));
        }
    }

    private static string GetBody(SdkLogs.LogRecord sdkLogRecord)
    {
        // If options.IncludeFormattedMessage is set,
        // then we want to send the formatted message as Body
        // (and OriginalFormat will be an attribute)
        var message = sdkLogRecord.FormattedMessage;

        // Fall back to Body if FormattedMessage is not available
        if (string.IsNullOrEmpty(message))
        {
            message = sdkLogRecord.Body;
        }

        return message ?? string.Empty;
    }
}
