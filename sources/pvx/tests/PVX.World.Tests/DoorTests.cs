using PVX.World;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace PVX.World.Tests;

public class DoorTests
{
    private static (Entity Entity, PVXDoorComponent Door) CreateDoor(Quaternion? initialRotation = null)
    {
        var entity = new Entity("Door");
        if (initialRotation is { } rotation)
        {
            entity.Transform.Rotation = rotation;
        }

        var door = new PVXDoorComponent();
        entity.Add(door);
        return (entity, door);
    }

    [Fact]
    public void Door_DefaultState_IsClosed()
    {
        var (_, door) = CreateDoor();

        Assert.Equal(PVXDoorState.Closed, door.State);
    }

    [Fact]
    public void Door_Closed_OffersOpen()
    {
        var (_, door) = CreateDoor();

        var descriptor = Assert.Single(door.GetActions(default));
        Assert.Equal(PVXDoorComponent.OpenActionId, descriptor.ActionId);
        Assert.True(descriptor.Availability.IsAvailable);
    }

    [Fact]
    public void Door_Open_OffersClose()
    {
        var (_, door) = CreateDoor();
        door.TryOpen();
        door.Advance(10f); // large delta: finish instantly regardless of duration

        var descriptor = Assert.Single(door.GetActions(default));
        Assert.Equal(PVXDoorComponent.CloseActionId, descriptor.ActionId);
        Assert.True(descriptor.Availability.IsAvailable);
    }

    [Fact]
    public void Door_Open_StartsTransition()
    {
        var (_, door) = CreateDoor();

        var result = door.TryOpen();

        Assert.True(result);
        Assert.Equal(PVXDoorState.Opening, door.State);
    }

    [Fact]
    public void Door_OpenTransition_ReachesOpen()
    {
        var (_, door) = CreateDoor();
        door.TransitionDuration = 1f;
        door.TryOpen();

        door.Advance(1f);

        Assert.Equal(PVXDoorState.Open, door.State);
    }

    [Fact]
    public void Door_CloseTransition_ReachesClosed()
    {
        var (_, door) = CreateDoor();
        door.TransitionDuration = 1f;
        door.TryOpen();
        door.Advance(1f);

        door.TryClose();
        door.Advance(1f);

        Assert.Equal(PVXDoorState.Closed, door.State);
    }

    [Fact]
    public void Door_Transition_IsTimeBased()
    {
        var (entity1, door1) = CreateDoor();
        door1.TransitionDuration = 1f;
        door1.TryOpen();
        door1.Advance(0.5f);
        door1.Advance(0.5f);

        var (entity2, door2) = CreateDoor();
        door2.TransitionDuration = 1f;
        door2.TryOpen();
        door2.Advance(1.0f);

        Assert.Equal(PVXDoorState.Open, door1.State);
        Assert.Equal(PVXDoorState.Open, door2.State);
        AssertApproximatelyEqual(entity1.Transform.Rotation, entity2.Transform.Rotation);
    }

    [Fact]
    public void Door_Transition_DoesNotModifyPosition()
    {
        var (entity, door) = CreateDoor();
        entity.Transform.Position = new Vector3(1, 2, 3);
        door.TransitionDuration = 1f;

        door.TryOpen();
        door.Advance(0.5f);

        Assert.Equal(new Vector3(1, 2, 3), entity.Transform.Position);
    }

    [Fact]
    public void Door_Transition_DoesNotModifyScale()
    {
        var (entity, door) = CreateDoor();
        entity.Transform.Scale = new Vector3(2, 2, 2);
        door.TransitionDuration = 1f;

        door.TryOpen();
        door.Advance(0.5f);

        Assert.Equal(new Vector3(2, 2, 2), entity.Transform.Scale);
    }

    [Fact]
    public void Door_NonIdentityInitialRotation_IsPreservedRelatively()
    {
        var initial = Quaternion.RotationAxis(Vector3.UnitY, MathUtil.DegreesToRadians(37f));
        var (entity, door) = CreateDoor(initial);
        door.OpenAngleDegrees = 90f;
        door.TransitionDuration = 1f;

        door.TryOpen();
        door.Advance(1f);

        var expectedOpen = initial * Quaternion.RotationAxis(Vector3.UnitY, MathUtil.DegreesToRadians(90f));
        AssertApproximatelyEqual(expectedOpen, entity.Transform.Rotation);

        door.TryClose();
        door.Advance(1f);
        AssertApproximatelyEqual(initial, entity.Transform.Rotation);
    }

