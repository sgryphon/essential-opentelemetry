using Google.Protobuf;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using ProtoCommon = OpenTelemetry.Proto.Common.V1;
using ProtoMetrics = OpenTelemetry.Proto.Metrics.V1;

namespace Essential.OpenTelemetry.Exporter;

/// <summary>
/// OTLP file exporter for OpenTelemetry metrics.
/// Outputs metrics in OTLP JSON Protobuf Encoding format compatible with the
/// OpenTelemetry Collector File Exporter and OTLP JSON File Receiver.
/// See <see cref="OtlpJsonSerializer"/> for serialization details.
/// </summary>
public class OtlpFileMetricExporter : OtlpFileExporter<Metric>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpFileMetricExporter"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the exporter.</param>
    public OtlpFileMetricExporter(OtlpFileOptions options)
        : base(options) { }

    /// <inheritdoc/>
    public override ExportResult Export(in Batch<Metric> batch)
    {
        var console = this.Options.Console;

        // Group metrics by instrumentation scope
        var scopeGroups = new Dictionary<string, List<Metric>>();

        foreach (var metric in batch)
        {
            var scopeName = metric.MeterName ?? string.Empty;
            if (!scopeGroups.ContainsKey(scopeName))
            {
                scopeGroups[scopeName] = new List<Metric>();
            }
            scopeGroups[scopeName].Add(metric);
        }

        lock (console.SyncRoot)
        {
            // Create OTLP MetricsData message
            var metricsData = new ProtoMetrics.MetricsData();
            var resourceMetrics = new ProtoMetrics.ResourceMetrics
            {
                Resource = CreateProtoResource(),
            };

            // Add scope metrics for each instrumentation scope
            foreach (var scopeGroup in scopeGroups)
            {
                var scopeMetrics = new ProtoMetrics.ScopeMetrics
                {
                    Scope = new ProtoCommon.InstrumentationScope
                    {
                        Name = scopeGroup.Key,
                        Version = scopeGroup.Value.FirstOrDefault()?.MeterVersion ?? string.Empty,
                    },
                };

                // Convert each Metric to OTLP proto Metric
                foreach (var metric in scopeGroup.Value)
                {
                    var protoMetrics = ConvertToOtlpMetrics(metric);
                    foreach (var protoMetric in protoMetrics)
                    {
                        scopeMetrics.Metrics.Add(protoMetric);
                    }
                }

                resourceMetrics.ScopeMetrics.Add(scopeMetrics);
            }

            metricsData.ResourceMetrics.Add(resourceMetrics);

            // Serialize directly to output stream in OTLP JSON Protobuf Encoding
            var stream = console.OpenStandardOutput();
            OtlpJsonSerializer.SerializeMetricsData(metricsData, stream);
        }

        return ExportResult.Success;
    }

    private static IEnumerable<ProtoMetrics.Metric> ConvertToOtlpMetrics(Metric metric)
    {
        // In OpenTelemetry SDK, a Metric may have multiple metric points with different tag sets.
        // We need to group them into a single OTLP Metric message.
        var protoMetric = new ProtoMetrics.Metric
        {
            Name = metric.Name,
            Description = metric.Description ?? string.Empty,
            Unit = metric.Unit ?? string.Empty,
        };

        // Determine metric type and convert data points
        switch (metric.MetricType)
        {
            case MetricType.LongGauge:
            case MetricType.DoubleGauge:
                ConvertGauge(metric, protoMetric);
                break;
            case MetricType.LongSum:
            case MetricType.DoubleSum:
                ConvertSum(metric, protoMetric);
                break;
            case MetricType.Histogram:
                ConvertHistogram(metric, protoMetric);
                break;
            case MetricType.ExponentialHistogram:
                ConvertExponentialHistogram(metric, protoMetric);
                break;
            default:
                // Unknown metric type, skip
                yield break;
        }

        yield return protoMetric;
    }

    private static void ConvertGauge(Metric metric, ProtoMetrics.Metric protoMetric)
    {
        var gauge = new ProtoMetrics.Gauge();

        foreach (var metricPoint in metric.GetMetricPoints())
        {
            var dataPoint = new ProtoMetrics.NumberDataPoint
            {
                StartTimeUnixNano = DateTimeOffsetToUnixNano(metricPoint.StartTime),
                TimeUnixNano = DateTimeOffsetToUnixNano(metricPoint.EndTime),
            };

            // Add attributes (tags)
            foreach (var tag in metricPoint.Tags)
            {
                dataPoint.Attributes.Add(CreateKeyValue(tag.Key, tag.Value));
            }

            // Set value based on type
            if (metric.MetricType == MetricType.LongGauge)
            {
                dataPoint.AsInt = metricPoint.GetGaugeLastValueLong();
            }
            else
            {
                dataPoint.AsDouble = metricPoint.GetGaugeLastValueDouble();
            }

            gauge.DataPoints.Add(dataPoint);
        }

        protoMetric.Gauge = gauge;
    }

    private static void ConvertSum(Metric metric, ProtoMetrics.Metric protoMetric)
    {
        var sum = new ProtoMetrics.Sum
        {
            AggregationTemporality = ProtoMetrics.AggregationTemporality.Cumulative,
            IsMonotonic = metric.MetricType.IsSum(),
        };

        foreach (var metricPoint in metric.GetMetricPoints())
        {
            var dataPoint = new ProtoMetrics.NumberDataPoint
            {
                StartTimeUnixNano = DateTimeOffsetToUnixNano(metricPoint.StartTime),
                TimeUnixNano = DateTimeOffsetToUnixNano(metricPoint.EndTime),
            };

            // Add attributes (tags)
            foreach (var tag in metricPoint.Tags)
            {
                dataPoint.Attributes.Add(CreateKeyValue(tag.Key, tag.Value));
            }

            // Set value based on type
            if (metric.MetricType == MetricType.LongSum)
            {
                dataPoint.AsInt = metricPoint.GetSumLong();
            }
            else
            {
                dataPoint.AsDouble = metricPoint.GetSumDouble();
            }

            sum.DataPoints.Add(dataPoint);
        }

        protoMetric.Sum = sum;
    }

    private static void ConvertHistogram(Metric metric, ProtoMetrics.Metric protoMetric)
    {
        var histogram = new ProtoMetrics.Histogram
        {
            AggregationTemporality = ProtoMetrics.AggregationTemporality.Cumulative,
        };

        foreach (var metricPoint in metric.GetMetricPoints())
        {
            var dataPoint = new ProtoMetrics.HistogramDataPoint
            {
                StartTimeUnixNano = DateTimeOffsetToUnixNano(metricPoint.StartTime),
                TimeUnixNano = DateTimeOffsetToUnixNano(metricPoint.EndTime),
                Count = (ulong)metricPoint.GetHistogramCount(),
                Sum = metricPoint.GetHistogramSum(),
            };

            // Add attributes (tags)
            foreach (var tag in metricPoint.Tags)
            {
                dataPoint.Attributes.Add(CreateKeyValue(tag.Key, tag.Value));
            }

            // Add min/max if available
            if (metricPoint.TryGetHistogramMinMaxValues(out double min, out double max))
            {
                dataPoint.Min = min;
                dataPoint.Max = max;
            }

            // Add bucket counts and explicit bounds
            foreach (var bucket in metricPoint.GetHistogramBuckets())
            {
                dataPoint.BucketCounts.Add((ulong)bucket.BucketCount);

                if (bucket.ExplicitBound != double.PositiveInfinity)
                {
                    dataPoint.ExplicitBounds.Add(bucket.ExplicitBound);
                }
            }

            histogram.DataPoints.Add(dataPoint);
        }

        protoMetric.Histogram = histogram;
    }

    private static void ConvertExponentialHistogram(Metric metric, ProtoMetrics.Metric protoMetric)
    {
        // ExponentialHistogram support is limited - use basic histogram data
        var exponentialHistogram = new ProtoMetrics.ExponentialHistogram
        {
            AggregationTemporality = ProtoMetrics.AggregationTemporality.Cumulative,
        };

        foreach (var metricPoint in metric.GetMetricPoints())
        {
            var dataPoint = new ProtoMetrics.ExponentialHistogramDataPoint
            {
                StartTimeUnixNano = DateTimeOffsetToUnixNano(metricPoint.StartTime),
                TimeUnixNano = DateTimeOffsetToUnixNano(metricPoint.EndTime),
                Count = (ulong)metricPoint.GetHistogramCount(),
                Sum = metricPoint.GetHistogramSum(),
            };

            // Add attributes (tags)
            foreach (var tag in metricPoint.Tags)
            {
                dataPoint.Attributes.Add(CreateKeyValue(tag.Key, tag.Value));
            }

            // Add min/max if available
            if (metricPoint.TryGetHistogramMinMaxValues(out double min, out double max))
            {
                dataPoint.Min = min;
                dataPoint.Max = max;
            }

            // TODO: Add exponential histogram bucket details
            // The ExponentialHistogramData API is complex and not well documented.
            // For now, we export basic count/sum/min/max data.
            // Full exponential histogram bucket support can be added later.

            exponentialHistogram.DataPoints.Add(dataPoint);
        }

        protoMetric.ExponentialHistogram = exponentialHistogram;
    }
}
