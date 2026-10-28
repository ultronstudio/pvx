namespace PVX.Interaction;

/// <summary>
/// Opaque, game-defined identity of an interaction action.
/// </summary>
/// <remarks>
/// PVX.Interaction has no built-in list of actions (no central "open",
/// "climb", "grab" enum): each game or future module defines and owns its
/// own action ids. <c>default(PVXInteractionActionId)</c> bypasses this
/// constructor's own validation, so boundaries that accept an id (such as
/// <see cref="PVXInteractionDescriptor"/>'s constructor) re-validate it,
/// mirroring <c>PVXDifficultyId</c>'s defensive pattern in PVX.Gameplay.
/// </remarks>
public readonly record struct PVXInteractionActionId
{
    public PVXInteractionActionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "PVX interaction action id must not be null or whitespace.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// The raw identifier string, e.g. <c>"open"</c> or <c>"climb_up"</c>.
    /// </summary>
    public string Value { get; }

    public override string ToString() => Value;
}
