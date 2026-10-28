using PVX.Gameplay;
using Xunit;

namespace PVX.Gameplay.Tests;

public class SessionLifecycleTests
{
    [Fact]
    public void InitialState_IsInactive()
    {
        var session = new PVXGameplaySession();

        Assert.Equal(PVXGameplayState.Inactive, session.State);
    }

    [Fact]
    public void InitialState_ControlIsFullySuppressed()
    {
        var session = new PVXGameplaySession();

        Assert.Equal(PVXControlCapabilities.None, session.Control.EffectiveCapabilities);
    }

    [Fact]
    public void Start_FromInactive_TransitionsToActive_AndRaisesGameplayStarted()
    {
        var session = new PVXGameplaySession();
        var raised = false;
        session.GameplayStarted += (_, _) => raised = true;

        session.Start();

        Assert.Equal(PVXGameplayState.Active, session.State);
        Assert.True(raised);
        Assert.Equal(PVXControlCapabilities.All, session.Control.EffectiveCapabilities);
    }

    [Fact]
    public void Start_WhenAlreadyActive_Throws()
    {
        var session = new PVXGameplaySession();
        session.Start();

        Assert.Throws<InvalidOperationException>(session.Start);
        Assert.Equal(PVXGameplayState.Active, session.State);
    }

    [Fact]
    public void Pause_FromActive_TransitionsToPaused_AndRaisesGameplayPaused()
    {
        var session = new PVXGameplaySession();
        session.Start();
        var raised = false;
        session.GameplayPaused += (_, _) => raised = true;

        session.Pause();

        Assert.Equal(PVXGameplayState.Paused, session.State);
        Assert.True(raised);
    }

    [Fact]
    public void Pause_WhenInactive_Throws()
    {
        var session = new PVXGameplaySession();

        Assert.Throws<InvalidOperationException>(session.Pause);
        Assert.Equal(PVXGameplayState.Inactive, session.State);
    }

    [Fact]
    public void Resume_FromPaused_TransitionsToActive_AndRaisesGameplayResumed()
    {
        var session = new PVXGameplaySession();
        session.Start();
        session.Pause();
        var raised = false;
        session.GameplayResumed += (_, _) => raised = true;

        session.Resume();

        Assert.Equal(PVXGameplayState.Active, session.State);
        Assert.True(raised);
    }

    [Fact]
    public void Resume_WhenActive_Throws()
    {
        var session = new PVXGameplaySession();
        session.Start();

        Assert.Throws<InvalidOperationException>(session.Resume);
        Assert.Equal(PVXGameplayState.Active, session.State);
    }

    [Fact]
    public void End_FromActive_TransitionsToEnded_AndRaisesEndingThenEnded()
    {
        var session = new PVXGameplaySession();
        session.Start();
        var order = new List<string>();
        session.GameplayEnding += (_, _) => order.Add("Ending");
        session.GameplayEnded += (_, _) => order.Add("Ended");

        session.End();

        Assert.Equal(PVXGameplayState.Ended, session.State);
        Assert.Equal(["Ending", "Ended"], order);
    }

    [Fact]
    public void End_FromPaused_Succeeds()
    {
        var session = new PVXGameplaySession();
        session.Start();
        session.Pause();

        session.End();

        Assert.Equal(PVXGameplayState.Ended, session.State);
    }

    [Fact]
    public void End_BeforeStart_Throws()
    {
        var session = new PVXGameplaySession();

        Assert.Throws<InvalidOperationException>(session.End);
        Assert.Equal(PVXGameplayState.Inactive, session.State);
    }

    [Fact]
    public void End_DisablesControl_AndStaysDisabled()
    {
        var session = new PVXGameplaySession();
        session.Start();
        using var restriction = session.Control.AcquireRestriction(PVXControlCapabilities.Look);

        session.End();

        Assert.Equal(PVXControlCapabilities.None, session.Control.EffectiveCapabilities);
    }

    [Fact]
    public void Start_WhenCapabilitiesChangedSubscriberThrows_TransitionsToFaulted_AndRethrows()
    {
        var session = new PVXGameplaySession();
        session.Control.CapabilitiesChanged += (_, _) => throw new InvalidOperationException("boom");

        Assert.Throws<InvalidOperationException>(session.Start);
        Assert.Equal(PVXGameplayState.Faulted, session.State);
    }

    [Fact]
    public void Operations_WhenFaulted_Throw()
    {
        var session = new PVXGameplaySession();
        session.Control.CapabilitiesChanged += (_, _) => throw new InvalidOperationException("boom");

        Assert.Throws<InvalidOperationException>(session.Start);
        Assert.Equal(PVXGameplayState.Faulted, session.State);

        Assert.Throws<InvalidOperationException>(session.Start);
        Assert.Throws<InvalidOperationException>(session.Pause);
        Assert.Throws<InvalidOperationException>(session.Resume);
        Assert.Throws<InvalidOperationException>(session.End);
    }

    [Fact]
    public void Start_WhenGameplayStartedSubscriberThrows_TransitionsToFaulted()
    {
        var session = new PVXGameplaySession();
        session.GameplayStarted += (_, _) => throw new InvalidOperationException("boom");

        Assert.Throws<InvalidOperationException>(session.Start);
        Assert.Equal(PVXGameplayState.Faulted, session.State);
    }

    [Fact]
    public void Pause_WhenGameplayPausedSubscriberThrows_TransitionsToFaulted()
    {
        var session = new PVXGameplaySession();
        session.Start();
        session.GameplayPaused += (_, _) => throw new InvalidOperationException("boom");

        Assert.Throws<InvalidOperationException>(session.Pause);
        Assert.Equal(PVXGameplayState.Faulted, session.State);
    }

    [Fact]
    public void Resume_WhenGameplayResumedSubscriberThrows_TransitionsToFaulted()
    {
        var session = new PVXGameplaySession();
        session.Start();
        session.Pause();
        session.GameplayResumed += (_, _) => throw new InvalidOperationException("boom");

        Assert.Throws<InvalidOperationException>(session.Resume);
        Assert.Equal(PVXGameplayState.Faulted, session.State);
    }

    [Fact]
    public void End_WhenGameplayEndingSubscriberThrows_TransitionsToFaulted()
    {
        var session = new PVXGameplaySession();
        session.Start();
        session.GameplayEnding += (_, _) => throw new InvalidOperationException("boom");

        Assert.Throws<InvalidOperationException>(session.End);
        Assert.Equal(PVXGameplayState.Faulted, session.State);
    }

    [Fact]
    public void End_WhenGameplayEndedSubscriberThrows_TransitionsToFaulted()
    {
        var session = new PVXGameplaySession();
        session.Start();
        session.GameplayEnded += (_, _) => throw new InvalidOperationException("boom");

        Assert.Throws<InvalidOperationException>(session.End);
        Assert.Equal(PVXGameplayState.Faulted, session.State);
    }
}
