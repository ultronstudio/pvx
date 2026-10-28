namespace PVX.Interaction;

/// <summary>
/// Whether an interaction action can currently be selected/executed, and
/// why not when it cannot.
/// </summary>
/// <remarks>
/// Deliberately generic: <see cref="Reason"/> is a short machine-oriented
/// reason code (e.g. <c>"locked"</c>), never a user-facing sentence and
/// never an external/game-specific condition like "requires basement key".
/// Interactables decide their own reason codes; PVX.Interaction never
/// interprets them.
/// </remarks>
public readonly record struct PVXInteractionAvailability
{
    /// <summary>
    /// Shared instance representing an available action.
    /// </summary>
    public static readonly PVXInteractionAvailability Available = new(isAvailable: true, reason: null);

    private PVXInteractionAvailability(bool isAvailable, string? reason)
    {
        IsAvailable = isAvailable;
        Reason = reason;
    }

    /// <summary>
    /// Whether the action can currently be executed.
    /// </summary>
    public bool IsAvailable { get; }

    /// <summary>
    /// Short reason code for why the action is unavailable, or
    /// <see langword="null"/> when <see cref="IsAvailable"/> is <see langword="true"/>.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Creates an unavailable result with the given reason code.
    /// </summary>
    public static PVXInteractionAvailability Unavailable(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "PVX interaction unavailability reason must not be null or whitespace.", nameof(reason));
        }

        return new PVXInteractionAvailability(isAvailable: false, reason);
    }
}
