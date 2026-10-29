using System.Globalization;
using System.Text.Json;

namespace PVX.Persistence;

/// <summary>
/// A generic, stringly-typed key/value save-data bag: every value is
/// stored and round-tripped as a string, with typed convenience
/// accessors layered on top. Deliberately not a save-file format, slot
/// system, or versioned schema — see this milestone's remarks for why
/// those are explicitly out of scope for 0.1.
/// </summary>
/// <remarks>
/// <para>
/// Stringly-typed storage (rather than a <c>Dictionary&lt;string, object&gt;</c>
/// or separate typed dictionaries) keeps <see cref="Serialize"/>/<see cref="Deserialize"/>
/// trivial (a flat <c>Dictionary&lt;string, string&gt;</c> round-trips through
/// JSON with no custom converters) and keeps every value representable
/// regardless of which typed accessor a caller used to read it back —
/// the same flexibility <c>PlayerPrefs</c>-style stores trade simplicity
/// for. A door's open/closed flag, an NPC's alert state, and a narrative
/// flag can all live in the same store without needing a shared schema.
/// </para>
/// <para>
/// Deliberately does not know about files, <c>PVXRuntime</c>, or any
/// specific game's keys — <see cref="PVXPersistenceFeature"/> owns file
/// I/O and runtime registration; this class is pure data plus notification,
/// testable with no engine or disk access at all.
/// </para>
/// </remarks>
public sealed class PVXSaveStore
{
    private readonly Dictionary<string, string> values = new();

    /// <summary>
    /// Raised whenever a value actually changes (set to a different value,
    /// newly set, or removed). Not raised when a set/remove is a no-op.
    /// </summary>
    public event EventHandler<PVXSaveValueChangedEventArgs>? ValueChanged;

    /// <summary>
    /// Whether <paramref name="key"/> currently has a value.
    /// </summary>
    public bool HasKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return values.ContainsKey(key);
    }

    /// <summary>
    /// Removes <paramref name="key"/>, if present. Raises <see cref="ValueChanged"/>
    /// with a <see langword="null"/> <see cref="PVXSaveValueChangedEventArgs.Current"/>
    /// only if it was actually present.
    /// </summary>
    public bool Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!values.TryGetValue(key, out var previous))
            return false;

        values.Remove(key);
        ValueChanged?.Invoke(this, new PVXSaveValueChangedEventArgs(key, previous, null));
        return true;
    }

    public string? GetString(string key, string? defaultValue = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        return values.TryGetValue(key, out var value) ? value : defaultValue;
    }

    public void SetString(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        SetRaw(key, value);
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        var raw = GetString(key);
        return raw is null ? defaultValue : raw == "1";
    }

    public void SetBool(string key, bool value) => SetRaw(key, value ? "1" : "0");

    public int GetInt(string key, int defaultValue = 0)
    {
        var raw = GetString(key);
        return raw is null ? defaultValue : int.Parse(raw, CultureInfo.InvariantCulture);
    }

    public void SetInt(string key, int value) => SetRaw(key, value.ToString(CultureInfo.InvariantCulture));

    public float GetFloat(string key, float defaultValue = 0f)
    {
        var raw = GetString(key);
        return raw is null ? defaultValue : float.Parse(raw, CultureInfo.InvariantCulture);
    }

    public void SetFloat(string key, float value) => SetRaw(key, value.ToString("R", CultureInfo.InvariantCulture));

    /// <summary>
    /// Removes every value. Raises <see cref="ValueChanged"/> once per
    /// removed key, in no particular order.
    /// </summary>
    public void Clear()
    {
        var keys = values.Keys.ToArray();
        foreach (var key in keys)
        {
            Remove(key);
        }
    }

    /// <summary>
    /// Serializes every value to a compact JSON object.
    /// </summary>
    public string Serialize() => JsonSerializer.Serialize(values);

    /// <summary>
    /// Replaces every value with the contents of <paramref name="json"/>
    /// (as produced by <see cref="Serialize"/>). Raises <see cref="ValueChanged"/>
    /// for every key whose value actually changes, including keys present
    /// before but absent from <paramref name="json"/> (removed).
    /// </summary>
    public void Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var incoming = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? throw new ArgumentException("JSON did not deserialize to a key/value object.", nameof(json));

        foreach (var key in values.Keys.ToArray())
        {
            if (!incoming.ContainsKey(key))
            {
                Remove(key);
            }
        }

        foreach (var (key, value) in incoming)
        {
            SetRaw(key, value);
        }
    }

    private void SetRaw(string key, string value)
    {
        if (values.TryGetValue(key, out var previous) && previous == value)
            return;

        values[key] = value;
        ValueChanged?.Invoke(this, new PVXSaveValueChangedEventArgs(key, previous, value));
    }
}
