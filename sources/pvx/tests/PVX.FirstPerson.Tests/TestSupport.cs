using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace PVX.FirstPerson.Tests;

internal static class PhysicsSceneHelpers
{
    /// <summary>
    /// Creates a standalone entity carrying only a solid (non-trigger) box
    /// collider, not yet attached to any scene.
    /// </summary>
    public static Entity CreateStaticBoxCollider(string name, Vector3 position, Vector3 size)
    {
        var entity = new Entity(name) { Transform = { Position = position } };
        var collider = new StaticColliderComponent();
        collider.ColliderShapes.Add(new BoxColliderShapeDesc { Size = size });
        entity.Add(collider);
        return entity;
    }

    /// <summary>
    /// Adds <paramref name="entities"/> to the root scene and waits a
    /// couple of frames so Stride's entity/physics processors actually
    /// attach them (same pattern as the engine's own physics tests).
    /// </summary>
    public static async Task AddAndSettle(this Game game, params Entity[] entities)
    {
        foreach (var entity in entities)
        {
            game.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
        }

        await game.Script.NextFrame();
        await game.Script.NextFrame();
    }

    public static async Task RemoveAndSettle(this Game game, params Entity[] entities)
    {
        foreach (var entity in entities)
        {
            entity.Scene = null;
        }

        await game.Script.NextFrame();
    }

    /// <summary>
    /// Same as <see cref="RemoveAndSettle"/>, but tolerant of an entity
    /// that never actually attached (e.g. attaching it threw, as in the
    /// "missing required reference" tests) — <c>entity.Scene</c> is
    /// already null in that case, which plain assignment handles fine, but
    /// this name documents the intent at call sites.
    /// </summary>
    public static Task RemoveAndSettleIfAttached(this Game game, params Entity[] entities) =>
        game.RemoveAndSettle(entities);
}
