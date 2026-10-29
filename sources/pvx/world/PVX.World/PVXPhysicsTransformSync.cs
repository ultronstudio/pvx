using Stride.Engine;
using Stride.Physics;

namespace PVX.World;

/// <summary>
/// Pushes an entity's (and its descendants') current transform into
/// Bullet's collision world. Required after directly writing
/// <see cref="TransformComponent"/> values on a static/kinematic collider:
/// <see cref="PhysicsComponent.UpdatePhysicsTransformation"/> is explicitly
/// documented as the way to "manually force movements", since a moved
/// collider is not automatically re-synced by the physics engine.
/// </summary>
/// <remarks>
/// Shared by every <c>PVX.World</c> component that animates its own entity
/// (<see cref="PVXDoorComponent"/>, <see cref="PVXDrawerComponent"/>)
/// rather than letting Bullet drive it. Descendants matter because a
/// handle/knob collider is typically a child of the animated entity: its
/// local transform never changes, but its WORLD transform does whenever
/// the parent moves.
/// </remarks>
public static class PVXPhysicsTransformSync
{
    public static void Sync(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        foreach (var component in entity.Components)
        {
            if (component is PhysicsComponent physicsComponent)
            {
                physicsComponent.UpdatePhysicsTransformation();
            }
        }

        foreach (var child in entity.Transform.Children)
        {
            if (child.Entity is { } childEntity)
            {
                Sync(childEntity);
            }
        }
    }
}
