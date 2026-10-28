namespace PVX.Runtime;

/// <summary>
/// Current lifecycle state of the PVX runtime.
/// </summary>
public enum PVXRuntimeState
{
    Created,
    Initializing,
    Running,
    Stopping,
    Stopped,
    Faulted
}
