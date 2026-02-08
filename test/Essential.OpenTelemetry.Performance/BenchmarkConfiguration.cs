namespace Essential.OpenTelemetry.Performance;

/// <summary>
/// Configuration options for performance benchmarks.
/// </summary>
public class BenchmarkConfiguration
{
    /// <summary>
    /// Number of log messages to write per iteration.
    /// </summary>
    public int LoggingIterations { get; set; } = 100;

    /// <summary>
    /// Number of activity/spans to create per iteration.
    /// </summary>
    public int TracingIterations { get; set; } = 100;

    /// <summary>
    /// Number of counters to create for metrics testing.
    /// </summary>
    public int MetricsCounterCount { get; set; } = 100;

    /// <summary>
    /// Export interval for metrics (in milliseconds)
    /// </summary>
    public int MetricsExportIntervalMilliseconds { get; set; } = 100;

    /// <summary>
    /// Number of times to increment each counter.
    /// </summary>
    public int MetricsIncrementsPerCounter { get; set; } = 10;
}
