using Stride.Engine;
using Stride.Games;
using Stride.Physics;

namespace PVX.Interaction.Stride.Tests;

/// <summary>
/// Starts exactly one headless <see cref="PhysicsHostGame"/> for an entire
/// xunit test class (via <see cref="Xunit.IClassFixture{TFixture}"/>) and
/// lets individual facts run scenarios on the game's own thread, since
/// Stride's scene/physics API is not safe to call from an arbitrary thread
/// while the game loop is running.
/// </summary>
/// <remarks>
/// Each real physics raycast test in this project needs the actual Bullet
/// native backend running inside a live Stride game, same as the engine's
/// own Stride.Physics.Tests; spinning up a full headless game per fact
/// would be correct but needlessly slow, so this fixture amortizes that
/// cost across every fact in its test class.
/// </remarks>
public sealed class PhysicsFixture : IDisposable
{
    private readonly PhysicsHostGame game;
    private readonly Thread gameThread;

    /// <summary>
    /// Set if the game loop thread itself crashed (e.g. an unhandled
    /// exception from deep inside the engine's Draw/Update), as opposed to
    /// a scenario-level failure surfaced through <see cref="RunScenario{TResult}"/>.
    /// Checked by every fact so such a crash is reported as a clear,
    /// attributable test failure instead of silently killing the test host process.
    /// </summary>
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
            Name = "PVX.Interaction.Stride.Tests physics game",
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

    /// <summary>
    /// Runs <paramref name="scenario"/> on the game's own microthread
    /// (via <see cref="ScriptSystem.AddTask"/>) and marshals its result (or
    /// exception) back to the caller. The scenario receives the game
    /// itself so it can use both <see cref="Game.SceneSystem"/> and
    /// <see cref="Game.Script"/> (e.g. to await frames while entities settle).
    /// </summary>
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

        // If the game loop thread itself crashes while this scenario is
        // pending, completion.Task would otherwise never resolve and the
        // test would hang instead of failing.
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
