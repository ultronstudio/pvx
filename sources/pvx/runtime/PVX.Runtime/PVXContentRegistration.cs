using System.Runtime.CompilerServices;

namespace PVX.Runtime;

/// <summary>
/// Forces a PVX content assembly's serializer registration to run before the
/// first time something out of a compiled scene needs it, instead of
/// depending on an incidental earlier type touch to have loaded it.
/// </summary>
/// <remarks>
/// <para>
/// An assembly's AssemblyProcessor-generated serializer registration
/// (<c>DataSerializerFactory.RegisterSerializationAssembly</c>, which is how
/// <c>PVXDoorComponent</c>, <c>PVXSwitchComponent</c>, etc. become
/// deserializable from a compiled <c>.sdscene</c> at all) runs from that
/// assembly's module initializer. The CLR only runs a module initializer
/// once something actually loads that module — and merely referencing a
/// type by name (a field type, a <c>using</c>, even <c>typeof(T)</c> itself)
/// does not reliably do that ahead of time. In this engine fork, calling
/// <c>Content.Load&lt;Scene&gt;</c> before anything else has happened to
/// construct an instance of a type from a given PVX content assembly throws
/// "No serializer available for type id ... and base type
/// Stride.Engine.EntityComponent" for every component type from that
/// not-yet-loaded assembly — confirmed by reproducing it with an isolated
/// repro that moves the <c>Content.Load</c> call earlier/later relative to
/// the first construction of a type from the assembly in question.
/// </para>
/// <para>
/// A PVX game must call <see cref="EnsureLoaded{T}"/> once per PVX content
/// assembly it references — passing any type from that assembly — before
/// its first <c>Content.Load</c> call. The natural place is the game's
/// constructor, alongside its <c>Runtime.AddFeature(...)</c> calls.
/// </para>
/// </remarks>
public static class PVXContentRegistration
{
    /// <summary>
    /// Ensures <typeparamref name="T"/>'s declaring assembly's module
    /// initializer (and therefore its serializer registration, if any) has
    /// run. Idempotent and safe to call more than once or from more than
    /// one game for the same assembly.
    /// </summary>
    public static void EnsureLoaded<T>() =>
        RuntimeHelpers.RunModuleConstructor(typeof(T).Module.ModuleHandle);
}
