using Stride.Core.Mathematics;
using Xunit;

namespace PVX.Interaction.Stride.Tests;

/// <summary>
/// <see cref="PVXStridePhysicsTargetProvider"/> must not trust
/// <see cref="IPVXStrideRayInteractionSource"/>'s documented invariants
/// blindly: it is a public interface, and a non-conforming third-party
/// implementation (one that does not go through <see cref="PVXStrideRayInteractionSource"/>'s
/// own validated constructor) could hand it NaN/Infinity/zero values that
/// must never reach the native Bullet raycast.
/// </summary>
/// <remarks>
/// These run without <see cref="PhysicsFixture"/>/a real game on purpose:
/// a well-formed defensive check rejects the input before the provider
/// ever needs a <c>Simulation</c>, which these tests also verify by
/// passing an accessor that throws if actually invoked.
/// </remarks>
public class ProviderNativeBoundaryHardeningTests
{
    private static PVXStridePhysicsTargetProvider CreateProviderThatMustNotTouchSimulation() =>
        new(() => throw new InvalidOperationException("Simulation should not be requested for an invalid ray."));

    [Fact]
    public void Provider_CustomSourceWithNaNOrigin_ReturnsNoCandidate()
    {
        var provider = CreateProviderThatMustNotTouchSimulation();
        var source = new UnvalidatedRayInteractionSource
        {
            Origin = new Vector3(float.NaN, 0, 0),
            Direction = new Vector3(0, 0, -1),
            MaxDistance = 5f,
        };

        var candidates = provider.GetCandidates(source);

        Assert.Empty(candidates);
    }

    [Fact]
    public void Provider_CustomSourceWithZeroDirection_ReturnsNoCandidate()
    {
        var provider = CreateProviderThatMustNotTouchSimulation();
        var source = new UnvalidatedRayInteractionSource
        {
            Origin = Vector3.Zero,
            Direction = Vector3.Zero,
            MaxDistance = 5f,
        };

        var candidates = provider.GetCandidates(source);

        Assert.Empty(candidates);
    }

    [Fact]
    public void Provider_CustomSourceWithInvalidDistance_ReturnsNoCandidate()
    {
        var provider = CreateProviderThatMustNotTouchSimulation();
        var source = new UnvalidatedRayInteractionSource
        {
            Origin = Vector3.Zero,
            Direction = new Vector3(0, 0, -1),
            MaxDistance = -1f,
        };

        var candidates = provider.GetCandidates(source);

        Assert.Empty(candidates);
    }
}
