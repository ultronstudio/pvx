namespace PVX.Gameplay;

/// <summary>
/// Owns the currently registered <see cref="IPVXPlayerProvider"/> and the
/// primary player it last resolved.
/// </summary>
public sealed class PVXPlayerContext
{
    private IPVXPlayerProvider? provider;
    private IPVXPlayer? primaryPlayer;

    /// <summary>
    /// The currently resolved primary player, or <see langword="null"/> if
    /// none is registered or the provider currently has none.
    /// </summary>
    public IPVXPlayer? PrimaryPlayer => primaryPlayer;

    /// <summary>
    /// Raised only when the resolved primary player reference actually changes.
    /// </summary>
    public event EventHandler<PVXPrimaryPlayerChangedEventArgs>? PrimaryPlayerChanged;

    /// <summary>
    /// Registers the provider used to resolve the primary player and
    /// immediately re-resolves it.
    /// </summary>
    public void RegisterProvider(IPVXPlayerProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        this.provider = provider;
        Refresh();
    }

    /// <summary>
    /// Clears the registered provider and the resolved primary player.
    /// Safe to call when no provider is registered.
    /// </summary>
    public void UnregisterProvider()
    {
        provider = null;
        SetPrimaryPlayer(null);
    }

    /// <summary>
    /// Re-resolves the primary player from the currently registered
    /// provider. No-op (clears to null) if no provider is registered.
    /// </summary>
    public void Refresh()
    {
        SetPrimaryPlayer(provider?.GetPrimaryPlayer());
    }

    private void SetPrimaryPlayer(IPVXPlayer? player)
    {
        if (ReferenceEquals(primaryPlayer, player))
            return;

        var previous = primaryPlayer;
        primaryPlayer = player;
        PrimaryPlayerChanged?.Invoke(this, new PVXPrimaryPlayerChangedEventArgs(previous, player));
    }
}
