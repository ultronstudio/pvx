namespace PVX.Gameplay;

/// <summary>
/// Owns the currently selected <see cref="PVXDifficultyId"/>.
/// </summary>
/// <remarks>
/// Changing difficulty has no effect by itself: future modules (horror,
/// puzzles, persistence, interaction, ...) observe <see cref="DifficultyChanged"/>
/// and decide their own behavior. PVX.Gameplay only owns the selected identity.
/// </remarks>
public sealed class PVXDifficultyService
{
    private PVXDifficultyId? current;

    /// <summary>
    /// The currently selected difficulty, or <see langword="null"/> if none has been set yet.
    /// </summary>
    public PVXDifficultyId? Current => current;

    /// <summary>
    /// Raised only when the selected difficulty actually changes.
    /// </summary>
    public event EventHandler<PVXDifficultyChangedEventArgs>? DifficultyChanged;

    /// <summary>
    /// Selects the current difficulty. No-op if it is already selected.
    /// </summary>
    public void SetDifficulty(PVXDifficultyId difficulty)
    {
        if (current == difficulty)
            return;

        var previous = current;
        current = difficulty;
        DifficultyChanged?.Invoke(this, new PVXDifficultyChangedEventArgs(previous, difficulty));
    }
}
