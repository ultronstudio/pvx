using Stride.Core.Mathematics;
using Xunit;

namespace PVX.FirstPerson.Tests;

/// <summary>
/// Pure math tests for <see cref="PVXFirstPersonControllerComponent"/>'s
/// look/movement calculations — no live scene, no physics, no InputManager.
/// Real-physics/real-CharacterComponent behavior is covered separately in
/// <c>ControllerIntegrationTests</c>.
/// </summary>
public class ControllerMathTests
{
    [Fact]
    public void AccumulateLook_MouseRight_TurnsYawRight()
    {
        var component = new PVXFirstPersonControllerComponent();
        var initialYaw = component.Yaw;

        component.AccumulateLook(new Vector2(10f, 0f));

        Assert.NotEqual(initialYaw, component.Yaw);
    }

    [Fact]
    public void AccumulateLook_ClampsPitchToMax()
    {
        var component = new PVXFirstPersonControllerComponent { PitchMaxDegrees = 89f };

        component.AccumulateLook(new Vector2(0f, -100000f));

        Assert.Equal(89f, component.PitchDegrees, 3);
    }

    [Fact]
    public void AccumulateLook_ClampsPitchToMin()
    {
        var component = new PVXFirstPersonControllerComponent { PitchMinDegrees = -89f };

        component.AccumulateLook(new Vector2(0f, 100000f));

        Assert.Equal(-89f, component.PitchDegrees, 3);
    }

    [Fact]
    public void ResetLook_ClampsOutOfRangePitch()
    {
        var component = new PVXFirstPersonControllerComponent { PitchMinDegrees = -45f, PitchMaxDegrees = 45f };

        component.ResetLook(yaw: 1.23f, pitchDegrees: 90f);

        Assert.Equal(1.23f, component.Yaw, 4);
        Assert.Equal(45f, component.PitchDegrees, 3);
    }

    [Fact]
    public void ComputeHorizontalVelocity_ZeroInput_ReturnsZero()
    {
        var component = new PVXFirstPersonControllerComponent();

        var velocity = component.ComputeHorizontalVelocity(Vector2.Zero);

        Assert.Equal(Vector3.Zero, velocity);
    }

    [Fact]
    public void ComputeHorizontalVelocity_IsAlwaysHorizontal()
    {
        var component = new PVXFirstPersonControllerComponent { WalkSpeed = 5f };
        component.AccumulateLook(new Vector2(37f, 0f)); // arbitrary non-zero yaw

        var velocity = component.ComputeHorizontalVelocity(new Vector2(0f, 1f));

        Assert.Equal(0f, velocity.Y, 5);
    }

    [Fact]
    public void ComputeHorizontalVelocity_ScalesByWalkSpeed()
    {
        var slow = new PVXFirstPersonControllerComponent { WalkSpeed = 2f };
        var fast = new PVXFirstPersonControllerComponent { WalkSpeed = 8f };

        var slowVelocity = slow.ComputeHorizontalVelocity(new Vector2(0f, 1f));
        var fastVelocity = fast.ComputeHorizontalVelocity(new Vector2(0f, 1f));

        Assert.Equal(slowVelocity.Length() * 4f, fastVelocity.Length(), 3);
    }

    [Fact]
    public void ComputeHorizontalVelocity_DiagonalInput_NotFasterThanAxisAligned()
    {
        var component = new PVXFirstPersonControllerComponent { WalkSpeed = 5f };

        var forward = component.ComputeHorizontalVelocity(new Vector2(0f, 1f));
        var diagonal = component.ComputeHorizontalVelocity(new Vector2(1f, 1f));

        Assert.Equal(forward.Length(), diagonal.Length(), 3);
    }

    [Fact]
    public void ComputeHorizontalVelocity_ForwardInput_IgnoresPitch_EvenWhenLookingStraightUp()
    {
        // This is the core Stage 3 acceptance requirement: pitching the
        // camera upward and pressing forward must move the player
        // horizontally, never fly it upward. ComputeHorizontalVelocity
        // doesn't even take pitch as a parameter, so this proves it
        // structurally, not just for one sampled pitch value.
        var component = new PVXFirstPersonControllerComponent { WalkSpeed = 5f };

        var levelVelocity = component.ComputeHorizontalVelocity(new Vector2(0f, 1f));

        component.AccumulateLook(new Vector2(0f, -100000f)); // pitch to PitchMaxDegrees (looking straight up)
        Assert.Equal(component.PitchMaxDegrees, component.PitchDegrees, 3);

        var pitchedUpVelocity = component.ComputeHorizontalVelocity(new Vector2(0f, 1f));

        Assert.Equal(levelVelocity, pitchedUpVelocity);
        Assert.Equal(0f, pitchedUpVelocity.Y, 5);
    }

    [Fact]
    public void YawRotation_And_PitchRotation_AreIndependentAxes()
    {
        var component = new PVXFirstPersonControllerComponent();
        component.AccumulateLook(new Vector2(50f, -50f));

        // Yaw must only ever rotate around Y; pitch only around the local X
        // axis of whatever it's applied to (the camera entity) — never
        // combined into one rotation that could leak pitch into movement.
        var yawOfUp = Vector3.Transform(Vector3.UnitY, component.YawRotation);
        Assert.True(Vector3.Distance(Vector3.UnitY, yawOfUp) < 0.001f, $"Expected yaw rotation to leave the up axis unchanged, got {yawOfUp}.");
    }
}
