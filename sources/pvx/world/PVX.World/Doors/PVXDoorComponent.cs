using System.ComponentModel;
using PVX.Interaction;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace PVX.World;

/// <summary>
/// A transform-animated, authoring-friendly door: <see cref="Open"/>/<see cref="Closed"/>
/// (and their transitions) driven entirely by local rotation, with no
/// physical hinge constraint, rigidbody, lock, key, or audio — see this
/// milestone's remarks for why those are explicitly out of scope for 0.1.
/// </summary>
/// <remarks>
/// <para>
/// The entity's authored orientation at the moment this component first
/// attaches is captured as the "closed" pose; the open pose is computed
/// relative to it by rotating <see cref="OpenAngleDegrees"/> around
/// <see cref="LocalHingeAxis"/>. This makes a non-identity authored
/// rotation (a door already angled in the editor) work correctly — the
/// hinge rotation composes with whatever orientation was authored, it
/// does not reset it. Capture happens once, on first attach
/// (<see cref="PVXDoorProcessor.OnEntityComponentAdding"/>); removing and
/// re-adding the same component instance does not re-capture the pose.
/// </para>
/// <para>
/// This component only owns the door's own state machine and transform.
/// It never checks gameplay/session state itself — <c>PVXInteractionService</c>
/// already gates whether a new interaction can begin via
/// <c>PVXControlCapabilities.Interact</c>; duplicating that check here
/// would be a second, possibly inconsistent gate.
/// </para>
/// <para>
/// <see cref="DefaultEntityComponentProcessorAttribute.ExecutionMode"/> is
/// pinned to <see cref="ExecutionMode.Runtime"/>: pose capture and
/// transition playback are gameplay simulation, not something GameStudio's
/// scene editor (<c>ExecutionMode.Editor</c>) should ever drive just from
/// displaying the entity. Every authored property (<see cref="OpenAngleDegrees"/>,
/// <see cref="TransitionDuration"/>, <see cref="StartsOpen"/>, <see cref="LocalHingeAxis"/>)
/// stays a plain <c>DataMember</c>, independent of the processor, so the
/// component remains fully inspectable/editable there regardless.
/// </para>
/// </remarks>
[DataContract("PVXDoorComponent")]
[Display("PVX Door")]
[DefaultEntityComponentProcessor(typeof(PVXDoorProcessor), ExecutionMode = ExecutionMode.Runtime)]
public sealed class PVXDoorComponent : ActivableEntityComponent, IPVXInteractable
{
    /// <summary>
    /// Action id offered while <see cref="PVXDoorState.Closed"/> (or busy
    /// transitioning out of it).
    /// </summary>
    public static readonly PVXInteractionActionId OpenActionId = new("open");

    /// <summary>
    /// Action id offered while <see cref="PVXDoorState.Open"/> (or busy
    /// transitioning out of it).
    /// </summary>
    public static readonly PVXInteractionActionId CloseActionId = new("close");

    private readonly DoorInteractionAction action;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool initialized;
    private float elapsedSeconds;

    public PVXDoorComponent()
    {
        action = new DoorInteractionAction(this);
    }

    /// <summary>
    /// Rotation, in degrees, applied around <see cref="LocalHingeAxis"/> to
    /// go from the authored closed pose to the open pose. Positive and
    /// negative values swing the door in opposite directions, so no
    /// separate "open direction" enum is needed.
    /// </summary>
    [DataMember(10)]
    [Display("Open Angle (Degrees)")]
    [DefaultValue(90f)]
    public float OpenAngleDegrees { get; set; } = 90f;

    /// <summary>
    /// Time, in seconds, for a full Opening/Closing transition.
    /// Non-positive or non-finite values (0, negative, NaN, Infinity) make
    /// the transition instantaneous rather than corrupting the animation.
    /// </summary>
    [DataMember(20)]
    [Display("Transition Duration (Seconds)")]
    [DefaultValue(0.6f)]
    public float TransitionDuration { get; set; } = 0.6f;

