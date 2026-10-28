using System.ComponentModel;
using PVX.Interaction;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace PVX.World;

/// <summary>
/// A generic stateful toggle: <see cref="PVXSwitchState.Off"/>/<see cref="PVXSwitchState.On"/>,
/// nothing else.
/// </summary>
/// <remarks>
/// Deliberately knows nothing about lights, doors, puzzles, narrative, or
/// any other system that might react to it — those are future modules
/// that observe <see cref="StateChanged"/>. Keeping this ignorant of its
/// consumers is what lets it stay generic and reusable.
/// </remarks>
[DataContract("PVXSwitchComponent")]
[Display("PVX Switch")]
[DefaultEntityComponentProcessor(typeof(PVXSwitchProcessor))]
public sealed class PVXSwitchComponent : ActivableEntityComponent, IPVXInteractable
{
    public static readonly PVXInteractionActionId ToggleActionId = new("toggle");

    private readonly SwitchInteractionAction action;
    private bool initialized;

    public PVXSwitchComponent()
    {
        action = new SwitchInteractionAction(this);
    }

    /// <summary>
    /// If true, the switch's initial state is <see cref="PVXSwitchState.On"/>.
    /// Applied once at first attach; does not raise <see cref="StateChanged"/>.
    /// </summary>
    [DataMember(10)]
    [Display("Starts On")]
    [DefaultValue(false)]
    public bool StartsOn { get; set; }

    private PVXSwitchState state = PVXSwitchState.Off;

    /// <summary>
    /// Current state. Only <see cref="Toggle"/>/<see cref="SetState"/> change it.
    /// </summary>
    /// <remarks>
    /// Reading this lazily runs <see cref="EnsureInitialized"/>, so
    /// <see cref="StartsOn"/> is reflected correctly even if queried
    /// before this component is attached to a running scene (e.g. in a
    /// unit test that never spins up a <see cref="PVXSwitchProcessor"/>).
    /// </remarks>
    [DataMemberIgnore]
    public PVXSwitchState State
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
    public event EventHandler<PVXSwitchStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Flips <see cref="State"/>.
    /// </summary>
    public void Toggle() => SetState(State == PVXSwitchState.Off ? PVXSwitchState.On : PVXSwitchState.Off);

    /// <summary>
    /// Sets <see cref="State"/> directly. Idempotent: setting the state it
    /// already has is a no-op and does not raise <see cref="StateChanged"/>.
    /// </summary>
    public void SetState(PVXSwitchState newState)
    {
        EnsureInitialized();

        if (state == newState)
            return;

        var previous = state;
        state = newState;
        StateChanged?.Invoke(this, new PVXSwitchStateChangedEventArgs(previous, newState));
    }

    /// <inheritdoc />
    public IReadOnlyList<PVXInteractionDescriptor> GetActions(PVXInteractionContext context)
    {
        var label = State == PVXSwitchState.Off ? "Turn On" : "Turn Off";
        return [new PVXInteractionDescriptor(ToggleActionId, action, priority: 100, label: label)];
    }

    internal void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;

        if (StartsOn)
        {
            state = PVXSwitchState.On;
        }
    }

    private sealed class SwitchInteractionAction(PVXSwitchComponent switchComponent) : IPVXInteractionAction
    {
        public bool CanExecute(PVXInteractionContext context) => true;

        public void Begin(PVXInteractionContext context)
        {
        }

        public bool Execute(PVXInteractionContext context)
        {
            switchComponent.Toggle();
            return true;
        }

        public void Cancel(PVXInteractionContext context)
        {
        }
    }
}
