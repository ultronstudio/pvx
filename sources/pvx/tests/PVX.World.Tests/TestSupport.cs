using PVX.Gameplay;
using PVX.Interaction;

namespace PVX.World.Tests;

internal sealed class FakeInteractionSource : IPVXInteractionSource
{
}

internal sealed class FakeTargetProvider : IPVXInteractionTargetProvider
{
    public List<PVXInteractionCandidate> Candidates { get; } = new();

    public IReadOnlyList<PVXInteractionCandidate> GetCandidates(IPVXInteractionSource source) => Candidates;
}

internal sealed class FakePlayer : IPVXPlayer
{
}

internal sealed class FakePlayerProvider : IPVXPlayerProvider
{
    public IPVXPlayer? Player { get; set; }

    public IPVXPlayer? GetPrimaryPlayer() => Player;
}

internal static class TestSessions
{
    /// <summary>
    /// A started gameplay session with a primary player already
    /// registered and full capabilities in effect — matches
    /// <c>PVX.Interaction.Tests</c>'s own helper.
    /// </summary>
    public static (PVXGameplaySession Session, FakePlayer Player) CreateActive()
    {
        var session = new PVXGameplaySession();
        session.Start();

        var player = new FakePlayer();
        session.Player.RegisterProvider(new FakePlayerProvider { Player = player });

        return (session, player);
    }
}
