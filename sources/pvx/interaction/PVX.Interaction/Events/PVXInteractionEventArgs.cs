namespace PVX.Interaction;

/// <summary>
/// Raised for interaction started/completed/cancelled/failed transitions.
/// </summary>
public sealed class PVXInteractionEventArgs(
    PVXInteractionContext context,
    PVXInteractionDescriptor descriptor,
    string? reason = null) : EventArgs
{
    public PVXInteractionContext Context { get; } = context;

    public PVXInteractionDescriptor Descriptor { get; } = descriptor;

    /// <summary>
    /// Short reason code; populated for <c>InteractionCancelled</c> and
    /// <c>InteractionFailed</c>, <see langword="null"/> for others.
    /// </summary>
    public string? Reason { get; } = reason;
}
