namespace PVX.Interaction;

/// <summary>
/// Read-only snapshot of the single in-progress interaction, if any.
/// </summary>
/// <remarks>
/// Fully immutable: <see cref="PVXInteractionService"/> advances hold
/// progress by replacing this instance wholesale on each update rather
/// than mutating it in place, so callers can never observe or corrupt
/// internal mutable state.
/// </remarks>
public sealed class PVXActiveInteraction
{
    internal PVXActiveInteraction(PVXInteractionContext context, PVXInteractionDescriptor descriptor, TimeSpan elapsed)
    {
        Context = context;
        Descriptor = descriptor;
        Elapsed = elapsed;
    }

    /// <summary>
    /// Source, target and interaction point this interaction was begun with.
    /// </summary>
    public PVXInteractionContext Context { get; }

    /// <summary>
    /// The action descriptor this interaction is executing.
    /// </summary>
    public PVXInteractionDescriptor Descriptor { get; }

    /// <summary>
    /// Elapsed hold time so far. Always <see cref="TimeSpan.Zero"/> for
    /// <see cref="PVXInteractionMode.Instant"/>.
    /// </summary>
    public TimeSpan Elapsed { get; }

    /// <summary>
    /// Normalized progress in [0, 1]. Always 1 for <see cref="PVXInteractionMode.Instant"/>.
    /// </summary>
    public double Progress
    {
        get
        {
            if (Descriptor.Mode != PVXInteractionMode.Hold || Descriptor.HoldDuration is not { } duration || duration <= TimeSpan.Zero)
                return 1.0;

            return Math.Clamp(Elapsed.TotalSeconds / duration.TotalSeconds, 0.0, 1.0);
        }
    }
}
