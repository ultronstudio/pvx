using PVX.World;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace PVX.World.Tests;

public class DrawerTests
{
    private static (Entity Entity, PVXDrawerComponent Drawer) CreateDrawer(Quaternion? initialRotation = null)
    {
        var entity = new Entity("Drawer");
        if (initialRotation is { } rotation)
        {
            entity.Transform.Rotation = rotation;
        }

        var drawer = new PVXDrawerComponent();
        entity.Add(drawer);
        return (entity, drawer);
    }

    [Fact]
    public void Drawer_DefaultState_IsClosed()
    {
        var (_, drawer) = CreateDrawer();

        Assert.Equal(PVXDrawerState.Closed, drawer.State);
    }

    [Fact]
    public void Drawer_Closed_OffersOpen()
    {
        var (_, drawer) = CreateDrawer();

        var descriptor = Assert.Single(drawer.GetActions(default));
        Assert.Equal(PVXDrawerComponent.OpenActionId, descriptor.ActionId);
        Assert.True(descriptor.Availability.IsAvailable);
    }

    [Fact]
    public void Drawer_Open_OffersClose()
    {
        var (_, drawer) = CreateDrawer();
        drawer.TryOpen();
        drawer.Advance(10f); // large delta: finish instantly regardless of duration

        var descriptor = Assert.Single(drawer.GetActions(default));
        Assert.Equal(PVXDrawerComponent.CloseActionId, descriptor.ActionId);
        Assert.True(descriptor.Availability.IsAvailable);
    }

    [Fact]
    public void Drawer_OpenTransition_ReachesOpen()
    {
        var (_, drawer) = CreateDrawer();
        drawer.TransitionDuration = 1f;
        drawer.TryOpen();

        drawer.Advance(1f);

        Assert.Equal(PVXDrawerState.Open, drawer.State);
    }

    [Fact]
    public void Drawer_CloseTransition_ReachesClosed()
    {
        var (_, drawer) = CreateDrawer();
        drawer.TransitionDuration = 1f;
        drawer.TryOpen();
        drawer.Advance(1f);

        drawer.TryClose();
        drawer.Advance(1f);

        Assert.Equal(PVXDrawerState.Closed, drawer.State);
    }

    [Fact]
    public void Drawer_OpenTransition_SlidesAlongDefaultAxis()
    {
        var (entity, drawer) = CreateDrawer();
        drawer.OpenDistance = 0.4f;
        drawer.TransitionDuration = 1f;
        var initialPosition = entity.Transform.Position;

        drawer.TryOpen();
        drawer.Advance(1f);

        var expected = initialPosition + new Vector3(0f, 0f, -0.4f); // default axis is local -Z
        AssertApproximatelyEqual(expected, entity.Transform.Position);
    }

    [Fact]
    public void Drawer_Transition_DoesNotModifyRotation()
    {
        var initial = Quaternion.RotationAxis(Vector3.UnitY, MathUtil.DegreesToRadians(20f));
        var (entity, drawer) = CreateDrawer(initial);
        drawer.TransitionDuration = 1f;

        drawer.TryOpen();
        drawer.Advance(0.5f);

        AssertApproximatelyEqual(initial, entity.Transform.Rotation);
    }

    [Fact]
    public void Drawer_Transition_DoesNotModifyScale()
    {
        var (entity, drawer) = CreateDrawer();
        entity.Transform.Scale = new Vector3(2, 2, 2);
        drawer.TransitionDuration = 1f;

        drawer.TryOpen();
        drawer.Advance(0.5f);

        Assert.Equal(new Vector3(2, 2, 2), entity.Transform.Scale);
    }

    [Fact]
    public void Drawer_Opening_IsBusy()
    {
        var (_, drawer) = CreateDrawer();
        drawer.TransitionDuration = 1f;
        drawer.TryOpen();

        var descriptor = Assert.Single(drawer.GetActions(default));
        Assert.False(descriptor.Availability.IsAvailable);
        Assert.Equal("busy", descriptor.Availability.Reason);
    }

    [Fact]
    public void Drawer_OpenWhileOpening_DoesNotStartSecondTransition()
    {
        var (_, drawer) = CreateDrawer();
        drawer.TransitionDuration = 1f;
        drawer.TryOpen();

        var result = drawer.TryOpen();

        Assert.False(result);
        Assert.Equal(PVXDrawerState.Opening, drawer.State);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void Drawer_InvalidDuration_TransitionsImmediately(float duration)
    {
        var (_, drawer) = CreateDrawer();
        drawer.TransitionDuration = duration;

        drawer.TryOpen();
        drawer.Advance(0f);

        Assert.Equal(PVXDrawerState.Open, drawer.State);
    }

    [Fact]
    public void Drawer_StateChanged_RaisedOnlyForActualStateChanges()
    {
        var (_, drawer) = CreateDrawer();
        drawer.TransitionDuration = 1f;
        var transitions = new List<(PVXDrawerState Previous, PVXDrawerState Current)>();
        drawer.StateChanged += (_, e) => transitions.Add((e.Previous, e.Current));

        drawer.TryOpen();
        drawer.Advance(1f);
        var redundant = drawer.TryOpen(); // already Open: must not raise again

        Assert.False(redundant);
        Assert.Equal(
            [(PVXDrawerState.Closed, PVXDrawerState.Opening), (PVXDrawerState.Opening, PVXDrawerState.Open)],
            transitions);
    }

    [Fact]
    public void Drawer_StartsOpen_InitialStateIsOpen_WithoutRaisingStateChanged()
    {
        var entity = new Entity("Drawer");
        var drawer = new PVXDrawerComponent { StartsOpen = true };
        var raised = false;
        drawer.StateChanged += (_, _) => raised = true;

        entity.Add(drawer);

        Assert.Equal(PVXDrawerState.Open, drawer.State);
        Assert.False(raised);
    }

    private static void AssertApproximatelyEqual(Vector3 expected, Vector3 actual, float tolerance = 1e-4f)
    {
        Assert.True((expected - actual).Length() < tolerance, $"Expected approximately {expected} but got {actual}.");
    }

    private static void AssertApproximatelyEqual(Quaternion expected, Quaternion actual, float tolerance = 1e-4f)
    {
        var dot = Quaternion.Dot(expected, actual);
        Assert.True(Math.Abs(Math.Abs(dot) - 1f) < tolerance, $"Expected approximately {expected} but got {actual}.");
    }
}
