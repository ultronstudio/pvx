namespace PVX.Persistence;

/// <summary>
/// Raised when a <see cref="PVXNarrativeFlags"/> flag's effective boolean
/// value actually changes.
/// </summary>
public sealed class PVXNarrativeFlagChangedEventArgs(
    string flagName,
    bool previous,
    bool current) : EventArgs
{
    public string FlagName { get; } = flagName;

    public bool Previous { get; } = previous;

    public bool Current { get; } = current;
}
