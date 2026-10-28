namespace PVX.Gameplay;

/// <summary>
/// Identity seam for the entity controlled by a player in a PVX game.
/// </summary>
/// <remarks>
/// Intentionally empty. The gameplay layer only needs to know that a
/// primary player exists and when it changes, never how it is
/// implemented (character, controller, entity, ...); that is owned by
/// future modules such as PVX.Characters.
/// </remarks>
public interface IPVXPlayer
{
}
