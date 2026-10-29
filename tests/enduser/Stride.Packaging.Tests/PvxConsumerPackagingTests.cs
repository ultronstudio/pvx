// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Stride.Packaging.Tests;

/// <summary>
/// Smoke-tests PVX.* packages as a real external consumer, the PVX analogue of
/// <see cref="NuGetConsumerTests"/>: a plain Microsoft.NET.Sdk project, outside
/// sources/pvx, with no ProjectReference back into the engine checkout —
/// PackageReference PVX.World (+ Stride.AssetCompiler for build-time asset
/// compilation) only, restored and built against the locally-built bin/packages
/// feed via a strict nuget.config.
/// </summary>
/// <remarks>
/// This is the thing Stage 5 exists to prove: a project created outside the PVX
/// engine repository can consume PVX.* purely through NuGet, with no relative
/// path into the source tree. PVX.World.dll, Stride.Engine.dll, Stride.Physics.dll
/// etc. all resolve as ordinary package dependencies (see PVX.World's own
/// generated nuspec for the dependency graph this exercises). It also goes one
/// step further than a bare restore+build: the asset compiler (delivered purely
/// via Stride.AssetCompiler's own NuGet build/buildTransitive payload, not
/// Stride.Build.Sdk) compiles a real .sdscene containing a PVXDoorComponent, and
/// the built consumer loads that compiled content at runtime and asserts the
/// authored property value survived.
/// </remarks>
[Collection("Packaging")]
public class PvxConsumerPackagingTests : IDisposable
{
    private readonly ITestOutputHelper output;
    private readonly List<string> caseDirs = [];
    public PvxConsumerPackagingTests(ITestOutputHelper output) => this.output = output;

    public void Dispose()
    {
        foreach (var caseDir in caseDirs)
            TestEnvironment.DeleteCaseDirectory(caseDir, output);
    }

    [Fact]
    public void ConsumerRestoresBuildsAndRunsAgainstPackageReferenceOnly()
    {
        var pvxVersion = TestEnvironment.ResolvePvxVersion();
        var strideVersion = TestEnvironment.ResolveStrideVersion();
        var fixtures = TestEnvironment.FixturesDir();

        var caseDir = Path.Combine(Path.GetTempPath(), "stride-packaging-tests", $"pvx-consumer-{Guid.NewGuid():N}");
        caseDirs.Add(caseDir);
        var consumerDir = Path.Combine(caseDir, "consumer");
        var nugetCache = Path.Combine(caseDir, "nuget");
        TestEnvironment.CopyDirectory(Path.Combine(fixtures, "PvxConsumer"), consumerDir);

        // No ProjectReference anywhere in this fixture — only this nuget.config
        // (pointing at bin/packages) stands between it and a real external
        // consumer's setup.
        NuGetConsumerFeed.WriteStrictNuGetConfig(consumerDir);

        var buildArgs = new[]
        {
            $"-p:PvxVersion={pvxVersion}",
            $"-p:StrideEngineVersion={strideVersion}",
            $"-p:RestorePackagesPath={nugetCache}",
        };
        var build = ConsumerBuild.Run(Path.Combine(consumerDir, "PvxConsumer.csproj"), consumerDir, output, timeoutMin: 10, buildArgs);
        Assert.True(build.ExitCode == 0, $"Consumer build should succeed (exit {build.ExitCode}).");

        // The asset compiler actually ran and compiled TestScene.sdscene's PVXDoorComponent —
        // not just that the C# project compiled.
        var binDir = Path.Combine(consumerDir, "bin", "Debug", "net10.0");
        var dataDb = Path.Combine(binDir, "data", "db");
        Assert.True(Directory.Exists(dataDb), $"Expected compiled content database at {dataDb}.");

        var run = Dotnet.Exec(["exec", Path.Combine(binDir, "PvxConsumer.dll")], binDir, output, timeoutMin: 2);
        Assert.True(run.ExitCode == 0, $"Consumer runtime content check failed (exit {run.ExitCode}).");
        Assert.Contains("CONTENT OK TestScene PVXDoorComponent.OpenAngleDegrees=77", run.Output);
    }
}
