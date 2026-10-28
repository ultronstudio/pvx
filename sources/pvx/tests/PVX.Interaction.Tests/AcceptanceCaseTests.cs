using PVX.Interaction;
using Xunit;

namespace PVX.Interaction.Tests;

/// <summary>
/// End-to-end scenario tests showing that door/switch/inspect/grab style
/// mechanics are all just generic actions over <see cref="IPVXInteractable"/> —
/// PVX.Interaction never special-cases any of them.
/// </summary>
public class AcceptanceCaseTests
{
    private sealed class DoorInteractable : IPVXInteractable
    {
        public bool IsOpen { get; private set; }

        public bool IsLocked { get; set; }

        public IReadOnlyList<PVXInteractionDescriptor> GetActions(PVXInteractionContext context)
        {
            var inspect = new PVXInteractionDescriptor(new PVXInteractionActionId("inspect"), new FakeAction { OnExecute = _ => { } }, priority: 50);

            if (IsOpen)
            {
                var close = new PVXInteractionDescriptor(
                    new PVXInteractionActionId("close"),
                    new FakeAction { OnExecute = _ => IsOpen = false },
                    priority: 100);
                return [close, inspect];
            }

            var openAvailability = IsLocked
                ? PVXInteractionAvailability.Unavailable("locked")
                : PVXInteractionAvailability.Available;

            var open = new PVXInteractionDescriptor(
                new PVXInteractionActionId("open"),
                new FakeAction { OnExecute = _ => IsOpen = true },
                priority: 100,
                availability: openAvailability);

            return [open, inspect];
        }
    }

    private static (PVXInteractionService Service, FakeInteractionSource Source, FakeTargetProvider Provider) CreateFocusedService(
        IPVXInteractable target)
    {
        var (session, _) = TestSessions.CreateActive();
        var provider = new FakeTargetProvider();
        provider.Candidates.Add(new PVXInteractionCandidate(target));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);
        return (service, source, provider);
    }

    [Fact]
    public void Door_Closed_PrimaryIsOpen()
    {
        var door = new DoorInteractable();
        var (service, _, _) = CreateFocusedService(door);

        Assert.Equal("open", service.PrimaryAction!.ActionId.Value);
    }

    [Fact]
    public void Door_OpenThenPrimaryBecomesClose()
    {
        var door = new DoorInteractable();
        var (service, source, _) = CreateFocusedService(door);

        service.BeginPrimaryInteraction(source);
        Assert.True(door.IsOpen);

        service.Update(source, TimeSpan.Zero);
        Assert.Equal("close", service.PrimaryAction!.ActionId.Value);
    }

    [Fact]
    public void LockedDoor_OpenUnavailable_InspectBecomesPrimary()
    {
        var door = new DoorInteractable { IsLocked = true };
        var (service, _, _) = CreateFocusedService(door);

        Assert.Equal("inspect", service.PrimaryAction!.ActionId.Value);
        Assert.Contains(service.Actions, a => a.ActionId.Value == "open" && !a.Availability.IsAvailable);
    }

    [Fact]
    public void LockedDoor_CannotBeOpened()
    {
        var door = new DoorInteractable { IsLocked = true };
        var (service, source, _) = CreateFocusedService(door);

        var openDescriptor = service.Actions.Single(a => a.ActionId.Value == "open");
        Assert.False(openDescriptor.Availability.IsAvailable);
        Assert.False(door.IsOpen);

        // Primary is "inspect" here (the only available one); executing it
        // must not open the locked door.
        service.BeginPrimaryInteraction(source);
        Assert.False(door.IsOpen);
    }

    [Fact]
    public void Switch_Toggle_IsInstant_AndExecutesOnlyOnceAcrossDoubleUpdate()
    {
        var toggled = 0;
        var action = new FakeAction { OnExecute = _ => toggled++ };
        var descriptor = new PVXInteractionDescriptor(new PVXInteractionActionId("toggle"), action, priority: 100);
        var switchTarget = new FakeInteractable { ActionsFactory = _ => [descriptor] };
        var (service, source, _) = CreateFocusedService(switchTarget);

        service.BeginPrimaryInteraction(source);
        service.Update(source, TimeSpan.Zero);
        service.Update(source, TimeSpan.Zero);

        Assert.Equal(1, toggled);
        Assert.Null(service.ActiveInteraction);
    }

    [Fact]
    public void Inspect_DelegatesToFakeInspectHandler()
    {
        var inspected = new List<string>();
        var action = new FakeAction { OnExecute = _ => inspected.Add("photo") };
        var descriptor = new PVXInteractionDescriptor(new PVXInteractionActionId("inspect"), action, priority: 50);
        var photo = new FakeInteractable { ActionsFactory = _ => [descriptor] };
        var (service, source, _) = CreateFocusedService(photo);

        var result = service.BeginPrimaryInteraction(source);

        Assert.True(result);
        Assert.Equal(["photo"], inspected);
    }

    [Fact]
    public void Grab_DelegatesToFakeManipulationHandler()
    {
        var grabbed = new List<object>();
        var action = new FakeAction { OnExecute = ctx => grabbed.Add(ctx.Target) };
        var descriptor = new PVXInteractionDescriptor(new PVXInteractionActionId("grab"), action, priority: 50);
        var box = new FakeInteractable { ActionsFactory = _ => [descriptor] };
        var (service, source, _) = CreateFocusedService(box);

        var result = service.BeginPrimaryInteraction(source);

        Assert.True(result);
        Assert.Same(box, Assert.Single(grabbed));
    }
}
