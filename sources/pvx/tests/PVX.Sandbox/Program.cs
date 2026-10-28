using PVX.Sandbox;
using Stride.Games;

// Headless context: no window is created, matching how this repo runs
// automated Stride games (see Stride.Graphics.Regression.GameTester), and
// keeping the sandbox deterministic across repeated runs.
using var game = new SandboxGame();
game.Run(GameContextFactory.NewGameContext(AppContextType.Headless));
