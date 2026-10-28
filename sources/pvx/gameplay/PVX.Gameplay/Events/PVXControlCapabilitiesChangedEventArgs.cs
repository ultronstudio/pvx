namespace PVX.Gameplay;

/// <summary>
/// Raised when <see cref="PVXControlService.EffectiveCapabilities"/> changes.
/// </summary>
public sealed class PVXControlCapabilitiesChangedEventArgs(
    PVXControlCapabilities previous,
    PVXControlCapabilities current) : EventArgs
{
    public PVXControlCapabilities Previous { get; } = previous;

    public PVXControlCapabilities Current { get; } = current;
}