    /// <summary>
    /// If true, the door's initial state is <see cref="PVXDoorState.Open"/>
    /// instead of <see cref="PVXDoorState.Closed"/>. Applied once at first
    /// attach; does not raise <see cref="StateChanged"/> (it is the
    /// starting state, not a transition).
    /// </summary>
    [DataMember(30)]
    [Display("Starts Open")]
    [DefaultValue(false)]
    public bool StartsOpen { get; set; }

    /// <summary>
    /// Local axis the door hinges around, relative to its own authored
    /// orientation. Defaults to the local up axis, the common case for a
    /// door authored with its pivot on the hinge side.
    /// </summary>
    [DataMember(40)]
    [Display("Local Hinge Axis")]
    public Vector3 LocalHingeAxis { get; set; } = Vector3.UnitY;

    private PVXDoorState state = PVXDoorState.Closed;

    /// <summary>
    /// Current lifecycle state. Cannot be set directly from outside —
    /// only <see cref="TryOpen"/>/<see cref="TryClose"/> (and the
    /// processor's own per-frame transition) ever change it.
    /// </summary>
    /// <remarks>
    /// Reading this lazily runs <see cref="EnsureInitialized"/>, so
    /// <see cref="StartsOpen"/> is reflected correctly even if queried
    /// before the entity is ever attached to a running scene (e.g. in a
    /// unit test that never spins up a <see cref="PVXDoorProcessor"/>).
    /// </remarks>
    [DataMemberIgnore]
    public PVXDoorState State
    {
        get
        {
            EnsureInitialized();
            return state;
        }
    }

    /// <summary>
    /// Raised only when <see cref="State"/> actually changes.
    /// </summary>
    public event EventHandler<PVXDoorStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Starts opening. No-op (returns <see langword="false"/>) unless
    /// currently <see cref="PVXDoorState.Closed"/> — in particular, calling
    /// this while already <see cref="PVXDoorState.Opening"/> does not
    /// restart or otherwise disturb the in-progress transition.
    /// </summary>
    public bool TryOpen()
    {
        EnsureInitialized();

        if (State != PVXDoorState.Closed)
            return false;

        elapsedSeconds = 0f;
        SetState(PVXDoorState.Opening);
        return true;
    }

