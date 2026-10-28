using PVX.Gameplay;
using PVX.Interaction;
using Xunit;

namespace PVX.Interaction.Tests;

public class GameplayIntegrationTests
{
    private static (PVXGameplaySession Session, PVXInteractionService Service, FakeInteractionSource Source, FakeAction Action)
        CreateFocusedHold(TimeSpan holdDuration)
    {
        var (session, _) = TestSessions.CreateActive();
        var action = new FakeAction();
        var descriptor = new PVXInteractionDescriptor(
            new PVXInteractionActionId("hold"), action, priority: 10,
            mode: PVXInteractionMode.Hold, holdDuration: holdDuration);
        var target = new FakeInteractable { ActionsFactory = _ => [descriptor] };
        var provider = new FakeTargetProvider();
        provider.Candidates.Add(new PVXInteractionCandidate(target));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);
        return (session, service, source, action);
    }

    [Fact]
    public void MissingInteractCapability_CannotBegin()
    {
        var (session, service, source, _) = CreateFocusedHold(TimeSpan.FromSeconds(2));
        using var restriction = session.Control.AcquireRestriction(PVXControlCapabilities.Move);

        var result = service.BeginPrimaryInteraction(source);

        Assert.False(result);
        Assert.Null(service.ActiveInteraction);
    }

    [Fact]
    public void PauseDuringHold_CancelsImmediately()
    {
        var (session, service, source, action) = CreateFocusedHold(TimeSpan.FromSeconds(2));
        service.BeginPrimaryInteraction(source);
        service.Update(source, TimeSpan.FromSeconds(1.4)); // 70% progress

        var cancelled = false;
        service.InteractionCancelled += (_, _) => cancelled = true;

        session.Pause();

        Assert.True(cancelled);
        Assert.Null(service.ActiveInteraction);
        Assert.Equal(1, action.CancelCount);
        Assert.Equal(0, action.ExecuteCount);
    }

    [Fact]
    public void Resume_DoesNotAutoRestartInteraction()
    {
        var (session, service, source, action) = CreateFocusedHold(TimeSpan.FromSeconds(2));
        service.BeginPrimaryInteraction(source);
        service.Update(source, TimeSpan.FromSeconds(1.0));

        session.Pause();
        session.Resume();

        Assert.Null(service.ActiveInteraction);
        Assert.Equal(0, action.ExecuteCount);

        // A further Update must not revive the cancelled hold either.
        service.Update(source, TimeSpan.FromSeconds(5));
        Assert.Null(service.ActiveInteraction);
        Assert.Equal(0, action.ExecuteCount);
    }

    [Fact]
    public void FaultedSession_CannotBegin()
    {
        var (session, service, source, _) = CreateFocusedHold(TimeSpan.FromSeconds(2));
        session.GameplayPaused += (_, _) => throw new InvalidOperationException("boom");

        Assert.Throws<InvalidOperationException>(session.Pause);
        Assert.Equal(PVXGameplayState.Faulted, session.State);

        var result = service.BeginPrimaryInteraction(source);

        Assert.False(result);
    }

    [Fact]
    public void FaultDuringActiveInteraction_Cancels()
    {
        var (session, service, source, action) = CreateFocusedHold(TimeSpan.FromSeconds(2));
        service.BeginPrimaryInteraction(source);
        session.GameplayEnding += (_, _) => throw new InvalidOperationException("boom");

        Assert.Throws<InvalidOperationException>(session.End);
        Assert.Equal(PVXGameplayState.Faulted, session.State);

        Assert.Null(service.ActiveInteraction);
        Assert.Equal(1, action.CancelCount);
    }

    [Fact]
    public void NoPrimaryPlayer_CannotBegin()
    {
        var session = new PVXGameplaySession();
        session.Start();
        // Deliberately no provider registered: PrimaryPlayer stays null.

        var action = new FakeAction();
        var descriptor = new PVXInteractionDescriptor(new PVXInteractionActionId("toggle"), action);
        var target = new FakeInteractable { ActionsFactory = _ => [descriptor] };
        var provider = new FakeTargetProvider();
        provider.Candidates.Add(new PVXInteractionCandidate(target));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);

        var result = service.BeginPrimaryInteraction(source);

        Assert.False(result);
        Assert.Equal(0, action.BeginCount);
    }

    [Fact]
    public void InactiveSession_CannotBegin()
    {
        var session = new PVXGameplaySession(); // never Started
        var player = new FakePlayer();
        session.Player.RegisterProvider(new FakePlayerProvider { Player = player });

        var action = new FakeAction();
        var descriptor = new PVXInteractionDescriptor(new PVXInteractionActionId("toggle"), action);
        var target = new FakeInteractable { ActionsFactory = _ => [descriptor] };
        var provider = new FakeTargetProvider();
        provider.Candidates.Add(new PVXInteractionCandidate(target));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);

        var result = service.BeginPrimaryInteraction(source);

        Assert.False(result);
    }
}
