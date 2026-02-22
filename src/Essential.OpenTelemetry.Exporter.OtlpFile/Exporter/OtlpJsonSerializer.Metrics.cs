using System.Text.Json;
using OpenTelemetry.Proto.Metrics.V1;
using ProtoMetrics = OpenTelemetry.Proto.Metrics.V1;

namespace Essential.OpenTelemetry.Exporter;

internal static partial class OtlpJsonSerializer
{
    /// <summary>
    /// Serializes the given <see cref="ProtoMetrics.MetricsData"/> to the specified output stream
    /// in OTLP JSON Protobuf Encoding format, followed by a newline.
    /// </summary>
    /// <param name="metricsData">The metrics data to serialize.</param>
    /// <param name="output">The stream to write the JSON output to.</param>
    internal static void SerializeMetricsData(ProtoMetrics.MetricsData metricsData, Stream output)
    {
        SerializeToStream(
            output,
            writer =>
                WriteOtlpData<MetricsData, ResourceMetrics, ScopeMetrics, Metric>(
                    writer,
                    metricsData,
                    "resourceMetrics",
                    data => data.ResourceMetrics,
                    resourceBlock => resourceBlock.Resource,
                    resourceBlock => resourceBlock.SchemaUrl,
                    "scopeMetrics",
                    resourceBlock => resourceBlock.ScopeMetrics,
                    scopeBlock => scopeBlock.Scope,
                    scopeBlock => scopeBlock.SchemaUrl,
                    "metrics",
                    scopeBlock => scopeBlock.Metrics,
                    WriteMetric
                )
        );
    }

