namespace PVX.Interaction;

/// <summary>
/// Immutable bundle of who is interacting, with what, and through which
/// interaction point. Flows through target queries, action availability
/// queries, and action execution alike.
/// </summary>
/// <remarks>
/// <see cref="Target"/> queries its own <see cref="IPVXInteractable.GetActions"/>
/// with a context carrying itself, so a target can branch on <see cref="Point"/>
/// to offer different actions per interaction point (e.g. a ladder offering
/// <c>Climb Up</c> at <c>BottomEntry</c> and <c>Climb Down</c> at <c>TopEntry</c>).
/// </remarks>
public readonly struct PVXInteractionContext
{
    public PVXInteractionContext(IPVXInteractionSource source, IPVXInteractable target, PVXInteractionPoint? point = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        Source = source;
        Target = target;
        Point = point;
    }

    /// <summary>
    /// Who is interacting.
    /// </summary>
    public IPVXInteractionSource Source { get; }

    /// <summary>
    /// The interactable this context concerns.
    /// </summary>
    public IPVXInteractable Target { get; }

    /// <summary>
    /// The specific interaction point on <see cref="Target"/>, or
    /// <see langword="null"/> if the target does not distinguish points.
    /// </summary>
    public PVXInteractionPoint? Point { get; }
}
