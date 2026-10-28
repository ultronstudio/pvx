using PVX.Interaction;
using Xunit;

namespace PVX.Interaction.Tests;

/// <summary>
/// Covers <see cref="PVXInteractionService.Dispose"/>: a PVX runtime can be
/// stopped and re-initialized while reusing the same feature instances,
/// which constructs a brand new <see cref="PVXInteractionService"/> on
/// every re-initialize without ever touching the previous one unless
/// something explicitly detaches it from the still-alive
/// <c>PVXGameplaySession</c> it was built on.
/// </summary>
public class ServiceLifecycleTests
{
    [Fact]
    public void Dispose_DoesNotCallActionCancel_AndClearsLocalState()
    {
        var (session, _) = TestSessions.CreateActive();
        var action = new FakeAction();
        var descriptor = new PVXInteractionDescriptor(
            new PVXInteractionActionId("hold"), action, mode: PVXInteractionMode.Hold, holdDuration: TimeSpan.FromSeconds(2));
        var target = new FakeInteractable { ActionsFactory = _ => [descriptor] };
        var provider = new FakeTargetProvider();
        provider.Candidates.Add(new PVXInteractionCandidate(target));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);
        service.BeginPrimaryInteraction(source);

        service.Dispose();

        Assert.Equal(0, action.CancelCount);
        Assert.Null(service.ActiveInteraction);
        Assert.Null(service.CurrentFocus);
        Assert.Empty(service.Actions);
        Assert.Null(service.PrimaryAction);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var (session, _) = TestSessions.CreateActive();
        var service = new PVXInteractionService(session);

        service.Dispose();
        service.Dispose();
    }

    [Fact]
    public void Dispose_UnsubscribesFromSessionEvents_SoTheDisposedInstanceNoLongerReactsToPause()
    {
        // Simulates PVXInteractionFeature.Shutdown() on one runtime
        // Stopped -> Initialize cycle: PVXGameplaySession outlives the old
        // PVXInteractionService built on top of it.
        var (session, _) = TestSessions.CreateActive();
        var action = new FakeAction();
        var descriptor = new PVXInteractionDescriptor(
            new PVXInteractionActionId("hold"), action, mode: PVXInteractionMode.Hold, holdDuration: TimeSpan.FromSeconds(2));
        var target = new FakeInteractable { ActionsFactory = _ => [descriptor] };
        var provider = new FakeTargetProvider();
        provider.Candidates.Add(new PVXInteractionCandidate(target));
        var oldService = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        oldService.Update(source, TimeSpan.Zero);

        oldService.Dispose();

        // Reconstruct active state directly on the disposed instance,
        // independent of what Dispose already cleared, so that if its
        // event subscriptions were still live, the Pause below would
        // observably cancel it.
        oldService.Update(source, TimeSpan.Zero);
        oldService.BeginPrimaryInteraction(source);
        Assert.NotNull(oldService.ActiveInteraction);

        session.Pause();

        // If Dispose had not unsubscribed OnGameplayBlockingTransition /
        // OnCapabilitiesChanged from the still-alive session, this Pause
        // would have cancelled oldService's interaction and invoked
        // action.Cancel() on behalf of an instance the host already
        // considers gone.
        Assert.NotNull(oldService.ActiveInteraction);
        Assert.Equal(0, action.CancelCount);
    }

    [Fact]
    public void TwoServicesOnSameSession_OnlyTheNonDisposedOneReactsToPause()
    {
        var (session, _) = TestSessions.CreateActive();

        var actionOld = new FakeAction();
        var oldTarget = new FakeInteractable
        {
            ActionsFactory = _ =>
            [
                new PVXInteractionDescriptor(new PVXInteractionActionId("hold"), actionOld, mode: PVXInteractionMode.Hold, holdDuration: TimeSpan.FromSeconds(2))
            ]
        };
        var oldProvider = new FakeTargetProvider();
        oldProvider.Candidates.Add(new PVXInteractionCandidate(oldTarget));
        var oldService = new PVXInteractionService(session) { TargetProvider = oldProvider };
        var source = new FakeInteractionSource();
        oldService.Update(source, TimeSpan.Zero);

        // "Restart": the old service is disposed, like PVXInteractionFeature.Shutdown()
        // would, and a new one is constructed on the same still-alive session,
        // like the next Initialize() would.
        oldService.Dispose();

        var actionNew = new FakeAction();
        var newTarget = new FakeInteractable
        {
            ActionsFactory = _ =>
            [
                new PVXInteractionDescriptor(new PVXInteractionActionId("hold"), actionNew, mode: PVXInteractionMode.Hold, holdDuration: TimeSpan.FromSeconds(2))
            ]
        };
        var newProvider = new FakeTargetProvider();
        newProvider.Candidates.Add(new PVXInteractionCandidate(newTarget));
        var newService = new PVXInteractionService(session) { TargetProvider = newProvider };
        newService.Update(source, TimeSpan.Zero);
        newService.BeginPrimaryInteraction(source);
        Assert.NotNull(newService.ActiveInteraction);

        session.Pause();

        Assert.Null(newService.ActiveInteraction);
        Assert.Equal(1, actionNew.CancelCount);
    }
}
