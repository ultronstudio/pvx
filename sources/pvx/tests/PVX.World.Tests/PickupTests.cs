using PVX.World;
using Stride.Engine;
using Xunit;

namespace PVX.World.Tests;

public class PickupTests
{
    private static (Entity Entity, PVXPickupComponent Pickup) CreatePickup()
    {
        var entity = new Entity("Pickup");
        var pickup = new PVXPickupComponent();
        entity.Add(pickup);
        return (entity, pickup);
    }

    [Fact]
    public void Pickup_DefaultState_IsIdle()
    {
        var (_, pickup) = CreatePickup();

        Assert.Equal(PVXPickupState.Idle, pickup.State);
    }

    [Fact]
    public void Pickup_Idle_OffersPickUp()
    {
        var (_, pickup) = CreatePickup();

        var descriptor = Assert.Single(pickup.GetActions(default));
        Assert.Equal(PVXPickupComponent.PickUpActionId, descriptor.ActionId);
        Assert.True(descriptor.Availability.IsAvailable);
    }

    [Fact]
    public void Pickup_Held_OffersNoActions()
    {
        var (_, pickup) = CreatePickup();
        pickup.TryPickUp();

        Assert.Empty(pickup.GetActions(default));
    }

    [Fact]
    public void Pickup_TryPickUp_IdleToHeld()
    {
        var (_, pickup) = CreatePickup();

        var result = pickup.TryPickUp();

        Assert.True(result);
        Assert.Equal(PVXPickupState.Held, pickup.State);
    }

    [Fact]
    public void Pickup_TryPickUp_WhileAlreadyHeld_Fails()
    {
        var (_, pickup) = CreatePickup();
        pickup.TryPickUp();

        var result = pickup.TryPickUp();

        Assert.False(result);
        Assert.Equal(PVXPickupState.Held, pickup.State);
    }

    [Fact]
    public void Pickup_TryDrop_HeldToIdle()
    {
        var (_, pickup) = CreatePickup();
        pickup.TryPickUp();

        var result = pickup.TryDrop();

        Assert.True(result);
        Assert.Equal(PVXPickupState.Idle, pickup.State);
    }

    [Fact]
    public void Pickup_TryDrop_WhileAlreadyIdle_Fails()
    {
        var (_, pickup) = CreatePickup();

        var result = pickup.TryDrop();

        Assert.False(result);
        Assert.Equal(PVXPickupState.Idle, pickup.State);
    }

    [Fact]
    public void Pickup_StateChanged_FiresOncePerActualChange()
    {
        var (_, pickup) = CreatePickup();
        var transitions = new List<(PVXPickupState Previous, PVXPickupState Current)>();
        pickup.StateChanged += (_, e) => transitions.Add((e.Previous, e.Current));

        pickup.TryPickUp();
        pickup.TryPickUp(); // already Held: no-op, must not raise again
        pickup.TryDrop();

        Assert.Equal(
            [(PVXPickupState.Idle, PVXPickupState.Held), (PVXPickupState.Held, PVXPickupState.Idle)],
            transitions);
    }
}
