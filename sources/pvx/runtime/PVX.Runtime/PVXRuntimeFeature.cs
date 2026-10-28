namespace PVX.Runtime;

/// <summary>
/// Convenience base implementation for PVX runtime features.
/// </summary>
public abstract class PVXRuntimeFeature : IPVXRuntimeFeature
{
    public virtual string Name => GetType().Name;

    public virtual void Initialize(PVXRuntimeContext context)
    {
    }

    public virtual void Update(in PVXFrameTime time)
    {
    }

    public virtual void Shutdown()
    {
    }
}
