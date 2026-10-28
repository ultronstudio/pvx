using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace PVX.World.Tests;

internal static class PhysicsTestHelpers
{
    public static StaticColliderComponent CreateBoxCollider(Vector3 size, bool isTrigger = false)
    {
        var collider = new StaticColliderComponent { IsTrigger = isTrigger };
        collider.ColliderShapes.Add(new BoxColliderShapeDesc { Size = size });
        return collider;
    }

    public static Simulation? GetSimulation(SceneSystem sceneSystem) =>
        sceneSystem.SceneInstance?.GetProcessor<PhysicsProcessor>()?.Simulation;

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
}
