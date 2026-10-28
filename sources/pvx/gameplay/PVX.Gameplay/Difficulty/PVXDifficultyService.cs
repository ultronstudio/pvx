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
    /// <exception cref="ArgumentException">
    /// <paramref name="difficulty"/> is <c>default</c> (its <see cref="PVXDifficultyId.Value"/>
    /// is null or whitespace). <c>default(PVXDifficultyId)</c> bypasses the
    /// constructor's own validation, so the boundary re-checks it here.
    /// </exception>
    public void SetDifficulty(PVXDifficultyId difficulty)
    {
        if (string.IsNullOrWhiteSpace(difficulty.Value))
        {
            throw new ArgumentException(
                "PVX difficulty id must not be null or whitespace.", nameof(difficulty));
        }

        if (current == difficulty)
            return;

        var previous = current;
        current = difficulty;
        DifficultyChanged?.Invoke(this, new PVXDifficultyChangedEventArgs(previous, difficulty));
    }
}
