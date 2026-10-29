using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using Xunit;

namespace PVX.FirstPerson.Tests;

/// <summary>
/// Pure transform/state tests for <see cref="PVXCarryComponent"/> — no live
/// scene, no running physics simulation, no InputManager. Exercises
/// <see cref="PVXCarryComponent.TryBeginCarry"/>/<see cref="PVXCarryComponent.EndCarry"/>'s
/// reparenting and <see cref="RigidbodyComponent.IsKinematic"/> toggling
/// directly, the same way <c>ControllerMathTests</c> exercises
/// <see cref="PVXFirstPersonControllerComponent"/>'s math without a live
/// scene/processor.
/// </summary>
public class CarryComponentTests
{
    private static (Entity CameraEntity, PVXCarryComponent Carry) CreateCarrier()
    {
        var cameraEntity = new Entity("Camera");
        var camera = new CameraComponent();
        cameraEntity.Add(camera);

        var carryEntity = new Entity("Player");
        var carry = new PVXCarryComponent { Camera = camera };
        carryEntity.Add(carry);

        return (cameraEntity, carry);
    }

    private static Entity CreatePickupTarget()
    {
        var entity = new Entity("Item");
        entity.Transform.Position = new Vector3(5f, 0f, 5f);
        return entity;
    }

    [Fact]
    public void TryBeginCarry_WithoutCamera_Fails()
    {
        var carry = new PVXCarryComponent();
        var target = CreatePickupTarget();

        var result = carry.TryBeginCarry(target);

        Assert.False(result);
        Assert.Null(carry.CarriedEntity);
    }

    [Fact]
    public void TryBeginCarry_NullTarget_Fails()
    {
        var (_, carry) = CreateCarrier();

        var result = carry.TryBeginCarry(null);

        Assert.False(result);
    }

    [Fact]
    public void TryBeginCarry_SetsCarriedEntity()
    {
        var (_, carry) = CreateCarrier();
        var target = CreatePickupTarget();

        var result = carry.TryBeginCarry(target);

        Assert.True(result);
        Assert.Same(target, carry.CarriedEntity);
    }

    [Fact]
    public void TryBeginCarry_WhileAlreadyCarrying_Fails()
    {
        var (_, carry) = CreateCarrier();
        carry.TryBeginCarry(CreatePickupTarget());

        var result = carry.TryBeginCarry(CreatePickupTarget());

        Assert.False(result);
    }

    [Fact]
    public void TryBeginCarry_ReparentsUnderCamera()
    {
        var (cameraEntity, carry) = CreateCarrier();
        var target = CreatePickupTarget();

        carry.TryBeginCarry(target);

        Assert.Same(cameraEntity.Transform, target.Transform.Parent);
    }

    [Fact]
    public void TryBeginCarry_SetsHoldOffset()
    {
        var (_, carry) = CreateCarrier();
        carry.HoldDistance = 1.5f;
        var target = CreatePickupTarget();

        carry.TryBeginCarry(target);

        Assert.Equal(new Vector3(0f, 0f, -1.5f), target.Transform.Position);
        Assert.Equal(Quaternion.Identity, target.Transform.Rotation);
    }

    // RigidbodyComponent.IsKinematic/LinearVelocity require a live physics
    // simulation (the entity must already be in a running Scene) — covered
    // by CarryIntegrationTests, not here (see ControllerMathTests/
    // ControllerIntegrationTests' own equivalent split).

    [Fact]
    public void EndCarry_WithoutCarrying_ReturnsNull()
    {
        var (_, carry) = CreateCarrier();

        var result = carry.EndCarry(throwIt: false);

        Assert.Null(result);
    }

    [Fact]
    public void EndCarry_ClearsCarriedEntity()
    {
        var (_, carry) = CreateCarrier();
        var target = CreatePickupTarget();
        carry.TryBeginCarry(target);

        carry.EndCarry(throwIt: false);

        Assert.Null(carry.CarriedEntity);
    }

    [Fact]
    public void EndCarry_UnparentsTarget()
    {
        var (_, carry) = CreateCarrier();
        var target = CreatePickupTarget();
        carry.TryBeginCarry(target);

        carry.EndCarry(throwIt: false);

        Assert.Null(target.Transform.Parent);
    }

    [Fact]
    public void AccumulateInspectRotation_WithoutCarrying_NoOp()
    {
        var (_, carry) = CreateCarrier();

        carry.AccumulateInspectRotation(new Vector2(1f, 0f));
        // Nothing to assert on carry itself; just must not throw.
    }

    [Fact]
    public void AccumulateInspectRotation_RotatesCarriedEntity()
    {
        var (_, carry) = CreateCarrier();
        var target = CreatePickupTarget();
        carry.TryBeginCarry(target);
        var initialRotation = target.Transform.Rotation;

        carry.AccumulateInspectRotation(new Vector2(0.1f, 0f));

        Assert.NotEqual(initialRotation, target.Transform.Rotation);
    }
}
