using Stride.Engine;

namespace PVX.World;

/// <summary>
/// Applies <see cref="PVXNpcComponent.StartsAlert"/> when an NPC first
/// attaches. Mirrors <see cref="PVXSwitchProcessor"/> exactly: no per-frame
/// animation of its own.
/// </summary>
public sealed class PVXNpcProcessor : EntityProcessor<PVXNpcComponent>
{
    protected override void OnEntityComponentAdding(Entity entity, PVXNpcComponent component, PVXNpcComponent data)
    {
        component.EnsureInitialized();
    }
}
