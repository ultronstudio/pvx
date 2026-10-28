namespace PVX.Gameplay;

/// <summary>
/// Raised when the selected <see cref="PVXDifficultyId"/> changes.
/// </summary>
public sealed class PVXDifficultyChangedEventArgs(
    PVXDifficultyId? previous,
    PVXDifficultyId? current) : EventArgs
{
    public PVXDifficultyId? Previous { get; } = previous;

    public PVXDifficultyId? Current { get; } = current;
}
