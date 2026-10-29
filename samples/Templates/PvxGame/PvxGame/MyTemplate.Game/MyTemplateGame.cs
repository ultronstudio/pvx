using System.Threading.Tasks;
using PVX.Gameplay;
using PVX.Runtime;

namespace MyTemplate;

/// <summary>
/// Minimal PVX game: boots the real PVX runtime (just PVXGameplayFeature — add
/// PVXInteractionFeature, PVX.World, PVX.FirstPerson, etc. as your game needs
/// them) and starts its gameplay session. Loads MainScene.sdscene like any
/// other Stride game — nothing PVX-specific is authored into it; it is here
/// so the project opens and runs immediately.
/// </summary>
public class MyTemplateGame : PVXGame
{
    public MyTemplateGame()
    {
        Runtime.AddFeature(new PVXGameplayFeature());
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        Runtime.Services.GetRequired<PVXGameplaySession>().Start();
    }
}
