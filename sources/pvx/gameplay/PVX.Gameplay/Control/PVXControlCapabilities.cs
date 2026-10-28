namespace PVX.Gameplay;

/// <summary>
/// Generic set of control actions a primary player may currently perform.
/// </summary>
/// <remarks>
/// Deliberately small and game-agnostic. Game-specific abilities (shoot,
/// reload, inventory, flashlight, ...) do not belong here; they are owned
/// by whichever future module implements them.
/// </remarks>
[Flags]
public enum PVXControlCapabilities
{
    None = 0,

    Look = 1 << 0,
    Move = 1 << 1,
    Interact = 1 << 2,
    PrimaryUse = 1 << 3,
    SecondaryUse = 1 << 4,

    All = Look | Move | Interact | PrimaryUse | SecondaryUse
}
