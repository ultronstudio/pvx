namespace PVX.Interaction;

/// <summary>
/// Opaque, game-defined identity of one interaction point on a target.
/// </summary>
/// <remarks>
/// A single target can expose more than one interaction point (e.g. a
/// door's <c>HandleFront</c> and <c>HandleBack</c>, or a ladder's
/// <c>BottomEntry</c> and <c>TopEntry</c>). PVX.Interaction only carries
/// this identity through target discovery and action queries; it has no
/// built-in points and no opinion on what any point means geometrically.
/// It does not carry a physical transform in this milestone.
/// </remarks>
public readonly record struct PVXInteractionPoint
{
    public PVXInteractionPoint(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "PVX interaction point id must not be null or whitespace.", nameof(id));
        }

        Id = id;
    }

    /// <summary>
    /// The raw identifier string, e.g. <c>"BottomEntry"</c>.
    /// </summary>
    public string Id { get; }

    public override string ToString() => Id;
}
