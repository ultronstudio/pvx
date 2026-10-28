using PVX.Gameplay;
using PVX.Interaction;
using PVX.Interaction.Stride;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace PVX.World.Tests;

/// <summary>
/// Real Stride/Bullet physics integration tests for <see cref="PVXDoorComponent"/>:
/// a genuine child handle collider, resolved by the real
/// <see cref="PVXStridePhysicsTargetProvider"/>, and a genuine check that
/// the handle's collider actually moves with the door leaf after it opens
/// (not just the visual transform) — see <see cref="PVXDoorComponent"/>'s
/// own remarks on <c>PhysicsComponent.UpdatePhysicsTransformation</c> for why
/// this needed explicit handling rather than being automatic.
/// </summary>
[Collection("PVX World Physics")]
public class DoorRealPhysicsTests
{
    private readonly PhysicsFixture fixture;

    public DoorRealPhysicsTests(PhysicsFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task RealPhysicsRaycast_OnHandleCollider_ResolvesDoorInteractable_WithHandlePoint()
    {
        var candidates = await fixture.RunScenario(async game =>
        {
            var doorLeaf = new Entity("DoorLeaf") { Transform = { Position = new Vector3(0, 0, -5) } };
            doorLeaf.Add(new PVXDoorComponent());

            var handle = new Entity("Handle");
            handle.Add(PhysicsTestHelpers.CreateBoxCollider(Vector3.One));
            handle.Add(new PVXInteractionPointComponent { PointId = "handle" });
            handle.Transform.Parent = doorLeaf.Transform;

            await game.AddAndSettle(doorLeaf);

            var provider = new PVXStridePhysicsTargetProvider(() => PhysicsTestHelpers.GetSimulation(game.SceneSystem));
            var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 10f);
            var result = provider.GetCandidates(source);

            await game.RemoveAndSettle(doorLeaf);
            return result;
        });

        var candidate = Assert.Single(candidates);
        Assert.IsType<PVXDoorComponent>(candidate.Target);
        Assert.Equal("handle", candidate.Point?.Id);
    }

    [Fact]
    public async Task DoorOpen_SyncsHandleColliderPhysicsTransform_ToNewWorldPosition()
    {
        var (beforeOpen, afterOpen, expectedAfterOpen) = await fixture.RunScenario(async game =>
        {
            var doorLeaf = new Entity("DoorLeaf") { Transform = { Position = new Vector3(2, 0, -5) } };
            var door = new PVXDoorComponent { TransitionDuration = 0f, OpenAngleDegrees = 90f };
            doorLeaf.Add(door);

            var handle = new Entity("Handle") { Transform = { Position = new Vector3(1, 0, 0) } };
            var handleCollider = PhysicsTestHelpers.CreateBoxCollider(new Vector3(0.2f));
            handle.Add(handleCollider);
            handle.Transform.Parent = doorLeaf.Transform;

            await game.AddAndSettle(doorLeaf);

            var before = handleCollider.PhysicsWorldTransform.TranslationVector;

            door.TryOpen();
            door.Advance(0f); // TransitionDuration 0 => instantaneous

            var after = handleCollider.PhysicsWorldTransform.TranslationVector;

            handle.Transform.UpdateWorldMatrix();
            var expected = handle.Transform.WorldMatrix.TranslationVector;

            await game.RemoveAndSettle(doorLeaf);
            return (before, after, expected);
        });

        Assert.True((afterOpen - beforeOpen).Length() > 0.1f,
            $"Expected the handle collider's physics-side position to actually change after opening (was {beforeOpen}, still {afterOpen}).");
        Assert.True((afterOpen - expectedAfterOpen).Length() < 0.01f,
            $"Expected handle collider physics transform {afterOpen} to match the entity's real world position {expectedAfterOpen} after opening.");
    }

    [Fact]
    public async Task FullPipeline_RealPhysicsHit_BecomesFocus_AndOpensDoorViaInteractionService()
    {
        var (primaryActionId, began, stateAfterBegin) = await fixture.RunScenario(async game =>
        {
            var doorLeaf = new Entity("Door") { Transform = { Position = new Vector3(0, 0, -5) } };
            var door = new PVXDoorComponent { TransitionDuration = 0f };
            doorLeaf.Add(door);
            doorLeaf.Add(PhysicsTestHelpers.CreateBoxCollider(Vector3.One));

            await game.AddAndSettle(doorLeaf);

            var session = new PVXGameplaySession();
            session.Start();
            session.Player.RegisterProvider(new FakePlayerProvider { Player = new FakePlayer() });

            var provider = new PVXStridePhysicsTargetProvider(() => PhysicsTestHelpers.GetSimulation(game.SceneSystem));
            var service = new PVXInteractionService(session) { TargetProvider = provider };
            var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 10f);

            service.Update(source, TimeSpan.Zero);
            var actionId = service.PrimaryAction?.ActionId.Value;

            var beginResult = service.BeginPrimaryInteraction(source);
            door.Advance(0f); // completes the instantaneous transition

            await game.RemoveAndSettle(doorLeaf);
            return (actionId, beginResult, door.State);
        });

        Assert.Equal("open", primaryActionId);
        Assert.True(began);
        Assert.Equal(PVXDoorState.Open, stateAfterBegin);
    }
}
