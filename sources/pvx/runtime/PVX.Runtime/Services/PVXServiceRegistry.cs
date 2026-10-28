namespace PVX.Runtime.Services;

/// <summary>
/// Lightweight service registry used by PVX runtime systems.
/// </summary>
public sealed class PVXServiceRegistry
{
    private readonly Dictionary<Type, object> services = new();

    /// <summary>
    /// Registers a service.
    /// </summary>
    public void Add<TService>(TService service)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(service);

        var type = typeof(TService);

        if (!services.TryAdd(type, service))
        {
            throw new InvalidOperationException(
                $"A PVX service of type '{type.FullName}' is already registered.");
        }
    }

    /// <summary>
    /// Registers or replaces a service.
    /// </summary>
    public void Set<TService>(TService service)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(service);

        services[typeof(TService)] = service;
    }

    /// <summary>
    /// Gets a service or returns null when it is not registered.
    /// </summary>
    public TService? Get<TService>()
        where TService : class
    {
        return services.TryGetValue(typeof(TService), out var service)
            ? (TService)service
            : null;
    }

    /// <summary>
    /// Gets a required service.
    /// </summary>
    public TService GetRequired<TService>()
        where TService : class
    {
        return Get<TService>()
            ?? throw new InvalidOperationException(
                $"Required PVX service '{typeof(TService).FullName}' is not registered.");
    }

    /// <summary>
    /// Removes a service.
    /// </summary>
    public bool Remove<TService>()
        where TService : class
    {
        return services.Remove(typeof(TService));
    }

    /// <summary>
    /// Returns true when a service is registered.
    /// </summary>
    public bool Contains<TService>()
        where TService : class
    {
        return services.ContainsKey(typeof(TService));
    }
}
