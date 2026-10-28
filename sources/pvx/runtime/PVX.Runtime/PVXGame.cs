using PVX.Runtime.Internal;
using Stride.Core.Reflection;
using Stride.Engine;

namespace PVX.Runtime;

/// <summary>
/// Base game host for applications built on PVX Engine.
/// </summary>
public class PVXGame : Game
{
    static PVXGame()
    {
        // Must run before anything in the process can touch a vendored binary
        // with a stale baked-in assembly identity (see
        // AssemblyIdentityFallbackResolver's remarks). A static constructor is
        // the earliest hook available to every PVXGame subclass.
        AssemblyIdentityFallbackResolver.Install();
    }

    public PVXGame()
    {
        Runtime = new PVXRuntime();

        // Make PVX runtime discoverable from the inherited Stride
        // service container during the transition period.
        Services.AddService(Runtime);

        var runtimeSystem = new PVXRuntimeSystem(
            Services,
            Runtime);

        GameSystems.Add(runtimeSystem);
    }

    /// <summary>
    /// PVX runtime associated with this game.
    /// </summary>
    public PVXRuntime Runtime { get; }

    protected override void Destroy()
    {
        try
        {
            // Normally shutdown already happened through UnloadContent.
            // This is a final safety net.
            Runtime.Shutdown();
        }
        finally
        {
            base.Destroy();
        }
    }
}
