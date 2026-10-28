using Stride.Core.Mathematics;
using Xunit;

namespace PVX.Interaction.Stride.Tests;

public class RaySourceTests
{
    [Fact]
    public void RaySource_RejectsZeroDirection()
    {
        Assert.Throws<ArgumentException>(() =>
            new PVXStrideRayInteractionSource(Vector3.Zero, Vector3.Zero, 5f));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    public void RaySource_RejectsNonPositiveMaxDistance(float maxDistance)
    {
        Assert.Throws<ArgumentException>(() =>
            new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), maxDistance));
    }

    [Fact]
    public void RaySource_RejectsInfiniteMaxDistance()
    {
        Assert.Throws<ArgumentException>(() =>
            new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), float.PositiveInfinity));
    }

    [Fact]
    public void RaySource_RejectsNaNMaxDistance()
    {
        Assert.Throws<ArgumentException>(() =>
            new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), float.NaN));
    }

    [Fact]
    public void RaySource_RejectsNaNDirection()
    {
        Assert.Throws<ArgumentException>(() =>
            new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(float.NaN, 0, 0), 5f));
    }

    [Fact]
    public void RaySource_RejectsInfiniteDirection()
    {
        Assert.Throws<ArgumentException>(() =>
            new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(float.PositiveInfinity, 0, 0), 5f));
    }

    [Fact]
    public void RaySource_NormalizesNonUnitDirection()
    {
        // (0,0,-10) must not act like a 10x longer ray than (0,0,-1):
        // MaxDistance alone controls range.
        var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -10), 5f);

        Assert.Equal(1f, source.Direction.Length(), precision: 4);
        Assert.Equal(new Vector3(0, 0, -1), source.Direction);
    }

    [Fact]
    public void RaySource_PreservesOriginAndMaxDistance()
    {
        var origin = new Vector3(1, 2, 3);
        var source = new PVXStrideRayInteractionSource(origin, new Vector3(0, 0, -1), 7.5f);

        Assert.Equal(origin, source.Origin);
        Assert.Equal(7.5f, source.MaxDistance);
    }

    [Fact]
    public void RaySource_NoOwner_IsNull()
    {
        var source = new PVXStrideRayInteractionSource(Vector3.Zero, new Vector3(0, 0, -1), 5f);

        Assert.Null(source.OwnerEntity);
    }
}
