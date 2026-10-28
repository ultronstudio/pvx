namespace PVX.Core;

/// <summary>
/// Defines the version of the PVX Engine product line.
/// </summary>
/// <remarks>
/// These constants are not auto-generated: Stride's build SDK disables
/// MSBuild's standard assembly version attribute generation for every
/// project (see GenerateAssemblyVersionAttribute in
/// sources/sdk/Stride.Build.Sdk/Sdk/Stride.PackageInfo.targets), so there
/// is no compiled attribute to read back at runtime. The single
/// authoritative MSBuild source of these values is
/// <c>sources/pvx/PVX.Version.props</c> — when bumping the PVX version,
/// update the properties there and mirror the same values here.
/// </remarks>
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
