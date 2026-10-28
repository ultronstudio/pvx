namespace PVX.Gameplay;

/// <summary>
/// Raised when the resolved primary player changes.
/// </summary>
public sealed class PVXPrimaryPlayerChangedEventArgs(
    IPVXPlayer? previous,
    IPVXPlayer? current) : EventArgs
{
    public IPVXPlayer? Previous { get; } = previous;

    public IPVXPlayer? Current { get; } = current;
}
