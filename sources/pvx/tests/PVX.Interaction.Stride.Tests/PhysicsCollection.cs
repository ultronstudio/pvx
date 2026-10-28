using Xunit;

namespace PVX.Interaction.Stride.Tests;

/// <summary>
/// Groups every test class that needs a live <see cref="PhysicsHostGame"/>
/// into one xunit collection sharing a single <see cref="PhysicsFixture"/>.
/// </summary>
/// <remarks>
/// Required, not just an optimization: <c>Stride.Engine.Game.PrepareContext()</c>
/// registers a process-global <c>VirtualFileSystem</c> provider at the
/// fixed mount point <c>"/asset/"</c>. xunit runs different test classes
/// in parallel by default, so two separately-constructed <see cref="PhysicsFixture"/>
/// instances (one per class, via <c>IClassFixture</c>) can both reach
/// <c>PrepareContext()</c> at once and throw
/// <c>"A Virtual File Provider with the root path '/asset/' already exists."</c>
/// Sharing one collection fixture guarantees only one <see cref="PhysicsHostGame"/>
/// — and therefore only one such registration — exists for this whole
/// test assembly.
/// </remarks>
[CollectionDefinition("PVX Physics")]
public sealed class PhysicsCollection : ICollectionFixture<PhysicsFixture>
{
}
