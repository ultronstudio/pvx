using System.ComponentModel;
using PVX.Interaction;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace PVX.World;

/// <summary>
/// A transform-animated, authoring-friendly drawer: <see cref="PVXDrawerState.Open"/>/<see cref="PVXDrawerState.Closed"/>
/// (and their transitions) driven entirely by local translation along
/// <see cref="LocalSlideAxis"/>, with no physical slide constraint,
/// rigidbody, lock, or audio — the same explicit non-goals
/// <see cref="PVXDoorComponent"/> documents for 0.1, just translating
/// instead of rotating.
/// </summary>
/// <remarks>
/// The entity's authored local position at the moment this component
/// first attaches is captured as the "closed" pose; the open pose is
/// <see cref="OpenDistance"/> further along <see cref="LocalSlideAxis"/>,
/// rotated by the entity's own authored orientation so the slide direction
/// is relative to however the drawer was authored (an angled drawer front
/// still slides "outward" rather than along a world axis). Capture happens
/// once, on first attach (<see cref="PVXDrawerProcessor.OnEntityComponentAdding"/>).
/// </remarks>
[DataContract("PVXDrawerComponent")]
[Display("PVX Drawer")]
[DefaultEntityComponentProcessor(typeof(PVXDrawerProcessor))]
public sealed class PVXDrawerComponent : ActivableEntityComponent, IPVXInteractable
{
    public static readonly PVXInteractionActionId OpenActionId = new("open");

    public static readonly PVXInteractionActionId CloseActionId = new("close");

    private readonly DrawerInteractionAction action;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool initialized;
    private float elapsedSeconds;

    public PVXDrawerComponent()
    {
        action = new DrawerInteractionAction(this);
    }

    /// <summary>
    /// Distance, in world units, the drawer slides along <see cref="LocalSlideAxis"/>
    /// to go from the authored closed pose to the open pose.
    /// </summary>
    [DataMember(10)]
    [Display("Open Distance")]
    [DefaultValue(0.3f)]
    public float OpenDistance { get; set; } = 0.3f;

    /// <summary>
    /// Time, in seconds, for a full Opening/Closing transition. Non-positive
    /// or non-finite values make the transition instantaneous rather than
    /// corrupting the animation.
    /// </summary>
    [DataMember(20)]
    [Display("Transition Duration (Seconds)")]
    [DefaultValue(0.5f)]
    public float TransitionDuration { get; set; } = 0.5f;

    /// <summary>
    /// If true, the drawer's initial state is <see cref="PVXDrawerState.Open"/>
    /// instead of <see cref="PVXDrawerState.Closed"/>. Applied once at first
    /// attach; does not raise <see cref="StateChanged"/>.
    /// </summary>
    [DataMember(30)]
    [Display("Starts Open")]
    [DefaultValue(false)]
    public bool StartsOpen { get; set; }

    /// <summary>
    /// Local axis the drawer slides along, relative to its own authored
    /// orientation. Defaults to local -Z ("toward the viewer"), the common
    /// case for a drawer front authored facing +Z.
    /// </summary>
    [DataMember(40)]
    [Display("Local Slide Axis")]
    public Vector3 LocalSlideAxis { get; set; } = -Vector3.UnitZ;

    private PVXDrawerState state = PVXDrawerState.Closed;

    /// <summary>
    /// Current lifecycle state. Cannot be set directly from outside — only
    /// <see cref="TryOpen"/>/<see cref="TryClose"/> (and the processor's own
    /// per-frame transition) ever change it.
    /// </summary>
    [DataMemberIgnore]
    public PVXDrawerState State
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
    public event EventHandler<PVXDrawerStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Starts opening. No-op (returns <see langword="false"/>) unless
    /// currently <see cref="PVXDrawerState.Closed"/>.
    /// </summary>
    public bool TryOpen()
    {
        EnsureInitialized();

        if (State != PVXDrawerState.Closed)
            return false;

        elapsedSeconds = 0f;
        SetState(PVXDrawerState.Opening);
        return true;
    }

