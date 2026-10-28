using PVX.Runtime.Services;

namespace PVX.Runtime;

/// <summary>
/// Shared context supplied to PVX runtime features.
/// </summary>
public sealed class PVXRuntimeContext
{
    internal PVXRuntimeContext(
        PVXRuntime runtime,
        PVXServiceRegistry services)
    {
        Runtime = runtime;
        Services = services;
    }

    /// <summary>
    /// Current PVX runtime instance.
    /// </summary>
    public PVXRuntime Runtime { get; }

    /// <summary>
    /// Services exposed by PVX.
    /// </summary>
    public PVXServiceRegistry Services { get; }
}
