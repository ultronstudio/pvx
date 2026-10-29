using Xunit;

namespace PVX.FirstPerson.Tests;

/// <summary>
/// Groups every test class that needs a live <see cref="FirstPersonHostGame"/>
/// into one xunit collection sharing a single <see cref="FirstPersonFixture"/> —
/// same rationale as PVX.Interaction.Stride.Tests's own <c>PhysicsCollection</c>:
/// <c>Stride.Engine.Game.PrepareContext()</c> registers a process-global
/// <c>VirtualFileSystem</c> provider, so only one live game may exist per
/// test assembly at a time.
/// </summary>
[CollectionDefinition("PVX FirstPerson")]
public sealed class FirstPersonCollection : ICollectionFixture<FirstPersonFixture>
{
}
