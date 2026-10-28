using Stride.Engine;
using Stride.Physics;

namespace PVX.Interaction.Stride.Tests;

/// <summary>
/// Minimal headless game host that guarantees a <see cref="SceneInstance"/>
/// (and therefore a <see cref="PhysicsProcessor"/>/<see cref="Simulation"/>
/// once a physics entity is added) exists, without going through the
/// asset pipeline: everything here is a programmatically-built scene,
/// matching this milestone's "no .sdscene assets" requirement.
/// </summary>
internal sealed class PhysicsHostGame : Game
{
    private readonly TaskCompletionSource ready = new();

    /// <summary>
    /// Completes once <see cref="Game.SceneSystem"/> has a <see cref="SceneInstance"/>
    /// with an empty root scene, ready for tests to add entities to.
    /// </summary>
    public Task Ready => ready.Task;

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        SceneSystem.SceneInstance ??= new SceneInstance(Services) { RootScene = new Scene() };

        ready.TrySetResult();
    }
}
