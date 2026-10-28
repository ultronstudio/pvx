using PVX.Gameplay;
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

    private int frameCount;

    public SandboxFeature(Action requestExit)
    {
        this.requestExit = requestExit ?? throw new ArgumentNullException(nameof(requestExit));
    }

    public override void Initialize(PVXRuntimeContext context)
    {
        Console.WriteLine("[PVX Sandbox] Initialize");
        Console.WriteLine($"[PVX Sandbox] Runtime: PVX Engine {context.Runtime.EngineVersion}");

        RunGameplayDemo(context);
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
    private void RunGameplayDemo(PVXRuntimeContext context)
    {
        var session = context.Services.GetRequired<PVXGameplaySession>();

        Console.WriteLine($"[PVX Sandbox] Gameplay state: {session.State}");

        session.Start();
        Console.WriteLine($"[PVX Sandbox] Gameplay state: {session.State}");

        session.Difficulty.SetDifficulty(new PVXDifficultyId("sandbox.normal"));
        Console.WriteLine($"[PVX Sandbox] Difficulty: {session.Difficulty.Current}");

        session.Player.RegisterProvider(new SandboxPlayerProvider());
        Console.WriteLine("[PVX Sandbox] Primary player registered");

        Console.WriteLine($"[PVX Sandbox] Controls: {session.Control.EffectiveCapabilities}");

        using (session.Control.AcquireRestriction(PVXControlCapabilities.Look, owner: this))
        {
            Console.WriteLine($"[PVX Sandbox] Controls: {session.Control.EffectiveCapabilities}");

            session.Pause();
            Console.WriteLine($"[PVX Sandbox] Gameplay state: {session.State}");
            Console.WriteLine($"[PVX Sandbox] Controls: {session.Control.EffectiveCapabilities}");

            session.Resume();
            Console.WriteLine($"[PVX Sandbox] Gameplay state: {session.State}");
            Console.WriteLine($"[PVX Sandbox] Controls: {session.Control.EffectiveCapabilities}");
        }

        Console.WriteLine($"[PVX Sandbox] Controls: {session.Control.EffectiveCapabilities}");

        session.End();
        Console.WriteLine($"[PVX Sandbox] Gameplay state: {session.State}");
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
