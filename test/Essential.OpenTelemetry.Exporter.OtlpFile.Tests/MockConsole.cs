using System.Text;
using Essential.System;

namespace Essential.OpenTelemetry.Exporter.OtlpFile.Tests;

/// <summary>
/// Mock implementation of IOutput for testing.
/// </summary>
internal sealed class MockConsole : IConsole
{
    private readonly MemoryStream memoryStream = new();

    /// <summary>
    /// Gets the lines that were written to the output.
    /// </summary>
    public IReadOnlyList<string> Lines =>
        Encoding
            .UTF8.GetString(this.memoryStream.ToArray())
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

    /// <inheritdoc/>
    public object SyncRoot { get; } = new();

    /// <inheritdoc/>
    public Stream OpenStandardOutput()
    {
        return this.memoryStream;
    }

    /// <summary>
    /// Gets all lines combined as a single string.
    /// </summary>
    /// <returns>The combined output string.</returns>
    public string GetOutput()
    {
        return Encoding.UTF8.GetString(this.memoryStream.ToArray()).TrimEnd('\n');
    }
}
