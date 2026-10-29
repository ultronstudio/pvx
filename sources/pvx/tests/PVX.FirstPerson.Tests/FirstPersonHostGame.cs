using PVX.Gameplay;
using PVX.Runtime;
using Stride.Engine;

namespace PVX.FirstPerson.Tests;

/// <summary>
/// Minimal headless game host, analogous to PVX.Interaction.Stride.Tests's
/// own <c>PhysicsHostGame</c>: guarantees a <see cref="SceneInstance"/> and
/// an active <see cref="PVXGameplaySession"/>, without going through the
/// asset pipeline (every test scene here is built programmatically).
/// </summary>
internal sealed class FirstPersonHostGame : PVXGame
{
    private readonly TaskCompletionSource ready = new();

    public FirstPersonHostGame()
    {
        // Matches Stride.Physics.Tests's own GameTest: a headless game
        // defaults to a variable timestep, which starves
        // CharacterComponent's gravity/falling of a consistent per-frame
        // simulation budget (Simulation.MaxTickDuration caps how much
        // simulated time one Update can catch up by) — fixed timestep is
        // what every other real-physics CharacterComponent test in this
        // engine relies on.
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromTicks(10_000_000 / 60);

        Runtime.AddFeature(new PVXGameplayFeature());
    }

    /// <summary>
    /// Completes once <see cref="Game.SceneSystem"/> has a <see cref="SceneInstance"/>
    /// with an empty root scene and the gameplay session is active.
    /// </summary>
    public Task Ready => ready.Task;

    public PVXGameplaySession Session { get; private set; } = null!;

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        SceneSystem.SceneInstance ??= new SceneInstance(Services) { RootScene = new Scene() };

        Session = Runtime.Services.GetRequired<PVXGameplaySession>();
        Session.Start();
        Session.Player.RegisterProvider(new HostPlayerProvider());

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