    /// <summary>
    /// Starts closing. No-op (returns <see langword="false"/>) unless
    /// currently <see cref="PVXDrawerState.Open"/>.
    /// </summary>
    public bool TryClose()
    {
        EnsureInitialized();

        if (State != PVXDrawerState.Open)
            return false;

        elapsedSeconds = 0f;
        SetState(PVXDrawerState.Closing);
        return true;
    }

    /// <inheritdoc />
    public IReadOnlyList<PVXInteractionDescriptor> GetActions(PVXInteractionContext context)
    {
        return State switch
        {
            PVXDrawerState.Closed =>
                [new PVXInteractionDescriptor(OpenActionId, action, priority: 100, label: "Open")],
            PVXDrawerState.Opening =>
                [new PVXInteractionDescriptor(OpenActionId, action, priority: 100, label: "Open", availability: PVXInteractionAvailability.Unavailable("busy"))],
            PVXDrawerState.Open =>
                [new PVXInteractionDescriptor(CloseActionId, action, priority: 100, label: "Close")],
            PVXDrawerState.Closing =>
                [new PVXInteractionDescriptor(CloseActionId, action, priority: 100, label: "Close", availability: PVXInteractionAvailability.Unavailable("busy"))],
            _ => Array.Empty<PVXInteractionDescriptor>(),
        };
    }

    internal void EnsureInitialized()
    {
        if (initialized)
            return;

        if (Entity is null)
            return;

        initialized = true;
        closedPosition = Entity.Transform.Position;

        var axis = LocalSlideAxis.LengthSquared() > 0f ? Vector3.Normalize(LocalSlideAxis) : -Vector3.UnitZ;
        var worldAxis = Vector3.Transform(axis, Entity.Transform.Rotation);
        openPosition = closedPosition + worldAxis * OpenDistance;

        if (StartsOpen)
        {
            Entity.Transform.Position = openPosition;
            state = PVXDrawerState.Open;
            PVXPhysicsTransformSync.Sync(Entity);
        }
    }

    /// <summary>
    /// Advances an in-progress transition by <paramref name="elapsedSeconds"/>
    /// of real time. No-op outside <see cref="PVXDrawerState.Opening"/>/<see cref="PVXDrawerState.Closing"/>.
    /// </summary>
    public void Advance(float elapsedSeconds)
    {
        if (State != PVXDrawerState.Opening && State != PVXDrawerState.Closing)
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
            var from = State == PVXDrawerState.Opening ? closedPosition : openPosition;
            var to = State == PVXDrawerState.Opening ? openPosition : closedPosition;
            Entity.Transform.Position = Vector3.Lerp(from, to, progress);
            PVXPhysicsTransformSync.Sync(Entity);
        }
    }

    private void CompleteTransition()
    {
        var next = State == PVXDrawerState.Opening ? PVXDrawerState.Open : PVXDrawerState.Closed;
        Entity.Transform.Position = next == PVXDrawerState.Open ? openPosition : closedPosition;
        PVXPhysicsTransformSync.Sync(Entity);
        SetState(next);
    }

    private void SetState(PVXDrawerState next)
    {
        if (state == next)
            return;

        var previous = state;
        state = next;
        StateChanged?.Invoke(this, new PVXDrawerStateChangedEventArgs(previous, next));
    }

    private sealed class DrawerInteractionAction(PVXDrawerComponent drawer) : IPVXInteractionAction
    {
        public bool CanExecute(PVXInteractionContext context) =>
            drawer.State is PVXDrawerState.Closed or PVXDrawerState.Open;

        public void Begin(PVXInteractionContext context)
        {
        }

        public bool Execute(PVXInteractionContext context) =>
            drawer.State switch
            {
                PVXDrawerState.Closed => drawer.TryOpen(),
                PVXDrawerState.Open => drawer.TryClose(),
                _ => false,
            };

        public void Cancel(PVXInteractionContext context)
        {
        }
    }
}
