namespace PVX.Gameplay;

/// <summary>
/// High-level semantic summary of <see cref="PVXControlService.EffectiveCapabilities"/>.
/// </summary>
/// <remarks>
/// This is convenience metadata derived from the effective capability set,
/// never the authority: callers must branch on capabilities, not on this
/// enum. <see cref="Cinematic"/> is reserved for a future cinematics
/// system to report through; this milestone never derives it on its own.
/// </remarks>
public enum PVXControlMode
{
    Full,
    Limited,
    Cinematic,
    Disabled
}
