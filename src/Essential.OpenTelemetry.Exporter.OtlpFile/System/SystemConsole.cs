namespace Essential.System;

/// <summary>
/// System output implementation that writes to standard output (console).
/// </summary>
internal sealed class SystemConsole : IConsole
{
    private readonly object syncRoot = new();

    /// <inheritdoc/>
    public object SyncRoot => this.syncRoot;

    /// <inheritdoc/>
    public Stream OpenStandardOutput()
    {
        return Console.OpenStandardOutput();
    }
}
