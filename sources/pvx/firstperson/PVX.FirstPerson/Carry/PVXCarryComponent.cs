using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Input;
using Stride.Physics;

namespace PVX.FirstPerson;

/// <summary>
/// Authoring entry point for carrying a <c>PVX.World</c> pickup in front of
/// the camera: holding it at a fixed offset, inspecting it (free rotation
/// while a key is held), dropping it in place, or throwing it.
/// </summary>
/// <remarks>
/// <para>
/// This is the one component in <c>PVX.FirstPerson</c> that reaches outside
/// movement/look: it reacts to <c>PVXInteractionService.InteractionCompleted</c>
/// for <c>PVX.World.PVXPickupComponent</c>'s "pick-up" action (wired by
/// <see cref="PVXCarryProcessor"/>) rather than owning any raycast/targeting
/// of its own — picking the right thing to carry is still the generic
/// interaction pipeline's job, same as every other interactable.
/// </para>
/// <para>
/// While held, the carried entity is reparented under <see cref="Camera"/>'s
/// entity with a fixed local offset (<see cref="HoldDistance"/> in front) so
/// it tracks the camera for free every frame with no per-frame world-space
/// math; inspecting adds an additional local rotation on top. Any
/// <see cref="RigidbodyComponent"/> on the carried entity is switched to
/// kinematic while held (so it stops reacting to gravity/collisions and
/// direct transform writes are respected) and switched back on release,
/// with an initial throw velocity applied if thrown.
/// </para>
/// <para>
/// Deliberately out of scope for 0.1: suppressing the player's own look
/// while inspecting (the held item spins independently on top of normal
/// look — see <see cref="AccumulateInspectRotation"/>), weight/encumbrance,
/// and carrying more than one item at a time (<see cref="TryBeginCarry"/>
/// fails outright while already carrying something).
/// </para>
/// <para>
/// <see cref="DefaultEntityComponentProcessorAttribute.ExecutionMode"/> is
/// pinned to <see cref="ExecutionMode.Runtime"/>: the processor requires a
/// live <c>PVXRuntime</c>/<c>PVXInteractionFeature</c> and throws clearly
/// if neither is registered — correct in an actual game, but would also
/// fire merely from displaying this component in GameStudio's scene
/// editor (<c>ExecutionMode.Editor</c>, no PVX runtime at all).
/// </para>
/// </remarks>
[DataContract("PVXCarryComponent")]
[Display("PVX Carry")]
[DefaultEntityComponentProcessor(typeof(PVXCarryProcessor), ExecutionMode = ExecutionMode.Runtime)]
public sealed class PVXCarryComponent : ActivableEntityComponent
{
    /// <summary>
    /// The camera the carried item is held in front of. Required.
    /// </summary>
    [DataMember(10)]
    public CameraComponent? Camera { get; set; }

    /// <summary>
    /// Distance, in world units, in front of <see cref="Camera"/> the
    /// carried item is held.
    /// </summary>
    [DataMember(20)]
    [Display("Hold Distance")]
    [DefaultValue(1f)]
    public float HoldDistance { get; set; } = 1f;

    /// <summary>
    /// Speed, in world units/second, applied along the camera's forward
    /// direction when a carried item is thrown.
    /// </summary>
    [DataMember(30)]
    [Display("Throw Force")]
    [DefaultValue(8f)]
    public float ThrowForce { get; set; } = 8f;

    /// <summary>
    /// Key that drops the carried item in place (no throw velocity).
    /// </summary>
    [DataMember(40)]
    [Display("Drop Key")]
    [DefaultValue(Keys.G)]
    public Keys DropKey { get; set; } = Keys.G;

    /// <summary>
    /// Mouse button that throws the carried item.
    /// </summary>
    [DataMember(50)]
    [Display("Throw Button")]
    [DefaultValue(MouseButton.Left)]
    public MouseButton ThrowButton { get; set; } = MouseButton.Left;

    /// <summary>
    /// Mouse button that, while held, rotates the carried item (for
    /// examining it) instead of the normal camera look.
    /// </summary>
    [DataMember(60)]
    [Display("Inspect Button")]
    [DefaultValue(MouseButton.Right)]
    public MouseButton InspectButton { get; set; } = MouseButton.Right;

