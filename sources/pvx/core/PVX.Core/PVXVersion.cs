namespace PVX.Core;

/// <summary>
/// Defines the version of the PVX Engine product line.
/// </summary>
public static class PVXVersion
{
    public const int Major = 0;
    public const int Minor = 1;
    public const int Patch = 0;

    public const string Prerelease = "dev";

    public const string Version = "0.1.0";
    public const string InformationalVersion = "0.1.0-dev";

    public static System.Version RuntimeVersion { get; } = new(Major, Minor, Patch);
}