    [Fact]
    public void Door_Opening_IsBusy()
    {
        var (_, door) = CreateDoor();
        door.TransitionDuration = 1f;
        door.TryOpen();

        var descriptor = Assert.Single(door.GetActions(default));
        Assert.False(descriptor.Availability.IsAvailable);
        Assert.Equal("busy", descriptor.Availability.Reason);
    }

    [Fact]
    public void Door_Closing_IsBusy()
    {
        var (_, door) = CreateDoor();
        door.TransitionDuration = 1f;
        door.TryOpen();
        door.Advance(1f);
        door.TryClose();

        var descriptor = Assert.Single(door.GetActions(default));
        Assert.False(descriptor.Availability.IsAvailable);
        Assert.Equal("busy", descriptor.Availability.Reason);
    }

    [Fact]
    public void Door_OpenWhileOpening_DoesNotStartSecondTransition()
    {
        var (_, door) = CreateDoor();
        door.TransitionDuration = 1f;
        door.TryOpen();

        var result = door.TryOpen();

        Assert.False(result);
        Assert.Equal(PVXDoorState.Opening, door.State);
    }

    [Fact]
    public void Door_CloseWhileClosing_DoesNotStartSecondTransition()
    {
        var (_, door) = CreateDoor();
        door.TransitionDuration = 1f;
        door.TryOpen();
        door.Advance(1f);
        door.TryClose();

        var result = door.TryClose();

        Assert.False(result);
        Assert.Equal(PVXDoorState.Closing, door.State);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void Door_InvalidDuration_TransitionsImmediately(float duration)
    {
        var (_, door) = CreateDoor();
        door.TransitionDuration = duration;

        door.TryOpen();
        door.Advance(0f);

        Assert.Equal(PVXDoorState.Open, door.State);
    }

    [Fact]
    public void Door_LargeDelta_DoesNotOvershoot()
    {
        var initial = Quaternion.RotationAxis(Vector3.UnitY, MathUtil.DegreesToRadians(10f));
        var (entity, door) = CreateDoor(initial);
        door.TransitionDuration = 0.5f;
        door.TryOpen();

        door.Advance(100f);

        Assert.Equal(PVXDoorState.Open, door.State);
        var expectedOpen = initial * Quaternion.RotationAxis(Vector3.UnitY, MathUtil.DegreesToRadians(90f));
        AssertApproximatelyEqual(expectedOpen, entity.Transform.Rotation);
    }

    [Fact]
    public void Door_StateChanged_RaisedOnlyForActualStateChanges()
    {
        var (_, door) = CreateDoor();
        door.TransitionDuration = 1f;
        var transitions = new List<(PVXDoorState Previous, PVXDoorState Current)>();
        door.StateChanged += (_, e) => transitions.Add((e.Previous, e.Current));

        door.TryOpen();
        door.Advance(1f);
        var redundant = door.TryOpen(); // already Open: must not raise again

        Assert.False(redundant);
        Assert.Equal(
            [(PVXDoorState.Closed, PVXDoorState.Opening), (PVXDoorState.Opening, PVXDoorState.Open)],
            transitions);
    }

    [Fact]
    public void Door_StartsOpen_InitialStateIsOpen_WithoutRaisingStateChanged()
    {
        var entity = new Entity("Door");
        var door = new PVXDoorComponent { StartsOpen = true };
        var raised = false;
        door.StateChanged += (_, _) => raised = true;

        entity.Add(door);

        Assert.Equal(PVXDoorState.Open, door.State);
        Assert.False(raised);
    }

    private static void AssertApproximatelyEqual(Quaternion expected, Quaternion actual, float tolerance = 1e-4f)
    {
        var dot = Quaternion.Dot(expected, actual);
        Assert.True(Math.Abs(Math.Abs(dot) - 1f) < tolerance, $"Expected approximately {expected} but got {actual}.");
    }
}
