namespace PVX.Interaction;

/// <summary>
/// How an interaction action's lifecycle unfolds once begun.
/// </summary>
/// <remarks>
/// Deliberately minimal for this milestone. A long-running mechanic (e.g.
/// climbing a ladder) is not a reason to add a <c>Continuous</c> mode here:
/// the action itself can be <see cref="Instant"/> and simply hand off to an
/// external system (traversal, inspection, manipulation, ...) that owns the
/// long-running behavior. A future <c>Continuous</c> mode would only be
/// warranted if the interaction lifecycle itself — not the handed-off
/// mechanic — needed to track something ongoing.
/// </remarks>
public enum PVXInteractionMode
{
    /// <summary>
    /// Begin, Execute and Complete/Fail happen synchronously, in one call.
    /// </summary>
    Instant,

    /// <summary>
    /// Begin starts progress tracking; Execute (Complete) only happens once
    /// elapsed hold time reaches the action's required duration. Can be
    /// cancelled at any point before completion.
    /// </summary>
    Hold
}
