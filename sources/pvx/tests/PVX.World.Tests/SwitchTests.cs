using PVX.World;
using Stride.Engine;
using Xunit;

namespace PVX.World.Tests;

public class SwitchTests
{
    private static (Entity Entity, PVXSwitchComponent Switch) CreateSwitch()
    {
        var entity = new Entity("Switch");
        var switchComponent = new PVXSwitchComponent();
        entity.Add(switchComponent);
        return (entity, switchComponent);
    }

    [Fact]
    public void Switch_DefaultState_Off()
    {
        var (_, switchComponent) = CreateSwitch();

        Assert.Equal(PVXSwitchState.Off, switchComponent.State);
    }

    [Fact]
    public void Switch_OffersToggle()
    {
        var (_, switchComponent) = CreateSwitch();

        var descriptor = Assert.Single(switchComponent.GetActions(default));
        Assert.Equal(PVXSwitchComponent.ToggleActionId, descriptor.ActionId);
        Assert.True(descriptor.Availability.IsAvailable);
    }

    [Fact]
    public void Switch_Toggle_OffToOn()
    {
        var (_, switchComponent) = CreateSwitch();

        switchComponent.Toggle();

        Assert.Equal(PVXSwitchState.On, switchComponent.State);
    }

    [Fact]
    public void Switch_Toggle_OnToOff()
    {
        var (_, switchComponent) = CreateSwitch();
        switchComponent.Toggle();

        switchComponent.Toggle();

        Assert.Equal(PVXSwitchState.Off, switchComponent.State);
    }

    [Fact]
    public void Switch_SetState_IsIdempotent()
    {
        var (_, switchComponent) = CreateSwitch();
        switchComponent.SetState(PVXSwitchState.On);
        var raiseCount = 0;
        switchComponent.StateChanged += (_, _) => raiseCount++;

        switchComponent.SetState(PVXSwitchState.On);

        Assert.Equal(0, raiseCount);
        Assert.Equal(PVXSwitchState.On, switchComponent.State);
    }

    [Fact]
    public void Switch_StateChanged_FiresOncePerActualChange()
    {
        var (_, switchComponent) = CreateSwitch();
        var transitions = new List<(PVXSwitchState Previous, PVXSwitchState Current)>();
        switchComponent.StateChanged += (_, e) => transitions.Add((e.Previous, e.Current));

        switchComponent.Toggle();
        switchComponent.Toggle();
        switchComponent.SetState(PVXSwitchState.Off); // already Off: no-op

        Assert.Equal(
            [(PVXSwitchState.Off, PVXSwitchState.On), (PVXSwitchState.On, PVXSwitchState.Off)],
            transitions);
    }

    [Fact]
    public void Switch_StartsOn_InitialStateIsOn_WithoutRaisingStateChanged()
    {
        var entity = new Entity("Switch");
        var switchComponent = new PVXSwitchComponent { StartsOn = true };
        var raised = false;
        switchComponent.StateChanged += (_, _) => raised = true;

        entity.Add(switchComponent);

        Assert.Equal(PVXSwitchState.On, switchComponent.State);
        Assert.False(raised);
    }

    [Fact]
    public void Switch_Label_ReflectsState()
    {
        var (_, switchComponent) = CreateSwitch();

        Assert.Equal("Turn On", switchComponent.GetActions(default)[0].Label);

        switchComponent.Toggle();

        Assert.Equal("Turn Off", switchComponent.GetActions(default)[0].Label);
    }
}
