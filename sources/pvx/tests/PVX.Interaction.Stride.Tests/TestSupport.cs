using PVX.Interaction;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace PVX.Interaction.Stride.Tests;

/// <summary>
/// Test-only interactable component (see milestone guidance: production
/// components like doors/ladders are a future milestone's job — this
/// bridge only needs to find whatever implements <see cref="IPVXInteractable"/>).
/// </summary>
internal sealed class TestInteractableComponent : EntityComponent, IPVXInteractable
{
    public Func<PVXInteractionContext, IReadOnlyList<PVXInteractionDescriptor>>? ActionsFactory { get; set; }

    public IReadOnlyList<PVXInteractionDescriptor> GetActions(PVXInteractionContext context) =>
        ActionsFactory?.Invoke(context) ?? Array.Empty<PVXInteractionDescriptor>();
}

internal sealed class TestInteractionAction : IPVXInteractionAction
{
    public int BeginCount { get; private set; }

    public int ExecuteCount { get; private set; }

    public bool CanExecute(PVXInteractionContext context) => true;

    public void Begin(PVXInteractionContext context) => BeginCount++;

    public bool Execute(PVXInteractionContext context)
    {
        ExecuteCount++;
        return true;
    }

    public void Cancel(PVXInteractionContext context)
    {
    }
}

internal static class PhysicsSceneHelpers
{
    /// <summary>
    /// Creates a solid (non-trigger) box collider component, not yet
    /// attached to any entity. Default <see cref="StaticColliderComponent.IsTrigger"/>
    /// is <see langword="false"/>, which is what makes it a genuinely
    /// blocking collider for raycasts (see <see cref="StaticColliderComponent"/>'s
    /// own OnAttach/IsTrigger logic).
    /// </summary>
    public static StaticColliderComponent CreateBoxCollider(Vector3 size, bool isTrigger = false)
    {
        var collider = new StaticColliderComponent { IsTrigger = isTrigger };
        collider.ColliderShapes.Add(new BoxColliderShapeDesc { Size = size });
        return collider;
    }

    /// <summary>
    /// Creates a standalone entity carrying only a solid box collider.
    /// </summary>
    public static Entity CreateStaticBoxCollider(string name, Vector3 position, Vector3 size, bool isTrigger = false)
    {
        var entity = new Entity(name);
        entity.Transform.Position = position;
        entity.Add(CreateBoxCollider(size, isTrigger));
        return entity;
    }

    public static Simulation? GetSimulation(SceneSystem sceneSystem) =>
        sceneSystem.SceneInstance?.GetProcessor<PhysicsProcessor>()?.Simulation;

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
}
