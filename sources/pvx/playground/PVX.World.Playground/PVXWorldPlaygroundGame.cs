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
/// builds a small, fully code-constructed scene — a door, a switch, a
/// player with a real-physics interaction controller, and a diagnostic
/// free-fly camera — to let a human actually play the interaction
/// vertical slice end to end.
/// </summary>
/// <remarks>
/// <para>
/// This project is deliberately code-only: it is not editor-openable, and
/// that is a documented, explicit limitation — see the milestone's final
/// report for why (this repository's asset-compilation SDK wiring for a
/// local-source-referencing game package outside Stride's own test
/// harnesses could not be verified, and shipping a hand-authored scene
/// asset that cannot actually be confirmed to open in GameStudio was
/// judged riskier than a fully working, actually-run code-only slice).
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

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (autoExitAfterFrames < 0)
            return;

        frameCount++;

        if (frameCount >= autoExitAfterFrames)
        {
            Console.WriteLine("[PVX Playground] Headless self-test: auto-exit after frame limit reached.");
            Exit();
        }
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
