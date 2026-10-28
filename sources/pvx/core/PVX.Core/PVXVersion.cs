namespace PVX.Core;

/// <summary>
/// Defines the version of the PVX Engine product line.
/// </summary>
/// <remarks>
/// Major/Minor/Patch/Prerelease/Version/InformationalVersion are not
/// declared here: the GeneratePVXVersion target in PVX.Core.csproj writes
/// them into PVXVersion.Generated.cs at build time, from the single
/// MSBuild source of PVX version numbers, <c>sources/pvx/PVX.Version.props</c>.
/// Bump the version there, not in a .cs file.
/// </remarks>
public static partial class PVXVersion
{
    public static System.Version RuntimeVersion { get; } = new(Major, Minor, Patch);
}
