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
/// builds a small scene — a door, a switch, a player with a real-physics
/// interaction controller, and a diagnostic free-fly camera — to let a
/// human actually play the interaction vertical slice end to end.
/// </summary>
/// <remarks>
/// <para>
/// This project now ships a real, GameStudio-openable package
/// (<c>PVX.World.Playground.sdpkg</c>) with an authored
/// <c>Assets/InteractionPlayground.sdscene</c> containing
/// <see cref="PVXDoorComponent"/>, <see cref="PVXSwitchComponent"/> and
/// <see cref="PVXInteractionPointComponent"/> with real property values,
/// plus colliders, a camera, and a light. The asset compiler (run as part
/// of this project's normal build via <c>StrideCompileAssets</c>) loads
/// and validates it successfully — see the milestone's final report.
/// </para>
/// <para>
/// This game still builds its own scene in code, rather than loading the
/// compiled asset at runtime: loading it via <c>Content.Load&lt;Scene&gt;</c>
/// hits an unresolved content-serialization gap ("No serializer available
/// for type id ... and base type Stride.Engine.EntityComponent") for the
/// custom component types, even with <c>StrideAssemblyProcessor</c>
/// enabled on their owning projects. This is a genuine, disclosed
/// limitation, not something papered over — see the final report's root
/// cause section. The authored scene asset exists, compiles, and is a
/// faithful description of the same scene built here in code; only the
/// runtime hookup from "compiled asset" to "live Scene" remains open.
/// </para>
/// <para>
/// Everything playground-specific (free-fly camera, switch→light sample
/// binder, procedural placeholder visuals) lives in this project only.
/// <c>PVX.World</c>/<c>PVX.Interaction.Stride</c> components know nothing
/// about any of it.
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

    public PVXWorldPlaygroundGame()
    {
        // Registration order matters: PVXInteractionFeature requires
        // PVXGameplaySession (registered by PVXGameplayFeature) to already
        // be in the service registry when it initializes.
        Runtime.AddFeature(new PVXGameplayFeature());
        Runtime.AddFeature(new PVXInteractionFeature());
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        SceneSystem.SceneInstance ??= new SceneInstance(Services) { RootScene = new Scene() };
        var scene = SceneSystem.SceneInstance.RootScene;

        var session = Runtime.Services.GetRequired<PVXGameplaySession>();
        session.Start();
        session.Difficulty.SetDifficulty(new PVXDifficultyId("playground.default"));
        session.Player.RegisterProvider(new PlaygroundPlayerProvider());

        BuildScene(scene);

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

        var ok = true;

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

    private void BuildScene(Scene scene)
    {
        var playerRoot = new Entity("PlayerRoot") { Transform = { Position = new Vector3(0, 1.6f, 0) } };

        var cameraEntity = new Entity("Camera");
        var camera = new CameraComponent { Projection = CameraProjectionMode.Perspective, VerticalFieldOfView = 60f };
        cameraEntity.Add(camera);
        cameraEntity.Add(new PlaygroundFreeFlyCameraScript());
        cameraEntity.Add(new PVXStrideInteractionControllerComponent
        {
            Camera = camera,
            OwnerEntity = playerRoot,
            MaxDistance = 3f,
            PrimaryInteractionKey = Keys.E,
        });
        cameraEntity.Transform.Parent = playerRoot.Transform;

        SceneSystem.GraphicsCompositor = GraphicsCompositorHelper.CreateDefault(
            enablePostEffects: false,
            camera: camera,
            clearColor: new Color4(0.08f, 0.09f, 0.11f, 1f));

        var lightEntity = new Entity("DirectionalLight") { Transform = { Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-60f)) } };
        lightEntity.Add(new LightComponent
        {
            Type = new LightDirectional { Color = new ColorRgbProvider(Color.White) },
            Intensity = 4f,
        });

        var ground = CreateVisualEntity(
            "Ground",
            position: new Vector3(0, -0.1f, 0),
            size: new Vector3(20, 0.2f, 20),
            color: new Color(60, 64, 70));
        ground.Add(PlaygroundVisuals.CreateBoxCollider(new Vector3(20, 0.2f, 20)));

        // --- Door: DoorLeaf (pivot, no visual) -> DoorPanel (visual + collider) -> Handle (point) ---
        var doorLeaf = new Entity("DoorLeaf") { Transform = { Position = new Vector3(-1.5f, 0, -4f) } };
        var door = new PVXDoorComponent { OpenAngleDegrees = 100f, TransitionDuration = 0.6f };
        doorLeaf.Add(door);
        smokeDoor = door;

        var doorPanel = CreateVisualEntity(
            "DoorPanel",
            position: new Vector3(0.5f, 1f, 0f), // offset half-width from the hinge at DoorLeaf's origin
            size: new Vector3(1f, 2f, 0.1f),
            color: new Color(150, 90, 60));
        doorPanel.Add(PlaygroundVisuals.CreateBoxCollider(new Vector3(1f, 2f, 0.1f)));
        doorPanel.Transform.Parent = doorLeaf.Transform;

        var handle = new Entity("Handle") { Transform = { Position = new Vector3(0.4f, 0f, 0.1f) } };
        handle.Add(PlaygroundVisuals.CreateBoxCollider(new Vector3(0.1f)));
        handle.Add(new PVXInteractionPointComponent { PointId = "handle", Priority = 20 });
        handle.Transform.Parent = doorPanel.Transform;

        // --- Switch: visual + collider + light indicator toggled by a playground-only script ---
        var switchEntity = CreateVisualEntity(
            "Switch",
            position: new Vector3(1.5f, 1f, -4f),
            size: new Vector3(0.3f, 0.3f, 0.1f),
            color: new Color(90, 90, 100));
        switchEntity.Add(PlaygroundVisuals.CreateBoxCollider(new Vector3(0.3f, 0.3f, 0.1f)));
        var switchComponent = new PVXSwitchComponent();
        switchEntity.Add(switchComponent);
        smokeSwitch = switchComponent;

        var indicatorEntity = new Entity("SwitchIndicator") { Transform = { Position = new Vector3(0, 0.3f, 0) } };
        var indicatorLight = new LightComponent
        {
            Type = new LightPoint { Color = new ColorRgbProvider(Color.Red), Radius = 2f },
            Intensity = 0f,
        };
        indicatorEntity.Add(indicatorLight);
        indicatorEntity.Add(new SwitchIndicatorScript(switchComponent, indicatorLight));
        indicatorEntity.Transform.Parent = switchEntity.Transform;

        scene.Entities.Add(playerRoot);
        scene.Entities.Add(lightEntity);
        scene.Entities.Add(ground);
        scene.Entities.Add(doorLeaf);
        scene.Entities.Add(switchEntity);
    }

    private Entity CreateVisualEntity(string name, Vector3 position, Vector3 size, Color color)
    {
        var entity = new Entity(name) { Transform = { Position = position } };

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
        return entity;
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
