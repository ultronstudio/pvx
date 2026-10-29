namespace PVX.World;

/// <summary>
/// Raised when a <see cref="PVXPickupComponent"/>'s <see cref="PVXPickupComponent.State"/> changes.
/// </summary>
public sealed class PVXPickupStateChangedEventArgs(
    PVXPickupState previous,
    PVXPickupState current) : EventArgs
{
    public PVXPickupState Previous { get; } = previous;

    public PVXPickupState Current { get; } = current;
}
