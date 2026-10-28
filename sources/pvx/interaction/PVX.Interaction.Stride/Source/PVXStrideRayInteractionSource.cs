using Stride.Core.Mathematics;
using Stride.Engine;

namespace PVX.Interaction.Stride;

/// <summary>
/// Explicit, immutable <see cref="IPVXStrideRayInteractionSource"/> for
/// tests and any host that already computes its own ray (editor preview,
/// a non-camera-driven source, deterministic integration tests, ...).
/// </summary>
public sealed class PVXStrideRayInteractionSource : IPVXStrideRayInteractionSource
{
    public PVXStrideRayInteractionSource(Vector3 origin, Vector3 direction, float maxDistance, Entity? ownerEntity = null)
    {
        Origin = ValidateOrigin(origin);
        Direction = ValidateAndNormalize(direction);
        MaxDistance = ValidateMaxDistance(maxDistance);
        OwnerEntity = ownerEntity;
    }

    public Vector3 Origin { get; }

    public Vector3 Direction { get; }

    public float MaxDistance { get; }

    public Entity? OwnerEntity { get; }

    /// <summary>
    /// Validates that <paramref name="origin"/> is finite (no NaN/Infinity
    /// components), so an already-corrupt transform fails loudly here
    /// instead of silently reaching the native raycast.
    /// </summary>
    internal static Vector3 ValidateOrigin(Vector3 origin)
    {
        if (!IsFinite(origin))
        {
            throw new ArgumentException(
                "PVX ray interaction origin must be finite (no NaN/Infinity components).", nameof(origin));
        }

        return origin;
    }

    /// <summary>
    /// Validates that <paramref name="direction"/> is finite and non-zero,
    /// then returns its normalized form. Shared with
    /// <see cref="PVXStrideCameraInteractionSource"/> so both sources fail
    /// closed on invalid rays the exact same way.
    /// </summary>
    internal static Vector3 ValidateAndNormalize(Vector3 direction)
    {
        if (!IsFinite(direction))
        {
            throw new ArgumentException(
                "PVX ray interaction direction must be finite (no NaN/Infinity components).", nameof(direction));
        }

        if (direction.LengthSquared() <= 0f)
        {
            throw new ArgumentException(
                "PVX ray interaction direction must not be zero-length.", nameof(direction));
        }

        direction.Normalize();
        return direction;
    }

    internal static float ValidateMaxDistance(float maxDistance)
    {
        if (!float.IsFinite(maxDistance) || maxDistance <= 0f)
        {
            throw new ArgumentException(
                "PVX ray interaction max distance must be a positive, finite number.", nameof(maxDistance));
        }

        return maxDistance;
    }

    private static bool IsFinite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
}
