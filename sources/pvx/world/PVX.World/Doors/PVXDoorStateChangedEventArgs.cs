namespace PVX.World;

/// <summary>
/// Raised when a <see cref="PVXDoorComponent"/>'s <see cref="PVXDoorComponent.State"/> changes.
/// </summary>
public sealed class PVXDoorStateChangedEventArgs(
    PVXDoorState previous,
    PVXDoorState current) : EventArgs
{
    public PVXDoorState Previous { get; } = previous;

    public PVXDoorState Current { get; } = current;
}
