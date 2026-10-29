using Stride.Engine;

namespace PVX.Dialogue;

/// <summary>
/// Registered as <see cref="PVXDialogueComponent"/>'s default processor so
/// the component type participates in the normal entity-component
/// lifecycle. A dialogue component has no per-frame animation of its
/// own — advancing only happens on interaction — so there is nothing to
/// do here yet (mirrors <see cref="PVX.World.PVXPickupProcessor"/>).
/// </summary>
public sealed class PVXDialogueProcessor : EntityProcessor<PVXDialogueComponent>
{
}
