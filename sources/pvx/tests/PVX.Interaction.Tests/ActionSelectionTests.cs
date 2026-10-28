using PVX.Interaction;
using Xunit;

namespace PVX.Interaction.Tests;

public class ActionSelectionTests
{
    private static PVXInteractionService CreateFocusedService(
        FakeInteractable target,
        out FakeInteractionSource source)
    {
        var (session, _) = TestSessions.CreateActive();
        var provider = new FakeTargetProvider();
        provider.Candidates.Add(new PVXInteractionCandidate(target));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);
        return service;
    }

    [Fact]
    public void OneAvailableAction_BecomesPrimary()
    {
        var action = new FakeAction();
        var descriptor = new PVXInteractionDescriptor(new PVXInteractionActionId("inspect"), action, priority: 10);
        var target = new FakeInteractable { ActionsFactory = _ => [descriptor] };

        var service = CreateFocusedService(target, out _);

        Assert.Same(descriptor, service.PrimaryAction);
        Assert.Null(service.SecondaryAction);
    }

    [Fact]
    public void MultipleActions_HigherPriorityIsPrimary()
    {
        var open = new PVXInteractionDescriptor(new PVXInteractionActionId("open"), new FakeAction(), priority: 100);
        var inspect = new PVXInteractionDescriptor(new PVXInteractionActionId("inspect"), new FakeAction(), priority: 50);
        var target = new FakeInteractable { ActionsFactory = _ => [open, inspect] };

        var service = CreateFocusedService(target, out _);

        Assert.Same(open, service.PrimaryAction);
        Assert.Same(inspect, service.SecondaryAction);
    }

    [Fact]
    public void UnavailableAction_IsNeverPrimary()
    {
        var open = new PVXInteractionDescriptor(
            new PVXInteractionActionId("open"),
            new FakeAction(),
            priority: 100,
            availability: PVXInteractionAvailability.Unavailable("locked"));
        var inspect = new PVXInteractionDescriptor(new PVXInteractionActionId("inspect"), new FakeAction(), priority: 50);
        var target = new FakeInteractable { ActionsFactory = _ => [open, inspect] };

        var service = CreateFocusedService(target, out _);

        Assert.Same(inspect, service.PrimaryAction);
        Assert.Contains(open, service.Actions);
    }

    [Fact]
    public void AvailableActionsList_IncludesUnavailableDescriptors()
    {
        var open = new PVXInteractionDescriptor(
            new PVXInteractionActionId("open"),
            new FakeAction(),
            priority: 100,
            availability: PVXInteractionAvailability.Unavailable("locked"));
        var target = new FakeInteractable { ActionsFactory = _ => [open] };

        var service = CreateFocusedService(target, out _);

        Assert.Single(service.Actions);
        Assert.False(service.Actions[0].Availability.IsAvailable);
        Assert.Null(service.PrimaryAction);
    }

    [Fact]
    public void ActionAvailabilityChanges_RecomputesPrimaryOnNextUpdate()
    {
        var locked = true;
        var inspect = new PVXInteractionDescriptor(new PVXInteractionActionId("inspect"), new FakeAction(), priority: 50);

        var target = new FakeInteractable
        {
            ActionsFactory = _ =>
            [
                new PVXInteractionDescriptor(
                    new PVXInteractionActionId("open"),
                    new FakeAction(),
                    priority: 100,
                    availability: locked ? PVXInteractionAvailability.Unavailable("locked") : PVXInteractionAvailability.Available),
                inspect
            ]
        };

        var service = CreateFocusedService(target, out var source);
        Assert.Same(inspect, service.PrimaryAction);

        locked = false;
        service.Update(source, TimeSpan.Zero);

        Assert.Equal("open", service.PrimaryAction!.ActionId.Value);
    }
}
