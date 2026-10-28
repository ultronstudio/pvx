using PVX.Runtime;
using PVX.Runtime.Services;

namespace PVX.Gameplay;

/// <summary>
/// Connects <see cref="PVXGameplaySession"/> to the PVX runtime: registers
/// it into the runtime's service registry on initialize and removes it on
/// shutdown, so it never outlives the feature.
/// </summary>
public sealed class PVXGameplayFeature : PVXRuntimeFeature
{
    private PVXServiceRegistry? services;

    /// <summary>
    /// The gameplay session owned by this feature.
    /// </summary>
    public PVXGameplaySession Session { get; } = new();

    public override void Initialize(PVXRuntimeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        services = context.Services;
        services.Add(Session);
    }

    /// <remarks>
    /// Deliberately does not call <see cref="PVXGameplaySession.End"/>: the
    /// runtime can be shutting down from a faulted state, and forcing an
    /// End here would run the session's own event subscribers (which can
    /// throw) as a side effect of tearing down an unrelated feature. The
    /// session is only removed from the registry; it does not survive
    /// past this call and callers must not keep using a reference to it
    /// once the owning runtime has shut down.
    /// </remarks>
    public override void Shutdown()
    {
        services?.Remove<PVXGameplaySession>();
        services = null;
    }
}
