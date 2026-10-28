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
        }
        catch
        {
            State = PVXGameplayState.Faulted;
            throw;
        }

        GameplayStarted?.Invoke(this, EventArgs.Empty);
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
        }
        catch
        {
            State = PVXGameplayState.Faulted;
            throw;
        }

        GameplayPaused?.Invoke(this, EventArgs.Empty);
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
        }
        catch
        {
            State = PVXGameplayState.Faulted;
            throw;
        }

        GameplayResumed?.Invoke(this, EventArgs.Empty);
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
        }
        catch
        {
            State = PVXGameplayState.Faulted;
            throw;
        }

        GameplayEnded?.Invoke(this, EventArgs.Empty);
    }
}
