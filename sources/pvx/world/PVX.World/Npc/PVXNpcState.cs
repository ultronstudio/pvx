namespace PVX.World;

/// <summary>
/// State of a <see cref="PVXNpcComponent"/>.
/// </summary>
/// <remarks>
/// Deliberately not a richer perception/behavior state machine: see
/// <see cref="PVXNpcComponent"/>'s own remarks for why pathfinding,
/// sensing, and reactive behavior are explicitly out of scope for 0.1.
/// </remarks>
public enum PVXNpcState
{
    Idle,
    Alert
}
