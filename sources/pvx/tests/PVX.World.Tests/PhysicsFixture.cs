using Stride.Engine;
using Stride.Games;

namespace PVX.World.Tests;

/// <summary>
/// Starts exactly one headless <see cref="PhysicsHostGame"/> for an entire
/// xunit test class (via a shared collection, see <see cref="PhysicsCollection"/>),
/// since Stride's scene/physics API is not safe to call from an arbitrary
/// thread while the game loop is running, and because
/// <c>Game.PrepareContext()</c> registers a process-global
/// <c>VirtualFileSystem</c> provider that cannot be registered twice
/// concurrently — see <c>PVX.Interaction.Stride.Tests.PhysicsFixture</c>'s
/// own remarks for the exact failure this avoids.
/// </summary>
public sealed class PhysicsFixture : IDisposable
{
    private readonly PhysicsHostGame game;
    private readonly Thread gameThread;

    public Exception? GameLoopFault { get; private set; }

    public PhysicsFixture()
    {
        game = new PhysicsHostGame();

        gameThread = new Thread(() =>
        {
            try
            {
                game.Run(GameContextFactory.NewGameContext(AppContextType.Headless));
            }
            catch (Exception ex)
            {
                GameLoopFault = ex;
            }
        })
        {
            IsBackground = true,
            Name = "PVX.World.Tests physics game",
        };
        gameThread.Start();

        Task.WhenAny(game.Ready, Task.Delay(TimeSpan.FromSeconds(30))).Wait();

        if (!game.Ready.IsCompleted)
        {
            if (GameLoopFault is not null)
                throw new InvalidOperationException("PhysicsHostGame crashed during startup.", GameLoopFault);

            throw new TimeoutException("PhysicsHostGame did not finish LoadContent within 30 seconds.");
        }
    }

    public SceneSystem SceneSystem => game.SceneSystem;

    public async Task<TResult> RunScenario<TResult>(Func<Game, Task<TResult>> scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var completion = new TaskCompletionSource<TResult>();

        game.Script.AddTask(async () =>
        {
            try
            {
                var result = await scenario(game);
                completion.TrySetResult(result);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });

        var finished = await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(30)));

        if (finished != completion.Task)
        {
            throw GameLoopFault is not null
                ? new InvalidOperationException("PhysicsHostGame crashed while running a scenario.", GameLoopFault)
                : new TimeoutException("Scenario did not complete within 30 seconds.");
        }

        return await completion.Task;
    }

    public void Dispose()
    {
        game.Exit();
        gameThread.Join(TimeSpan.FromSeconds(10));
        game.Dispose();
    }
}
