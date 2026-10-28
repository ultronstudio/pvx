using System.Diagnostics;
using System.Text;
using Xunit;

namespace PVX.World.Tests;

/// <summary>
/// Launches the real, already-built <c>PVX.World.Playground.exe</c> as a
/// separate OS process (never instantiating the game class inside this
/// test host) and asserts it starts, initializes real physics, drives
/// Door/Switch through their public API, and exits cleanly. This is the
/// one thing the 70+ in-process real-physics tests elsewhere in this
/// solution cannot prove: that the exact same code works when launched as
/// a standalone apphost, not just under the xunit test host (the two
/// differ in assembly-binding behavior — see
/// Stride.Core.Reflection.AssemblyIdentityFallbackResolver's remarks for
/// why that distinction matters here).
/// </summary>
public sealed class StandaloneSmokeTests
{
    [Fact]
    public void StandaloneExecutable_HeadlessSmoke_ExitsZeroWithAllChecksPassing()
    {
        var (exitCode, output) = RunSmoke();

        Assert.True(output.Contains("[PVX Playground] Ready."), $"Missing Ready marker. Output:\n{output}");
        Assert.True(output.Contains("[PVX Playground Smoke] PASS"), $"Smoke checks did not report PASS. Output:\n{output}");
        Assert.False(output.Contains("[PVX Playground Smoke] FAIL"), $"Smoke checks reported FAIL. Output:\n{output}");
        Assert.False(output.Contains("TypeInitializationException"), $"Standalone process hit a type-init crash. Output:\n{output}");
        Assert.False(output.Contains("Unhandled exception"), $"Standalone process hit an unhandled exception. Output:\n{output}");
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void StandaloneExecutable_HeadlessSmoke_IsStableAcrossThreeConsecutiveLaunches()
    {
        for (var i = 0; i < 3; i++)
        {
            var (exitCode, output) = RunSmoke();
            Assert.Equal(0, exitCode);
            Assert.True(output.Contains("[PVX Playground Smoke] PASS"), $"Run {i + 1} did not PASS. Output:\n{output}");
        }
    }

    private static (int ExitCode, string Output) RunSmoke()
    {
        var exePath = FindPlaygroundExecutable();

        var startInfo = new ProcessStartInfo(exePath, "--headless-smoke")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(exePath),
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start process: {exePath}");

        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) output.AppendLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var exited = process.WaitForExit(60_000);
        if (!exited)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"PVX.World.Playground.exe --headless-smoke did not exit within 60s. Output so far:\n{output}");
        }

        return (process.ExitCode, output.ToString());
    }

    /// <summary>
    /// Locates the sibling PVX.World.Playground project's build output by
    /// deriving the Configuration/TargetFramework segments from this test
    /// assembly's own output path, rather than hardcoding either — both
    /// projects are built with the same solution configuration.
    /// </summary>
    private static string FindPlaygroundExecutable()
    {
        var testOutputDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var tfm = Path.GetFileName(testOutputDir);
        var configuration = Path.GetFileName(Path.GetDirectoryName(testOutputDir) ?? string.Empty);

        if (string.IsNullOrEmpty(tfm) || string.IsNullOrEmpty(configuration))
            throw new InvalidOperationException($"Could not derive Configuration/TargetFramework from test output path: {testOutputDir}");

        // sources/pvx/tests/PVX.World.Tests/bin/<Config>/<TFM> -> sources/pvx/playground/PVX.World.Playground/bin/<Config>/<TFM>
        var repoSourcesPvxDir = Path.GetFullPath(Path.Combine(testOutputDir, "..", "..", "..", "..", ".."));
        var exePath = Path.Combine(repoSourcesPvxDir, "playground", "PVX.World.Playground", "bin", configuration, tfm, "PVX.World.Playground.exe");

        if (!File.Exists(exePath))
            throw new FileNotFoundException($"PVX.World.Playground.exe not found. Build it first (dotnet build sources/pvx/playground/PVX.World.Playground/PVX.World.Playground.csproj -c {configuration}). Looked at: {exePath}", exePath);

        return exePath;
    }
}