    private static void WriteMetric(Utf8JsonWriter writer, ProtoMetrics.Metric metric)
    {
        writer.WriteStartObject();

        // Proto field order: name(1), description(2), unit(3), data(gauge/sum/histogram/etc)

        if (!string.IsNullOrEmpty(metric.Name))
        {
            writer.WriteString("name", metric.Name);
        }

        if (!string.IsNullOrEmpty(metric.Description))
        {
            writer.WriteString("description", metric.Description);
        }

        if (!string.IsNullOrEmpty(metric.Unit))
        {
            writer.WriteString("unit", metric.Unit);
        }

        // Write metric data based on type
        switch (metric.DataCase)
        {
            case ProtoMetrics.Metric.DataOneofCase.Gauge:
                writer.WritePropertyName("gauge");
                WriteGauge(writer, metric.Gauge);
                break;
            case ProtoMetrics.Metric.DataOneofCase.Sum:
                writer.WritePropertyName("sum");
                WriteSum(writer, metric.Sum);
                break;
            case ProtoMetrics.Metric.DataOneofCase.Histogram:
                writer.WritePropertyName("histogram");
                WriteHistogram(writer, metric.Histogram);
                break;
            case ProtoMetrics.Metric.DataOneofCase.ExponentialHistogram:
                writer.WritePropertyName("exponentialHistogram");
                WriteExponentialHistogram(writer, metric.ExponentialHistogram);
                break;
            case ProtoMetrics.Metric.DataOneofCase.Summary:
                writer.WritePropertyName("summary");
                WriteSummary(writer, metric.Summary);
                break;
        }

        // Write metadata if present
        if (metric.Metadata.Count > 0)
        {
            writer.WriteStartArray("metadata");
            foreach (var kv in metric.Metadata)
            {
                WriteKeyValue(writer, kv);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteGauge(Utf8JsonWriter writer, ProtoMetrics.Gauge gauge)
    {
        writer.WriteStartObject();

        if (gauge.DataPoints.Count > 0)
        {
            writer.WriteStartArray("dataPoints");
            foreach (var dataPoint in gauge.DataPoints)
            {
                WriteNumberDataPoint(writer, dataPoint);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteSum(Utf8JsonWriter writer, ProtoMetrics.Sum sum)
    {
        writer.WriteStartObject();

        if (sum.DataPoints.Count > 0)
        {
            writer.WriteStartArray("dataPoints");
            foreach (var dataPoint in sum.DataPoints)
            {
                WriteNumberDataPoint(writer, dataPoint);
            }

            writer.WriteEndArray();
        }

        if (sum.AggregationTemporality != ProtoMetrics.AggregationTemporality.Unspecified)
        {
            // Enum → integer per OTLP JSON Protobuf Encoding
            writer.WriteNumber("aggregationTemporality", (int)sum.AggregationTemporality);
        }

        if (sum.IsMonotonic)
        {
            writer.WriteBoolean("isMonotonic", sum.IsMonotonic);
        }

        writer.WriteEndObject();
    }

    private static void WriteHistogram(Utf8JsonWriter writer, ProtoMetrics.Histogram histogram)
    {
        writer.WriteStartObject();

        if (histogram.DataPoints.Count > 0)
        {
            writer.WriteStartArray("dataPoints");
            foreach (var dataPoint in histogram.DataPoints)
            {
                WriteHistogramDataPoint(writer, dataPoint);
            }

            writer.WriteEndArray();
        }

        if (histogram.AggregationTemporality != ProtoMetrics.AggregationTemporality.Unspecified)
        {
            // Enum → integer per OTLP JSON Protobuf Encoding
            writer.WriteNumber("aggregationTemporality", (int)histogram.AggregationTemporality);
        }

        writer.WriteEndObject();
    }

    private static void WriteExponentialHistogram(
        Utf8JsonWriter writer,
        ProtoMetrics.ExponentialHistogram exponentialHistogram
    )
    {
        writer.WriteStartObject();

        if (exponentialHistogram.DataPoints.Count > 0)
        {
            writer.WriteStartArray("dataPoints");
            foreach (var dataPoint in exponentialHistogram.DataPoints)
            {
                WriteExponentialHistogramDataPoint(writer, dataPoint);
            }

            writer.WriteEndArray();
        }

        if (
            exponentialHistogram.AggregationTemporality
            != ProtoMetrics.AggregationTemporality.Unspecified
        )
        {
            // Enum → integer per OTLP JSON Protobuf Encoding
            writer.WriteNumber(
                "aggregationTemporality",
                (int)exponentialHistogram.AggregationTemporality
            );
        }

        writer.WriteEndObject();
    }

    private static void WriteSummary(Utf8JsonWriter writer, ProtoMetrics.Summary summary)
    {
        writer.WriteStartObject();

        if (summary.DataPoints.Count > 0)
        {
            writer.WriteStartArray("dataPoints");
            foreach (var dataPoint in summary.DataPoints)
            {
                WriteSummaryDataPoint(writer, dataPoint);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteNumberDataPoint(
        Utf8JsonWriter writer,
        ProtoMetrics.NumberDataPoint dataPoint
    )
    {
        writer.WriteStartObject();

        WriteAttributes(writer, dataPoint.Attributes);
        WriteTimestamp(writer, "startTimeUnixNano", dataPoint.StartTimeUnixNano);
        WriteTimestamp(writer, "timeUnixNano", dataPoint.TimeUnixNano);

        // Write value based on type
        switch (dataPoint.ValueCase)
        {
            case ProtoMetrics.NumberDataPoint.ValueOneofCase.AsDouble:
                writer.WriteNumber("asDouble", dataPoint.AsDouble);
                break;
            case ProtoMetrics.NumberDataPoint.ValueOneofCase.AsInt:
                // int64 → string in JSON per protobuf convention
                writer.WriteString("asInt", dataPoint.AsInt.ToString());
                break;
        }

        if (dataPoint.Exemplars.Count > 0)
        {
            writer.WriteStartArray("exemplars");
            foreach (var exemplar in dataPoint.Exemplars)
            {
                WriteExemplar(writer, exemplar);
            }

            writer.WriteEndArray();
        }

        if (dataPoint.Flags != 0)
        {
            writer.WriteNumber("flags", dataPoint.Flags);
        }

        writer.WriteEndObject();
    }

    private static void WriteHistogramDataPoint(
        Utf8JsonWriter writer,
        ProtoMetrics.HistogramDataPoint dataPoint
    )
    {
        writer.WriteStartObject();

        WriteAttributes(writer, dataPoint.Attributes);
        WriteTimestamp(writer, "startTimeUnixNano", dataPoint.StartTimeUnixNano);
        WriteTimestamp(writer, "timeUnixNano", dataPoint.TimeUnixNano);

        if (dataPoint.Count != 0)
        {
            // uint64 → string in JSON per protobuf convention
            writer.WriteString("count", dataPoint.Count.ToString());
        }

        if (dataPoint.Sum != 0)
        {
            writer.WriteNumber("sum", dataPoint.Sum);
        }

        if (dataPoint.BucketCounts.Count > 0)
        {
            writer.WriteStartArray("bucketCounts");
            foreach (var count in dataPoint.BucketCounts)
            {
                // uint64 → string in JSON per protobuf convention
                writer.WriteStringValue(count.ToString());
            }

            writer.WriteEndArray();
        }

        if (dataPoint.ExplicitBounds.Count > 0)
        {
            writer.WriteStartArray("explicitBounds");
            foreach (var bound in dataPoint.ExplicitBounds)
            {
                writer.WriteNumberValue(bound);
            }

            writer.WriteEndArray();
        }

        if (dataPoint.Exemplars.Count > 0)
        {
            writer.WriteStartArray("exemplars");
            foreach (var exemplar in dataPoint.Exemplars)
            {
                WriteExemplar(writer, exemplar);
            }

            writer.WriteEndArray();
        }

        if (dataPoint.Flags != 0)
        {
            writer.WriteNumber("flags", dataPoint.Flags);
        }

        if (dataPoint.Min != 0)
        {
            writer.WriteNumber("min", dataPoint.Min);
        }

        if (dataPoint.Max != 0)
        {
            writer.WriteNumber("max", dataPoint.Max);
        }

        writer.WriteEndObject();
    }

    private static void WriteExponentialHistogramDataPoint(
        Utf8JsonWriter writer,
        ProtoMetrics.ExponentialHistogramDataPoint dataPoint
    )
    {
        writer.WriteStartObject();

        WriteAttributes(writer, dataPoint.Attributes);
        WriteTimestamp(writer, "startTimeUnixNano", dataPoint.StartTimeUnixNano);
        WriteTimestamp(writer, "timeUnixNano", dataPoint.TimeUnixNano);

        if (dataPoint.Count != 0)
        {
            // uint64 → string in JSON per protobuf convention
            writer.WriteString("count", dataPoint.Count.ToString());
        }

        if (dataPoint.Sum != 0)
        {
            writer.WriteNumber("sum", dataPoint.Sum);
        }

        if (dataPoint.Scale != 0)
        {
            writer.WriteNumber("scale", dataPoint.Scale);
        }

        if (dataPoint.ZeroCount != 0)
        {
            // uint64 → string in JSON per protobuf convention
            writer.WriteString("zeroCount", dataPoint.ZeroCount.ToString());
        }

        if (dataPoint.Positive != null && dataPoint.Positive.BucketCounts.Count > 0)
        {
            writer.WritePropertyName("positive");
            WriteExponentialHistogramBuckets(writer, dataPoint.Positive);
        }

        if (dataPoint.Negative != null && dataPoint.Negative.BucketCounts.Count > 0)
        {
            writer.WritePropertyName("negative");
            WriteExponentialHistogramBuckets(writer, dataPoint.Negative);
        }

        if (dataPoint.Flags != 0)
        {
            writer.WriteNumber("flags", dataPoint.Flags);
        }

        if (dataPoint.Exemplars.Count > 0)
        {
            writer.WriteStartArray("exemplars");
            foreach (var exemplar in dataPoint.Exemplars)
            {
                WriteExemplar(writer, exemplar);
            }

            writer.WriteEndArray();
        }

        if (dataPoint.Min != 0)
        {
            writer.WriteNumber("min", dataPoint.Min);
        }

        if (dataPoint.Max != 0)
        {
            writer.WriteNumber("max", dataPoint.Max);
        }

        if (dataPoint.ZeroThreshold != 0)
        {
            writer.WriteNumber("zeroThreshold", dataPoint.ZeroThreshold);
        }

        writer.WriteEndObject();
    }

    private static void WriteExponentialHistogramBuckets(
        Utf8JsonWriter writer,
        ProtoMetrics.ExponentialHistogramDataPoint.Types.Buckets buckets
    )
    {
        writer.WriteStartObject();

        if (buckets.Offset != 0)
        {
            writer.WriteNumber("offset", buckets.Offset);
        }

        if (buckets.BucketCounts.Count > 0)
        {
            writer.WriteStartArray("bucketCounts");
            foreach (var count in buckets.BucketCounts)
            {
                // uint64 → string in JSON per protobuf convention
                writer.WriteStringValue(count.ToString());
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void WriteSummaryDataPoint(
        Utf8JsonWriter writer,
        ProtoMetrics.SummaryDataPoint dataPoint
    )
    {
        writer.WriteStartObject();

        WriteAttributes(writer, dataPoint.Attributes);
        WriteTimestamp(writer, "startTimeUnixNano", dataPoint.StartTimeUnixNano);
        WriteTimestamp(writer, "timeUnixNano", dataPoint.TimeUnixNano);

        if (dataPoint.Count != 0)
        {
            // uint64 → string in JSON per protobuf convention
            writer.WriteString("count", dataPoint.Count.ToString());
        }

        if (dataPoint.Sum != 0)
        {
            writer.WriteNumber("sum", dataPoint.Sum);
        }

        if (dataPoint.QuantileValues.Count > 0)
        {
            writer.WriteStartArray("quantileValues");
            foreach (var quantileValue in dataPoint.QuantileValues)
            {
                WriteSummaryDataPointValueAtQuantile(writer, quantileValue);
            }

            writer.WriteEndArray();
        }

        if (dataPoint.Flags != 0)
        {
            writer.WriteNumber("flags", dataPoint.Flags);
        }

        writer.WriteEndObject();
    }

    private static void WriteSummaryDataPointValueAtQuantile(
        Utf8JsonWriter writer,
        ProtoMetrics.SummaryDataPoint.Types.ValueAtQuantile valueAtQuantile
    )
    {
        writer.WriteStartObject();

        if (valueAtQuantile.Quantile != 0)
        {
            writer.WriteNumber("quantile", valueAtQuantile.Quantile);
        }

        if (valueAtQuantile.Value != 0)
        {
            writer.WriteNumber("value", valueAtQuantile.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteExemplar(Utf8JsonWriter writer, ProtoMetrics.Exemplar exemplar)
    {
        writer.WriteStartObject();

        if (exemplar.FilteredAttributes.Count > 0)
        {
            writer.WriteStartArray("filteredAttributes");
            foreach (var attr in exemplar.FilteredAttributes)
            {
                WriteKeyValue(writer, attr);
            }

            writer.WriteEndArray();
        }

        WriteTimestamp(writer, "timeUnixNano", exemplar.TimeUnixNano);

        // Write value based on type
        switch (exemplar.ValueCase)
        {
            case ProtoMetrics.Exemplar.ValueOneofCase.AsDouble:
                writer.WriteNumber("asDouble", exemplar.AsDouble);
                break;
            case ProtoMetrics.Exemplar.ValueOneofCase.AsInt:
                // int64 → string in JSON per protobuf convention
                writer.WriteString("asInt", exemplar.AsInt.ToString());
                break;
        }

        WriteHexBytesField(writer, "spanId", exemplar.SpanId);
        WriteHexBytesField(writer, "traceId", exemplar.TraceId);

        writer.WriteEndObject();
    }
}
