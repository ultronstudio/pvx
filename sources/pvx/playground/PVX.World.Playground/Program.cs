using PVX.World.Playground;
using Stride.Games;

// Default: a real window for manual testing (Mouse: look while holding
// right button. WASD: move. E: interact.).
//
// Pass --headless-smoke (or set PVX_PLAYGROUND_HEADLESS=1, kept for
// backward compatibility) to instead run headless, verify door/switch
// state transitions through their real public API, and auto-exit with a
// non-zero exit code on failure — purely for automated startup
// verification (CI, or this milestone's own build verification). The
// interactive experience is always the real target; this is a secondary,
// explicitly-opt-in mode.
var headless = args.Contains("--headless-smoke") || Environment.GetEnvironmentVariable("PVX_PLAYGROUND_HEADLESS") == "1";

using var game = new PVXWorldPlaygroundGame();

if (headless)
{
    game.AutoExitAfterFrames(180);
    game.RequestHeadlessSmokeVerification();
    game.Run(GameContextFactory.NewGameContext(AppContextType.Headless));
    return game.SmokeVerificationFailed ? 1 : 0;
}

game.Run(GameContextFactory.NewGameContextDesktop(1280, 720));
return 0;
