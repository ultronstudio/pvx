namespace PVX.Interaction;

/// <summary>
/// Identity seam for whoever is initiating interaction queries and actions.
/// </summary>
/// <remarks>
/// Intentionally empty. PVX.Interaction only needs some identity to pass
/// through target discovery and action execution; it never assumes the
/// source is a first-person camera or any other concrete implementation.
/// A source can be a player, an AI agent, an editor preview, a test
/// harness, or a future remote interaction source.
/// </remarks>
public interface IPVXInteractionSource
{
}
