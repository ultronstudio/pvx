using Stride.Engine;
using Stride.Rendering;

namespace PVX.World.Playground;

/// <summary>
/// Sample-only visual feedback binder: turns an indicator light on/off to
/// follow a <see cref="PVXSwitchComponent"/>'s state, purely so a human
/// testing the playground can SEE the switch toggle. This kind of
/// switch→light binding deliberately lives here, not in <c>PVX.World</c>:
/// <see cref="PVXSwitchComponent"/> itself must stay ignorant of lights,
/// doors, or any other consumer of its <c>StateChanged</c> event.
/// </summary>
internal sealed class SwitchIndicatorScript : StartupScript
{
    private readonly PVXSwitchComponent switchComponent;
    private readonly LightComponent indicatorLight;

    public SwitchIndicatorScript(PVXSwitchComponent switchComponent, LightComponent indicatorLight)
    {
        this.switchComponent = switchComponent;
        this.indicatorLight = indicatorLight;
    }

    public override void Start()
    {
        ApplyState();
        switchComponent.StateChanged += (_, _) => ApplyState();
    }

    public override void Cancel()
    {
        // ScriptComponent's own teardown; nothing else to release here —
        // the event subscription dies with this script instance.
    }

    private void ApplyState()
    {
        indicatorLight.Intensity = switchComponent.State == PVXSwitchState.On ? 8f : 0f;
    }
}
