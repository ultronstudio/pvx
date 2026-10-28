namespace PVX.Gameplay;

/// <summary>
/// Resolves the current primary player on demand, without PVX.Gameplay
/// knowing how that player is implemented.
/// </summary>
public interface IPVXPlayerProvider
{
    /// <summary>
    /// Returns the current primary player, or <see langword="null"/> if
    /// none is currently available.
    /// </summary>
    IPVXPlayer? GetPrimaryPlayer();
}
