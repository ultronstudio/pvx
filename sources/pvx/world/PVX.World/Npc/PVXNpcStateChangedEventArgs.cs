namespace PVX.World;

/// <summary>
/// Raised when a <see cref="PVXNpcComponent"/>'s <see cref="PVXNpcComponent.State"/> changes.
/// </summary>
public sealed class PVXNpcStateChangedEventArgs(
    PVXNpcState previous,
    PVXNpcState current) : EventArgs
{
    public PVXNpcState Previous { get; } = previous;

    public PVXNpcState Current { get; } = current;
}
