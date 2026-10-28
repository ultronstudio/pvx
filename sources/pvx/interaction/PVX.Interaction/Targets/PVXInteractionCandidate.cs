namespace PVX.Interaction;

/// <summary>
/// One potential interaction target reported by an <see cref="IPVXInteractionTargetProvider"/>.
/// </summary>
/// <remarks>
/// Carries only what focus selection needs. Deliberately has no PVX-neutral
/// geometry type in this milestone; <see cref="Distance"/> and <see cref="Score"/>
/// are plain optional numbers a provider may fill in, not positions.
/// </remarks>
public readonly struct PVXInteractionCandidate
{
    public PVXInteractionCandidate(
        IPVXInteractable target,
        PVXInteractionPoint? point = null,
        int priority = 0,
        float? distance = null,
        float? score = null)
    {
        ArgumentNullException.ThrowIfNull(target);

        Target = target;
        Point = point;
        Priority = priority;
        Distance = distance;
        Score = score;
    }

    /// <summary>
    /// The candidate target.
    /// </summary>
    public IPVXInteractable Target { get; }

    /// <summary>
    /// The specific interaction point on <see cref="Target"/>, if any.
    /// </summary>
    public PVXInteractionPoint? Point { get; }

    /// <summary>
    /// Focus selection priority: higher wins among simultaneous candidates.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Optional provider-supplied distance, for providers that have one.
    /// </summary>
    public float? Distance { get; }

    /// <summary>
    /// Optional provider-supplied score, for providers that have one.
    /// </summary>
    public float? Score { get; }
}
