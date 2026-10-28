using PVX.Gameplay;
using PVX.Runtime;
using Stride.Engine;

namespace PVX.Interaction.Stride.Tests;

/// <summary>
/// Minimal headless game host that guarantees a <see cref="SceneInstance"/>
/// exists, without going through the asset pipeline (everything is a
/// programmatically-built scene), and that also boots a real PVX runtime
/// with <see cref="PVXGameplayFeature"/> and <see cref="PVXInteractionFeature"/>
/// registered and an active gameplay session — needed by
/// <c>InteractionControllerTests</c>, which exercises
/// <see cref="PVXStrideInteractionControllerComponent"/> through the real
/// runtime feature wiring rather than a bare <c>PVXInteractionService</c>.
/// </summary>
internal sealed class PhysicsHostGame : PVXGame
{
    private readonly TaskCompletionSource ready = new();

    public PhysicsHostGame()
    {
        Runtime.AddFeature(new PVXGameplayFeature());
        Runtime.AddFeature(new PVXInteractionFeature());
    }

    /// <summary>
    /// Completes once <see cref="Game.SceneSystem"/> has a <see cref="SceneInstance"/>
    /// with an empty root scene and the gameplay session is active, ready
    /// for tests to add entities to.
    /// </summary>
    public Task Ready => ready.Task;

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        SceneSystem.SceneInstance ??= new SceneInstance(Services) { RootScene = new Scene() };

        var session = Runtime.Services.GetRequired<PVXGameplaySession>();
        session.Start();
        session.Player.RegisterProvider(new HostPlayerProvider());

        ready.TrySetResult();
    }

    private sealed class HostPlayer : IPVXPlayer
    {
    }

    private sealed class HostPlayerProvider : IPVXPlayerProvider
    {
        private readonly HostPlayer player = new();

        public IPVXPlayer? GetPrimaryPlayer() => player;
    }
}
