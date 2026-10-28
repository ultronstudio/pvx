using Stride.Engine;

namespace PVX.World.Tests;

/// <summary>
/// Minimal headless game host guaranteeing a <see cref="SceneInstance"/>
/// exists, without any asset pipeline. Same pattern as
/// <c>PVX.Interaction.Stride.Tests.PhysicsHostGame</c>; duplicated here
/// rather than shared, since xunit collection fixtures (see
/// <see cref="PhysicsFixture"/>) cannot be shared across test assemblies.
/// </summary>
internal sealed class PhysicsHostGame : Game
{
    private readonly TaskCompletionSource ready = new();

    public Task Ready => ready.Task;

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        SceneSystem.SceneInstance ??= new SceneInstance(Services) { RootScene = new Scene() };

        ready.TrySetResult();
    }
}
