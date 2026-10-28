using PVX.Gameplay;
using PVX.Interaction;
using PVX.Interaction.Stride;
using PVX.Runtime;
using PVX.World;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Compositing;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;

namespace PVX.World.Playground;

/// <summary>
/// Manual-test playground game: boots the real PVX runtime (Gameplay +
/// Interaction features, exactly like a production PVX game would), then
/// loads its scene from the compiled <c>InteractionPlayground</c> asset —
/// a door, a switch, and a player capsule with a real-physics interaction
/// controller — to let a human actually play the interaction vertical
/// slice end to end.
/// </summary>
/// <remarks>
/// <para>
/// This project ships a real, GameStudio-openable package
/// (<c>PVX.World.Playground.sdpkg</c>) with an authored
/// <c>Assets/InteractionPlayground.sdscene</c> containing
/// <see cref="PVXDoorComponent"/>, <see cref="PVXSwitchComponent"/> and
/// <see cref="PVXInteractionPointComponent"/> with real property values,
/// plus colliders, a camera, and a light. <see cref="Content.Load{Scene}"/>
/// of that asset is the canonical runtime path — this game does not
/// reconstruct an equivalent scene in C#.
/// </para>
/// <para>
/// Root cause of why that load used to fail ("No serializer available for
/// type id ... and base type Stride.Engine.EntityComponent"): the
/// serializer registration an AssemblyProcessor-generated module
/// initializer installs for <c>PVX.World</c>/<c>PVX.Interaction.Stride</c>
/// only runs once the CLR actually loads that assembly — and nothing in a
/// from-scratch process had done so yet by the time the very first
/// <c>Content.Load</c> ran, since merely referencing a type by name (a
/// field type, a <c>using</c>) does not force that. <see cref="PVXContentRegistration.EnsureLoaded{T}"/>,
/// called in this class's constructor before anything else, forces it
/// deterministically instead of relying on an incidental earlier
/// construction of one of these types to have done it by accident.
/// </para>
/// <para>
/// Everything playground-specific (free-fly camera, the interaction
/// controller, the switch→light sample binder, procedural placeholder
/// visuals) is attached in code, after loading, onto entities found by
/// name in the loaded scene. <c>PVX.World</c>/<c>PVX.Interaction.Stride</c>
/// components know nothing about any of it, and none of it is authored
/// into the scene asset itself.
/// </para>
/// </remarks>
public sealed class PVXWorldPlaygroundGame : PVXGame
{
    private int autoExitAfterFrames = -1;
    private int frameCount;
    private bool smokeVerificationRequested;
    private bool smokeVerificationDone;
    private PVXDoorComponent? smokeDoor;
    private PVXSwitchComponent? smokeSwitch;
    private Entity? smokeHandleEntity;
    private PVXInteractionPointComponent? smokeHandle;

    public PVXWorldPlaygroundGame()
    {
        // Must happen before anything in this process can hit the first
        // Content.Load of a scene referencing a type from these assemblies
        // (see PVXContentRegistration's remarks).
        PVXContentRegistration.EnsureLoaded<PVXDoorComponent>();
        PVXContentRegistration.EnsureLoaded<PVXInteractionPointComponent>();

        // Registration order matters: PVXInteractionFeature requires
        // PVXGameplaySession (registered by PVXGameplayFeature) to already
        // be in the service registry when it initializes.
        Runtime.AddFeature(new PVXGameplayFeature());
        Runtime.AddFeature(new PVXInteractionFeature());
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        var scene = Content.Load<Scene>("InteractionPlayground");
        SceneSystem.SceneInstance = new SceneInstance(Services) { RootScene = scene };

        var session = Runtime.Services.GetRequired<PVXGameplaySession>();
        session.Start();
        session.Difficulty.SetDifficulty(new PVXDifficultyId("playground.default"));
        session.Player.RegisterProvider(new PlaygroundPlayerProvider());

        DecorateScene(scene);

        Console.WriteLine("[PVX Playground] Ready.");
        Console.WriteLine("[PVX Playground] Mouse: look (hold right mouse button). W/A/S/D: move. E: interact. Esc: exit.");
    }

    /// <summary>
    /// Opt-in, headless-mode-only startup verification: requests exit
    /// after a fixed number of frames instead of running until the user
    /// closes the window. Never used by the normal interactive run.
    /// </summary>
    public void AutoExitAfterFrames(int frames) => autoExitAfterFrames = frames;

