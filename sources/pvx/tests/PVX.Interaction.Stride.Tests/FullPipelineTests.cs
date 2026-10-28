using PVX.Gameplay;
using PVX.Interaction;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace PVX.Interaction.Stride.Tests;

/// <summary>
/// End-to-end acceptance tests: a real physics raycast hit resolved by
/// <see cref="PVXStridePhysicsTargetProvider"/> flows all the way into a
/// <see cref="PVXInteractionService"/>'s <c>CurrentFocus</c>/<c>PrimaryAction</c>.
/// This is the actual bridge the milestone exists to prove.
/// </summary>
public class FullPipelineTests : IClassFixture<PhysicsFixture>
{
    private readonly PhysicsFixture fixture;

    public FullPipelineTests(PhysicsFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task FullPipeline_PhysicsHitBecomesCurrentFocus()
    {
        var focusTargetIsInteractable = await fixture.RunScenario(async game =>
        {
            var target = new Entity("Door") { Transform = { Position = new Vector3(0, 0, -5) } };
            var action = new TestInteractionAction();
            target.Add(new TestInteractableComponent
            {
                ActionsFactory = _ => [new PVXInteractionDescriptor(new PVXInteractionActionId("open"), action, priority: 100, label: "Open")]
            });
            target.Add(PhysicsSceneHelpers.CreateBoxCollider(Vector3.One));

            await game.AddAndSettle(target);

            var session = new PVXGameplaySession();
            session.Start();
            session.Player.RegisterProvider(new FullPipelineTestPlayerProvider());

            var provider = new PVXStridePhysicsTargetProvider(() => PhysicsSceneHelpers.GetSimulation(game.SceneSystem));
            var service = new PVXInteractionService(session) { TargetProvider = provider };
            var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 10f);

            service.Update(source, TimeSpan.Zero);

            var isInteractable = service.CurrentFocus?.Target is TestInteractableComponent;

            await game.RemoveAndSettle(target);
            return isInteractable;
        });

        Assert.True(focusTargetIsInteractable);
    }

    [Fact]
    public async Task FullPipeline_PhysicsHitExposesPrimaryAction()
    {
        var (primaryActionId, beginResult, executeCount) = await fixture.RunScenario(async game =>
        {
            var target = new Entity("Door") { Transform = { Position = new Vector3(0, 0, -5) } };
            var action = new TestInteractionAction();
            target.Add(new TestInteractableComponent
            {
                ActionsFactory = _ => [new PVXInteractionDescriptor(new PVXInteractionActionId("open"), action, priority: 100, label: "Open")]
            });
            target.Add(PhysicsSceneHelpers.CreateBoxCollider(Vector3.One));

            await game.AddAndSettle(target);

            var session = new PVXGameplaySession();
            session.Start();
            session.Player.RegisterProvider(new FullPipelineTestPlayerProvider());

            var provider = new PVXStridePhysicsTargetProvider(() => PhysicsSceneHelpers.GetSimulation(game.SceneSystem));
            var service = new PVXInteractionService(session) { TargetProvider = provider };
            var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 10f);

            service.Update(source, TimeSpan.Zero);
            var actionId = service.PrimaryAction?.ActionId.Value;
            var result = service.BeginPrimaryInteraction(source);

            await game.RemoveAndSettle(target);
            return (actionId, result, action.ExecuteCount);
        });

        Assert.Equal("open", primaryActionId);
        Assert.True(beginResult);
        Assert.Equal(1, executeCount);
    }

    [Fact]
    public async Task FullPipeline_WallOcclusion_NoFocus()
    {
        var hasFocus = await fixture.RunScenario(async game =>
        {
            var wall = PhysicsSceneHelpers.CreateStaticBoxCollider("Wall", new Vector3(0, 0, -3), new Vector3(5, 5, 0.2f));
            var door = new Entity("Door") { Transform = { Position = new Vector3(0, 0, -5) } };
            door.Add(new TestInteractableComponent
            {
                ActionsFactory = _ => [new PVXInteractionDescriptor(new PVXInteractionActionId("open"), new TestInteractionAction(), priority: 100)]
            });
            door.Add(PhysicsSceneHelpers.CreateBoxCollider(Vector3.One));

            await game.AddAndSettle(wall, door);

            var session = new PVXGameplaySession();
            session.Start();
            session.Player.RegisterProvider(new FullPipelineTestPlayerProvider());

            var provider = new PVXStridePhysicsTargetProvider(() => PhysicsSceneHelpers.GetSimulation(game.SceneSystem));
            var service = new PVXInteractionService(session) { TargetProvider = provider };
            var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 10f);

            service.Update(source, TimeSpan.Zero);
            var result = service.CurrentFocus is not null;

            await game.RemoveAndSettle(wall, door);
            return result;
        });

        Assert.False(hasFocus);
    }

    private sealed class FullPipelineTestPlayer : IPVXPlayer
    {
    }

    private sealed class FullPipelineTestPlayerProvider : IPVXPlayerProvider
    {
        private readonly FullPipelineTestPlayer player = new();

        public IPVXPlayer? GetPrimaryPlayer() => player;
    }
}
