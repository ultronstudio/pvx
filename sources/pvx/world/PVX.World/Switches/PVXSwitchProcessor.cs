using Stride.Engine;

namespace PVX.World;

/// <summary>
/// Applies <see cref="PVXSwitchComponent.StartsOn"/> when a switch first
/// attaches. A switch has no per-frame animation of its own, so this
/// processor does not override <see cref="EntityProcessor{T}.Update"/>.
/// </summary>
public sealed class PVXSwitchProcessor : EntityProcessor<PVXSwitchComponent>
{
    protected override void OnEntityComponentAdding(Entity entity, PVXSwitchComponent component, PVXSwitchComponent data)
    {
        component.EnsureInitialized();
    }
}
