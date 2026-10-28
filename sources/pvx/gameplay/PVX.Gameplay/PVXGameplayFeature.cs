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

    public override void Shutdown()
    {
        services?.Remove<PVXGameplaySession>();
        services = null;
    }
}
