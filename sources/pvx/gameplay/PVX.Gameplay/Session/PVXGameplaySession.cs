namespace PVX.Gameplay;

/// <summary>
/// Owns the lifecycle of a single gameplay session: whether a game is
/// actually being played right now, who the primary player is, what they
/// are currently allowed to do, and which difficulty is selected.
/// </summary>
/// <remarks>
/// Pausing and ending are implemented as control restrictions like any
/// other: <see cref="Pause"/> acquires a <see cref="PVXControlCapabilities.None"/>
/// restriction, and <see cref="Resume"/> releases it, so capabilities
/// restored on resume are exactly whatever other restrictions (dialogue,
/// cinematics, ...) still allow — never unconditionally "everything".
///
/// Each lifecycle method is atomic: the state assignment and its
/// corresponding event notification both run inside the same try block, so
/// a throwing subscriber transitions the session to <see cref="PVXGameplayState.Faulted"/>
/// exactly like a failure in the method's own logic would — it never
/// leaves <see cref="State"/> claiming a transition succeeded while the
/// call that was supposed to produce it is still unwinding with an exception.
///
/// <see cref="PVXGameplayState.Faulted"/> always fails closed: control
/// capabilities become <see cref="PVXControlCapabilities.None"/>, even if
/// the failure happened right after a restriction that was suppressing
/// control was lifted (e.g. a <see cref="GameplayStarted"/> subscriber
/// throwing immediately after <see cref="Start"/> lifted the pre-start
/// suspension).
/// </remarks>
public sealed class PVXGameplaySession
{
    private PVXControlRestriction? lifecycleSuspension;

    public PVXGameplaySession()
    {
        Control = new PVXControlService();
        Player = new PVXPlayerContext();
        Difficulty = new PVXDifficultyService();

        // No gameplay session is active yet: control starts fully suppressed.
        lifecycleSuspension = Control.AcquireRestriction(PVXControlCapabilities.None, owner: this);
    }

    /// <summary>
    /// Current lifecycle state of this session.
    /// </summary>
    public PVXGameplayState State { get; private set; } = PVXGameplayState.Inactive;

    /// <summary>
    /// Primary player control-capability aggregator for this session.
    /// </summary>
    public PVXControlService Control { get; }

    /// <summary>
    /// Primary player identity/provider registration for this session.
    /// </summary>
    public PVXPlayerContext Player { get; }

    /// <summary>
    /// Selected difficulty/profile identity for this session.
    /// </summary>
    public PVXDifficultyService Difficulty { get; }

    public event EventHandler? GameplayStarted;

    public event EventHandler? GameplayPaused;

    public event EventHandler? GameplayResumed;

    public event EventHandler? GameplayEnding;

    public event EventHandler? GameplayEnded;

    /// <summary>
    /// Starts the session. Valid only from <see cref="PVXGameplayState.Inactive"/>.
    /// </summary>
    public void Start()
    {
        if (State != PVXGameplayState.Inactive)
        {
            throw new InvalidOperationException(
                $"PVX gameplay session cannot start from state '{State}'.");
        }

        State = PVXGameplayState.Starting;

        try
        {
            lifecycleSuspension?.Dispose();
            lifecycleSuspension = null;

            State = PVXGameplayState.Active;
            GameplayStarted?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            TransitionToFaulted();
            throw;
        }
    }

    /// <summary>
    /// Pauses the session. Valid only from <see cref="PVXGameplayState.Active"/>.
    /// Effective control capabilities become <see cref="PVXControlCapabilities.None"/>.
    /// </summary>
    public void Pause()
    {
        if (State != PVXGameplayState.Active)
        {
            throw new InvalidOperationException(
                $"PVX gameplay session cannot pause from state '{State}'.");
        }

        try
        {
            lifecycleSuspension = Control.AcquireRestriction(PVXControlCapabilities.None, owner: this);
            State = PVXGameplayState.Paused;
            GameplayPaused?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            TransitionToFaulted();
            throw;
        }
    }

    /// <summary>
    /// Resumes the session. Valid only from <see cref="PVXGameplayState.Paused"/>.
    /// Restores whatever capabilities other active restrictions still allow.
    /// </summary>
    public void Resume()
    {
        if (State != PVXGameplayState.Paused)
        {
            throw new InvalidOperationException(
                $"PVX gameplay session cannot resume from state '{State}'.");
        }

        try
        {
            lifecycleSuspension?.Dispose();
            lifecycleSuspension = null;

            State = PVXGameplayState.Active;
            GameplayResumed?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            TransitionToFaulted();
            throw;
        }
    }

    /// <summary>
    /// Ends the session. Valid from <see cref="PVXGameplayState.Active"/> or
    /// <see cref="PVXGameplayState.Paused"/>. Terminal: control capabilities
    /// remain <see cref="PVXControlCapabilities.None"/> afterward.
    /// </summary>
    public void End()
    {
        if (State is not (PVXGameplayState.Active or PVXGameplayState.Paused))
        {
            throw new InvalidOperationException(
                $"PVX gameplay session cannot end from state '{State}'.");
        }

        State = PVXGameplayState.Ending;

        try
        {
            GameplayEnding?.Invoke(this, EventArgs.Empty);

            lifecycleSuspension ??= Control.AcquireRestriction(PVXControlCapabilities.None, owner: this);
            State = PVXGameplayState.Ended;
            GameplayEnded?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            TransitionToFaulted();
            throw;
        }
    }

    /// <summary>
    /// Moves to <see cref="PVXGameplayState.Faulted"/> and fails control
    /// closed: ensures a capability-suppressing restriction is held,
    /// regardless of which lifecycle step failed or whether one was
    /// already lifted earlier in the same call.
    /// </summary>
    private void TransitionToFaulted()
    {
        State = PVXGameplayState.Faulted;

        // Check actual effective capabilities, not the lifecycleSuspension
        // field: a restriction's Dispose() removes it from PVXControlService
        // (and recomputes EffectiveCapabilities) before it can throw, so a
        // throwing CapabilitiesChanged subscriber can leave this field
        // pointing at an already-disposed, already-removed token while
        // capabilities are in fact no longer suppressed.
        if (Control.EffectiveCapabilities == PVXControlCapabilities.None)
            return;

        // Uses the non-notifying path, not the public AcquireRestriction:
        // we're very likely here because a CapabilitiesChanged subscriber
        // just threw, and the public, notifying overload would re-enter
        // that same subscriber, roll itself back on failure (by design,
        // for ordinary callers), and leave this session un-suppressed.
        lifecycleSuspension = Control.AcquireRestrictionWithoutNotification(PVXControlCapabilities.None, owner: this);
    }
}
