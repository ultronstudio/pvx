using PVX.Cinematics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace PVX.Cinematics.Tests;

public class CinematicComponentTests
{
    private static (Entity Entity, PVXCinematicComponent Cinematic) CreateCinematic(Vector3? startPosition = null)
    {
        var entity = new Entity("Rig");
        if (startPosition is { } position)
        {
            entity.Transform.Position = position;
        }

        var cinematic = new PVXCinematicComponent();
        entity.Add(cinematic);
        return (entity, cinematic);
    }

    private static PVXCinematicWaypoint Waypoint(Vector3 position, float duration = 1f) =>
        new() { Position = position, Rotation = Quaternion.Identity, Duration = duration };

    [Fact]
    public void DefaultState_IsNotPlaying()
    {
        var (_, cinematic) = CreateCinematic();

        Assert.False(cinematic.IsPlaying);
    }

    [Fact]
    public void Play_WithWaypoints_StartsPlaying()
    {
        var (_, cinematic) = CreateCinematic();
        cinematic.Waypoints.Add(Waypoint(new Vector3(10, 0, 0)));

        var result = cinematic.Play();

        Assert.True(result);
        Assert.True(cinematic.IsPlaying);
    }

    [Fact]
    public void Play_NoWaypoints_Fails()
    {
        var (_, cinematic) = CreateCinematic();

        var result = cinematic.Play();

        Assert.False(result);
    }

    [Fact]
    public void Play_WhileAlreadyPlaying_Fails()
    {
        var (_, cinematic) = CreateCinematic();
        cinematic.Waypoints.Add(Waypoint(new Vector3(10, 0, 0)));
        cinematic.Play();

        var result = cinematic.Play();

        Assert.False(result);
    }

    [Fact]
    public void Advance_MovesTowardFirstWaypoint()
    {
        var (entity, cinematic) = CreateCinematic(Vector3.Zero);
        cinematic.Waypoints.Add(Waypoint(new Vector3(10, 0, 0), duration: 2f));
        cinematic.Play();

        cinematic.Advance(1f); // halfway through a 2s leg

        AssertApproximatelyEqual(new Vector3(5, 0, 0), entity.Transform.Position);
    }

    [Fact]
    public void Advance_ReachesWaypointExactlyAtDuration()
    {
        var (entity, cinematic) = CreateCinematic(Vector3.Zero);
        cinematic.Waypoints.Add(Waypoint(new Vector3(10, 0, 0), duration: 1f));
        cinematic.Play();

        cinematic.Advance(1f);

        AssertApproximatelyEqual(new Vector3(10, 0, 0), entity.Transform.Position);
    }

    [Fact]
    public void Advance_PastLastWaypoint_Completes()
    {
        var (entity, cinematic) = CreateCinematic(Vector3.Zero);
        cinematic.Waypoints.Add(Waypoint(new Vector3(10, 0, 0), duration: 1f));
        cinematic.Play();

        cinematic.Advance(1f);

        Assert.False(cinematic.IsPlaying);
        AssertApproximatelyEqual(new Vector3(10, 0, 0), entity.Transform.Position);
    }

    [Fact]
    public void Advance_MultiWaypointPath_ContinuesToSecondLeg()
    {
        var (entity, cinematic) = CreateCinematic(Vector3.Zero);
        cinematic.Waypoints.Add(Waypoint(new Vector3(10, 0, 0), duration: 1f));
        cinematic.Waypoints.Add(Waypoint(new Vector3(10, 10, 0), duration: 1f));
        cinematic.Play();

        cinematic.Advance(1f); // reaches first waypoint
        cinematic.Advance(0.5f); // halfway through second leg

        AssertApproximatelyEqual(new Vector3(10, 5, 0), entity.Transform.Position);
        Assert.True(cinematic.IsPlaying);
    }

    [Fact]
    public void Advance_LargeDelta_DoesNotOvershootWithinOneLeg()
    {
        var (entity, cinematic) = CreateCinematic(Vector3.Zero);
        cinematic.Waypoints.Add(Waypoint(new Vector3(10, 0, 0), duration: 1f));
        cinematic.Play();

        cinematic.Advance(100f);

        Assert.False(cinematic.IsPlaying);
        AssertApproximatelyEqual(new Vector3(10, 0, 0), entity.Transform.Position);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void Advance_InvalidDuration_SnapsImmediately(float duration)
    {
        var (entity, cinematic) = CreateCinematic(Vector3.Zero);
        cinematic.Waypoints.Add(Waypoint(new Vector3(10, 0, 0), duration));
        cinematic.Play();

        cinematic.Advance(0f);

        AssertApproximatelyEqual(new Vector3(10, 0, 0), entity.Transform.Position);
        Assert.False(cinematic.IsPlaying);
    }

    [Fact]
    public void Stop_WhilePlaying_StopsAtCurrentPose_RaisesCompleted()
    {
        var (entity, cinematic) = CreateCinematic(Vector3.Zero);
        cinematic.Waypoints.Add(Waypoint(new Vector3(10, 0, 0), duration: 2f));
        cinematic.Play();
        cinematic.Advance(1f); // halfway
        var raised = false;
        cinematic.Completed += (_, _) => raised = true;

        cinematic.Stop();

        Assert.True(raised);
        Assert.False(cinematic.IsPlaying);
        AssertApproximatelyEqual(new Vector3(5, 0, 0), entity.Transform.Position); // left where it was, not snapped
    }

    [Fact]
    public void Stop_WhileNotPlaying_NoOp()
    {
        var (_, cinematic) = CreateCinematic();
        var raised = false;
        cinematic.Completed += (_, _) => raised = true;

        cinematic.Stop();

        Assert.False(raised);
    }

    [Fact]
    public void Completed_RaisedOnceOnNaturalEnd()
    {
        var (_, cinematic) = CreateCinematic(Vector3.Zero);
        cinematic.Waypoints.Add(Waypoint(new Vector3(10, 0, 0), duration: 1f));
        cinematic.Play();
        var raiseCount = 0;
        cinematic.Completed += (_, _) => raiseCount++;

        cinematic.Advance(1f);

        Assert.Equal(1, raiseCount);
    }

    [Fact]
    public void AfterCompletion_CanPlayAgain()
    {
        var (entity, cinematic) = CreateCinematic(Vector3.Zero);
        cinematic.Waypoints.Add(Waypoint(new Vector3(10, 0, 0), duration: 1f));
        cinematic.Play();
        cinematic.Advance(1f); // completes at (10,0,0)

        var result = cinematic.Play();

        Assert.True(result);
        Assert.True(cinematic.IsPlaying);
    }

    private static void AssertApproximatelyEqual(Vector3 expected, Vector3 actual, float tolerance = 1e-3f)
    {
        Assert.True((expected - actual).Length() < tolerance, $"Expected approximately {expected} but got {actual}.");
    }
}
