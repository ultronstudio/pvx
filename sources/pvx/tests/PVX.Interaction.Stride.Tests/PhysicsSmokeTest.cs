using Stride.Core.Mathematics;
using Xunit;

namespace PVX.Interaction.Stride.Tests;

/// <summary>
/// Minimal standalone probe: does a real Bullet raycast against a
/// programmatically-built box collider work at all in this environment?
/// Run and inspected first, before the full test matrix, given this
/// repo's known unrelated BulletSharp/native issue affecting some
/// upstream Stride.Physics.Tests/Stride.Navigation.Tests runs.
/// </summary>
[Collection("PVX Physics")]
public class PhysicsSmokeTest
{
    private readonly PhysicsFixture fixture;

    public PhysicsSmokeTest(PhysicsFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task RealBulletRaycast_HitsBoxCollider()
    {
        var hit = await fixture.RunScenario(async game =>
        {
            var box = PhysicsSceneHelpers.CreateStaticBoxCollider("Box", new Vector3(0, 0, -5), Vector3.One);
            await game.AddAndSettle(box);

            var simulation = PhysicsSceneHelpers.GetSimulation(game.SceneSystem);
            Assert.NotNull(simulation);

            var result = simulation!.Raycast(new Vector3(0, 0, 0), new Vector3(0, 0, -10));

            await game.RemoveAndSettle(box);

            return result;
        });

        Assert.True(hit.Succeeded, "Expected the raycast to hit the box collider.");
    }
}
