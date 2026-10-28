namespace PVX.Interaction;

/// <summary>
/// Supplies candidate interaction targets for a source, without
/// PVX.Interaction knowing how candidates are found.
/// </summary>
/// <remarks>
/// PVX.Interaction never implements physics raycasts or shape casts
/// itself. A future Stride integration layer can implement this by doing
/// a camera raycast and mapping hits to <see cref="IPVXInteractable"/>
/// targets; this milestone only consumes the abstraction, which makes it
/// trivial to fake in unit tests.
/// </remarks>
public interface IPVXInteractionTargetProvider
{
    /// <summary>
    /// Returns the current candidate targets for <paramref name="source"/>.
    /// </summary>
    IReadOnlyList<PVXInteractionCandidate> GetCandidates(IPVXInteractionSource source);
}
