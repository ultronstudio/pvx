using PVX.Interaction;
using Xunit;

namespace PVX.Interaction.Tests;

public class InstantInteractionTests
{
    private static (PVXInteractionService Service, FakeInteractionSource Source, FakeAction Action) CreateFocusedInstant(
        PVXInteractionAvailability? availability = null)
    {
        var (session, _) = TestSessions.CreateActive();
        var action = new FakeAction();
        var descriptor = new PVXInteractionDescriptor(
            new PVXInteractionActionId("toggle"), action, priority: 10, availability: availability);
        var target = new FakeInteractable { ActionsFactory = _ => [descriptor] };
        var provider = new FakeTargetProvider();
        provider.Candidates.Add(new PVXInteractionCandidate(target));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);
        return (service, source, action);
    }

    [Fact]
    public void BeginPrimary_Succeeds_RunsBeginExecuteComplete()
    {
        var (service, source, action) = CreateFocusedInstant();

        var started = false;
        var completed = false;
        service.InteractionStarted += (_, _) => started = true;
        service.InteractionCompleted += (_, _) => completed = true;

        var result = service.BeginPrimaryInteraction(source);

        Assert.True(result);
        Assert.True(started);
        Assert.True(completed);
        Assert.Equal(1, action.BeginCount);
        Assert.Equal(1, action.ExecuteCount);
        Assert.Null(service.ActiveInteraction);
    }

    [Fact]
    public void BeginPrimary_ExecutesOnlyOnce()
    {
        var (service, source, action) = CreateFocusedInstant();

        service.BeginPrimaryInteraction(source);
        service.Update(source, TimeSpan.Zero);
        service.Update(source, TimeSpan.Zero);

        Assert.Equal(1, action.ExecuteCount);
    }

    [Fact]
    public void BeginPrimary_WhenExecuteFails_RaisesFailed_NotCompleted()
    {
        var (service, source, action) = CreateFocusedInstant();
        action.ExecuteResult = false;

        var completed = false;
        var failedReason = (string?)null;
        service.InteractionCompleted += (_, _) => completed = true;
        service.InteractionFailed += (_, e) => failedReason = e.Reason;

        var result = service.BeginPrimaryInteraction(source);

        Assert.True(result);
        Assert.False(completed);
        Assert.NotNull(failedReason);
        Assert.Null(service.ActiveInteraction);
    }

    [Fact]
    public void BeginPrimary_WhenUnavailable_ReturnsFalse_AndDoesNotCallAction()
    {
        var (service, source, action) = CreateFocusedInstant(PVXInteractionAvailability.Unavailable("locked"));

        var result = service.BeginPrimaryInteraction(source);

        Assert.False(result);
        Assert.Equal(0, action.BeginCount);
    }

    [Fact]
    public void BeginPrimary_WhenCanExecuteFalse_ReturnsFalse_AndDoesNotBegin()
    {
        var (service, source, action) = CreateFocusedInstant();
        action.CanExecuteResult = false;

        var result = service.BeginPrimaryInteraction(source);

        Assert.False(result);
        Assert.Equal(0, action.BeginCount);
        Assert.Null(service.ActiveInteraction);
    }

    [Fact]
    public void BeginPrimary_WhenNoFocus_ReturnsFalse()
    {
        var (session, _) = TestSessions.CreateActive();
        var service = new PVXInteractionService(session) { TargetProvider = new FakeTargetProvider() };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);

        Assert.False(service.BeginPrimaryInteraction(source));
    }
}
