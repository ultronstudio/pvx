using PVX.Gameplay;
using PVX.Interaction;
using PVX.Runtime;

namespace PVX.Sandbox;

/// <summary>
/// Exercises the PVX runtime lifecycle (initialize, update, shutdown) and
/// requests the host game to exit after a small, fixed number of updates,
/// so the sandbox is deterministic and safe to run repeatedly.
/// </summary>
public sealed class SandboxFeature : PVXRuntimeFeature
{
    private const int MaxLoggedFrames = 3;
    private const int MaxFrames = 10;

    private readonly Action requestExit;
    private readonly PVXInteractionFeature interactionFeature;

    private int frameCount;

    public SandboxFeature(Action requestExit, PVXInteractionFeature interactionFeature)
    {
        this.requestExit = requestExit ?? throw new ArgumentNullException(nameof(requestExit));
        this.interactionFeature = interactionFeature ?? throw new ArgumentNullException(nameof(interactionFeature));
    }

    public override void Initialize(PVXRuntimeContext context)
    {
        Console.WriteLine("[PVX Sandbox] Initialize");
        Console.WriteLine($"[PVX Sandbox] Runtime: PVX Engine {context.Runtime.EngineVersion}");

        var session = context.Services.GetRequired<PVXGameplaySession>();

        RunGameplayDemo(session);
        RunInteractionDemo(context, session);

        session.End();
        Console.WriteLine($"[PVX Sandbox] Gameplay state: {session.State}");
    }

    public override void Update(in PVXFrameTime time)
    {
        frameCount++;

        if (frameCount <= MaxLoggedFrames)
        {
            Console.WriteLine($"[PVX Sandbox] Update frame {frameCount}");
        }

        if (frameCount >= MaxFrames)
        {
            requestExit();
        }
    }

    public override void Shutdown()
    {
        Console.WriteLine("[PVX Sandbox] Shutdown");
    }

    /// <summary>
    /// Exercises PVX.Gameplay through the <see cref="PVXGameplaySession"/>
    /// registered by <see cref="PVXGameplayFeature"/>: lifecycle, primary
    /// player, difficulty, and composable control restrictions.
    /// </summary>
    private void RunGameplayDemo(PVXGameplaySession session)
    {
        Console.WriteLine($"[PVX Sandbox] Gameplay state: {session.State}");

        session.Start();
        Console.WriteLine($"[PVX Sandbox] Gameplay state: {session.State}");

        session.Difficulty.SetDifficulty(new PVXDifficultyId("sandbox.normal"));
        Console.WriteLine($"[PVX Sandbox] Difficulty: {session.Difficulty.Current}");

        session.Player.RegisterProvider(new SandboxPlayerProvider());
        Console.WriteLine("[PVX Sandbox] Primary player registered");

        Console.WriteLine($"[PVX Sandbox] Controls: {session.Control.EffectiveCapabilities}");
    }

    /// <summary>
    /// Exercises PVX.Interaction end to end through the
    /// <see cref="PVXInteractionService"/> registered by
    /// <see cref="PVXInteractionFeature"/>: target discovery, focus,
    /// multiple actions, instant and hold interactions, the ladder
    /// hand-off acceptance case, and Gameplay.Interact/pause integration.
    /// </summary>
    private void RunInteractionDemo(PVXRuntimeContext context, PVXGameplaySession session)
    {
        var service = context.Services.GetRequired<PVXInteractionService>();
        var provider = new SandboxTargetProvider();
        service.TargetProvider = provider;

        var source = new SandboxInteractionSource();
        var traversal = new SandboxTraversalHandler();

        // --- Door: instant interaction ---------------------------------
        var door = new SandboxDoor();
        provider.Current = new PVXInteractionCandidate(door);
        service.Update(source, TimeSpan.Zero);
        Console.WriteLine($"[PVX Sandbox] Interaction focus: SandboxDoor, primary action: {service.PrimaryAction?.ActionId}");

        service.BeginPrimaryInteraction(source);
        Console.WriteLine($"[PVX Sandbox] Interaction state after Open: ActiveInteraction={service.ActiveInteraction is not null}");

        // --- Hold object: begin, partial progress, cancel ---------------
        var holdObject = new SandboxHoldObject();
        provider.Current = new PVXInteractionCandidate(holdObject);
        service.Update(source, TimeSpan.Zero);
        Console.WriteLine($"[PVX Sandbox] Interaction focus: SandboxHoldObject, primary action: {service.PrimaryAction?.ActionId}");

        service.BeginPrimaryInteraction(source);
        service.Update(source, TimeSpan.FromSeconds(1.0));
        Console.WriteLine($"[PVX Sandbox] Hold progress: {service.ActiveInteraction?.Progress:P0}");

        service.CancelActiveInteraction();
        Console.WriteLine($"[PVX Sandbox] Hold cancelled: ActiveInteraction={service.ActiveInteraction is not null}");

        // --- Ladder: bottom entry hands off to a fake traversal handler --
        var ladder = new SandboxLadder(traversal);
        provider.Current = new PVXInteractionCandidate(ladder, SandboxLadder.BottomEntry);
        service.Update(source, TimeSpan.Zero);
        Console.WriteLine($"[PVX Sandbox] Interaction focus: SandboxLadder.BottomEntry, primary action: {service.PrimaryAction?.ActionId}");

        service.BeginPrimaryInteraction(source);

        // --- Pause blocks execution; resume does not auto-restart --------
        session.Pause();
        Console.WriteLine($"[PVX Sandbox] Gameplay state: {session.State}, Controls: {session.Control.EffectiveCapabilities}");

        var blockedResult = service.BeginPrimaryInteraction(source);
        Console.WriteLine($"[PVX Sandbox] Interaction begin while paused: allowed={blockedResult}");

        session.Resume();
        Console.WriteLine($"[PVX Sandbox] Gameplay state: {session.State}, Controls: {session.Control.EffectiveCapabilities}");

        // Let the remaining per-frame updates drive the service through
        // PVXInteractionFeature.Update too, not only this manual demo.
        interactionFeature.Source = source;
    }

    private sealed class SandboxPlayer : IPVXPlayer
    {
    }

    private sealed class SandboxPlayerProvider : IPVXPlayerProvider
    {
        private readonly SandboxPlayer player = new();

        public IPVXPlayer? GetPrimaryPlayer() => player;
    }
}
