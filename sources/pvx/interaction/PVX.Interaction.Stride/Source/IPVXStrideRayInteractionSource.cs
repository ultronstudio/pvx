using Stride.Core.Mathematics;
using Stride.Engine;

namespace PVX.Interaction.Stride;

/// <summary>
/// Stride-specific interaction source: a single ray a physics target
/// provider can cast against the scene.
/// </summary>
/// <remarks>
/// <see cref="IPVXInteractionSource"/> itself is a bare identity seam on
/// purpose; this interface is the Stride bridge's own extension of it,
/// carrying exactly what <see cref="PVXStridePhysicsTargetProvider"/>
/// needs and nothing else (no screen-space picking, no cone/sphere
/// targeting — see its own remarks for why).
/// </remarks>
public interface IPVXStrideRayInteractionSource : IPVXInteractionSource
{
    /// <summary>
    /// World-space ray origin.
    /// </summary>
    Vector3 Origin { get; }

    /// <summary>
    /// World-space ray direction. Always unit length: implementations
    /// normalize it themselves rather than relying on callers to.
    /// </summary>
    Vector3 Direction { get; }

    /// <summary>
    /// Maximum interaction distance along <see cref="Direction"/>. Always positive.
    /// </summary>
    float MaxDistance { get; }

    /// <summary>
    /// Entity whose own collider and descendants' colliders must be
    /// ignored by the raycast (e.g. the player's own body/capsule), or
    /// <see langword="null"/> to not ignore anything.
    /// </summary>
    Entity? OwnerEntity { get; }
}
