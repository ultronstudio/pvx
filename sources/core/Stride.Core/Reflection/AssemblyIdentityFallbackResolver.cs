// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Runtime.Loader;

namespace Stride.Core.Reflection;

/// <summary>
/// Falls back to an already-resolvable, same-simple-named assembly when the
/// CLR's default binder rejects a load request solely because of a version
/// mismatch.
/// </summary>
/// <remarks>
/// Some vendored, prebuilt third-party binaries this engine depends on (e.g.
/// <c>deps/BulletPhysics/BulletSharp.NetStandard.dll</c>) were processed, at some
/// point before this binary was last rebuilt upstream, by Stride's own
/// AssemblyProcessor against whatever Stride.Core AssemblyVersion was current at
/// the time, and have that exact version baked as a literal string into static
/// <c>Type.GetType(assemblyQualifiedName)</c> calls inside their module
/// initializers (e.g. "Stride.Core, Version=4.0.0.1, ..."). Any fork, rebrand, or
/// version-numbering change to Stride.Core's own AssemblyVersion (see
/// StrideVersion.AssemblyVersion) therefore stops matching, and those calls throw
/// <see cref="FileNotFoundException"/> under the CLR's default, strict binder used
/// by a normal apphost/standalone process (a game, the asset compiler, or an
/// editor host) — even though an assembly with the exact same simple name is
/// already loaded in the process. (.NET test hosts such as VSTest's testhost
/// install their own lenient resolver, which is why identical physics code can run
/// fine under `dotnet test` while failing as a standalone executable — confirmed
/// with an isolated <c>Type.GetType</c> repro with no Bullet/Physics code involved
/// at all.)
/// <para>
/// This resolver is deliberately generic (it matches by simple assembly name only,
/// never a specific assembly) because the same stale-baked-identity failure mode
/// could recur for any other vendored binary that embeds an assembly-qualified
/// name against a Stride assembly whose version a consumer has since changed.
/// </para>
/// </remarks>
public static class AssemblyIdentityFallbackResolver
{
    private static bool installed;

    /// <summary>
    /// Registers the fallback on <see cref="AssemblyLoadContext.Default"/>. Safe to
    /// call multiple times; only the first call has an effect. Must run before any
    /// code path that could trigger the stale-identity load request — as early as
    /// possible in process startup (e.g. the first line of Main, or a base type's
    /// static constructor for a host that doesn't control Main directly).
    /// </summary>
    public static void Install()
    {
        if (installed)
            return;

        installed = true;
        AssemblyLoadContext.Default.Resolving += ResolveBySimpleName;
    }

    private static Assembly? ResolveBySimpleName(AssemblyLoadContext context, AssemblyName requested)
    {
        foreach (var loaded in context.Assemblies)
        {
            if (string.Equals(loaded.GetName().Name, requested.Name, StringComparison.Ordinal))
                return loaded;
        }

        // Not already loaded: retry once with version/culture/token stripped so the
        // default binder can still match by simple name alone. Only when the
        // original request actually carried a version — otherwise this *is* already
        // the stripped retry, and returning null here avoids re-entering Resolving
        // indefinitely for a genuinely missing assembly.
        if (requested.Version is null)
            return null;

        try
        {
            return context.LoadFromAssemblyName(new AssemblyName(requested.Name));
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}
