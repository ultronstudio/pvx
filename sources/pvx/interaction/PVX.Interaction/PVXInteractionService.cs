using PVX.Gameplay;

namespace PVX.Interaction;

/// <summary>
/// Drives the generic interaction pipeline: resolves focus from a
/// <see cref="IPVXInteractionTargetProvider"/>, exposes the focused
/// target's available actions, and runs at most one active interaction
/// (instant or hold) at a time.
/// </summary>
/// <remarks>
/// Bound to a single <see cref="PVXGameplaySession"/> for its whole
/// lifetime and gates every new interaction on that session's primary
/// player, state and <see cref="PVXControlCapabilities.Interact"/>
/// capability. This milestone deliberately supports only one active
/// interaction, driven by the session's primary player; multi-source
/// interaction scheduling is out of scope.
///
/// Every terminal transition (<c>Completed</c>/<c>Cancelled</c>/<c>Failed</c>)
/// clears <see cref="ActiveInteraction"/> before raising its event, so a
/// throwing subscriber can never leave a dangling active interaction
/// blocking all future ones — the same lesson PVX.Gameplay's own hardening
/// already established for its lifecycle events.
/// </remarks>
public sealed class PVXInteractionService
{
    private static readonly IReadOnlyList<PVXInteractionDescriptor> NoActions = Array.Empty<PVXInteractionDescriptor>();

    public PVXInteractionService(PVXGameplaySession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        Session = session;
        Actions = NoActions;

        session.Player.PrimaryPlayerChanged += OnPrimaryPlayerChanged;
        session.Control.CapabilitiesChanged += OnCapabilitiesChanged;
        session.GameplayPaused += OnGameplayBlockingTransition;
        session.GameplayEnding += OnGameplayBlockingTransition;
    }

    /// <summary>
    /// The gameplay session this service is bound to.
    /// </summary>
    public PVXGameplaySession Session { get; }

    /// <summary>
    /// Target discovery used to resolve <see cref="CurrentFocus"/>. Settable
    /// at any time; a <see langword="null"/> provider simply yields no focus.
    /// </summary>
    public IPVXInteractionTargetProvider? TargetProvider { get; set; }

    /// <summary>
    /// The currently focused candidate, or <see langword="null"/> if none.
    /// </summary>
    public PVXInteractionCandidate? CurrentFocus { get; private set; }

    /// <summary>
    /// All actions currently offered by <see cref="CurrentFocus"/>'s target,
    /// including unavailable ones (so UI can still show e.g. "Locked").
    /// Empty when there is no current focus.
    /// </summary>
    public IReadOnlyList<PVXInteractionDescriptor> Actions { get; private set; }

    /// <summary>
    /// Highest-priority currently AVAILABLE action, or <see langword="null"/>
    /// if none of <see cref="Actions"/> is available right now.
    /// </summary>
    public PVXInteractionDescriptor? PrimaryAction { get; private set; }

    /// <summary>
    /// Next-highest-priority currently available action after <see cref="PrimaryAction"/>.
    /// </summary>
    public PVXInteractionDescriptor? SecondaryAction { get; private set; }

    /// <summary>
    /// The single in-progress interaction, or <see langword="null"/> if none.
    /// </summary>
    public PVXActiveInteraction? ActiveInteraction { get; private set; }

    public event EventHandler<PVXFocusChangedEventArgs>? FocusChanged;

    public event EventHandler<PVXInteractionEventArgs>? InteractionStarted;

    public event EventHandler<PVXInteractionEventArgs>? InteractionCompleted;

    public event EventHandler<PVXInteractionEventArgs>? InteractionCancelled;

    public event EventHandler<PVXInteractionEventArgs>? InteractionFailed;

