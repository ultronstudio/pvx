namespace PVX.World;

/// <summary>
/// Raised when a <see cref="PVXSwitchComponent"/>'s <see cref="PVXSwitchComponent.State"/> changes.
/// </summary>
public sealed class PVXSwitchStateChangedEventArgs(
    PVXSwitchState previous,
    PVXSwitchState current) : EventArgs
{
    public PVXSwitchState Previous { get; } = previous;

    public PVXSwitchState Current { get; } = current;
}
