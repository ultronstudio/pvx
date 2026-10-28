namespace PVX.Gameplay;

/// <summary>
/// Opaque, game-defined identity of a gameplay difficulty/profile.
/// </summary>
/// <remarks>
/// PVX.Gameplay only stores and reports the currently selected id; it has
/// no built-in difficulty names and no opinion on what any id means. Each
/// game (and each future PVX module that reacts to difficulty) defines
/// and interprets its own ids.
/// </remarks>
public readonly record struct PVXDifficultyId
{
    public PVXDifficultyId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "PVX difficulty id must not be null or whitespace.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// The raw identifier string, e.g. <c>"shadowless.standard"</c>.
    /// </summary>
    public string Value { get; }

    public override string ToString() => Value;
}
