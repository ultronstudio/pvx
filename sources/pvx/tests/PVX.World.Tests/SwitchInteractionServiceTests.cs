using PVX.Interaction;
using Stride.Engine;
using Xunit;

namespace PVX.World.Tests;

/// <summary>
/// Exercises <see cref="PVXSwitchComponent"/> through a real
/// <see cref="PVXInteractionService"/>, not direct <c>switchComponent.Toggle()</c> calls.
/// </summary>
public class SwitchInteractionServiceTests
{
    [Fact]
    public void FocusSwitch_PrimaryActionIsToggle_BeginTogglesOffToOn_AndBackToOff()
    {
        var (session, _) = TestSessions.CreateActive();
        var entity = new Entity("Switch");
        var switchComponent = new PVXSwitchComponent();
        entity.Add(switchComponent);

        var provider = new FakeTargetProvider();
        provider.Candidates.Add(new PVXInteractionCandidate(switchComponent));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();

        service.Update(source, TimeSpan.Zero);
        Assert.Equal(PVXSwitchComponent.ToggleActionId, service.PrimaryAction!.ActionId);

        var firstBegin = service.BeginPrimaryInteraction(source);
        Assert.True(firstBegin);
        Assert.Equal(PVXSwitchState.On, switchComponent.State);

        var secondBegin = service.BeginPrimaryInteraction(source);
        Assert.True(secondBegin);
        Assert.Equal(PVXSwitchState.Off, switchComponent.State);
    }
}
