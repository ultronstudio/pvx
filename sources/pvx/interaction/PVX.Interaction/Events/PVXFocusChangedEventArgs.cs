namespace PVX.Interaction;

/// <summary>
/// Raised when <see cref="PVXInteractionService.CurrentFocus"/> actually changes.
/// </summary>
public sealed class PVXFocusChangedEventArgs(
    PVXInteractionCandidate? previous,
    PVXInteractionCandidate? current) : EventArgs
{
    public PVXInteractionCandidate? Previous { get; } = previous;

    public PVXInteractionCandidate? Current { get; } = current;
}
