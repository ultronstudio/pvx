using System;
using System.Threading.Tasks;
using PVX.FirstPerson;
using PVX.Gameplay;
using PVX.Interaction;
using PVX.Interaction.Stride;
using PVX.Runtime;
using PVX.World;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering.Compositing;

namespace MyTemplate;

public class MyTemplateGame : PVXGame
{
    public MyTemplateGame()
    {
        // Must happen before this process's first Content.Load of a scene
        // referencing a type from these assemblies — see
        // PVXContentRegistration's remarks.
        PVXContentRegistration.EnsureLoaded<PVXDoorComponent>();
        PVXContentRegistration.EnsureLoaded<PVXInteractionPointComponent>();
        PVXContentRegistration.EnsureLoaded<PVXFirstPersonControllerComponent>();

        // Registration order matters: PVXInteractionFeature requires
        // PVXGameplaySession (registered by PVXGameplayFeature) to already
        // be in the service registry when it initializes.
        Runtime.AddFeature(new PVXGameplayFeature());
        Runtime.AddFeature(new PVXInteractionFeature());
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        var scene = Content.Load<Scene>("MainScene");
        SceneSystem.SceneInstance = new SceneInstance(Services) { RootScene = scene };

        Runtime.Services.GetRequired<PVXGameplaySession>().Start();

        WireScene(scene);
    }

    /// <summary>
    /// Wires the two things <c>MainScene.sdscene</c> deliberately does not
    /// author itself: the graphics compositor (found via whichever entity
    /// in the loaded scene carries a <see cref="CameraComponent"/>) and the
    /// interaction controller's self-ignore owner (the player root entity,
    /// so its own collider/descendants never occlude its own interaction
    /// raycast).
    /// </summary>
    private void WireScene(Scene scene)
    {
        var playerRoot = FindEntity(scene, "Player") ?? throw new InvalidOperationException("MainScene.sdscene has no 'Player' entity.");
        var cameraEntity = FindEntity(scene, "Camera") ?? throw new InvalidOperationException("MainScene.sdscene has no 'Camera' entity.");
        var camera = cameraEntity.Get<CameraComponent>() ?? throw new InvalidOperationException("'Camera' entity has no CameraComponent.");

        var interactionController = cameraEntity.Get<PVXStrideInteractionControllerComponent>();
        if (interactionController is not null)
            interactionController.OwnerEntity = playerRoot;

        SceneSystem.GraphicsCompositor = GraphicsCompositorHelper.CreateDefault(
            enablePostEffects: false,
            camera: camera,
            clearColor: new Color4(0.03f, 0.03f, 0.04f, 1f));
    }

    private static Entity? FindEntity(Scene scene, string name)
    {
        foreach (var entity in scene.Entities)
        {
            if (FindEntity(entity, name) is { } found)
                return found;
        }
        return null;
    }

    private static Entity? FindEntity(Entity entity, string name)
    {
        if (entity.Name == name)
            return entity;

        foreach (var child in entity.Transform.Children)
        {
            if (child.Entity is { } childEntity && FindEntity(childEntity, name) is { } found)
                return found;
        }

        return null;
    }
}
