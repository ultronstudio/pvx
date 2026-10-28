namespace PVX.Interaction;

/// <summary>
/// Behavior behind one interaction action, separate from its
/// <see cref="PVXInteractionDescriptor"/> metadata.
/// </summary>
/// <remarks>
/// PVX.Interaction never knows what an action physically does. A door's
/// <c>Open</c> action, a ladder's <c>Climb</c> action and a box's
/// <c>Grab</c> action all implement this same contract and simply hand off
/// to whatever external system (door state, future traversal, future
/// physical manipulation, ...) owns the real behavior.
/// </remarks>
public interface IPVXInteractionAction
{
    /// <summary>
    /// Final gate checked immediately before <see cref="Begin"/>, in
    /// addition to the owning descriptor's <see cref="PVXInteractionDescriptor.Availability"/>.
    /// </summary>
    bool CanExecute(PVXInteractionContext context);

    /// <summary>
    /// Called once, synchronously, when the interaction begins (both for
    /// <see cref="PVXInteractionMode.Instant"/> and <see cref="PVXInteractionMode.Hold"/>).
    /// </summary>
    void Begin(PVXInteractionContext context);

    /// <summary>
    /// Performs the action's actual effect: immediately after
    /// <see cref="Begin"/> for <see cref="PVXInteractionMode.Instant"/>, or
    /// once hold progress reaches completion for <see cref="PVXInteractionMode.Hold"/>.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> if the action failed to perform its effect;
    /// the interaction is then reported as failed, never as completed.
    /// </returns>
    bool Execute(PVXInteractionContext context);

    /// <summary>
    /// Called when a begun interaction is cancelled before completion
    /// (hold released early, focus lost, capability lost, ...).
    /// </summary>
    void Cancel(PVXInteractionContext context);
}
