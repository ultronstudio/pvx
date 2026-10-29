namespace PVX.Dialogue;

/// <summary>
/// Advances through an ordered sequence of <see cref="PVXDialogueLine"/>s,
/// one at a time. Pure state machine — no UI, no text rendering, no
/// timing/auto-advance: a game's UI layer reacts to <see cref="LineChanged"/>/<see cref="Completed"/>
/// and calls <see cref="Advance"/> on its own input (the same split
/// <see cref="PVXDialogueComponent"/>'s own remarks describe between this
/// engine and a game's presentation layer).
/// </summary>
public sealed class PVXDialogueRunner
{
    private IReadOnlyList<PVXDialogueLine> lines = Array.Empty<PVXDialogueLine>();
    private int index = -1;

    /// <summary>
    /// Whether a sequence is currently in progress.
    /// </summary>
    public bool IsActive => index >= 0 && index < lines.Count;

    /// <summary>
    /// The line currently being shown, or <see langword="null"/> while not active.
    /// </summary>
    public PVXDialogueLine? CurrentLine => IsActive ? lines[index] : null;

    /// <summary>
    /// Raised whenever <see cref="CurrentLine"/> changes to a new, non-null line.
    /// </summary>
    public event EventHandler<PVXDialogueLineChangedEventArgs>? LineChanged;

    /// <summary>
    /// Raised when a sequence finishes (by reaching its end) or is stopped early.
    /// </summary>
    public event EventHandler? Completed;

    /// <summary>
    /// Starts <paramref name="newLines"/> from its first line. No-op
    /// (returns <see langword="false"/>) while already active, or if
    /// <paramref name="newLines"/> is <see langword="null"/>/empty.
    /// </summary>
    public bool Start(IReadOnlyList<PVXDialogueLine>? newLines)
    {
        if (IsActive || newLines is null || newLines.Count == 0)
            return false;

        lines = newLines;
        index = 0;
        LineChanged?.Invoke(this, new PVXDialogueLineChangedEventArgs(lines[index]));
        return true;
    }

    /// <summary>
    /// Moves to the next line. If that moves past the last line, the
    /// sequence ends: <see cref="Completed"/> is raised, <see cref="IsActive"/>
    /// becomes <see langword="false"/>, and this returns <see langword="false"/>.
    /// No-op (returns <see langword="false"/>) while not active.
    /// </summary>
    public bool Advance()
    {
        if (!IsActive)
            return false;

        index++;

        if (index >= lines.Count)
        {
            index = -1;
            lines = Array.Empty<PVXDialogueLine>();
            Completed?.Invoke(this, EventArgs.Empty);
            return false;
        }

        LineChanged?.Invoke(this, new PVXDialogueLineChangedEventArgs(lines[index]));
        return true;
    }

    /// <summary>
    /// Ends the sequence early, raising <see cref="Completed"/>. No-op while not active.
    /// </summary>
    public void Stop()
    {
        if (!IsActive)
            return;

        index = -1;
        lines = Array.Empty<PVXDialogueLine>();
        Completed?.Invoke(this, EventArgs.Empty);
    }
}
