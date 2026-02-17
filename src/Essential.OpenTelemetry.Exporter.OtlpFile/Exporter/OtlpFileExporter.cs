using System.Globalization;
using OpenTelemetry;
using ProtoCommon = OpenTelemetry.Proto.Common.V1;
using ProtoResource = OpenTelemetry.Proto.Resource.V1;

namespace Essential.OpenTelemetry.Exporter;

/// <summary>
/// Base class for OTLP file exporters.
/// Provides common functionality for exporting OpenTelemetry data in OTLP JSON Protobuf Encoding format.
/// </summary>
/// <typeparam name="T">The type of telemetry data to export.</typeparam>
public abstract class OtlpFileExporter<T> : BaseExporter<T>
    where T : class
{
    /// <summary>
    /// The number of ticks representing the Unix epoch (January 1, 1970 00:00:00 UTC).
    /// </summary>
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

    private const long NanosecondsPerTick = 1_000_000 / TimeSpan.TicksPerMillisecond;

    /// <summary>
    /// Gets the configuration options for the exporter.
    /// </summary>
    protected OtlpFileOptions Options { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpFileExporter{T}"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the exporter.</param>
    protected OtlpFileExporter(OtlpFileOptions options)
    {
        this.Options = options ?? new OtlpFileOptions();
    }

    /// <summary>
    /// Creates a protobuf Resource message from the OpenTelemetry resource.
    /// </summary>
    /// <returns>A protobuf Resource message containing the resource attributes.</returns>
    protected ProtoResource.Resource CreateProtoResource()
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

    /// <summary>
    /// Creates a protobuf KeyValue message from a key and value.
    /// </summary>
    /// <param name="key">The attribute key.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>A protobuf KeyValue message.</returns>
    protected static ProtoCommon.KeyValue CreateKeyValue(string key, object? value)
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

    /// <summary>
    /// Converts a DateTimeOffset to Unix nanoseconds.
    /// </summary>
    /// <param name="dateTimeOffset">The DateTimeOffset to convert.</param>
    /// <returns>The Unix timestamp in nanoseconds.</returns>
    protected static ulong DateTimeOffsetToUnixNano(DateTimeOffset dateTimeOffset)
    {
        var unixTicks = dateTimeOffset.ToUniversalTime().Ticks - UnixEpochTicks;
        return (ulong)unixTicks * NanosecondsPerTick;
    }
}
