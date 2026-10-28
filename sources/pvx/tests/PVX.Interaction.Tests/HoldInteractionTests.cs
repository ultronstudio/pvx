using PVX.Interaction;
using Xunit;

namespace PVX.Interaction.Tests;

public class HoldInteractionTests
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
    public void Begin_StartsActiveInteraction_WithZeroProgress()
    {
        var (service, source, action) = CreateFocusedHold(TimeSpan.FromSeconds(2));

        var result = service.BeginPrimaryInteraction(source);

        Assert.True(result);
        Assert.NotNull(service.ActiveInteraction);
        Assert.Equal(0.0, service.ActiveInteraction!.Progress);
        Assert.Equal(1, action.BeginCount);
        Assert.Equal(0, action.ExecuteCount);
    }

    [Fact]
    public void Progress_IncrementsDeterministically()
    {
        var (service, source, _) = CreateFocusedHold(TimeSpan.FromSeconds(2));
        service.BeginPrimaryInteraction(source);

        service.Update(source, TimeSpan.FromSeconds(0.5));
        Assert.Equal(0.25, service.ActiveInteraction!.Progress, precision: 3);

        service.Update(source, TimeSpan.FromSeconds(0.5));
        Assert.Equal(0.5, service.ActiveInteraction!.Progress, precision: 3);
    }

    [Fact]
    public void ReleaseBeforeCompletion_Cancels_AndResetsActiveInteraction()
    {
        var (service, source, action) = CreateFocusedHold(TimeSpan.FromSeconds(2));
        service.BeginPrimaryInteraction(source);
        service.Update(source, TimeSpan.FromSeconds(0.5));

        var cancelled = false;
        service.InteractionCancelled += (_, _) => cancelled = true;

        var result = service.CancelActiveInteraction();

        Assert.True(result);
        Assert.True(cancelled);
        Assert.Null(service.ActiveInteraction);
        Assert.Equal(1, action.CancelCount);
        Assert.Equal(0, action.ExecuteCount);
    }

    [Fact]
    public void CompletionAtDurationThreshold_RaisesCompleted()
    {
        var (service, source, action) = CreateFocusedHold(TimeSpan.FromSeconds(2));
        service.BeginPrimaryInteraction(source);

        var completed = false;
        service.InteractionCompleted += (_, _) => completed = true;

        service.Update(source, TimeSpan.FromSeconds(1.0));
        service.Update(source, TimeSpan.FromSeconds(1.0));

        Assert.True(completed);
        Assert.Equal(1, action.ExecuteCount);
        Assert.Null(service.ActiveInteraction);
    }

    [Fact]
    public void DoesNotCompleteTwice()
    {
        var (service, source, action) = CreateFocusedHold(TimeSpan.FromSeconds(1));
        service.BeginPrimaryInteraction(source);

        service.Update(source, TimeSpan.FromSeconds(2));
        service.Update(source, TimeSpan.FromSeconds(2));

        Assert.Equal(1, action.ExecuteCount);
    }

    [Fact]
    public void CancelActiveInteraction_WhenNothingActive_IsNoOp()
    {
        var (service, _, _) = CreateFocusedHold(TimeSpan.FromSeconds(1));

        var result = service.CancelActiveInteraction();

        Assert.False(result);
    }

    [Fact]
    public void CancelActiveInteraction_AfterCompletion_IsNoOp_NotDestructive()
    {
        var (service, source, action) = CreateFocusedHold(TimeSpan.FromSeconds(1));
        service.BeginPrimaryInteraction(source);
        service.Update(source, TimeSpan.FromSeconds(2));

        var result = service.CancelActiveInteraction();

        Assert.False(result);
        Assert.Equal(0, action.CancelCount);
    }

    [Fact]
    public void BeginPrimary_WhenAlreadyActive_ReturnsFalse()
    {
        var (service, source, _) = CreateFocusedHold(TimeSpan.FromSeconds(2));
        service.BeginPrimaryInteraction(source);

        var result = service.BeginPrimaryInteraction(source);

        Assert.False(result);
    }
}
