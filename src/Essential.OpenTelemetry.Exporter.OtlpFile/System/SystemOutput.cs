namespace Essential.System;

/// <summary>
/// System output implementation that writes to standard output (console).
/// </summary>
internal sealed class SystemOutput : IOutput
{
    private readonly object syncRoot = new();

    /// <inheritdoc/>
    public object SyncRoot => this.syncRoot;

    /// <inheritdoc/>
    public void WriteLine(string value)
    {
        Console.WriteLine(value);
    }
}
