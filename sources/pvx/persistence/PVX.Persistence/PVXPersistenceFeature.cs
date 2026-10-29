using PVX.Runtime;
using PVX.Runtime.Services;

namespace PVX.Persistence;

/// <summary>
/// Connects a <see cref="PVXSaveStore"/> to the PVX runtime: registers it
/// into the runtime's service registry on initialize, and owns loading it
/// from (and saving it to) a file on disk.
/// </summary>
/// <remarks>
/// File I/O is synchronous and uses the whole file as one JSON document —
/// deliberately not async, chunked, or versioned: see <see cref="PVXSaveStore"/>'s
/// own remarks for why a save-file format/schema is out of scope for 0.1.
/// </remarks>
public sealed class PVXPersistenceFeature : PVXRuntimeFeature
{
    private PVXServiceRegistry? services;

    /// <summary>
    /// The save-data store owned by this feature, available once initialized.
    /// </summary>
    public PVXSaveStore? Store { get; private set; }

    public override void Initialize(PVXRuntimeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        services = context.Services;
        Store = new PVXSaveStore();
        services.Add(Store);
    }

    /// <summary>
    /// Writes <see cref="Store"/>'s current contents to <paramref name="path"/>,
    /// overwriting it if it already exists.
    /// </summary>
    public void SaveToFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Store is null)
            throw new InvalidOperationException("PVXPersistenceFeature has not been initialized yet.");

        File.WriteAllText(path, Store.Serialize());
    }

    /// <summary>
    /// Replaces <see cref="Store"/>'s contents with the contents of
    /// <paramref name="path"/>. No-op if <paramref name="path"/> does not
    /// exist — a first run with no prior save is not an error.
    /// </summary>
    public bool LoadFromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Store is null)
            throw new InvalidOperationException("PVXPersistenceFeature has not been initialized yet.");

        if (!File.Exists(path))
            return false;

        Store.Deserialize(File.ReadAllText(path));
        return true;
    }

    public override void Shutdown()
    {
        services?.Remove<PVXSaveStore>();
        services = null;
        Store = null;
    }
}
