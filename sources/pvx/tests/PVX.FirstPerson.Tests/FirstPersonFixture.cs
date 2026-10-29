using Stride.Engine;
using Stride.Games;
using Stride.Input;

namespace PVX.FirstPerson.Tests;

/// <summary>
/// Starts exactly one headless <see cref="FirstPersonHostGame"/> for an
/// entire xunit test class (via <see cref="Xunit.IClassFixture{TFixture}"/>),
/// same rationale as PVX.Interaction.Stride.Tests's own <c>PhysicsFixture</c>:
/// the real Bullet native backend needs a live Stride game, and spinning up
/// a fresh one per fact would be needlessly slow.
/// </summary>
public sealed class FirstPersonFixture : IDisposable
{
    private readonly FirstPersonHostGame game;
    private readonly Thread gameThread;

    public Exception? GameLoopFault { get; private set; }

    public FirstPersonFixture()
    {
        game = new FirstPersonHostGame();

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
            Name = "PVX.FirstPerson.Tests physics game",
        };
        gameThread.Start();

        Task.WhenAny(game.Ready, Task.Delay(TimeSpan.FromSeconds(30))).Wait();

        if (!game.Ready.IsCompleted)
        {
            if (GameLoopFault is not null)
                throw new InvalidOperationException("FirstPersonHostGame crashed during startup.", GameLoopFault);

            throw new TimeoutException("FirstPersonHostGame did not finish LoadContent within 30 seconds.");
        }

        // Mutating InputManager.Sources is only safe from the game loop's
        // own thread (InputManager.Update, called every frame from that
        // thread, enumerates it) — so this goes through the same
        // microthread-marshaling RunScenario every other scenario uses,
        // not a direct call from this (the test) thread.
        (Keyboard, Mouse) = RunScenario(_ =>
        {
            // Order matters: the source must be registered with
            // InputManager.Sources BEFORE adding devices to it, so the
            // Devices.CollectionChanged hookup InputManager wires up when
            // a source is added is already in place to observe the
            // keyboard/mouse being added (matching Stride.Input.Tests's
            // own HeadlessInput helper) — adding devices first means
            // InputManager never learns about them at all.
            var source = new InputSourceSimulated();
            game.Input.Sources.Add(source);
            var keyboard = source.AddKeyboard();
            var mouse = source.AddMouse();
            return Task.FromResult((keyboard, mouse));
        }).GetAwaiter().GetResult();
    }

    public SceneSystem SceneSystem => game.SceneSystem;

    public Gameplay.PVXGameplaySession Session => game.Session;

    public KeyboardSimulated Keyboard { get; }

    public MouseSimulated Mouse { get; }

    /// <summary>
    /// Runs <paramref name="scenario"/> on the game's own microthread (via
    /// <see cref="ScriptSystem.AddTask"/>) and marshals its result (or
    /// exception) back to the caller.
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

        var finished = await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(30)));

        if (finished != completion.Task)
        {
            throw GameLoopFault is not null
                ? new InvalidOperationException("FirstPersonHostGame crashed while running a scenario.", GameLoopFault)
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
