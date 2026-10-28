namespace PVX.Core;

/// <summary>
/// Provides product identity and build information for PVX Engine.
/// </summary>
public static class PVXInfo
{
    public const string EngineName = "PVX Engine";
    public const string StudioName = "PVX Studio";
    public const string ProductName = "PVX";

    public const string CompanyName = "ultron01";

    public const string UpstreamEngine = "Stride";

    /// <summary>
    /// The inherited Stride version this PVX build was forked/rebased from.
    /// Generated from <c>sources/shared/SharedAssemblyInfo.cs</c> (see
    /// <see cref="PVXVersion.UpstreamVersion"/>) rather than hand-duplicated
    /// here, so it cannot drift from the actual inherited Stride identity.
    /// </summary>
    public static string UpstreamVersion => PVXVersion.UpstreamVersion;

    public static string EngineDisplayName => $"{EngineName} {PVXVersion.InformationalVersion}";
    public static string StudioDisplayName => $"{StudioName} {PVXVersion.InformationalVersion}";
}
