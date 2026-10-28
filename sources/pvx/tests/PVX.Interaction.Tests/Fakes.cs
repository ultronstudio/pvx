using PVX.Gameplay;
using PVX.Interaction;

namespace PVX.Interaction.Tests;

internal sealed class FakeInteractionSource : IPVXInteractionSource
{
}

internal sealed class FakePlayer : IPVXPlayer
{
}

internal sealed class FakePlayerProvider : IPVXPlayerProvider
{
    public IPVXPlayer? Player { get; set; }

    public IPVXPlayer? GetPrimaryPlayer() => Player;
}

internal sealed class FakeAction : IPVXInteractionAction
{
    public int CanExecuteCount { get; private set; }

    public int BeginCount { get; private set; }

    public int ExecuteCount { get; private set; }

    public int CancelCount { get; private set; }

    public bool CanExecuteResult { get; set; } = true;

    public bool ExecuteResult { get; set; } = true;

    public Action<PVXInteractionContext>? OnExecute { get; set; }

    public Exception? ThrowOnExecute { get; set; }

    public bool CanExecute(PVXInteractionContext context)
    {
        CanExecuteCount++;
        return CanExecuteResult;
    }

    public void Begin(PVXInteractionContext context)
    {
        BeginCount++;
    }

    public bool Execute(PVXInteractionContext context)
    {
        ExecuteCount++;

        if (ThrowOnExecute is { } exception)
            throw exception;

        OnExecute?.Invoke(context);
        return ExecuteResult;
    }

    public void Cancel(PVXInteractionContext context)
    {
        CancelCount++;
    }
}

internal sealed class FakeInteractable : IPVXInteractable
{
    public Func<PVXInteractionContext, IReadOnlyList<PVXInteractionDescriptor>> ActionsFactory { get; set; } =
        static _ => Array.Empty<PVXInteractionDescriptor>();

    public IReadOnlyList<PVXInteractionDescriptor> GetActions(PVXInteractionContext context) => ActionsFactory(context);
}

internal sealed class FakeTargetProvider : IPVXInteractionTargetProvider
{
    public List<PVXInteractionCandidate> Candidates { get; } = new();

    public IReadOnlyList<PVXInteractionCandidate> GetCandidates(IPVXInteractionSource source) => Candidates;
}

/// <summary>
/// A traversal handler stand-in for the ladder acceptance case: records
/// hand-off requests without implementing any actual movement, matching
/// what a future PVX.Characters.Traversal module would receive.
/// </summary>
internal sealed class FakeTraversalHandler
{
    public List<string> EnteredLadders { get; } = new();

    public void EnterLadder(string direction) => EnteredLadders.Add(direction);
}

internal static class TestSessions
{
    /// <summary>
    /// A started gameplay session with a primary player already registered
    /// and full capabilities (including Interact) in effect — the common
    /// precondition for interaction tests that don't specifically exercise
    /// gameplay-blocking scenarios.
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
