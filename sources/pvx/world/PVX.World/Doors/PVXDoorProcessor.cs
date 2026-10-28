using Stride.Engine;
using Stride.Games;

namespace PVX.World;

/// <summary>
/// Drives every <see cref="PVXDoorComponent"/> in the scene: captures the
/// closed pose on attach, and advances in-progress transitions by real
/// elapsed time each frame.
/// </summary>
/// <remarks>
/// Deliberately a plain per-component loop, not a scene scan: <see cref="ComponentDatas"/>
/// already holds exactly the doors that exist, maintained by the base
/// <see cref="EntityProcessor{T}"/> as entities are added/removed, so
/// there is no reflection, LINQ, or manual tracking here.
/// </remarks>
public sealed class PVXDoorProcessor : EntityProcessor<PVXDoorComponent>
{
    protected override void OnEntityComponentAdding(Entity entity, PVXDoorComponent component, PVXDoorComponent data)
    {
        component.EnsureInitialized();
    }

    public override void Update(GameTime time)
    {
        var elapsed = (float)time.Elapsed.TotalSeconds;

        foreach (var pair in ComponentDatas)
        {
            var door = pair.Key;

            if (!door.Enabled)
                continue;

            door.Advance(elapsed);
        }
    }
}