    /// <summary>
    /// Starts closing. No-op (returns <see langword="false"/>) unless
    /// currently <see cref="PVXDoorState.Open"/>.
    /// </summary>
    public bool TryClose()
    {
        EnsureInitialized();

        if (State != PVXDoorState.Open)
            return false;

        elapsedSeconds = 0f;
        SetState(PVXDoorState.Closing);
        return true;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns exactly one descriptor at a time: the action relevant to
    /// the current state, marked unavailable with reason <c>"busy"</c>
    /// while a transition is in progress rather than offering the
    /// opposite action (0.1 does not support reversing mid-transition).
    /// </remarks>
    public IReadOnlyList<PVXInteractionDescriptor> GetActions(PVXInteractionContext context)
    {
        return State switch
        {
            PVXDoorState.Closed =>
                [new PVXInteractionDescriptor(OpenActionId, action, priority: 100, label: "Open")],
            PVXDoorState.Opening =>
                [new PVXInteractionDescriptor(OpenActionId, action, priority: 100, label: "Open", availability: PVXInteractionAvailability.Unavailable("busy"))],
            PVXDoorState.Open =>
                [new PVXInteractionDescriptor(CloseActionId, action, priority: 100, label: "Close")],
            PVXDoorState.Closing =>
                [new PVXInteractionDescriptor(CloseActionId, action, priority: 100, label: "Close", availability: PVXInteractionAvailability.Unavailable("busy"))],
            _ => Array.Empty<PVXInteractionDescriptor>(),
        };
    }

    internal void EnsureInitialized()
    {
        if (initialized)
            return;

        // Not attached to an entity yet: nothing to capture. State keeps
        // reporting the Closed default until this component is actually
        // attached and this can run for real.
        if (Entity is null)
            return;

        initialized = true;
        closedRotation = Entity.Transform.Rotation;

        var axis = LocalHingeAxis.LengthSquared() > 0f ? Vector3.Normalize(LocalHingeAxis) : Vector3.UnitY;
        var hingeDelta = Quaternion.RotationAxis(axis, MathUtil.DegreesToRadians(OpenAngleDegrees));
        openRotation = closedRotation * hingeDelta;

        if (StartsOpen)
        {
            Entity.Transform.Rotation = openRotation;
            state = PVXDoorState.Open;

            // A collider may have already attached (and captured the
            // pre-StartsOpen pose) before this runs, depending on
            // processor ordering; force it back in sync immediately.
            PVXPhysicsTransformSync.Sync(Entity);
        }
    }

    /// <summary>
    /// Advances an in-progress transition by <paramref name="elapsedSeconds"/>
    /// of real time. No-op outside <see cref="PVXDoorState.Opening"/>/<see cref="PVXDoorState.Closing"/>.
    /// </summary>
    /// <remarks>
    /// Public (not just processor-internal) deliberately: it is the one
    /// seam that lets tests drive a deterministic transition without a
    /// live Stride scene/processor, the same relationship
    /// <c>PVXInteractionService.Update</c> has with its own owning feature.
    /// <see cref="PVXDoorProcessor"/> is the only thing expected to call
    /// this during normal gameplay, with real per-frame elapsed time.
    /// </remarks>
    public void Advance(float elapsedSeconds)
    {
        if (State != PVXDoorState.Opening && State != PVXDoorState.Closing)
            return;

        var duration = TransitionDuration;

        if (!float.IsFinite(duration) || duration <= 0f)
        {
            CompleteTransition();
            return;
        }

        this.elapsedSeconds += elapsedSeconds;
        var progress = this.elapsedSeconds / duration;

        if (progress >= 1f)
        {
            CompleteTransition();
        }
        else
        {
            var from = State == PVXDoorState.Opening ? closedRotation : openRotation;
            var to = State == PVXDoorState.Opening ? openRotation : closedRotation;
            Entity.Transform.Rotation = Quaternion.Slerp(from, to, progress);
            PVXPhysicsTransformSync.Sync(Entity);
        }
    }

    private void CompleteTransition()
    {
        var next = State == PVXDoorState.Opening ? PVXDoorState.Open : PVXDoorState.Closed;
        Entity.Transform.Rotation = next == PVXDoorState.Open ? openRotation : closedRotation;
        PVXPhysicsTransformSync.Sync(Entity);
        SetState(next);
    }

    private void SetState(PVXDoorState next)
    {
        if (state == next)
            return;

        var previous = state;
        state = next;
        StateChanged?.Invoke(this, new PVXDoorStateChangedEventArgs(previous, next));
    }

    /// <summary>
    /// The door's own interaction behavior: dispatches to
    /// <see cref="TryOpen"/>/<see cref="TryClose"/> depending on the
    /// door's current state at execution time, so there is exactly one
    /// place that starts a transition — not a second, parallel path.
    /// </summary>
    private sealed class DoorInteractionAction(PVXDoorComponent door) : IPVXInteractionAction
    {
        public bool CanExecute(PVXInteractionContext context) =>
            door.State is PVXDoorState.Closed or PVXDoorState.Open;

        public void Begin(PVXInteractionContext context)
        {
        }

        public bool Execute(PVXInteractionContext context) =>
            door.State switch
            {
                PVXDoorState.Closed => door.TryOpen(),
                PVXDoorState.Open => door.TryClose(),
                _ => false,
            };

        public void Cancel(PVXInteractionContext context)
        {
        }
    }
}
