using PVX.World.Playground;
using Stride.Games;

// Default: a real window for manual testing (Mouse: look while holding
// right button. WASD: move. E: interact.).
//
// Set PVX_PLAYGROUND_HEADLESS=1 to instead run headless and auto-exit
// after a fixed number of frames, purely for automated startup
// verification (CI, or this milestone's own build verification) — the
// interactive experience is always the real target, this is a secondary,
// explicitly-opt-in mode.
using var game = new PVXWorldPlaygroundGame();

if (Environment.GetEnvironmentVariable("PVX_PLAYGROUND_HEADLESS") == "1")
{
    game.AutoExitAfterFrames(180);
    game.Run(GameContextFactory.NewGameContext(AppContextType.Headless));
}
else
{
    game.Run(GameContextFactory.NewGameContextDesktop(1280, 720));
}
