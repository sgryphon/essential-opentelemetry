using Essential.System;

namespace Essential.OpenTelemetry.Exporter.OtlpFile.Tests;

/// <summary>
/// Mock implementation of IOutput for testing.
/// </summary>
internal sealed class MockOutput : IOutput
{
    private readonly List<string> lines = new();

    /// <summary>
    /// Gets the lines that were written to the output.
    /// </summary>
    public IReadOnlyList<string> Lines => this.lines;

    /// <inheritdoc/>
    public object SyncRoot { get; } = new();

    /// <inheritdoc/>
    public void WriteLine(string value)
    {
        this.lines.Add(value);
    }

    /// <summary>
    /// Gets all lines combined as a single string.
    /// </summary>
    /// <returns>The combined output string.</returns>
    public string GetOutput()
    {
        return string.Join(Environment.NewLine, this.lines);
    }
}
