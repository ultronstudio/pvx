using PVX.Interaction;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace PVX.Interaction.Stride.Tests;

/// <summary>
/// Real Stride/Bullet physics integration tests for <see cref="PVXStridePhysicsTargetProvider"/>.
/// Shares one headless game/simulation across all facts via <see cref="PhysicsFixture"/>
/// (see its own remarks for why), adding and removing each scenario's own
/// entities so facts stay independent despite the shared simulation.
/// </summary>
[Collection("PVX Physics")]
public class PhysicsTargetProviderTests
{
    private readonly PhysicsFixture fixture;

    public PhysicsTargetProviderTests(PhysicsFixture fixture)
    {
        this.fixture = fixture;
    }

    private static PVXStridePhysicsTargetProvider CreateProvider(Game game) =>
        new(() => PhysicsSceneHelpers.GetSimulation(game.SceneSystem));

    private static Entity CreateInteractableEntity(string name, Vector3 position)
    {
        var entity = new Entity(name);
        entity.Transform.Position = position;
        entity.Add(new TestInteractableComponent());
        entity.Add(PhysicsSceneHelpers.CreateBoxCollider(Vector3.One));
        return entity;
    }

    [Fact]
    public async Task Provider_NoHit_ReturnsNoCandidate()
    {
        var candidates = await fixture.RunScenario(async game =>
        {
            var provider = CreateProvider(game);
            var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 10f);
            return provider.GetCandidates(source);
        });

