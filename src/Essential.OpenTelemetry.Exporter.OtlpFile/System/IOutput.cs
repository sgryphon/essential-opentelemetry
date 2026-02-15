namespace Essential.System;

/// <summary>
/// Interface for output operations to support JSONL export and testing.
/// </summary>
internal interface IOutput
{
    /// <summary>
    /// Gets an object that can be used to synchronize access to the IOutput.
    /// </summary>
    object SyncRoot { get; }

    /// <summary>
    /// Writes the specified string value followed by a line terminator to the output.
    /// </summary>
    /// <param name="value">The string to write.</param>
    void WriteLine(string value);
}
