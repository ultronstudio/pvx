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
}
