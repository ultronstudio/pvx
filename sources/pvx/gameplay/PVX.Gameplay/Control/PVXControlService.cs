namespace PVX.Gameplay;

/// <summary>
/// Aggregates composable restriction tokens into the primary player's
/// currently effective <see cref="PVXControlCapabilities"/>.
/// </summary>
/// <remarks>
/// Effective capabilities are <see cref="BaseCapabilities"/> intersected
/// with every currently active restriction's allowed set. Restrictions
/// compose independently of acquire/release order: releasing one while
/// others remain active only restores the capabilities those other
/// restrictions still allow, never the full base set.
/// </remarks>
public sealed class PVXControlService
{
    private readonly List<PVXControlRestriction> restrictions = [];

    /// <summary>
    /// Capabilities available with no restrictions active.
    /// </summary>
    public PVXControlCapabilities BaseCapabilities => PVXControlCapabilities.All;

    /// <summary>
    /// Currently effective capabilities: <see cref="BaseCapabilities"/>
    /// narrowed by every active restriction.
    /// </summary>
    public PVXControlCapabilities EffectiveCapabilities { get; private set; } = PVXControlCapabilities.All;

    /// <summary>
    /// Convenience semantic summary derived from <see cref="EffectiveCapabilities"/>.
    /// </summary>
    public PVXControlMode Mode =>
        EffectiveCapabilities == PVXControlCapabilities.None ? PVXControlMode.Disabled :
        EffectiveCapabilities == BaseCapabilities ? PVXControlMode.Full :
        PVXControlMode.Limited;

    /// <summary>
    /// Raised whenever <see cref="EffectiveCapabilities"/> actually changes.
    /// </summary>
    public event EventHandler<PVXControlCapabilitiesChangedEventArgs>? CapabilitiesChanged;

    /// <summary>
    /// Acquires a restriction limiting effective capabilities to <paramref name="allowed"/>
    /// for as long as the returned token is not disposed.
    /// </summary>
    /// <param name="allowed">Capabilities this restriction permits.</param>
    /// <param name="owner">Optional diagnostic tag; never used as restriction identity.</param>
    /// <exception cref="Exception">
    /// Propagates whatever a <see cref="CapabilitiesChanged"/> subscriber throws.
    /// Transactional: if this call throws, the restriction was not retained
    /// and <see cref="EffectiveCapabilities"/> is exactly what it was before
    /// the call — there is no token to dispose and nothing to clean up.
    /// </exception>
    public PVXControlRestriction AcquireRestriction(PVXControlCapabilities allowed, object? owner = null)
    {
        var restriction = new PVXControlRestriction(this, allowed, owner);
        restrictions.Add(restriction);

        try
        {
            Recompute(notify: true);
            return restriction;
        }
        catch
        {
            // Roll back without re-entering user code: notify:false can't
            // throw, so this can't mask the exception being propagated.
            restrictions.Remove(restriction);
            Recompute(notify: false);
            throw;
        }
    }

    /// <summary>
    /// Acquires a restriction without ever invoking <see cref="CapabilitiesChanged"/>.
    /// </summary>
    /// <remarks>
    /// For <c>PVXGameplaySession</c>'s fault handling only: a session
    /// transitioning to <see cref="PVXGameplayState.Faulted"/> because a
    /// <see cref="CapabilitiesChanged"/> subscriber threw must still be able
    /// to suppress control, even though re-entering that same broken
    /// notification path is exactly what just failed. The public,
    /// notifying <see cref="AcquireRestriction"/> cannot give that
    /// guarantee — a persistently throwing subscriber would just roll this
    /// one back too.
    /// </remarks>
    internal PVXControlRestriction AcquireRestrictionWithoutNotification(PVXControlCapabilities allowed, object? owner)
    {
        var restriction = new PVXControlRestriction(this, allowed, owner);
        restrictions.Add(restriction);
        Recompute(notify: false);
        return restriction;
    }

    internal void Release(PVXControlRestriction restriction)
    {
        if (restrictions.Remove(restriction))
        {
            Recompute(notify: true);
        }
    }

    private void Recompute(bool notify)
    {
        var next = ComputeEffectiveCapabilities();

        if (next == EffectiveCapabilities)
            return;

        var previous = EffectiveCapabilities;
        EffectiveCapabilities = next;

        if (notify)
        {
            CapabilitiesChanged?.Invoke(this, new PVXControlCapabilitiesChangedEventArgs(previous, next));
        }
    }

    private PVXControlCapabilities ComputeEffectiveCapabilities()
    {
        var result = BaseCapabilities;

        foreach (var restriction in restrictions)
        {
            result &= restriction.Allowed;
        }

        return result;
    }
}
