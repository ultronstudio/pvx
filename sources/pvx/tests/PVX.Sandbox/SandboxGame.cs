using PVX.Runtime;

namespace PVX.Sandbox;

/// <summary>
/// Minimal PVX game host used to validate the PVX runtime lifecycle end to end.
/// </summary>
public sealed class SandboxGame : PVXGame
{
    public SandboxGame()
    {
        Runtime.AddFeature(new SandboxFeature(Exit));
    }
}
