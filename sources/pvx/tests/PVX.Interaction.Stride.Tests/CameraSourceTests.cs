using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace PVX.Interaction.Stride.Tests;

public class CameraSourceTests
{
    private static Entity CreateCameraEntity(Vector3 position, Quaternion rotation)
    {
        var entity = new Entity("Camera");
        entity.Transform.Position = position;
        entity.Transform.Rotation = rotation;
        entity.Add(new CameraComponent());

        // No running SceneInstance in these tests: compute WorldMatrix
        // directly instead of relying on a scene update tick.
        entity.Transform.UpdateWorldMatrix();

        return entity;
    }

    [Fact]
    public void CameraSource_UsesCorrectStrideForwardDirection_Identity()
    {
        var entity = CreateCameraEntity(Vector3.Zero, Quaternion.Identity);
        var camera = entity.Get<CameraComponent>();

        var source = new PVXStrideCameraInteractionSource(camera, maxDistance: 10f);

        // Stride's world forward is (-M31,-M32,-M33); with no rotation the
        // engine's right-handed convention looks down -Z.
        AssertApproximatelyEqual(new Vector3(0, 0, -1), source.Direction);
    }

    [Fact]
    public void CameraSource_UsesCorrectStrideForwardDirection_Rotated()
    {
        var rotation = Quaternion.RotationAxis(Vector3.UnitY, MathUtil.PiOverTwo);
        var entity = CreateCameraEntity(Vector3.Zero, rotation);
        var camera = entity.Get<CameraComponent>();

        var source = new PVXStrideCameraInteractionSource(camera, maxDistance: 10f);

        // Cross-check against Stride's own vector transform of the known
        // unrotated forward, rather than hand-deriving trigonometry.
        var expected = Vector3.Transform(new Vector3(0, 0, -1), rotation);
        AssertApproximatelyEqual(expected, source.Direction);
    }

    [Fact]
    public void CameraSource_UsesEntityWorldPositionAsOrigin()
    {
        var position = new Vector3(5, 1, -3);
        var entity = CreateCameraEntity(position, Quaternion.Identity);
        var camera = entity.Get<CameraComponent>();

        var source = new PVXStrideCameraInteractionSource(camera, maxDistance: 10f);

        AssertApproximatelyEqual(position, source.Origin);
    }

    [Fact]
    public void CameraSource_DirectionIsAlwaysUnitLength()
    {
        var entity = CreateCameraEntity(Vector3.Zero, Quaternion.RotationAxis(Vector3.UnitX, 0.37f));
        var camera = entity.Get<CameraComponent>();

        var source = new PVXStrideCameraInteractionSource(camera, maxDistance: 10f);

        Assert.Equal(1f, source.Direction.Length(), precision: 4);
    }

    [Fact]
    public void CameraSource_RejectsNonPositiveMaxDistance()
    {
        var entity = CreateCameraEntity(Vector3.Zero, Quaternion.Identity);
        var camera = entity.Get<CameraComponent>();

        Assert.Throws<ArgumentException>(() => new PVXStrideCameraInteractionSource(camera, maxDistance: 0f));
    }

    [Fact]
    public void CameraSource_NullCamera_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PVXStrideCameraInteractionSource(null!, maxDistance: 5f));
    }

    private static void AssertApproximatelyEqual(Vector3 expected, Vector3 actual, float tolerance = 1e-4f)
    {
        Assert.True(
            (expected - actual).Length() < tolerance,
            $"Expected approximately {expected} but got {actual}.");
    }
}