    /// <summary>
    /// Opt-in, headless-mode-only verification that the standalone process
    /// didn't just start, but actually initialized real gameplay state:
    /// Door/Switch exist in their expected initial state, and transition
    /// correctly through their own public API (the same API the real
    /// interaction pipeline drives — the raycast/focus portion of that
    /// pipeline is already covered end to end by the xunit real-physics
    /// suites, so this exercises the part specific to a standalone process:
    /// that the door/switch/physics machinery actually runs here at all).
    /// Prints a PASS/FAIL line Program.cs's smoke mode can key off, and
    /// requests a non-zero exit (via <see cref="SmokeVerificationFailed"/>)
    /// on failure.
    /// </summary>
    public void RequestHeadlessSmokeVerification() => smokeVerificationRequested = true;

    public bool SmokeVerificationFailed { get; private set; }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (smokeVerificationRequested && !smokeVerificationDone && frameCount >= 2)
            RunSmokeVerification();

        if (autoExitAfterFrames < 0)
            return;

        frameCount++;

        if (frameCount >= autoExitAfterFrames)
        {
            Console.WriteLine("[PVX Playground] Headless self-test: auto-exit after frame limit reached.");
            Exit();
        }
    }

    private void RunSmokeVerification()
    {
        smokeVerificationDone = true;

        var door = smokeDoor ?? throw new InvalidOperationException("Smoke verification requested but no door was built.");
        var switchComponent = smokeSwitch ?? throw new InvalidOperationException("Smoke verification requested but no switch was built.");
        var handle = smokeHandle ?? throw new InvalidOperationException("Smoke verification requested but no interaction point was found.");
        var handleEntity = smokeHandleEntity ?? throw new InvalidOperationException("Smoke verification requested but no handle entity was found.");

        var ok = true;

        // Authored property values survived the full
        // PVX Studio -> asset compiler -> Content.Load round trip, not
        // just the component types.
        ok &= Check("Door OpenAngleDegrees authored value survived compile+load", door.OpenAngleDegrees == 100f);
        ok &= Check("Door TransitionDuration authored value survived compile+load", door.TransitionDuration == 0.6f);
        ok &= Check("Handle PointId authored value survived compile+load", handle.PointId == "handle");
        ok &= Check("Handle Priority authored value survived compile+load", handle.Priority == 20);

        // Hierarchy survived: Handle -> DoorPanel -> DoorLeaf, exactly as authored.
        var doorPanelTransform = handleEntity.Transform.Parent;
        var doorPanelEntity = doorPanelTransform?.Entity;
        ok &= Check("Handle's parent is DoorPanel", doorPanelEntity?.Name == "DoorPanel");
        ok &= Check("DoorPanel's parent is DoorLeaf", doorPanelTransform?.Parent?.Entity?.Name == "DoorLeaf");

        ok &= Check("Door initial state is Closed", door.State == PVXDoorState.Closed);
        ok &= Check("Door.TryOpen() returns true", door.TryOpen());
        // TryOpen() only starts the Opening transition; let it run to completion.
        for (var i = 0; i < 120 && door.State != PVXDoorState.Open; i++)
            door.Advance(1f / 60f);
        ok &= Check("Door reaches Open state", door.State == PVXDoorState.Open);

        ok &= Check("Switch initial state is Off", switchComponent.State == PVXSwitchState.Off);
        switchComponent.Toggle();
        ok &= Check("Switch reaches On state after Toggle()", switchComponent.State == PVXSwitchState.On);

        SmokeVerificationFailed = !ok;
        Console.WriteLine(ok ? "[PVX Playground Smoke] PASS" : "[PVX Playground Smoke] FAIL");
    }

    private static bool Check(string description, bool condition)
    {
        Console.WriteLine($"[PVX Playground Smoke] {description}: {(condition ? "OK" : "FAILED")}");
        return condition;
    }

    /// <summary>
    /// Attaches everything that is deliberately NOT authored into
    /// <c>InteractionPlayground.sdscene</c> — the free-fly camera, the
    /// physics-raycast interaction controller, the graphics compositor,
    /// the switch's light-indicator sample binder, and procedural
    /// placeholder visuals for the colliders the asset already describes —
    /// onto the entities the compiled scene actually produced. Every
    /// transform, collider and PVX component value comes from the loaded
    /// scene; nothing gameplay-relevant is reconstructed here.
    /// </summary>
    private void DecorateScene(Scene scene)
    {
        var playerRoot = FindEntity(scene, "PlayerRoot") ?? throw new InvalidOperationException("InteractionPlayground.sdscene has no 'PlayerRoot' entity.");
        var cameraEntity = FindEntity(scene, "Camera") ?? throw new InvalidOperationException("InteractionPlayground.sdscene has no 'Camera' entity.");
        var camera = cameraEntity.Get<CameraComponent>() ?? throw new InvalidOperationException("'Camera' entity has no CameraComponent.");

        cameraEntity.Add(new PlaygroundFreeFlyCameraScript());
        cameraEntity.Add(new PVXStrideInteractionControllerComponent
        {
            Camera = camera,
            OwnerEntity = playerRoot,
            MaxDistance = 3f,
            PrimaryInteractionKey = Keys.E,
        });

        SceneSystem.GraphicsCompositor = GraphicsCompositorHelper.CreateDefault(
            enablePostEffects: false,
            camera: camera,
            clearColor: new Color4(0.08f, 0.09f, 0.11f, 1f));

        AttachVisual(FindEntity(scene, "Ground"), new Vector3(20, 0.2f, 20), new Color(60, 64, 70));
        AttachVisual(FindEntity(scene, "DoorPanel"), new Vector3(1f, 2f, 0.1f), new Color(150, 90, 60));
        AttachVisual(FindEntity(scene, "Switch"), new Vector3(0.3f, 0.3f, 0.1f), new Color(90, 90, 100));

        var doorLeaf = FindEntity(scene, "DoorLeaf") ?? throw new InvalidOperationException("InteractionPlayground.sdscene has no 'DoorLeaf' entity.");
        smokeDoor = doorLeaf.Get<PVXDoorComponent>() ?? throw new InvalidOperationException("'DoorLeaf' entity has no PVXDoorComponent.");

        var switchEntity = FindEntity(scene, "Switch") ?? throw new InvalidOperationException("InteractionPlayground.sdscene has no 'Switch' entity.");
        smokeSwitch = switchEntity.Get<PVXSwitchComponent>() ?? throw new InvalidOperationException("'Switch' entity has no PVXSwitchComponent.");

        smokeHandleEntity = FindEntity(scene, "Handle") ?? throw new InvalidOperationException("InteractionPlayground.sdscene has no 'Handle' entity.");
        smokeHandle = smokeHandleEntity.Get<PVXInteractionPointComponent>() ?? throw new InvalidOperationException("'Handle' entity has no PVXInteractionPointComponent.");

        var indicatorEntity = new Entity("SwitchIndicator") { Transform = { Position = new Vector3(0, 0.3f, 0) } };
        var indicatorLight = new LightComponent
        {
            Type = new LightPoint { Color = new ColorRgbProvider(Color.Red), Radius = 2f },
            Intensity = 0f,
        };
        indicatorEntity.Add(indicatorLight);
        indicatorEntity.Add(new SwitchIndicatorScript(smokeSwitch, indicatorLight));
        indicatorEntity.Transform.Parent = switchEntity.Transform;
    }

    /// <summary>
    /// Depth-first search for an entity by name across the whole scene
    /// (root entities and every descendant reachable through
    /// <see cref="TransformComponent.Children"/>), since the compiled
    /// scene is a hierarchy rather than a flat, directly-indexable list.
    /// </summary>
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

    private void AttachVisual(Entity? entity, Vector3 size, Color color)
    {
        if (entity is null)
            return;

        var model = new Model();
        new ProceduralModelDescriptor(new CubeProceduralModel { Size = size }).GenerateModel(Services, model);
        model.Materials.Add(Material.New(GraphicsDevice, new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color)),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            },
        }));

        entity.Add(new ModelComponent { Model = model });
    }

    private sealed class PlaygroundPlayer : IPVXPlayer
    {
    }

    private sealed class PlaygroundPlayerProvider : IPVXPlayerProvider
    {
        private readonly PlaygroundPlayer player = new();

        public IPVXPlayer? GetPrimaryPlayer() => player;
    }
}
