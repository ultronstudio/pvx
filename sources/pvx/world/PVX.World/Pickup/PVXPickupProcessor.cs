using Stride.Engine;

namespace PVX.World;

/// <summary>
/// Registered as <see cref="PVXPickupComponent"/>'s default processor so
/// the component type participates in the normal entity-component
/// lifecycle. A pickup has no per-frame animation or deferred
/// initialization of its own (unlike <see cref="PVXDoorComponent"/>'s
/// captured closed pose), so there is nothing to do here yet.
/// </summary>
public sealed class PVXPickupProcessor : EntityProcessor<PVXPickupComponent>
{
}
