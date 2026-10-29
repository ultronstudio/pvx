using System.ComponentModel;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Input;

namespace PVX.Interaction.Stride;

/// <summary>
/// Authoring entry point for the minimal scene-runtime glue needed to
/// actually play PVX interactions: binds a camera to a
/// <see cref="PVXStrideCameraInteractionSource"/>, binds the scene's
/// physics simulation to a <see cref="PVXStridePhysicsTargetProvider"/>,
/// wires both into the running <c>PVXInteractionFeature</c>/<c>PVXInteractionService</c>,
/// and turns a single key press into <c>PVXInteractionService.BeginPrimaryInteraction</c>.
/// </summary>
/// <remarks>
/// This is intentionally the only place in the Stride bridge that reads
/// input: <c>PVX.World</c> components (doors, switches, ...) must stay
/// input-agnostic, reacting only to interaction actions, never to key
/// state directly. See <see cref="PVXStrideInteractionControllerProcessor"/>
/// for the actual wiring/lifecycle and its ownership policy.
/// <para>
/// <see cref="DefaultEntityComponentProcessorAttribute.ExecutionMode"/> is
/// pinned to <see cref="ExecutionMode.Runtime"/>: the processor requires a
/// live <c>PVXRuntime</c>/<c>PVXInteractionFeature</c> and throws clearly
/// if neither is registered — correct in an actual game, but would also
/// fire merely from displaying this component in GameStudio's scene
/// editor (<c>ExecutionMode.Editor</c>, no PVX runtime at all).
/// </para>
/// </remarks>
[DataContract("PVXStrideInteractionControllerComponent")]
[Display("PVX Interaction Controller")]
[DefaultEntityComponentProcessor(typeof(PVXStrideInteractionControllerProcessor), ExecutionMode = ExecutionMode.Runtime)]
public sealed class PVXStrideInteractionControllerComponent : ActivableEntityComponent
{
    /// <summary>
    /// Camera the interaction ray is cast from. Required.
    /// </summary>
    [DataMember(10)]
    public CameraComponent? Camera { get; set; }

    /// <summary>
    /// Entity whose own collider and descendants are ignored by the
    /// physics target provider (typically the player root). Optional.
    /// </summary>
    [DataMember(20)]
    public Entity? OwnerEntity { get; set; }

    /// <summary>
    /// Maximum interaction distance. Generic/configurable on purpose —
    /// never hardcoded in the provider itself.
    /// </summary>
    [DataMember(30)]
    [DefaultValue(2.5f)]
    public float MaxDistance { get; set; } = 2.5f;

    /// <summary>
    /// Key that begins the primary interaction on a key-down edge (not
    /// while held). Input rebinding, action maps, and gamepad support are
    /// explicitly out of scope for this milestone.
    /// </summary>
    [DataMember(40)]
    [DefaultValue(Keys.E)]
    public Keys PrimaryInteractionKey { get; set; } = Keys.E;
}
