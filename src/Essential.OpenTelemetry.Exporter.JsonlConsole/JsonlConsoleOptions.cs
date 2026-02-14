using Essential.System;

namespace Essential.OpenTelemetry;

/// <summary>
/// Options for the JSONL console exporter.
/// </summary>
public class JsonlConsoleOptions
{
    // By default use a shared system output, so that exporters can synchronise on the same lock
    private static readonly SystemOutput sharedSystemOutput = new();

    /// <summary>
    /// Gets or sets the output to use for writing. Defaults to SystemOutput (stdout).
    /// </summary>
    internal IOutput Output { get; set; } = sharedSystemOutput;
}
