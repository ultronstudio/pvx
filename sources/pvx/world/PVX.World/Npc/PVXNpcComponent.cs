using System.ComponentModel;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace PVX.World;

/// <summary>
/// A generic NPC identity/alert-state marker: <see cref="PVXNpcState.Idle"/>/<see cref="PVXNpcState.Alert"/>,
/// plus a game-facing <see cref="DisplayName"/>, nothing else.
/// </summary>
/// <remarks>
/// Deliberately knows nothing about perception (seeing/hearing the
/// player), pathfinding, patrol routes, or reactive behavior — those are
/// a future module's job, observing <see cref="StateChanged"/> or driving
/// <see cref="SetAlert"/> directly, the same separation
/// <see cref="PVXSwitchComponent"/>'s own remarks describe for its
/// consumers. Talking to an NPC is a separate concern entirely: add a
/// <c>PVX.Dialogue</c> <c>PVXDialogueComponent</c> to the same entity if it
/// should be talkable — this component is not an <see cref="PVX.Interaction.IPVXInteractable"/>
/// itself.
/// </remarks>
[DataContract("PVXNpcComponent")]
[Display("PVX NPC")]
[DefaultEntityComponentProcessor(typeof(PVXNpcProcessor))]
public sealed class PVXNpcComponent : ActivableEntityComponent
{
    /// <summary>
    /// Game-facing name (for dialogue attribution, UI, etc.) — distinct
    /// from <see cref="Entity.Name"/>, which is an editor/debug identifier.
    /// </summary>
    [DataMember(10)]
    [DefaultValue("")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// If true, the NPC's initial state is <see cref="PVXNpcState.Alert"/>
    /// instead of <see cref="PVXNpcState.Idle"/>. Applied once at first
    /// attach; does not raise <see cref="StateChanged"/>.
    /// </summary>
    [DataMember(20)]
    [Display("Starts Alert")]
    [DefaultValue(false)]
    public bool StartsAlert { get; set; }

    private PVXNpcState state = PVXNpcState.Idle;
    private bool initialized;

    /// <summary>
    /// Current state. Only <see cref="SetAlert"/> changes it.
    /// </summary>
    [DataMemberIgnore]
    public PVXNpcState State
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
    public event EventHandler<PVXNpcStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Sets <see cref="State"/> to <see cref="PVXNpcState.Alert"/> (<paramref name="alert"/>
    /// <see langword="true"/>) or <see cref="PVXNpcState.Idle"/> (<see langword="false"/>).
    /// Idempotent: setting the state it already has is a no-op and does
    /// not raise <see cref="StateChanged"/>.
    /// </summary>
    public void SetAlert(bool alert)
    {
        EnsureInitialized();

        var next = alert ? PVXNpcState.Alert : PVXNpcState.Idle;

        if (state == next)
            return;

        var previous = state;
        state = next;
        StateChanged?.Invoke(this, new PVXNpcStateChangedEventArgs(previous, next));
    }

    internal void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;

        if (StartsAlert)
        {
            state = PVXNpcState.Alert;
        }
    }
}
