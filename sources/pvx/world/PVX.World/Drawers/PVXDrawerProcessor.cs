using Stride.Engine;
using Stride.Games;

namespace PVX.World;

/// <summary>
/// Drives every <see cref="PVXDrawerComponent"/> in the scene: captures the
/// closed pose on attach, and advances in-progress transitions by real
/// elapsed time each frame. Mirrors <see cref="PVXDoorProcessor"/> exactly.
/// </summary>
public sealed class PVXDrawerProcessor : EntityProcessor<PVXDrawerComponent>
{
    protected override void OnEntityComponentAdding(Entity entity, PVXDrawerComponent component, PVXDrawerComponent data)
    {
        component.EnsureInitialized();
    }

    public override void Update(GameTime time)
    {
        var elapsed = (float)time.Elapsed.TotalSeconds;

        foreach (var pair in ComponentDatas)
        {
            var drawer = pair.Key;

            if (!drawer.Enabled)
                continue;

            drawer.Advance(elapsed);
        }
    }
}
