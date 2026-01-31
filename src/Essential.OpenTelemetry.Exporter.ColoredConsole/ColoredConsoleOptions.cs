using Essential.System;

namespace Essential.OpenTelemetry.Exporter;

public class ColoredConsoleOptions
{
    private const string DefaultTimestampFormat = "HH:mm:ss ";

    // By default use a shared system console, so that exporters can synchronise on the same lock
    private static SystemConsole sharedSystemConsole = new();

    /// <summary>
    /// Gets or sets the timestamp format string. If empty, no timestamp is output.
    /// </summary>
    public string TimestampFormat { get; set; } = DefaultTimestampFormat;

    /// <summary>
    /// Gets or sets a value indicating whether to use UTC timestamps. If false, local time is used.
    /// </summary>
    public bool UseUtcTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the console to use for output. Defaults to SystemConsole.
    /// </summary>
    internal IConsole Console { get; set; } = sharedSystemConsole;
}
