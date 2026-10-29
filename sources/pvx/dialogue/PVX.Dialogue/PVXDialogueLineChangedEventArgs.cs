namespace PVX.Dialogue;

/// <summary>
/// Raised when a <see cref="PVXDialogueRunner"/>'s <see cref="PVXDialogueRunner.CurrentLine"/> changes.
/// </summary>
public sealed class PVXDialogueLineChangedEventArgs(PVXDialogueLine currentLine) : EventArgs
{
    public PVXDialogueLine CurrentLine { get; } = currentLine;
}
