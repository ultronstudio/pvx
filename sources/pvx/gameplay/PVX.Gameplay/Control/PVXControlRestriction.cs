namespace PVX.Gameplay;

/// <summary>
/// A disposable token returned by <see cref="PVXControlService.AcquireRestriction"/>.
/// While held, it limits <see cref="PVXControlService.EffectiveCapabilities"/>
/// to its <see cref="Allowed"/> set; disposing it lifts that limit.
/// </summary>
public sealed class PVXControlRestriction : IDisposable
{
    private readonly PVXControlService owner;
    private bool disposed;

    internal PVXControlRestriction(PVXControlService owner, PVXControlCapabilities allowed, object? ownerTag)
    {
        this.owner = owner;
        Allowed = allowed;
        OwnerTag = ownerTag;
    }

    /// <summary>
    /// Capabilities this restriction allows. Combined with every other
    /// active restriction's <see cref="Allowed"/> set and the base
    /// capabilities to compute the effective capability set.
    /// </summary>
    public PVXControlCapabilities Allowed { get; }

    /// <summary>
    /// Optional diagnostic tag identifying who acquired this restriction.
    /// Never used for restriction identity or removal; the token itself is.
    /// </summary>
    public object? OwnerTag { get; }

    /// <summary>
    /// Releases this restriction. Safe to call more than once.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        owner.Release(this);
    }
}
