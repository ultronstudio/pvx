namespace PVX.World;

/// <summary>
/// Lifecycle state of a <see cref="PVXDoorComponent"/>.
/// </summary>
/// <remarks>
/// Deliberately not a <c>bool IsOpen</c>: a door spends real, observable
/// time mid-transition, and <see cref="PVXDoorComponent"/>'s own
/// interaction availability (busy while transitioning) depends on that
/// being a distinct, queryable state rather than inferred from progress.
/// </remarks>
public enum PVXDoorState
{
    Closed,
    Opening,
    Open,
    Closing
}
