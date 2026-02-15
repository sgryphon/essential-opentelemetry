using System.Globalization;
using System.Text.Json;
using Google.Protobuf;
using ProtoCommon = OpenTelemetry.Proto.Common.V1;
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
internal static partial class OtlpJsonSerializer
{
    private static readonly byte[] NewLineBytes = new byte[] { (byte)'\n' };

    /// <summary>
    /// Serializes data to an output stream using a template pattern.
    /// Creates a Utf8JsonWriter, invokes the write action, flushes, and writes the trailing newline.
    /// </summary>
    /// <param name="output">The stream to write to.</param>
    /// <param name="writeAction">The action that writes the data using the writer.</param>
    private static void SerializeToStream(Stream output, Action<Utf8JsonWriter> writeAction)
    {
        using (var writer = new Utf8JsonWriter(output, options: default))
        {
            writeAction(writer);
            writer.Flush();
        }

        output.Write(NewLineBytes, 0, NewLineBytes.Length);
        output.Flush();
    }

    /// <summary>
    /// Conditionally writes an attributes array and optional dropped attributes count.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="attributes">The collection of KeyValue attributes to write.</param>
    /// <param name="droppedCount">The count of dropped attributes.</param>
    private static void WriteAttributes(
        Utf8JsonWriter writer,
        IEnumerable<ProtoCommon.KeyValue> attributes,
        uint droppedCount = 0
    )
    {
        var attributesList = attributes as IList<ProtoCommon.KeyValue> ?? attributes.ToList();
        if (attributesList.Count > 0)
        {
            writer.WriteStartArray("attributes");
            foreach (var attr in attributesList)
            {
                WriteKeyValue(writer, attr);
            }

            writer.WriteEndArray();
        }

        if (droppedCount != 0)
        {
            writer.WriteNumber("droppedAttributesCount", droppedCount);
        }
    }

    /// <summary>
    /// Conditionally writes a ByteString field as lowercase hex.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="fieldName">The field name to write.</param>
    /// <param name="bytes">The ByteString value.</param>
    private static void WriteHexBytesField(
        Utf8JsonWriter writer,
        string fieldName,
        ByteString bytes
    )
    {
        if (!bytes.IsEmpty)
        {
            writer.WriteString(fieldName, ByteStringToHexLower(bytes));
        }
    }

    /// <summary>
    /// Conditionally writes a ulong nanosecond timestamp as a string.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="fieldName">The field name to write.</param>
    /// <param name="timestampNano">The timestamp in nanoseconds.</param>
    private static void WriteTimestamp(Utf8JsonWriter writer, string fieldName, ulong timestampNano)
    {
        if (timestampNano != 0)
        {
            writer.WriteString(fieldName, timestampNano.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void WriteResource(Utf8JsonWriter writer, ProtoResource.Resource resource)
    {
        writer.WriteStartObject();
        WriteAttributes(writer, resource.Attributes);
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