    /// <summary>
    /// Rotation, in degrees per normalized unit of mouse movement, applied
    /// to the carried item while inspecting. Same units as
    /// <see cref="PVXFirstPersonControllerComponent.MouseSensitivity"/>.
    /// </summary>
    [DataMember(70)]
    [Display("Inspect Rotation Speed")]
    [DefaultValue(1000f)]
    public float InspectRotationSpeed { get; set; } = 1000f;

    /// <summary>
    /// The entity currently being carried, or <see langword="null"/> if none.
    /// Runtime state, not authored.
    /// </summary>
    [DataMemberIgnore]
    public Entity? CarriedEntity { get; private set; }

    /// <summary>
    /// Whether the carried item is currently being rotated for inspection.
    /// Runtime state, not authored; has no effect while <see cref="CarriedEntity"/> is <see langword="null"/>.
    /// </summary>
    [DataMemberIgnore]
    public bool IsInspecting { get; set; }

    /// <summary>
    /// Begins carrying <paramref name="target"/>: reparents it under
    /// <see cref="Camera"/>'s entity at <see cref="HoldDistance"/>, and
    /// switches any <see cref="RigidbodyComponent"/> on it to kinematic.
    /// No-op (returns <see langword="false"/>) while already carrying
    /// something, if <paramref name="target"/> is <see langword="null"/>,
    /// or if <see cref="Camera"/> is unset.
    /// </summary>
    public bool TryBeginCarry(Entity? target)
    {
        if (CarriedEntity is not null || target is null || Camera?.Entity is not { } cameraEntity)
            return false;

        if (GetRigidbody(target) is { } rigidbody)
        {
            rigidbody.IsKinematic = true;
            rigidbody.LinearVelocity = Vector3.Zero;
            rigidbody.AngularVelocity = Vector3.Zero;
        }

        target.Transform.Parent = cameraEntity.Transform;
        target.Transform.Position = new Vector3(0f, 0f, -HoldDistance);
        target.Transform.Rotation = Quaternion.Identity;
        PVX.World.PVXPhysicsTransformSync.Sync(target);

        CarriedEntity = target;
        IsInspecting = false;
        return true;
    }

    /// <summary>
    /// Ends carrying the current <see cref="CarriedEntity"/>, unparenting
    /// it back to the scene root at its current world pose, restoring any
    /// <see cref="RigidbodyComponent"/> to dynamic, and optionally applying
    /// a throw velocity along the camera's forward direction. No-op
    /// (returns <see langword="null"/>) while not carrying anything.
    /// </summary>
    public Entity? EndCarry(bool throwIt)
    {
        if (CarriedEntity is not { } target)
            return null;

        // WorldMatrix is a plain field, not recomputed on read: force it
        // current before decomposing (it would otherwise still reflect
        // whatever pose existed before this frame's reparenting/inspect
        // rotation, outside a live scene where TransformProcessor would
        // have already refreshed it).
        target.Transform.UpdateWorldMatrix();
        var worldMatrix = target.Transform.WorldMatrix;
        worldMatrix.Decompose(out _, out Quaternion worldRotation, out Vector3 worldPosition);

        target.Transform.Parent = null;
        target.Transform.Position = worldPosition;
        target.Transform.Rotation = worldRotation;

        if (GetRigidbody(target) is { } rigidbody)
        {
            rigidbody.IsKinematic = false;

            if (throwIt)
            {
                var forward = Vector3.Transform(-Vector3.UnitZ, worldRotation);
                rigidbody.LinearVelocity = forward * ThrowForce;
            }
        }

        PVX.World.PVXPhysicsTransformSync.Sync(target);

        CarriedEntity = null;
        IsInspecting = false;
        return target;
    }

    /// <summary>
    /// Accumulates a frame's mouse-delta look input as an additional local
    /// rotation on <see cref="CarriedEntity"/>, on top of whatever pose
    /// <see cref="TryBeginCarry"/> set. No-op while not carrying anything.
    /// Unclamped, unlike normal player look: examining an object from any
    /// angle, including upside down, is the point.
    /// </summary>
    public void AccumulateInspectRotation(Vector2 mouseDelta)
    {
        if (CarriedEntity is not { } target)
            return;

        var yaw = MathUtil.DegreesToRadians(mouseDelta.X * InspectRotationSpeed);
        var pitch = MathUtil.DegreesToRadians(mouseDelta.Y * InspectRotationSpeed);
        target.Transform.Rotation *= Quaternion.RotationYawPitchRoll(yaw, pitch, 0f);
    }

    private static RigidbodyComponent? GetRigidbody(Entity entity) => entity.Get<RigidbodyComponent>();
}
