using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using Xunit;

namespace PVX.FirstPerson.Tests;

/// <summary>
/// Real-physics tests for <see cref="PVXCarryComponent"/>'s
/// <see cref="RigidbodyComponent"/> toggling: a bare <c>RigidbodyComponent</c>
/// not yet attached to a running <c>Simulation</c> throws on
/// <c>LinearVelocity</c>/most physics calls, so this behavior cannot be
/// exercised by <c>CarryComponentTests</c>' pure/no-scene style — see its
/// own remarks. <see cref="PVXCarryComponent"/> itself is used directly
/// here (not through its processor): the item being carried is attached to
/// the live scene (so its rigidbody is real), the carrier is not (so no
/// <c>PVXInteractionFeature</c> is required, matching
/// <see cref="FirstPersonHostGame"/>'s minimal setup).
/// </summary>
[Collection("PVX FirstPerson")]
public class CarryIntegrationTests
{
    private readonly FirstPersonFixture fixture;

    public CarryIntegrationTests(FirstPersonFixture fixture)
    {
        this.fixture = fixture;
    }

    private static (Entity Entity, RigidbodyComponent Rigidbody) CreateDynamicItem(Vector3 position)
    {
        var entity = new Entity("Item") { Transform = { Position = position } };
        var rigidbody = new RigidbodyComponent();
        rigidbody.ColliderShapes.Add(new BoxColliderShapeDesc { Size = new Vector3(0.2f, 0.2f, 0.2f) });
        entity.Add(rigidbody);
        return (entity, rigidbody);
    }

    private static Entity CreateCamera() => new("Camera") { Transform = { Position = new Vector3(0, 2f, 0) } };

    [Fact]
    public async Task TryBeginCarry_SwitchesLiveRigidbodyToKinematic()
    {
        var (began, isKinematic) = await fixture.RunScenario(async game =>
        {
            var (item, rigidbody) = CreateDynamicItem(new Vector3(0, 5, 0));
            var cameraEntity = CreateCamera();
            cameraEntity.Add(new CameraComponent());
            await game.AddAndSettle(item, cameraEntity);

            var carry = new PVXCarryComponent { Camera = cameraEntity.Get<CameraComponent>() };
            var result = carry.TryBeginCarry(item);

            await game.RemoveAndSettle(item, cameraEntity);
            return (result, rigidbody.IsKinematic);
        });

        Assert.True(began);
        Assert.True(isKinematic);
    }

    [Fact]
    public async Task EndCarry_WithThrow_AppliesForwardVelocityToLiveRigidbody()
    {
        var speed = await fixture.RunScenario(async game =>
        {
            var (item, rigidbody) = CreateDynamicItem(new Vector3(0, 5, 0));
            var cameraEntity = CreateCamera();
            cameraEntity.Add(new CameraComponent());
            await game.AddAndSettle(item, cameraEntity);

            var carry = new PVXCarryComponent { Camera = cameraEntity.Get<CameraComponent>(), ThrowForce = 10f };
            carry.TryBeginCarry(item);
            carry.EndCarry(throwIt: true);

            var result = rigidbody.LinearVelocity.Length();

            await game.RemoveAndSettle(item, cameraEntity);
            return result;
        });

        Assert.Equal(10f, speed, 2);
    }

    [Fact]
    public async Task EndCarry_WithoutThrow_RestoresDynamicWithoutVelocity()
    {
        var (isKinematic, speed) = await fixture.RunScenario(async game =>
        {
            var (item, rigidbody) = CreateDynamicItem(new Vector3(0, 5, 0));
            var cameraEntity = CreateCamera();
            cameraEntity.Add(new CameraComponent());
            await game.AddAndSettle(item, cameraEntity);

            var carry = new PVXCarryComponent { Camera = cameraEntity.Get<CameraComponent>() };
            carry.TryBeginCarry(item);
            carry.EndCarry(throwIt: false);

            var result = (rigidbody.IsKinematic, rigidbody.LinearVelocity.Length());

            await game.RemoveAndSettle(item, cameraEntity);
            return result;
        });

        Assert.False(isKinematic);
        Assert.Equal(0f, speed, 3);
    }
}
