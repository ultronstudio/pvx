using PVX.Gameplay;

namespace PVX.Gameplay.Tests;

internal sealed class FakePlayer : IPVXPlayer
{
}

internal sealed class FakePlayerProvider : IPVXPlayerProvider
{
    public IPVXPlayer? Player { get; set; }

    public IPVXPlayer? GetPrimaryPlayer() => Player;
}
