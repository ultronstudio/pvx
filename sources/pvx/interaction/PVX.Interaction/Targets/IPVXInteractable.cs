namespace PVX.Interaction;

/// <summary>
/// A target that can offer one or more interaction actions.
/// </summary>
/// <remarks>
/// Deliberately not a single <c>Interact()</c> method: a door offers
/// <c>Open</c>/<c>Close</c>/<c>Inspect</c>/<c>Unlock</c>, a ladder offers
/// <c>Climb Up</c>/<c>Climb Down</c>, and PVX.Interaction must be able to
/// pick the right one by priority and availability rather than assuming
/// there is only ever one thing to do with a target.
/// </remarks>
public interface IPVXInteractable
{
    /// <summary>
    /// Returns the actions this target currently offers for the given
    /// context. Called on every focus update, so implementations should
    /// build a small, fresh list reflecting current state rather than
    /// caching stale availability.
    /// </summary>
    IReadOnlyList<PVXInteractionDescriptor> GetActions(PVXInteractionContext context);
}
