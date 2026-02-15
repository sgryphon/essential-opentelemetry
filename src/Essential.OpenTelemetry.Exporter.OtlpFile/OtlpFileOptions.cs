using Essential.System;

namespace Essential.OpenTelemetry;

/// <summary>
/// Options for the OTLP file exporter.
/// </summary>
public class OtlpFileOptions
{
    // By default use a shared system console, so that exporters can synchronise on the same lock
    private static readonly SystemConsole sharedSystemConsole = new();

    /// <summary>
    /// Gets or sets the output to use for writing. Defaults to SystemOutput (stdout).
    /// </summary>
    internal IConsole Console { get; set; } = sharedSystemConsole;
}
