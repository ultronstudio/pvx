namespace PVX.Interaction;

/// <summary>
/// Metadata for one interaction action offered by a target: identity,
/// priority, mode and current availability. The actual behavior lives in
/// <see cref="Action"/>, kept separate so the descriptor stays plain data.
/// </summary>
/// <remarks>
/// Interactables build a fresh list of descriptors on every query (see
/// <see cref="IPVXInteractable.GetActions"/>), so <see cref="Availability"/>
/// is always a snapshot of "right now" — there is no mutable availability
/// state to keep in sync by hand.
/// </remarks>
public sealed class PVXInteractionDescriptor
{
    public PVXInteractionDescriptor(
        PVXInteractionActionId actionId,
        IPVXInteractionAction action,
        int priority = 0,
        PVXInteractionMode mode = PVXInteractionMode.Instant,
        TimeSpan? holdDuration = null,
        string? label = null,
        PVXInteractionAvailability? availability = null)
    {
        // default(PVXInteractionActionId) bypasses its own constructor's
        // validation, so this boundary re-checks it, mirroring PVXDifficultyId.
        if (string.IsNullOrWhiteSpace(actionId.Value))
        {
            throw new ArgumentException(
                "PVX interaction action id must not be default/empty.", nameof(actionId));
        }

        ArgumentNullException.ThrowIfNull(action);

        if (mode == PVXInteractionMode.Hold && (holdDuration is null || holdDuration.Value <= TimeSpan.Zero))
        {
            throw new ArgumentException(
                "PVX interaction descriptors in Hold mode require a positive HoldDuration.", nameof(holdDuration));
        }

        ActionId = actionId;
        Action = action;
        Priority = priority;
        Mode = mode;
        HoldDuration = mode == PVXInteractionMode.Hold ? holdDuration : null;
        Label = label;
        Availability = availability ?? PVXInteractionAvailability.Available;
    }

    /// <summary>
    /// Strongly-typed, game-defined identity of this action.
    /// </summary>
    public PVXInteractionActionId ActionId { get; }

    /// <summary>
    /// Behavior behind this action.
    /// </summary>
    public IPVXInteractionAction Action { get; }

    /// <summary>
    /// Selection priority: higher wins when more than one action is available.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Instant or hold lifecycle.
    /// </summary>
    public PVXInteractionMode Mode { get; }

    /// <summary>
    /// Required hold duration. Always <see langword="null"/> for
    /// <see cref="PVXInteractionMode.Instant"/>; always positive for
    /// <see cref="PVXInteractionMode.Hold"/>.
    /// </summary>
    public TimeSpan? HoldDuration { get; }

    /// <summary>
    /// Optional plain display text. PVX.Interaction has no localization
    /// system of its own; this is a plain string for 0.1.
    /// </summary>
    public string? Label { get; }

    /// <summary>
    /// Whether this action can currently be executed, and why not if it can't.
    /// </summary>
    public PVXInteractionAvailability Availability { get; }
}
