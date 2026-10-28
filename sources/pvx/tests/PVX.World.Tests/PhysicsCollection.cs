using Xunit;

namespace PVX.World.Tests;

/// <summary>
/// Groups every test class that needs a live <see cref="PhysicsHostGame"/>
/// into one xunit collection sharing a single <see cref="PhysicsFixture"/>.
/// See <see cref="PhysicsFixture"/>'s remarks for why this is required,
/// not just an optimization.
/// </summary>
[CollectionDefinition("PVX World Physics")]
public sealed class PhysicsCollection : ICollectionFixture<PhysicsFixture>
{
}
