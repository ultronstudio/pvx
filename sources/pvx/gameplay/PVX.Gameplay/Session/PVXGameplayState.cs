namespace PVX.Gameplay;

/// <summary>
/// Lifecycle state of a <see cref="PVXGameplaySession"/>.
/// </summary>
/// <remarks>
/// This is distinct from <c>PVXRuntimeState</c>: the PVX runtime can be
/// running (main menu, editor preview, loading screen) while no gameplay
/// session is active, paused, or already ended.
/// </remarks>
public enum PVXGameplayState
{
    Inactive,
    Starting,
    Active,
    Paused,
    Ending,
    Ended,
    Faulted
}
