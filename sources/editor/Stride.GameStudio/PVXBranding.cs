namespace Stride.GameStudio;

internal static class PVXBranding
{
    public const string EngineName = "PVX Engine";
    public const string StudioName = "PVX Studio";

    public const string Version = StrideVersion.NuGetVersion;

    // Informace o původnu kódu
    public const string UpstreamName = "Stride";
    public const string UpstreamVersion = "4.5.0-dev";

    public static string StudioDisplayName => $"{StudioName} {Version}";
    public static string EngineDisplayName => $"{EngineName} {Version}";
}
