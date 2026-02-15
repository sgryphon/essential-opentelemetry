namespace Essential.System;

/// <summary>
/// Interface for output operations to support JSONL export and testing.
/// </summary>
internal interface IConsole
{
    /// <summary>
    /// Gets an object that can be used to synchronize access to the IOutput.
    /// </summary>
    object SyncRoot { get; }

    /// <summary>
    /// Opens the standard output stream.
    /// </summary>
    /// <returns>The standard output stream.</returns>
    Stream OpenStandardOutput();
}
