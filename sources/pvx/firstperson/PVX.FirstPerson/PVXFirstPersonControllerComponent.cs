using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Physics;

namespace PVX.FirstPerson;

/// <summary>
/// Authoring entry point for the PVX first-person grounded character
/// foundation: WASD/analog horizontal movement relative to this entity's
/// own yaw, and mouse look split between this entity's yaw and
/// <see cref="Camera"/>'s pitch.
/// </summary>
/// <remarks>
/// <para>
/// Expected hierarchy: this component (and <see cref="Character"/>) live on
/// the player root entity; <see cref="Camera"/> is a child entity's
/// <see cref="CameraComponent"/>. Yaw rotates the player root (so movement,
/// which is computed from yaw only, turns with the view); pitch rotates
/// only the camera entity. This split is what makes "look up and press
/// forward" move the player horizontally instead of flying it upward —
/// movement direction is derived from <see cref="Yaw"/> alone and never
/// reads <see cref="PitchDegrees"/>.
/// </para>
/// <para>
/// All actual math (look accumulation, pitch clamping, local-to-world
/// horizontal movement direction) lives here as plain, Stride-independent
/// methods so it can be unit tested without a live scene/physics world.
/// <see cref="PVXFirstPersonControllerProcessor"/> owns everything that
/// genuinely needs the engine: reading <c>InputManager</c>, mouse
/// capture, gameplay-capability gating, and driving <see cref="Character"/>/
/// the entity transforms from the values computed here.
/// </para>
/// <para>
/// <see cref="DefaultEntityComponentProcessorAttribute.ExecutionMode"/> is
/// pinned to <see cref="ExecutionMode.Runtime"/>: the processor requires a
/// live <c>PVXRuntime</c>/<c>PVXGameplaySession</c> and throws clearly if
/// neither is registered, which is correct in an actual game but would
/// otherwise also fire when an entity carrying this component is merely
/// being displayed in GameStudio's scene editor (<c>ExecutionMode.Editor</c>,
/// no PVX runtime at all) — the component must stay authorable there even
/// though its processor must not run.
/// </para>
/// </remarks>
[DataContract("PVXFirstPersonControllerComponent")]
[Display("PVX First-Person Controller")]
[DefaultEntityComponentProcessor(typeof(PVXFirstPersonControllerProcessor), ExecutionMode = ExecutionMode.Runtime)]
public sealed class PVXFirstPersonControllerComponent : ActivableEntityComponent
{
    /// <summary>
    /// The pitch target. Required: a camera entity, normally a child of
    /// this component's entity.
    /// </summary>
    [DataMember(10)]
    public CameraComponent? Camera { get; set; }

    /// <summary>
    /// The physics body driving grounded movement/collision. Required,
    /// normally on this component's own entity.
    /// </summary>
    [DataMember(20)]
    public CharacterComponent? Character { get; set; }

    /// <summary>
    /// Horizontal movement speed, in world units/second.
    /// </summary>
    [DataMember(30)]
    [Display("Walk Speed")]
    [DefaultValue(4f)]
    public float WalkSpeed { get; set; } = 4f;

    /// <summary>
    /// Mouse look sensitivity, in degrees of rotation per normalized unit
    /// of mouse movement across the screen (<c>InputManager.MouseDelta</c>'s
    /// own units — a fraction of screen size, not raw pixels).
    /// </summary>
    [DataMember(40)]
    [Display("Mouse Sensitivity")]
    [DefaultValue(1000f)]
    public float MouseSensitivity { get; set; } = 1000f;

    /// <summary>
    /// Minimum (most-downward) pitch, in degrees.
    /// </summary>
    [DataMember(50)]
    [Display("Pitch Min (Degrees)")]
    [DefaultValue(-89f)]
    public float PitchMinDegrees { get; set; } = -89f;

    /// <summary>
    /// Maximum (most-upward) pitch, in degrees.
    /// </summary>
    [DataMember(60)]
    [Display("Pitch Max (Degrees)")]
    [DefaultValue(89f)]
    public float PitchMaxDegrees { get; set; } = 89f;

    /// <summary>
    /// Current yaw, in radians. Runtime state, not authored.
    /// </summary>
    [DataMemberIgnore]
    public float Yaw { get; private set; }

    /// <summary>
    /// Current pitch, in degrees, always within
    /// [<see cref="PitchMinDegrees"/>, <see cref="PitchMaxDegrees"/>].
    /// Runtime state, not authored.
    /// </summary>
    [DataMemberIgnore]
    public float PitchDegrees { get; private set; }

    /// <summary>
    /// The player root's yaw-only rotation, derived from <see cref="Yaw"/>.
    /// </summary>
    [DataMemberIgnore]
    public Quaternion YawRotation => Quaternion.RotationY(Yaw);

    /// <summary>
    /// The camera's pitch-only rotation, derived from <see cref="PitchDegrees"/>.
    /// </summary>
    [DataMemberIgnore]
    public Quaternion PitchRotation => Quaternion.RotationX(MathUtil.DegreesToRadians(PitchDegrees));

    /// <summary>
    /// Resets <see cref="Yaw"/>/<see cref="PitchDegrees"/> to a known pose.
    /// Not called automatically: a game/test opts in explicitly (e.g. after
    /// teleporting the player) rather than this component silently
    /// overriding an authored starting orientation on attach.
    /// </summary>
    public void ResetLook(float yaw = 0f, float pitchDegrees = 0f)
    {
        Yaw = yaw;
        PitchDegrees = MathUtil.Clamp(pitchDegrees, PitchMinDegrees, PitchMaxDegrees);
    }

    /// <summary>
    /// Accumulates a frame's mouse-delta look input into <see cref="Yaw"/>/
    /// <see cref="PitchDegrees"/>, clamping pitch. <paramref name="mouseDelta"/>
    /// is in the same units <c>InputManager.MouseDelta</c> reports (a
    /// normalized fraction of screen size moved this frame, not raw
    /// pixels); X is horizontal, Y is vertical.
    /// </summary>
    public void AccumulateLook(Vector2 mouseDelta)
    {
        Yaw -= MathUtil.DegreesToRadians(mouseDelta.X * MouseSensitivity);
        PitchDegrees = MathUtil.Clamp(
            PitchDegrees - mouseDelta.Y * MouseSensitivity,
            PitchMinDegrees,
            PitchMaxDegrees);
    }

    /// <summary>
    /// Computes the world-space, always-horizontal (Y = 0) movement
    /// velocity for a frame of local input, rotated by <see cref="Yaw"/>
    /// only — never by pitch, which is exactly what keeps "look up, press
    /// forward" from flying the player upward. <paramref name="localInput"/>
    /// is in local body space: X is strafe (positive = right), Y is
    /// forward (positive = forward); it is normalized internally if
    /// non-zero, so diagonal input is not faster than axis-aligned input.
    /// </summary>
    public Vector3 ComputeHorizontalVelocity(Vector2 localInput)
    {
        if (localInput == Vector2.Zero)
            return Vector3.Zero;

        if (localInput.LengthSquared() > 1f)
            localInput.Normalize();

        // Local forward is -Z, local right is +X (Stride convention; see
        // Matrix.Forward: (-M31, -M32, -M33), which is -Z at identity).
        var localDirection = new Vector3(localInput.X, 0f, -localInput.Y);
        var worldDirection = Vector3.Transform(localDirection, YawRotation);
        return worldDirection * WalkSpeed;
    }
}