        Assert.Empty(candidates);
    }

    [Fact]
    public async Task Provider_HitInteractable_ReturnsCandidate()
    {
        var candidates = await fixture.RunScenario(async game =>
        {
            var target = CreateInteractableEntity("Target", new Vector3(0, 0, -5));
            await game.AddAndSettle(target);

            var provider = CreateProvider(game);
            var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 10f);
            var result = provider.GetCandidates(source);

            await game.RemoveAndSettle(target);
            return result;
        });

        var candidate = Assert.Single(candidates);
        Assert.IsType<TestInteractableComponent>(candidate.Target);
    }

    [Fact]
    public async Task Provider_PopulatesHitDistance()
    {
        var candidates = await fixture.RunScenario(async game =>
        {
            var target = CreateInteractableEntity("Target", new Vector3(0, 0, -5));
            await game.AddAndSettle(target);

            var provider = CreateProvider(game);
            var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 10f);
            var result = provider.GetCandidates(source);

            await game.RemoveAndSettle(target);
            return result;
        });

        var candidate = Assert.Single(candidates);
        // Box half-extent is 0.5 along Z, centered at z=-5: front face at z=-4.5.
        Assert.NotNull(candidate.Distance);
        Assert.InRange(candidate.Distance!.Value, 4.4f, 4.6f);
    }

    [Fact]
    public async Task Provider_ChildCollider_ResolvesParentInteractable()
    {
        var (candidates, doorRootName) = await fixture.RunScenario(async game =>
        {
            var doorRoot = new Entity("DoorRoot");
            doorRoot.Add(new TestInteractableComponent());

            var handle = new Entity("Handle") { Transform = { Position = new Vector3(0, 0, -5) } };
            handle.Add(PhysicsSceneHelpers.CreateBoxCollider(Vector3.One));
            handle.Transform.Parent = doorRoot.Transform;

            await game.AddAndSettle(doorRoot);

            var provider = CreateProvider(game);
            var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 10f);
            var result = provider.GetCandidates(source);

            await game.RemoveAndSettle(doorRoot);
            return (result, doorRoot.Name);
        });

        var candidate = Assert.Single(candidates);
        var interactable = Assert.IsType<TestInteractableComponent>(candidate.Target);
        Assert.Equal(doorRootName, interactable.Entity.Name);
    }

    [Fact]
    public async Task Provider_InteractionPoint_MapsPointId()
    {
        var candidates = await fixture.RunScenario(async game =>
        {
            var ladderRoot = new Entity("LadderRoot");
            ladderRoot.Add(new TestInteractableComponent());

            var bottom = new Entity("Bottom") { Transform = { Position = new Vector3(0, 0, -5) } };
            bottom.Add(PhysicsSceneHelpers.CreateBoxCollider(Vector3.One));
            bottom.Add(new PVXInteractionPointComponent { PointId = "bottom_entry" });
            bottom.Transform.Parent = ladderRoot.Transform;

            await game.AddAndSettle(ladderRoot);

            var provider = CreateProvider(game);
            var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 10f);
            var result = provider.GetCandidates(source);

            await game.RemoveAndSettle(ladderRoot);
            return result;
        });

        var candidate = Assert.Single(candidates);
        Assert.Equal("bottom_entry", candidate.Point?.Id);
    }

    [Fact]
    public async Task Provider_PointPriority_MapsCandidatePriority()
    {
        var candidates = await fixture.RunScenario(async game =>
        {
            var root = new Entity("Root");
            root.Add(new TestInteractableComponent());

            var point = new Entity("Point") { Transform = { Position = new Vector3(0, 0, -5) } };
            point.Add(PhysicsSceneHelpers.CreateBoxCollider(Vector3.One));
            point.Add(new PVXInteractionPointComponent { PointId = "top_entry", Priority = 42 });
            point.Transform.Parent = root.Transform;

            await game.AddAndSettle(root);

            var provider = CreateProvider(game);
            var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 10f);
            var result = provider.GetCandidates(source);

            await game.RemoveAndSettle(root);
            return result;
        });

        var candidate = Assert.Single(candidates);
        Assert.Equal(42, candidate.Priority);
    }

    [Fact]
    public async Task Provider_NoPoint_ReturnsNullPoint()
    {
        var candidates = await fixture.RunScenario(async game =>
        {
            var target = CreateInteractableEntity("Prop", new Vector3(0, 0, -5));
            await game.AddAndSettle(target);

            var provider = CreateProvider(game);
            var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 10f);
            var result = provider.GetCandidates(source);

            await game.RemoveAndSettle(target);
            return result;
        });

        var candidate = Assert.Single(candidates);
        Assert.Null(candidate.Point);
    }

    [Fact]
    public async Task Provider_NonInteractableBlockingHit_DoesNotRayThrough()
    {
        var candidates = await fixture.RunScenario(async game =>
        {
            var wall = PhysicsSceneHelpers.CreateStaticBoxCollider("Wall", new Vector3(0, 0, -3), new Vector3(5, 5, 0.2f));
            var door = CreateInteractableEntity("Door", new Vector3(0, 0, -5));

            await game.AddAndSettle(wall, door);

            var provider = CreateProvider(game);
            var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 10f);
            var result = provider.GetCandidates(source);

            await game.RemoveAndSettle(wall, door);
            return result;
        });

        // The wall blocks the view of the door behind it: no candidate,
        // even though an interactable genuinely exists further along the ray.
        Assert.Empty(candidates);
    }

    [Fact]
    public async Task Provider_OwnerEntity_IsIgnored()
    {
        var candidates = await fixture.RunScenario(async game =>
        {
            var playerCollider = PhysicsSceneHelpers.CreateStaticBoxCollider("PlayerCollider", new Vector3(0, 0, -1), Vector3.One);
            var door = CreateInteractableEntity("Door", new Vector3(0, 0, -5));

            await game.AddAndSettle(playerCollider, door);

            var provider = CreateProvider(game);
            var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 10f, ownerEntity: playerCollider);
            var result = provider.GetCandidates(source);

            await game.RemoveAndSettle(playerCollider, door);
            return result;
        });

        var candidate = Assert.Single(candidates);
        Assert.IsType<TestInteractableComponent>(candidate.Target);
    }

    [Fact]
    public async Task Provider_OwnerDescendant_IsIgnored()
    {
        var candidates = await fixture.RunScenario(async game =>
        {
            var playerRoot = new Entity("PlayerRoot");

            var playerColliderChild = new Entity("PlayerColliderChild") { Transform = { Position = new Vector3(0, 0, -1) } };
            playerColliderChild.Add(PhysicsSceneHelpers.CreateBoxCollider(Vector3.One));
            playerColliderChild.Transform.Parent = playerRoot.Transform;

            var door = CreateInteractableEntity("Door", new Vector3(0, 0, -5));

            await game.AddAndSettle(playerRoot, door);

            var provider = CreateProvider(game);
            // Owner is the ROOT; the actual collider hit is on a CHILD of it.
            var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 10f, ownerEntity: playerRoot);
            var result = provider.GetCandidates(source);

            await game.RemoveAndSettle(playerRoot, door);
            return result;
        });

        var candidate = Assert.Single(candidates);
        Assert.IsType<TestInteractableComponent>(candidate.Target);
    }

    [Fact]
    public async Task Provider_CustomSourceWithNonUnitDirection_NormalizesDefensively()
    {
        var candidates = await fixture.RunScenario(async game =>
        {
            var target = CreateInteractableEntity("Target", new Vector3(0, 0, -5));
            await game.AddAndSettle(target);

            var provider = CreateProvider(game);

            // A non-conforming source reporting a 0.1-length direction: if
            // the provider trusted it as already unit length, MaxDistance
            // 5 would only reach z=-0.5 and miss the target entirely.
            // Defensive normalization must still reach z=-5.
            var source = new UnvalidatedRayInteractionSource
            {
                Origin = Vector3.Zero,
                Direction = new Vector3(0, 0, -0.1f),
                MaxDistance = 5f,
            };

            var result = provider.GetCandidates(source);

            await game.RemoveAndSettle(target);
            return result;
        });

        Assert.Single(candidates);
    }

    [Fact]
    public async Task Provider_DeterministicAcrossRepeatedQueries()
    {
        var (first, second) = await fixture.RunScenario(async game =>
        {
            var target = CreateInteractableEntity("Target", new Vector3(0, 0, -5));
            await game.AddAndSettle(target);

            var provider = CreateProvider(game);
            var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 10f);

            var firstResult = provider.GetCandidates(source);
            var secondResult = provider.GetCandidates(source);

            await game.RemoveAndSettle(target);
            return (firstResult, secondResult);
        });

        var a = Assert.Single(first);
        var b = Assert.Single(second);
        Assert.Same(a.Target, b.Target);
        Assert.Equal(a.Distance, b.Distance);
    }
}
