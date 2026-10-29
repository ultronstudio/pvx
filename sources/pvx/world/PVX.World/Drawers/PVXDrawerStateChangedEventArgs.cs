namespace PVX.World;

/// <summary>
/// Raised when a <see cref="PVXDrawerComponent"/>'s <see cref="PVXDrawerComponent.State"/> changes.
/// </summary>
public sealed class PVXDrawerStateChangedEventArgs(
    PVXDrawerState previous,
    PVXDrawerState current) : EventArgs
{
    public PVXDrawerState Previous { get; } = previous;

    public PVXDrawerState Current { get; } = current;
}
