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
    public const string UpstreamVersion = "4.5.0-dev";

    public static string EngineDisplayName => $"{EngineName} {PVXVersion.InformationalVersion}";
    public static string StudioDisplayName => $"{StudioName} {PVXVersion.InformationalVersion}";
}