    /// <summary>
    /// Re-resolves focus from <see cref="TargetProvider"/> and advances any
    /// active hold interaction. Must be called once per frame/tick by the
    /// owning feature or host.
    /// </summary>
    public void Update(IPVXInteractionSource source, TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(source);

        // Catches the one gap event subscriptions cannot: PVXGameplaySession
        // can move to Faulted through an internal path that deliberately
        // does not raise CapabilitiesChanged (see PVXGameplaySession's own
        // remarks on AcquireRestrictionWithoutNotification). Polling session
        // state here, instead of trusting notifications alone, keeps this
        // service correct even when that path is taken.
        CancelIfNowBlocked();

        UpdateFocusAndActions(source);
        RevalidateActiveInteractionAgainstCurrentActions();
        AdvanceActiveHold(elapsed);
    }

    /// <summary>
    /// Begins <see cref="PrimaryAction"/> on <see cref="CurrentFocus"/>, if
    /// one is currently allowed to begin.
    /// </summary>
    public bool BeginPrimaryInteraction(IPVXInteractionSource source) => TryBeginInteraction(PrimaryAction, source);

    /// <summary>
    /// Begins <see cref="SecondaryAction"/> on <see cref="CurrentFocus"/>, if
    /// one is currently allowed to begin.
    /// </summary>
    public bool BeginSecondaryInteraction(IPVXInteractionSource source) => TryBeginInteraction(SecondaryAction, source);

    /// <summary>
    /// Cancels the active interaction, if any. Idempotent: a no-op when
    /// nothing is active (including when the last interaction already
    /// completed).
    /// </summary>
    public bool CancelActiveInteraction() => CancelActiveInteraction(reason: "explicit");

    private bool TryBeginInteraction(PVXInteractionDescriptor? descriptor, IPVXInteractionSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (descriptor is null)
            return false;

        if (ActiveInteraction is not null)
            return false;

        if (CurrentFocus is not { } focus)
            return false;

        if (!CanBeginInteractionNow())
            return false;

        if (!descriptor.Availability.IsAvailable)
            return false;

        var context = new PVXInteractionContext(source, focus.Target, focus.Point);

        if (!descriptor.Action.CanExecute(context))
            return false;

        descriptor.Action.Begin(context);
        ActiveInteraction = new PVXActiveInteraction(context, descriptor, TimeSpan.Zero);
        InteractionStarted?.Invoke(this, new PVXInteractionEventArgs(context, descriptor));

        if (descriptor.Mode == PVXInteractionMode.Instant)
        {
            ExecuteAndFinish(context, descriptor);
        }

        return true;
    }

    private void UpdateFocusAndActions(IPVXInteractionSource source)
    {
        var candidates = TargetProvider?.GetCandidates(source);

        PVXInteractionCandidate? selected = null;

        if (candidates is not null)
        {
            foreach (var candidate in candidates)
            {
                if (selected is null || candidate.Priority > selected.Value.Priority)
                {
                    selected = candidate;
                }
            }
        }

        var previous = CurrentFocus;
        var focusChanged = !FocusRefersToSameTarget(previous, selected);

        if (focusChanged && ActiveInteraction is not null)
        {
            // The active interaction belongs to the previous focus; a moving
            // focus must not silently carry it over to a different target.
            CancelActiveInteraction(reason: "focus-changed");
        }

        CurrentFocus = selected;

        Actions = selected is { } focus
            ? focus.Target.GetActions(new PVXInteractionContext(source, focus.Target, focus.Point))
            : NoActions;

        RecomputePrimaryAndSecondary();

        if (focusChanged)
        {
            FocusChanged?.Invoke(this, new PVXFocusChangedEventArgs(previous, selected));
        }
    }

    private void RevalidateActiveInteractionAgainstCurrentActions()
    {
        if (ActiveInteraction is not { } active)
            return;

        PVXInteractionDescriptor? current = null;

        foreach (var action in Actions)
        {
            if (action.ActionId == active.Descriptor.ActionId)
            {
                current = action;
                break;
            }
        }

        if (current is null || !current.Availability.IsAvailable)
        {
            CancelActiveInteraction(reason: "unavailable");
        }
    }

