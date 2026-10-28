// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace Stride.Core.Reflection;

/// <summary>
/// Falls back to an already-resolvable, same-simple-named assembly when the
/// CLR's default binder rejects a load request for one of a small,
/// explicitly known set of assembly names solely because of a version
/// mismatch.
/// </summary>
/// <remarks>
/// <c>deps/BulletPhysics/BulletSharp.NetStandard.dll</c> is a vendored, prebuilt
/// third-party binary whose module initializer makes static
/// <c>Type.GetType("Stride.Core, Version=4.0.0.1, ...")</c> calls. That exact
/// version was baked in whenever this binary was last processed by Stride's own
/// AssemblyProcessor against whatever Stride.Core AssemblyVersion was current at
/// the time — a point that predates even this fork's inherited Stride 4.5 line
/// (confirmed by grepping the binary: it requests 4.0.0.1 regardless of whether
/// Stride.Core's AssemblyVersion is 4.5.0.0 or something else entirely). So this
/// is not a problem this fork introduced by rebranding/renumbering Stride.Core;
/// it is a pre-existing stale-baked-identity defect in that one vendored binary,
/// and restoring Stride.Core's inherited version identity does not fix it.
/// <para>
/// Under the CLR's default, strict binder used by a normal apphost/standalone
/// process (a game, the asset compiler, or an editor host), that stale request
/// throws <see cref="FileNotFoundException"/> — even though an assembly with the
/// exact same simple name is already loaded in the process. (.NET test hosts such
/// as VSTest's testhost install their own lenient resolver, which is why identical
/// physics code can run fine under <c>dotnet test</c> while failing as a
/// standalone executable — confirmed with an isolated <c>Type.GetType</c> repro
/// with no Bullet/Physics code involved at all.)
/// </para>
/// <para>
/// This resolver is deliberately narrow: it only engages for assembly simple
/// names on <see cref="KnownStaleIdentityAssemblyNames"/>, so a genuinely
/// incompatible assembly version request for anything else still fails loudly
/// with the CLR's normal <see cref="FileNotFoundException"/> instead of silently
/// binding to whatever same-named assembly happens to already be loaded. Add a
/// name to that list only when a specific vendored binary is confirmed (by the
/// same kind of investigation as above, not by guessing) to bake a stale
/// assembly-qualified name against it.
/// </para>
/// </remarks>
public static class AssemblyIdentityFallbackResolver
{
    /// <summary>
    /// Assembly simple names confirmed to be requested with a stale, baked-in
    /// version by a specific vendored binary. See the type remarks for
    /// <c>Stride.Core</c> / <c>BulletSharp.NetStandard.dll</c>.
    /// </summary>
    private static readonly HashSet<string> KnownStaleIdentityAssemblyNames = new(StringComparer.Ordinal)
    {
        "Stride.Core",
    };

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
        if (requested.Name is null || !KnownStaleIdentityAssemblyNames.Contains(requested.Name))
            return null; // Not a known stale-identity case: let the CLR fail loudly.

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
