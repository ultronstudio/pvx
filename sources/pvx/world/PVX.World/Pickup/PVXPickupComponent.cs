using PVX.Interaction;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace PVX.World;

/// <summary>
/// A generic "can be picked up" marker: <see cref="PVXPickupState.Idle"/>/<see cref="PVXPickupState.Held"/>,
/// nothing else.
/// </summary>
/// <remarks>
/// Deliberately knows nothing about hands, cameras, carrying, inspecting,
/// or throwing — those live in whichever player-controller layer actually
/// has a camera/hand to hold things with (e.g. <c>PVX.FirstPerson</c>'s
/// carry component), the same separation <see cref="PVXSwitchComponent"/>'s
/// own remarks describe for its consumers. This component only owns
/// whether it is currently idle in the world or held by someone/something;
/// it never moves its own entity.
/// </remarks>
[DataContract("PVXPickupComponent")]
[Display("PVX Pickup")]
[DefaultEntityComponentProcessor(typeof(PVXPickupProcessor))]
public sealed class PVXPickupComponent : ActivableEntityComponent, IPVXInteractable
{
    public static readonly PVXInteractionActionId PickUpActionId = new("pick-up");

    private readonly PickupInteractionAction action;

    public PVXPickupComponent()
    {
        action = new PickupInteractionAction(this);
    }

    private PVXPickupState state = PVXPickupState.Idle;

    /// <summary>
    /// Current state. Only <see cref="TryPickUp"/>/<see cref="TryDrop"/> change it.
    /// </summary>
    [DataMemberIgnore]
    public PVXPickupState State => state;

    /// <summary>
    /// Raised only when <see cref="State"/> actually changes.
    /// </summary>
    public event EventHandler<PVXPickupStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Transitions to <see cref="PVXPickupState.Held"/>. No-op (returns
    /// <see langword="false"/>) unless currently <see cref="PVXPickupState.Idle"/> —
    /// in particular, this does not support being held by more than one
    /// carrier at a time.
    /// </summary>
    public bool TryPickUp() => SetState(PVXPickupState.Idle, PVXPickupState.Held);

    /// <summary>
    /// Transitions back to <see cref="PVXPickupState.Idle"/>. No-op unless
    /// currently <see cref="PVXPickupState.Held"/>.
    /// </summary>
    public bool TryDrop() => SetState(PVXPickupState.Held, PVXPickupState.Idle);

    /// <inheritdoc />
    /// <remarks>
    /// Offers "Pick Up" only while <see cref="PVXPickupState.Idle"/>: once
    /// held, there is nothing left to do with this target through the
    /// generic raycast-interaction pipeline — the carrier that already has
    /// it owns drop/throw/inspect directly.
    /// </remarks>
    public IReadOnlyList<PVXInteractionDescriptor> GetActions(PVXInteractionContext context)
    {
        return state == PVXPickupState.Idle
            ? [new PVXInteractionDescriptor(PickUpActionId, action, priority: 100, label: "Pick Up")]
            : [];
    }

    private bool SetState(PVXPickupState expected, PVXPickupState next)
    {
        if (state != expected)
            return false;

        var previous = state;
        state = next;
        StateChanged?.Invoke(this, new PVXPickupStateChangedEventArgs(previous, next));
        return true;
    }

    private sealed class PickupInteractionAction(PVXPickupComponent pickup) : IPVXInteractionAction
    {
        public bool CanExecute(PVXInteractionContext context) => pickup.State == PVXPickupState.Idle;

        public void Begin(PVXInteractionContext context)
        {
        }

        public bool Execute(PVXInteractionContext context) => pickup.TryPickUp();

        public void Cancel(PVXInteractionContext context)
        {
        }
    }
}
