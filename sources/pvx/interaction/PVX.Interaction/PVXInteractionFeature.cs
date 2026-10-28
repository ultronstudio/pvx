using PVX.Gameplay;
using PVX.Runtime;
using PVX.Runtime.Services;

namespace PVX.Interaction;

/// <summary>
/// Connects <see cref="PVXInteractionService"/> to the PVX runtime:
/// resolves the already-registered <see cref="PVXGameplaySession"/>,
/// registers the service into the runtime's service registry on
/// initialize, drives it once per update, and removes it on shutdown.
/// </summary>
/// <remarks>
/// Must be registered after <see cref="PVXGameplayFeature"/>: PVX runtime
/// features initialize in registration order (see <c>PVXRuntime.Initialize</c>),
/// and this feature requires <see cref="PVXGameplaySession"/> to already be
/// in the service registry when it initializes. If it is not,
/// <see cref="PVXServiceRegistry.GetRequired{TService}"/> fails clearly
/// rather than this feature silently doing nothing.
/// </remarks>
public sealed class PVXInteractionFeature : PVXRuntimeFeature
{
    private PVXServiceRegistry? services;

    /// <summary>
    /// The interaction service owned by this feature, available once initialized.
    /// </summary>
    public PVXInteractionService? Service { get; private set; }

    /// <summary>
    /// Interaction source driven each update. PVX.Interaction never assumes
    /// a first-person camera or any concrete source, so the host game must
    /// set this explicitly; <see cref="Update"/> is a no-op while it is <see langword="null"/>.
    /// </summary>
    public IPVXInteractionSource? Source { get; set; }

    public override void Initialize(PVXRuntimeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var session = context.Services.GetRequired<PVXGameplaySession>();

        services = context.Services;
        Service = new PVXInteractionService(session);
        services.Add(Service);
    }

    public override void Update(in PVXFrameTime time)
    {
        if (Service is null || Source is null)
            return;

        Service.Update(Source, time.Elapsed);
    }

    /// <remarks>
    /// Disposes the service (detaching it from <see cref="PVXGameplaySession"/>'s
    /// events — see <see cref="PVXInteractionService.Dispose"/>) before
    /// removing it from the registry, so a later Initialize on this same
    /// feature instance (the PVX runtime can be stopped and re-initialized
    /// while reusing feature instances) starts from a clean slate instead
    /// of leaving this instance's subscriptions on the still-alive session.
    /// Dispose() deliberately does not force-cancel an active interaction
    /// or run user event subscribers as a side effect of teardown, mirroring
    /// <see cref="PVXGameplayFeature.Shutdown"/>'s own restraint.
    /// </remarks>
    public override void Shutdown()
    {
        Service?.Dispose();
        services?.Remove<PVXInteractionService>();
        services = null;
        Service = null;
    }
}