    private void AdvanceActiveHold(TimeSpan elapsed)
    {
        if (ActiveInteraction is not { } active || active.Descriptor.Mode != PVXInteractionMode.Hold)
            return;

        var holdDuration = active.Descriptor.HoldDuration ?? TimeSpan.Zero;
        var newElapsed = active.Elapsed + elapsed;

        if (newElapsed >= holdDuration)
        {
            ExecuteAndFinish(active.Context, active.Descriptor);
        }
        else
        {
            ActiveInteraction = new PVXActiveInteraction(active.Context, active.Descriptor, newElapsed);
        }
    }

    private void ExecuteAndFinish(PVXInteractionContext context, PVXInteractionDescriptor descriptor)
    {
        bool success;

        try
        {
            success = descriptor.Action.Execute(context);
        }
        catch
        {
            // Clear before rethrowing: an action that throws mid-execution
            // must not leave a dangling active interaction behind either.
            ActiveInteraction = null;
            throw;
        }

        ActiveInteraction = null;

        if (success)
        {
            InteractionCompleted?.Invoke(this, new PVXInteractionEventArgs(context, descriptor));
        }
        else
        {
            InteractionFailed?.Invoke(this, new PVXInteractionEventArgs(context, descriptor, reason: "execute-failed"));
        }
    }

    private bool CancelActiveInteraction(string reason)
    {
        if (ActiveInteraction is not { } active)
            return false;

        // Cleared before notifying anyone: a throwing Cancel() or a
        // throwing InteractionCancelled subscriber must not leave this
        // service believing an interaction is still active.
        ActiveInteraction = null;

        try
        {
            active.Descriptor.Action.Cancel(active.Context);
        }
        finally
        {
            InteractionCancelled?.Invoke(this, new PVXInteractionEventArgs(active.Context, active.Descriptor, reason));
        }

        return true;
    }

    private void RecomputePrimaryAndSecondary()
    {
        PVXInteractionDescriptor? primary = null;
        PVXInteractionDescriptor? secondary = null;

        foreach (var action in Actions)
        {
            if (!action.Availability.IsAvailable)
                continue;

            if (primary is null || action.Priority > primary.Priority)
            {
                secondary = primary;
                primary = action;
            }
            else if (secondary is null || action.Priority > secondary.Priority)
            {
                secondary = action;
            }
        }

        PrimaryAction = primary;
        SecondaryAction = secondary;
    }

    private void OnPrimaryPlayerChanged(object? sender, PVXPrimaryPlayerChangedEventArgs e)
    {
        // Unconditional: the active interaction's context holds an implicit
        // dependency on "the" primary player for this milestone, so any
        // change invalidates it regardless of current capabilities.
        CancelActiveInteraction(reason: "player-changed");
    }

    private void OnCapabilitiesChanged(object? sender, PVXControlCapabilitiesChangedEventArgs e) => CancelIfNowBlocked();

    private void OnGameplayBlockingTransition(object? sender, EventArgs e) => CancelIfNowBlocked();

    private void CancelIfNowBlocked()
    {
        if (ActiveInteraction is null)
            return;

        if (CanContinueActiveInteraction())
            return;

        var reason = Session.Control.EffectiveCapabilities.HasFlag(PVXControlCapabilities.Interact)
            ? "session-state"
            : "capability-lost";

        CancelActiveInteraction(reason);
    }

    private bool CanContinueActiveInteraction() =>
        Session.State == PVXGameplayState.Active
        && Session.Control.EffectiveCapabilities.HasFlag(PVXControlCapabilities.Interact);

    private bool CanBeginInteractionNow() =>
        CanContinueActiveInteraction() && Session.Player.PrimaryPlayer is not null;

    private static bool FocusRefersToSameTarget(PVXInteractionCandidate? a, PVXInteractionCandidate? b)
    {
        if (a is null || b is null)
            return a is null && b is null;

        return ReferenceEquals(a.Value.Target, b.Value.Target) && a.Value.Point == b.Value.Point;
    }
}
