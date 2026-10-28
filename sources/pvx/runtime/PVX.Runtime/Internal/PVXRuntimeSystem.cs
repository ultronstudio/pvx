using Stride.Core;
using Stride.Games;

namespace PVX.Runtime.Internal;

/// <summary>
/// Bridges the inherited Stride game-system lifecycle into PVX.Runtime.
/// </summary>
internal sealed class PVXRuntimeSystem : GameSystemBase
{
    private readonly PVXRuntime runtime;

    public PVXRuntimeSystem(
        IServiceRegistry services,
        PVXRuntime runtime)
        : base(services)
    {
        this.runtime = runtime
            ?? throw new ArgumentNullException(nameof(runtime));

        Enabled = true;

        // For now PVX gameplay runs after most default zero-order systems.
        // We can introduce explicit PVX scheduling later.
        UpdateOrder = 1000;
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        runtime.Initialize();
    }

    public override void Update(GameTime gameTime)
    {
        var frameTime = new PVXFrameTime(
            gameTime.Total,
            gameTime.Elapsed,
            gameTime.WarpElapsed,
            gameTime.FrameCount);

        runtime.Update(in frameTime);
    }

    protected override void UnloadContent()
    {
        try
        {
            runtime.Shutdown();
        }
        finally
        {
            base.UnloadContent();
        }
    }
}
