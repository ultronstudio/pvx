namespace PVX.Persistence;

/// <summary>
/// Semantic, boolean-only view over a <see cref="PVXSaveStore"/> for
/// narrative progress: "has the player met this NPC", "has this key been
/// found", and similar one-shot story flags.
/// </summary>
/// <remarks>
/// Deliberately a thin wrapper, not a second parallel store: flags live in
/// the same <see cref="PVXSaveStore"/> everything else persists to (under
/// a reserved key prefix), so they save/load for free with the rest of the
/// game's state. Deliberately out of scope for 0.1: variables beyond
/// booleans (counters, string state), quest/objective structure, and
/// conditions/branching — a future narrative module can build those on top
/// of <see cref="PVXSaveStore"/> directly, the same way this one does.
/// </remarks>
public sealed class PVXNarrativeFlags : IDisposable
{
    private const string KeyPrefix = "narrative.flag.";

    private readonly PVXSaveStore store;

    public PVXNarrativeFlags(PVXSaveStore store)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.store.ValueChanged += OnStoreValueChanged;
    }

    /// <summary>
    /// Raised only when a flag's effective boolean value actually changes.
    /// </summary>
    public event EventHandler<PVXNarrativeFlagChangedEventArgs>? FlagChanged;

    /// <summary>
    /// Whether <paramref name="flagName"/> is currently set. Unset flags
    /// read as <see langword="false"/>, never throw.
    /// </summary>
    public bool IsSet(string flagName)
    {
        ArgumentNullException.ThrowIfNull(flagName);
        return store.GetBool(KeyPrefix + flagName);
    }

    /// <summary>
    /// Sets <paramref name="flagName"/> to <paramref name="value"/> (defaults
    /// to <see langword="true"/> — the common "mark this as having
    /// happened" call shape).
    /// </summary>
    public void Set(string flagName, bool value = true)
    {
        ArgumentNullException.ThrowIfNull(flagName);
        store.SetBool(KeyPrefix + flagName, value);
    }

    /// <summary>
    /// Unsets <paramref name="flagName"/>. Equivalent to <c>Set(flagName, false)</c>.
    /// </summary>
    public void Unset(string flagName) => Set(flagName, false);

    /// <summary>
    /// Unsubscribes from <see cref="PVXSaveStore.ValueChanged"/>. A
    /// <see cref="PVXNarrativeFlags"/> instance must not be used after this.
    /// </summary>
    public void Dispose() => store.ValueChanged -= OnStoreValueChanged;

    private void OnStoreValueChanged(object? sender, PVXSaveValueChangedEventArgs e)
    {
        if (!e.Key.StartsWith(KeyPrefix, StringComparison.Ordinal))
            return;

        var previous = e.Previous == "1";
        var current = e.Current == "1";

        if (previous == current)
            return;

        FlagChanged?.Invoke(this, new PVXNarrativeFlagChangedEventArgs(e.Key[KeyPrefix.Length..], previous, current));
    }
}
