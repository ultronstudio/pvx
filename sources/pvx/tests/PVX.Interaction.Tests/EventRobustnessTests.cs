using PVX.Interaction;
using Xunit;

namespace PVX.Interaction.Tests;

public class EventRobustnessTests
{
    private static (PVXInteractionService Service, FakeInteractionSource Source, FakeAction Action) CreateFocusedHold(
        TimeSpan holdDuration)
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
        return (service, source, action);
    }

    [Fact]
    public void ThrowingCancelledSubscriber_DoesNotLeaveDanglingActiveInteraction()
    {
        var (service, source, _) = CreateFocusedHold(TimeSpan.FromSeconds(2));
        service.BeginPrimaryInteraction(source);
        service.InteractionCancelled += (_, _) => throw new InvalidOperationException("boom");

        Assert.Throws<InvalidOperationException>(() => service.CancelActiveInteraction());

        Assert.Null(service.ActiveInteraction);

        // The service must still be usable afterward: a second interaction
        // can begin normally instead of being blocked by stale state.
        var result = service.BeginPrimaryInteraction(source);
        Assert.True(result);
    }

    [Fact]
    public void ThrowingCompletedSubscriber_DoesNotLeaveDanglingActiveInteraction_AndExecutesOnlyOnce()
    {
        var (service, source, action) = CreateFocusedHold(TimeSpan.FromSeconds(1));
        service.InteractionCompleted += (_, _) => throw new InvalidOperationException("boom");
        service.BeginPrimaryInteraction(source);

        Assert.Throws<InvalidOperationException>(() => service.Update(source, TimeSpan.FromSeconds(2)));

        Assert.Null(service.ActiveInteraction);
        Assert.Equal(1, action.ExecuteCount);
    }

    [Fact]
    public void ThrowingCompletedSubscriber_SubsequentUpdates_DoNotReExecute()
    {
        var (service, source, action) = CreateFocusedHold(TimeSpan.FromSeconds(1));
        var shouldThrow = true;
        service.InteractionCompleted += (_, _) =>
        {
            if (shouldThrow) throw new InvalidOperationException("boom");
        };
        service.BeginPrimaryInteraction(source);

        Assert.Throws<InvalidOperationException>(() => service.Update(source, TimeSpan.FromSeconds(2)));
        shouldThrow = false;

        service.Update(source, TimeSpan.FromSeconds(2));
        service.Update(source, TimeSpan.FromSeconds(2));

        Assert.Equal(1, action.ExecuteCount);
    }

    [Fact]
    public void ThrowingStartedSubscriber_Hold_CleansUp()
    {
        var (service, source, action) = CreateFocusedHold(TimeSpan.FromSeconds(2));
        EventHandler<PVXInteractionEventArgs> throwingHandler = (_, _) => throw new InvalidOperationException("boom");
        service.InteractionStarted += throwingHandler;

        Assert.Throws<InvalidOperationException>(() => service.BeginPrimaryInteraction(source));

        // Begin() itself succeeded before the throwing subscriber ran, but
        // the service must not be left believing the interaction is still
        // active: nothing would ever resolve it (Update only advances a
        // live ActiveInteraction, and there isn't one anymore), so it is
        // cleaned up as if cancelled, with the action's own Cancel() called.
        Assert.Null(service.ActiveInteraction);
        Assert.Equal(1, action.BeginCount);
        Assert.Equal(0, action.ExecuteCount);
        Assert.Equal(1, action.CancelCount);

        // The service must still be usable afterward.
        service.InteractionStarted -= throwingHandler;
        var result = service.BeginPrimaryInteraction(source);
        Assert.True(result);
    }

    [Fact]
    public void ThrowingStartedSubscriber_Instant_CleansUp()
    {
        var (session, _) = TestSessions.CreateActive();
        var action = new FakeAction();
        var descriptor = new PVXInteractionDescriptor(new PVXInteractionActionId("toggle"), action, priority: 10);
        var target = new FakeInteractable { ActionsFactory = _ => [descriptor] };
        var provider = new FakeTargetProvider();
        provider.Candidates.Add(new PVXInteractionCandidate(target));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);
        EventHandler<PVXInteractionEventArgs> throwingHandler = (_, _) => throw new InvalidOperationException("boom");
        service.InteractionStarted += throwingHandler;

        Assert.Throws<InvalidOperationException>(() => service.BeginPrimaryInteraction(source));

        // Without this cleanup, Instant would be worse than Hold: nothing
        // in Update() ever advances/executes a non-Hold ActiveInteraction,
        // so a dangling one here would block every future interaction forever.
        Assert.Null(service.ActiveInteraction);
        Assert.Equal(1, action.BeginCount);
        Assert.Equal(0, action.ExecuteCount);
        Assert.Equal(1, action.CancelCount);

        service.InteractionStarted -= throwingHandler;
        var result = service.BeginPrimaryInteraction(source);
        Assert.True(result);
    }

    [Fact]
    public void ThrowingFocusChangedSubscriber_StateStaysConsistent()
    {
        var (session, _) = TestSessions.CreateActive();
        var provider = new FakeTargetProvider();
        var targetA = new FakeInteractable();
        provider.Candidates.Add(new PVXInteractionCandidate(targetA));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);

        service.FocusChanged += (_, _) => throw new InvalidOperationException("boom");

        var targetB = new FakeInteractable();
        provider.Candidates.Clear();
        provider.Candidates.Add(new PVXInteractionCandidate(targetB));

        Assert.Throws<InvalidOperationException>(() => service.Update(source, TimeSpan.Zero));

        // CurrentFocus was already updated to the new target before the
        // throwing subscriber ran, so the service reflects reality, not a
        // half-applied transition.
        Assert.Same(targetB, service.CurrentFocus!.Value.Target);
    }

    [Fact]
    public void ExecuteThrows_ClearsActiveInteraction_BeforeRethrowing()
    {
        var (service, source, action) = CreateFocusedHold(TimeSpan.FromSeconds(1));
        action.ThrowOnExecute = new InvalidOperationException("boom");
        service.BeginPrimaryInteraction(source);

        Assert.Throws<InvalidOperationException>(() => service.Update(source, TimeSpan.FromSeconds(2)));

        Assert.Null(service.ActiveInteraction);
    }
}
