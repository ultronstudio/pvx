using PVX.Runtime;
using Stride.Engine;
using Xunit;

namespace PVX.Interaction.Stride.Tests;

/// <summary>
/// Lifecycle tests for <see cref="PVXStrideInteractionControllerComponent"/>:
/// attaching claims <c>PVXInteractionFeature.Source</c>/<c>PVXInteractionService.TargetProvider</c>,
/// detaching releases them, and a second controller can cleanly take over
/// after that release — see the processor's own remarks for the full
/// ownership policy.
/// </summary>
/// <remarks>
/// Deliberately does not exercise the "two controllers attached at once"
/// conflict-throw path here: that throw happens inside entity/component
/// attachment processing on the shared <see cref="PhysicsHostGame"/>'s
/// own game loop, and this fixture is shared (via <see cref="PhysicsCollection"/>)
/// across every real-physics test in this assembly. Verified instead by
/// direct code review of <c>PVXStrideInteractionControllerProcessor.GenerateComponentData</c>:
/// risking a crash of the shared game loop over every other test in this
/// collection is not worth it just to cover that one branch.
/// </remarks>
[Collection("PVX Physics")]
public class InteractionControllerTests
{
    private readonly PhysicsFixture fixture;

    public InteractionControllerTests(PhysicsFixture fixture)
    {
        this.fixture = fixture;
    }

    private static Entity CreateControllerEntity(string name)
    {
        var entity = new Entity(name);
        var camera = new CameraComponent();
        entity.Add(camera);
        entity.Add(new PVXStrideInteractionControllerComponent
        {
            Camera = camera,
            MaxDistance = 5f,
        });
        return entity;
    }

    private static PVXInteractionFeature GetInteractionFeature(PVXRuntime runtime)
    {
        foreach (var feature in runtime.Features)
        {
            if (feature is PVXInteractionFeature found)
                return found;
        }

        throw new InvalidOperationException("PVXInteractionFeature not found on the runtime.");
    }

    [Fact]
    public async Task Attach_ClaimsSourceAndProvider_Detach_ReleasesThem()
    {
        var (sourceClaimed, providerClaimed, sourceReleased, providerReleased) = await fixture.RunScenario(async game =>
        {
            var runtime = game.Services.GetService<PVXRuntime>()!;
            var feature = GetInteractionFeature(runtime);

            var controller = CreateControllerEntity("Controller1");
            await game.AddAndSettle(controller);

            var claimedSource = feature.Source is not null;
            var claimedProvider = feature.Service!.TargetProvider is not null;

            await game.RemoveAndSettle(controller);

            var releasedSource = feature.Source is null;
            var releasedProvider = feature.Service!.TargetProvider is null;

            return (claimedSource, claimedProvider, releasedSource, releasedProvider);
        });

        Assert.True(sourceClaimed, "Expected attaching the controller to claim PVXInteractionFeature.Source.");
        Assert.True(providerClaimed, "Expected attaching the controller to claim PVXInteractionService.TargetProvider.");
        Assert.True(sourceReleased, "Expected detaching the controller to release Source it still owned.");
        Assert.True(providerReleased, "Expected detaching the controller to release TargetProvider it still owned.");
    }

    [Fact]
    public async Task SecondController_CanClaimOwnership_AfterFirstProperlyReleases()
    {
        var (firstSourceIsNotNull, secondSourceIsNotNull, sourcesDiffer) = await fixture.RunScenario(async game =>
        {
            var runtime = game.Services.GetService<PVXRuntime>()!;
            var feature = GetInteractionFeature(runtime);

            var first = CreateControllerEntity("First");
            await game.AddAndSettle(first);
            var firstSource = feature.Source;

            await game.RemoveAndSettle(first);

            var second = CreateControllerEntity("Second");
            await game.AddAndSettle(second);
            var secondSource = feature.Source;

            await game.RemoveAndSettle(second);

            return (firstSource is not null, secondSource is not null, !ReferenceEquals(firstSource, secondSource));
        });

        Assert.True(firstSourceIsNotNull);
        Assert.True(secondSourceIsNotNull);
        Assert.True(sourcesDiffer, "Expected the second controller's source to be its own, not the first controller's leftover instance.");
    }
}
