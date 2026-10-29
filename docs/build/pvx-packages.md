# Consuming PVX packages from an external game project

Every `PVX.*` assembly auto-packs into `bin/packages` on a normal `dotnet build`, the
same way `Stride.*` does (see [versioning.md](versioning.md) for the version itself —
PVX has its own line, driven by [`sources/pvx/PVX.Version.props`](../../sources/pvx/PVX.Version.props),
separate from Stride's inherited version). No extra `pack` step is needed.

## Local dev workflow

1. **Build PVX dev packages** — build the engine once (any PVX project's build pulls in
   its own dependencies): `dotnet build sources/pvx/world/PVX.World/PVX.World.csproj`.
   This (re)populates `bin/packages/PVX.*.nupkg` and `bin/packages/Stride.*.nupkg`.
2. **Create a game project** outside `sources/pvx`, anywhere — it does not need to be
   inside this checkout at all. A plain `Microsoft.NET.Sdk` executable project works;
   it does not need `Stride.Build.Sdk` or a source-tree `ProjectReference`.
3. **Reference PVX via `PackageReference`**, e.g. `<PackageReference Include="PVX.World" Version="0.1.0-dev" />`
   (match whatever version is actually in `bin/packages`). To also compile `.sdscene`
   assets at build time, add `<PackageReference Include="Stride.AssetCompiler" Version="4.5.0-dev" IncludeAssets="build;buildTransitive" />`
   and set `<OutputType>Exe</OutputType>` — `Stride.AssetCompiler`'s own NuGet package
   ships a runnable compiler plus `build`/`buildTransitive` MSBuild targets, so this
   works with no reference back into the engine checkout.
4. **Point `nuget.config` at the local feed**, with package source mapping so `PVX`/`PVX.*`
   (and `Stride`/`Stride.*`) resolve from `bin/packages` instead of falling through to
   `nuget.org` (see this repo's own [`nuget.config`](../../nuget.config) for the exact
   pattern — a `PVX`/`PVX.*` mapping was added there for this reason).
5. **Restore and build** as normal (`dotnet restore` / `dotnet build`). Nothing PVX-specific
   is needed beyond the `nuget.config`.

Shadowless (PVX's actual dogfood game, a separate repository entirely outside this
checkout) currently still uses `ProjectReference` into this checkout rather than this
workflow (see its `Shadowless.Game.csproj`'s own comments for the two cross-repo-boundary
build fixes that needed either way: `Directory.Build.props`/`Directory.Packages.props`
don't auto-discover across a repo boundary, and `SkipSourceLink=true` since it isn't part
of the `ultronstudio/pvx` GitHub repo). Converting it to `PackageReference` once this
workflow is exercised more is a natural follow-up, not yet done.

## Proof

[`tests/enduser/Stride.Packaging.Tests/PvxConsumerPackagingTests.cs`](../../tests/enduser/Stride.Packaging.Tests/PvxConsumerPackagingTests.cs)
builds a minimal external consumer (`Fixtures/PvxConsumer`) against an isolated NuGet
cache and asserts it restores, builds, compiles a `.sdscene` containing a `PVXDoorComponent`,
and that the compiled content loads at runtime with the authored property value intact —
with no `ProjectReference` anywhere in the fixture.
