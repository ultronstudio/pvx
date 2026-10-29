using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace PVX.Templates.Tests;

/// <summary>
/// End-to-end smoke for the PVX template package: dotnet pack → dotnet new install →
/// dotnet new &lt;template&gt; for both pvx-game and pvx-psychological-horror. Mirrors
/// Stride.Templates.Tests' StrideGameTemplateSmokeTests (same preprocessor pipeline, same
/// reasoning for not running a real `dotnet restore` here — a fresh CI runner that only
/// built the PVX templates slnf wouldn't reliably have Stride.Engine/PVX.* in bin/packages
/// or on nuget.org). A real restore+build against the local bin/packages feed has been
/// verified manually; see docs/build/pvx-packages.md.
/// </summary>
public class PvxTemplateSmokeTests
{
    private readonly ITestOutputHelper output;

    public PvxTemplateSmokeTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void PackInstallInstantiate()
    {
        var repoRoot = FindRepoRoot();
        var enginePackagesDir = Path.Combine(repoRoot, "bin", "packages");
        Assert.True(Directory.Exists(enginePackagesDir),
            $"Expected bin/packages/ to exist at {enginePackagesDir}. Build the engine first " +
            "(any normal test run does this transitively).");

        const string packageId = "PVX.Templates.Games";
        var csproj = Path.Combine(repoRoot, "sources", "pvx", "templates", packageId, $"{packageId}.csproj");
        Assert.True(File.Exists(csproj), $"Templates csproj not found at {csproj}");

        var nupkg = FindNupkg(enginePackagesDir, packageId);
        if (nupkg == null)
        {
            output.WriteLine($"No pre-built nupkg for {packageId}; building it.");
            var packResult = RunDotnet(repoRoot, "build", csproj, "-c", "Debug", "--nologo", "-v", "minimal");
            Assert.True(packResult.exitCode == 0, $"dotnet build {packageId} failed:\n{packResult.output}");
            nupkg = FindNupkg(enginePackagesDir, packageId);
        }
        Assert.NotNull(nupkg);
        output.WriteLine($"Using nupkg: {nupkg}");

        var workspace = Path.Combine(Path.GetTempPath(), "pvx-template-smoke-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(workspace);
        try
        {
            RunDotnet(workspace, "new", "uninstall", packageId);

            var installResult = RunDotnet(workspace, "new", "install", nupkg);
            Assert.True(installResult.exitCode == 0, $"dotnet new install {nupkg} failed:\n{installResult.output}");

            try
            {
                InstantiateAndValidate(workspace, templateShortName: "pvx-game", projectName: "SmokePvxGame",
                    expectedPackageReferences: ["PVX.Runtime", "PVX.Gameplay", "Stride.Engine"]);
                InstantiateAndValidate(workspace, templateShortName: "pvx-psychological-horror", projectName: "SmokeHorror",
                    expectedPackageReferences: ["PVX.Runtime", "PVX.Gameplay", "PVX.Interaction", "PVX.Interaction.Stride", "PVX.World", "PVX.FirstPerson", "Stride.Engine"]);
            }
            finally
            {
                RunDotnet(workspace, "new", "uninstall", packageId);
            }
        }
        finally
        {
            try { Directory.Delete(workspace, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }

    /// <summary>
    /// Instantiates the given template with <c>-n &lt;projectName&gt;</c> and statically
    /// validates the generated game csproj: every expected PackageReference is present with
    /// a non-empty, metadata-free version (what a real restore would ultimately validate).
    /// </summary>
    private void InstantiateAndValidate(string workspace, string templateShortName, string projectName, string[] expectedPackageReferences)
    {
        var newResult = RunDotnet(workspace, "new", templateShortName, "-n", projectName);
        Assert.True(newResult.exitCode == 0, $"dotnet new {templateShortName} failed:\n{newResult.output}");

        var instantiated = Path.Combine(workspace, projectName);
        Assert.True(Directory.Exists(instantiated), $"Instantiated project dir missing at {instantiated}");

        var gameCsproj = Path.Combine(instantiated, $"{projectName}.Game", $"{projectName}.Game.csproj");
        Assert.True(File.Exists(gameCsproj), $"Expected game csproj at {gameCsproj}");

        var csprojText = File.ReadAllText(gameCsproj);
        foreach (var packageId in expectedPackageReferences)
        {
            var match = Regex.Match(csprojText, $"<PackageReference Include=\"{Regex.Escape(packageId)}\"\\s+Version=\"([^\"]+)\"");
            Assert.True(match.Success, $"Generated {Path.GetFileName(gameCsproj)} missing {packageId} PackageReference:\n{csprojText}");
            Assert.True(match.Groups[1].Value.Length > 0, $"{packageId} PackageReference has an empty version in {gameCsproj}");
            Assert.DoesNotContain("+", match.Groups[1].Value);
            Assert.DoesNotContain("$", match.Groups[1].Value);
        }

        var windowsCsproj = Path.Combine(instantiated, $"{projectName}.Windows", $"{projectName}.Windows.csproj");
        Assert.True(File.Exists(windowsCsproj), $"Expected Windows exec csproj at {windowsCsproj}");
    }

    private static string? FindNupkg(string dir, string packageId)
    {
        var prefix = packageId + ".";
        return Directory.EnumerateFiles(dir, "*.nupkg")
            .Where(f =>
            {
                var name = Path.GetFileName(f);
                return name.StartsWith(prefix, StringComparison.Ordinal)
                    && name.Length > prefix.Length
                    && char.IsDigit(name[prefix.Length]);
            })
            .OrderByDescending(File.GetLastWriteTime)
            .FirstOrDefault();
    }

    private (int exitCode, string output) RunDotnet(string workingDir, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        var combined = (stdout + stderr).Trim();
        output.WriteLine($"$ dotnet {string.Join(' ', args)}");
        output.WriteLine($"  → exit {proc.ExitCode}");
        if (combined.Length > 0)
            output.WriteLine(combined);
        return (proc.ExitCode, combined);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Stride.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate repo root (no Stride.slnx found walking up from test bin dir)");
    }
}
