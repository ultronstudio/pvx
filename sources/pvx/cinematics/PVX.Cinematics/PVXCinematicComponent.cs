using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace PVX.Cinematics;

/// <summary>
/// A transform-animated camera-rig path: plays through an authored
/// sequence of <see cref="PVXCinematicWaypoint"/>s by moving this
/// component's own entity, the same way <see cref="PVX.World"/>'s
/// Door/Drawer animate their own entity rather than owning a separate
/// rigid-body/constraint.
/// </summary>
/// <remarks>
/// <para>
/// This component only owns the path/playback state machine and its own
/// entity's transform. It never touches a camera, the graphics
/// compositor, or <c>PVXControlCapabilities</c> directly — a game wires an
/// actual <see cref="CameraComponent"/> as a child of (or onto) this
/// entity and swaps the active camera itself, the same way
/// <c>ShadowlessGame.WireScene</c> wires a scene's camera into the
/// compositor. Only <see cref="PVXCinematicProcessor"/> reaches outside
/// this component, to optionally strip player control for the cutscene's
/// duration via <see cref="RestrictPlayerControlWhilePlaying"/>.
/// </para>
/// <para>
/// Deliberately out of scope for 0.1: branching paths, looping,
/// pause/resume mid-leg, and any camera-specific behavior (field of view
/// animation, look-at targets) — this only moves a transform through a
/// sequence of poses.
/// </para>
/// <para>
/// <see cref="DefaultEntityComponentProcessorAttribute.ExecutionMode"/> is
/// pinned to <see cref="ExecutionMode.Runtime"/>: the processor requires a
/// live <c>PVXRuntime</c>/<c>PVXGameplaySession</c> and throws clearly if
/// neither is registered — correct in an actual game, but would also fire
/// merely from displaying this component in GameStudio's scene editor
/// (<c>ExecutionMode.Editor</c>, no PVX runtime at all).
/// </para>
/// </remarks>
[DataContract("PVXCinematicComponent")]
[Display("PVX Cinematic")]
[DefaultEntityComponentProcessor(typeof(PVXCinematicProcessor), ExecutionMode = ExecutionMode.Runtime)]
public sealed class PVXCinematicComponent : ActivableEntityComponent
{
    /// <summary>
    /// The path, played in order from <see cref="Play"/>'s starting pose.
    /// </summary>
    [DataMember(10)]
    public List<PVXCinematicWaypoint> Waypoints { get; set; } = new();

    /// <summary>
    /// If true, <see cref="PVXCinematicProcessor"/> strips the active
    /// player's control capabilities for as long as this is playing.
    /// </summary>
    [DataMember(20)]
    [Display("Restrict Player Control While Playing")]
    [DefaultValue(true)]
    public bool RestrictPlayerControlWhilePlaying { get; set; } = true;

    private Vector3 segmentStartPosition;
    private Quaternion segmentStartRotation;
    private int waypointIndex = -1;
    private float segmentElapsedSeconds;

    /// <summary>
    /// Whether a path is currently playing.
    /// </summary>
    [DataMemberIgnore]
    public bool IsPlaying => waypointIndex >= 0 && waypointIndex < Waypoints.Count;

    /// <summary>
    /// Raised when the path finishes (every waypoint reached) or is
    /// stopped early via <see cref="Stop"/>.
    /// </summary>
    public event EventHandler? Completed;

    /// <summary>
    /// Starts playing <see cref="Waypoints"/> from this entity's current
    /// pose. No-op (returns <see langword="false"/>) while already
    /// playing, if <see cref="Waypoints"/> is empty, or if this component
    /// is not yet attached to an entity.
    /// </summary>
    public bool Play()
    {
        if (IsPlaying || Waypoints.Count == 0 || Entity is null)
            return false;

        segmentStartPosition = Entity.Transform.Position;
        segmentStartRotation = Entity.Transform.Rotation;
        waypointIndex = 0;
        segmentElapsedSeconds = 0f;
        return true;
    }

    /// <summary>
    /// Stops playing immediately, raising <see cref="Completed"/>. No-op
    /// while not playing. Leaves the entity at whatever pose it had
    /// reached, rather than snapping it to the final waypoint.
    /// </summary>
    public void Stop()
    {
        if (!IsPlaying)
            return;

        waypointIndex = -1;
        Completed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Advances playback by <paramref name="elapsedSeconds"/> of real
    /// time. No-op while not playing.
    /// </summary>
    public void Advance(float elapsedSeconds)
    {
        if (!IsPlaying)
            return;

        var target = Waypoints[waypointIndex];
        var duration = target.Duration;

        if (!float.IsFinite(duration) || duration <= 0f)
        {
            CompleteLeg(target);
            return;
        }

        segmentElapsedSeconds += elapsedSeconds;
        var progress = segmentElapsedSeconds / duration;

        if (progress >= 1f)
        {
            CompleteLeg(target);
        }
        else
        {
            Entity.Transform.Position = Vector3.Lerp(segmentStartPosition, target.Position, progress);
            Entity.Transform.Rotation = Quaternion.Slerp(segmentStartRotation, target.Rotation, progress);
        }
    }

    private void CompleteLeg(PVXCinematicWaypoint target)
    {
        Entity.Transform.Position = target.Position;
        Entity.Transform.Rotation = target.Rotation;

        segmentStartPosition = target.Position;
        segmentStartRotation = target.Rotation;
        segmentElapsedSeconds = 0f;
        waypointIndex++;

        if (waypointIndex >= Waypoints.Count)
        {
            waypointIndex = -1;
            Completed?.Invoke(this, EventArgs.Empty);
        }
    }
}
