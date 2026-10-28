using PVX.Gameplay;
using PVX.Interaction;
using PVX.Runtime;

namespace PVX.Sandbox;

/// <summary>
/// Minimal PVX game host used to validate the PVX runtime lifecycle end to end.
/// </summary>
public sealed class SandboxGame : PVXGame
{
    public SandboxGame()
    {
        // Registration order matters: PVX runtime features initialize in
        // registration order, and PVXInteractionFeature requires
        // PVXGameplaySession (registered by PVXGameplayFeature) to already
        // be in the service registry. SandboxFeature in turn requires both.
        Runtime.AddFeature(new PVXGameplayFeature());
        var interaction = Runtime.AddFeature(new PVXInteractionFeature());
        Runtime.AddFeature(new SandboxFeature(Exit, interaction));
    }
}
