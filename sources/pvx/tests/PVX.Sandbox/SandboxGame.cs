using PVX.Gameplay;
using PVX.Runtime;

namespace PVX.Sandbox;

/// <summary>
/// Minimal PVX game host used to validate the PVX runtime lifecycle end to end.
/// </summary>
public sealed class SandboxGame : PVXGame
{
    public SandboxGame()
    {
        // Registered before SandboxFeature so PVXGameplaySession is already
        // in the service registry by the time SandboxFeature initializes.
        Runtime.AddFeature(new PVXGameplayFeature());
        Runtime.AddFeature(new SandboxFeature(Exit));
    }
}
