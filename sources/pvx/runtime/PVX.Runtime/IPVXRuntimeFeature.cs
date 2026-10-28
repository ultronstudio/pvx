namespace PVX.Runtime;

/// <summary>
/// A runtime feature managed by PVX.
/// </summary>
public interface IPVXRuntimeFeature
{
    /// <summary>
    /// Human-readable feature name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Called when the feature enters the running PVX runtime.
    /// </summary>
    void Initialize(PVXRuntimeContext context);

    /// <summary>
    /// Called once per PVX update.
    /// </summary>
    void Update(in PVXFrameTime time);

    /// <summary>
    /// Called when the runtime is shutting down.
    /// </summary>
    void Shutdown();
}
