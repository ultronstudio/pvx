using PVX.Core;
using PVX.Runtime.Services;

namespace PVX.Runtime;

/// <summary>
/// Owns the lifecycle and feature set of a running PVX game.
/// </summary>
public sealed class PVXRuntime
{
    private readonly List<IPVXRuntimeFeature> features = new();
    private readonly PVXRuntimeContext context;

    private int initializedFeatureCount;

    public PVXRuntime()
    {
        Services = new PVXServiceRegistry();
        context = new PVXRuntimeContext(this, Services);

        Services.Add(this);
        Services.Add(context);
    }

    /// <summary>
    /// Current runtime state.
    /// </summary>
    public PVXRuntimeState State { get; private set; } =
        PVXRuntimeState.Created;

    /// <summary>
    /// Registered PVX runtime services.
    /// </summary>
    public PVXServiceRegistry Services { get; }

    /// <summary>
    /// Registered runtime features, in initialization/update order.
    /// </summary>
    public IReadOnlyList<IPVXRuntimeFeature> Features => features;

    /// <summary>
    /// PVX Engine version associated with this runtime.
    /// </summary>
    public string EngineVersion => PVXVersion.InformationalVersion;

    /// <summary>
    /// Registers a runtime feature.
    /// Features must be registered before initialization.
    /// </summary>
    public TFeature AddFeature<TFeature>(TFeature feature)
        where TFeature : class, IPVXRuntimeFeature
    {
        ArgumentNullException.ThrowIfNull(feature);

        if (State is not PVXRuntimeState.Created
            and not PVXRuntimeState.Stopped)
        {
            throw new InvalidOperationException(
                $"Cannot register PVX feature '{feature.Name}' while runtime is in state '{State}'.");
        }

        if (features.Any(x => x.GetType() == feature.GetType()))
        {
            throw new InvalidOperationException(
                $"PVX feature '{feature.GetType().FullName}' is already registered.");
        }

        features.Add(feature);

        return feature;
    }

    internal void Initialize()
    {
        if (State == PVXRuntimeState.Running)
            return;

        if (State is not PVXRuntimeState.Created
            and not PVXRuntimeState.Stopped)
        {
            throw new InvalidOperationException(
                $"PVX runtime cannot initialize from state '{State}'.");
        }

        State = PVXRuntimeState.Initializing;
        initializedFeatureCount = 0;

        try
        {
            foreach (var feature in features)
            {
                feature.Initialize(context);
                initializedFeatureCount++;
            }

            State = PVXRuntimeState.Running;
        }
        catch
        {
            RollbackInitializedFeatures();
            State = PVXRuntimeState.Faulted;
            throw;
        }
    }

    internal void Update(in PVXFrameTime time)
    {
        if (State != PVXRuntimeState.Running)
            return;

        try
        {
            foreach (var feature in features)
            {
                feature.Update(in time);
            }
        }
        catch
        {
            State = PVXRuntimeState.Faulted;
            throw;
        }
    }

    internal void Shutdown()
    {
        if (State is PVXRuntimeState.Created
            or PVXRuntimeState.Stopped)
        {
            State = PVXRuntimeState.Stopped;
            return;
        }

        if (State == PVXRuntimeState.Stopping)
            return;

        State = PVXRuntimeState.Stopping;

        Exception? firstError = null;

        for (var i = initializedFeatureCount - 1; i >= 0; i--)
        {
            try
            {
                features[i].Shutdown();
            }
            catch (Exception ex)
            {
                firstError ??= ex;
            }
        }

        initializedFeatureCount = 0;
        State = PVXRuntimeState.Stopped;

        if (firstError != null)
        {
            throw new InvalidOperationException(
                "One or more PVX runtime features failed during shutdown.",
                firstError);
        }
    }

    private void RollbackInitializedFeatures()
    {
        for (var i = initializedFeatureCount - 1; i >= 0; i--)
        {
            try
            {
                features[i].Shutdown();
            }
            catch
            {
                // Preserve the original initialization exception.
            }
        }

        initializedFeatureCount = 0;
    }
}
