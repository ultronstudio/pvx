namespace PVX.Persistence;

/// <summary>
/// Raised when a <see cref="PVXSaveStore"/> value actually changes.
/// </summary>
public sealed class PVXSaveValueChangedEventArgs(
    string key,
    string? previous,
    string? current) : EventArgs
{
    public string Key { get; } = key;

    /// <summary>
    /// Raw previous value, or <see langword="null"/> if the key did not exist before.
    /// </summary>
    public string? Previous { get; } = previous;

    /// <summary>
    /// Raw current value, or <see langword="null"/> if the key was removed.
    /// </summary>
    public string? Current { get; } = current;
}
