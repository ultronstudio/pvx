using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace PVX.Interaction.Stride;

/// <summary>
/// Resolves <see cref="PVXInteractionCandidate"/>s from a real Stride
/// physics raycast: source ray → nearest non-owner hit → ancestor search
/// for an <see cref="IPVXInteractable"/> and an optional <see cref="PVXInteractionPointComponent"/>.
/// </summary>
/// <remarks>
/// Deliberately returns at most one candidate: this is a center-ray
/// provider, not cone/sphere/multi-target selection — those are a
/// different future provider. It never implements interaction actions,
/// UI, or any game-specific mechanic; it only turns a physics hit into
/// the generic target identity <see cref="PVX.Interaction"/> already
/// understands.
/// </remarks>
public sealed class PVXStridePhysicsTargetProvider : IPVXInteractionTargetProvider
{
    private readonly Func<Simulation?> simulationAccessor;
    private readonly List<HitResult> hitsBuffer = [];

    public PVXStridePhysicsTargetProvider(Func<Simulation?> simulationAccessor)
    {
        this.simulationAccessor = simulationAccessor ?? throw new ArgumentNullException(nameof(simulationAccessor));
    }

    /// <summary>
    /// Collision group the cast ray itself belongs to.
    /// </summary>
    public CollisionFilterGroups FilterGroup { get; set; } = CollisionFilterGroups.DefaultFilter;

    /// <summary>
    /// Which collision groups this query can hit. Deliberately generic:
    /// no hardcoded game-specific layers here. Configure per game/module
    /// to separate world geometry, interactive props, triggers, player/NPC
    /// colliders, etc.
    /// </summary>
    public CollisionFilterGroupFlags InteractionQueryMask { get; set; } = CollisionFilterGroupFlags.AllFilter;

    /// <summary>
    /// Whether the query also hits trigger colliders. Off by default, matching
    /// <see cref="Simulation.Raycast(Vector3, Vector3, CollisionFilterGroups, CollisionFilterGroupFlags, bool, EFlags)"/>'s own default.
    /// </summary>
    public bool HitTriggers { get; set; }

    /// <summary>
    /// Resolves at most one candidate for <paramref name="source"/>.
    /// Returns no candidates if <paramref name="source"/> is not an
    /// <see cref="IPVXStrideRayInteractionSource"/>, if no physics
    /// simulation is currently available, if nothing is hit, or if the
    /// nearest non-owner hit is not resolvable to an <see cref="IPVXInteractable"/>
    /// (world geometry blocking the view of something interactable behind
    /// it must not yield a candidate).
    /// </summary>
    public IReadOnlyList<PVXInteractionCandidate> GetCandidates(IPVXInteractionSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is not IPVXStrideRayInteractionSource raySource)
            return Array.Empty<PVXInteractionCandidate>();

        var simulation = simulationAccessor();
        if (simulation is null)
            return Array.Empty<PVXInteractionCandidate>();

        var from = raySource.Origin;
        var to = from + raySource.Direction * raySource.MaxDistance;

        hitsBuffer.Clear();
        simulation.RaycastPenetrating(from, to, hitsBuffer, FilterGroup, InteractionQueryMask, HitTriggers);

        HitResult? nearest = null;

        foreach (var hit in hitsBuffer)
        {
            if (!hit.Succeeded || hit.Collider?.Entity is not { } hitEntity)
                continue;

            if (raySource.OwnerEntity is { } owner && IsOwnerOrDescendant(hitEntity, owner))
                continue;

            if (nearest is null || hit.HitFraction < nearest.Value.HitFraction)
                nearest = hit;
        }

        if (nearest is not { } nearestHit)
            return Array.Empty<PVXInteractionCandidate>();

        var (interactable, point, priority) = ResolveInteractable(nearestHit.Collider!.Entity);
        if (interactable is null)
            return Array.Empty<PVXInteractionCandidate>();

        var distance = Vector3.Distance(from, nearestHit.Point);
        return [new PVXInteractionCandidate(interactable, point, priority, distance)];
    }

    /// <summary>
    /// True when <paramref name="entity"/> is <paramref name="owner"/> or a
    /// descendant of it, by walking up <paramref name="entity"/>'s ancestor chain.
    /// </summary>
    private static bool IsOwnerOrDescendant(Entity entity, Entity owner)
    {
        for (var current = entity; current is not null; current = current.Transform.Parent?.Entity)
        {
            if (ReferenceEquals(current, owner))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Walks up from <paramref name="hitEntity"/> (inclusive) looking for
    /// the nearest ancestor with an <see cref="IPVXInteractable"/> component,
    /// stopping the search there. Along the way it remembers the nearest
    /// <see cref="PVXInteractionPointComponent"/> found at or below that
    /// root — never above it, which would belong to an unrelated ancestor
    /// system.
    /// </summary>
    private static (IPVXInteractable? Interactable, PVXInteractionPoint? Point, int Priority) ResolveInteractable(Entity hitEntity)
    {
        PVXInteractionPointComponent? pointComponent = null;

        for (var current = hitEntity; current is not null; current = current.Transform.Parent?.Entity)
        {
            pointComponent ??= current.Get<PVXInteractionPointComponent>();

            IPVXInteractable? interactable = null;

            foreach (var component in current.Components)
            {
                if (component is IPVXInteractable candidate)
                {
                    // First component wins: deterministic by Stride's own
                    // component declaration/attachment order, not nondeterministic.
                    interactable = candidate;
                    break;
                }
            }

            if (interactable is not null)
            {
                // Blank/whitespace PointId is authoring-friendly "no point
                // yet", not a validation failure (see PVXInteractionPointComponent).
                var point = pointComponent is { } pc && !string.IsNullOrWhiteSpace(pc.PointId)
                    ? new PVXInteractionPoint(pc.PointId)
                    : (PVXInteractionPoint?)null;

                return (interactable, point, pointComponent?.Priority ?? 0);
            }
        }

        return (null, null, 0);
    }
}
