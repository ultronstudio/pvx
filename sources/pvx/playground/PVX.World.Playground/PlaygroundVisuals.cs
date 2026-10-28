using Stride.Core.Mathematics;
using Stride.Physics;

namespace PVX.World.Playground;

/// <summary>
/// Playground-only helper for building simple solid colliders. Not part
/// of any production PVX assembly.
/// </summary>
internal static class PlaygroundVisuals
{
    public static StaticColliderComponent CreateBoxCollider(Vector3 size)
    {
        var collider = new StaticColliderComponent();
        collider.ColliderShapes.Add(new BoxColliderShapeDesc { Size = size });
        return collider;
    }
}
