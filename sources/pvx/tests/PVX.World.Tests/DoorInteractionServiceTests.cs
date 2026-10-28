using PVX.Interaction;
using Stride.Engine;
using Xunit;

namespace PVX.World.Tests;

/// <summary>
/// Exercises <see cref="PVXDoorComponent"/> through a real
/// <see cref="PVXInteractionService"/> (not direct <c>door.Open()</c>
/// calls), proving the generic interaction layer and the door's own
/// state machine agree on what "Open"/"Close" mean.
/// </summary>
public class DoorInteractionServiceTests
{
    [Fact]
    public void FocusDoor_Open_ThenFocusAgain_PrimaryActionBecomesClose()
    {
        var (session, _) = TestSessions.CreateActive();
        var entity = new Entity("Door");
        var door = new PVXDoorComponent { TransitionDuration = 0f }; // instantaneous, for a single Update tick
        entity.Add(door);

        var provider = new FakeTargetProvider();
        provider.Candidates.Add(new PVXInteractionCandidate(door));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();

        service.Update(source, TimeSpan.Zero);
        Assert.Equal(PVXDoorComponent.OpenActionId, service.PrimaryAction!.ActionId);

        var began = service.BeginPrimaryInteraction(source);
        Assert.True(began);

        // Instant transition (duration 0) completes synchronously inside
        // Advance(), which the processor would call; here we call it
        // directly since there is no running scene/processor.
        door.Advance(0f);

        service.Update(source, TimeSpan.Zero);

        Assert.Equal(PVXDoorState.Open, door.State);
        Assert.Equal(PVXDoorComponent.CloseActionId, service.PrimaryAction!.ActionId);
    }

    [Fact]
    public void BeginPrimaryInteraction_WhileOpening_IsUnavailable_DoesNotStartSecondTransition()
    {
        var (session, _) = TestSessions.CreateActive();
        var entity = new Entity("Door");
        var door = new PVXDoorComponent { TransitionDuration = 1f };
        entity.Add(door);

        var provider = new FakeTargetProvider();
        provider.Candidates.Add(new PVXInteractionCandidate(door));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();

        service.Update(source, TimeSpan.Zero);
        service.BeginPrimaryInteraction(source); // Closed -> Opening

        service.Update(source, TimeSpan.Zero);
        Assert.Null(service.PrimaryAction); // only descriptor is "open", unavailable ("busy")

        var secondBegin = service.BeginPrimaryInteraction(source);

        Assert.False(secondBegin);
        Assert.Equal(PVXDoorState.Opening, door.State);
    }
}
