namespace PVX.Runtime;

/// <summary>
/// Timing information for one PVX runtime update.
/// </summary>
public readonly record struct PVXFrameTime(
    TimeSpan Total,
    TimeSpan Elapsed,
    TimeSpan ScaledElapsed,
    int FrameIndex);
