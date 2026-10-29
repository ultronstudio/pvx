namespace PVX.World;

/// <summary>
/// Lifecycle state of a <see cref="PVXDrawerComponent"/>.
/// </summary>
/// <remarks>
/// Mirrors <see cref="PVXDoorState"/>'s own reasoning: a drawer spends
/// real, observable time mid-transition, and that must be a distinct,
/// queryable state rather than inferred from slide progress.
/// </remarks>
public enum PVXDrawerState
{
    Closed,
    Opening,
    Open,
    Closing
}
