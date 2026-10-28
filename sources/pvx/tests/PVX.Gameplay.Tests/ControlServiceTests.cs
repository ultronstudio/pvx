using PVX.Gameplay;
using Xunit;

namespace PVX.Gameplay.Tests;

public class ControlServiceTests
{
    [Fact]
    public void Default_EffectiveCapabilitiesIsAll()
    {
        var control = new PVXControlService();

        Assert.Equal(PVXControlCapabilities.All, control.EffectiveCapabilities);
        Assert.Equal(PVXControlMode.Full, control.Mode);
    }

    [Fact]
    public void SingleRestriction_LimitsEffectiveCapabilities()
    {
        var control = new PVXControlService();

        using var restriction = control.AcquireRestriction(PVXControlCapabilities.Look);

        Assert.Equal(PVXControlCapabilities.Look, control.EffectiveCapabilities);
        Assert.Equal(PVXControlMode.Limited, control.Mode);
    }

    [Fact]
    public void OverlappingRestrictions_IntersectCapabilities_AndRestoreCorrectlyOnRemoval_AtoB()
    {
        var control = new PVXControlService();

        var a = control.AcquireRestriction(PVXControlCapabilities.Look | PVXControlCapabilities.Interact);
        var b = control.AcquireRestriction(PVXControlCapabilities.Look);

        Assert.Equal(PVXControlCapabilities.Look, control.EffectiveCapabilities);

        a.Dispose();
        Assert.Equal(PVXControlCapabilities.Look, control.EffectiveCapabilities);

        b.Dispose();
        Assert.Equal(PVXControlCapabilities.All, control.EffectiveCapabilities);
    }

    [Fact]
    public void OverlappingRestrictions_IntersectCapabilities_AndRestoreCorrectlyOnRemoval_BtoA()
    {
        var control = new PVXControlService();

        var a = control.AcquireRestriction(PVXControlCapabilities.Look | PVXControlCapabilities.Interact);
        var b = control.AcquireRestriction(PVXControlCapabilities.Look);

        Assert.Equal(PVXControlCapabilities.Look, control.EffectiveCapabilities);

        b.Dispose();
        Assert.Equal(PVXControlCapabilities.Look | PVXControlCapabilities.Interact, control.EffectiveCapabilities);

        a.Dispose();
        Assert.Equal(PVXControlCapabilities.All, control.EffectiveCapabilities);
    }

    [Fact]
    public void Dispose_Twice_IsSafe()
    {
        var control = new PVXControlService();
        var restriction = control.AcquireRestriction(PVXControlCapabilities.Look);

        restriction.Dispose();
        restriction.Dispose();

        Assert.Equal(PVXControlCapabilities.All, control.EffectiveCapabilities);
    }

    [Fact]
    public void Dispose_DoesNotCorruptUnrelatedRestrictions()
    {
        var control = new PVXControlService();
        var a = control.AcquireRestriction(PVXControlCapabilities.Look);
        var b = control.AcquireRestriction(PVXControlCapabilities.Move);

        a.Dispose();
        a.Dispose();

        Assert.Equal(PVXControlCapabilities.Move, control.EffectiveCapabilities);

        b.Dispose();
        Assert.Equal(PVXControlCapabilities.All, control.EffectiveCapabilities);
    }

    [Fact]
    public void CapabilitiesChanged_FiresOnlyWhenEffectiveValueChanges()
    {
        var control = new PVXControlService();
        var raiseCount = 0;
        control.CapabilitiesChanged += (_, _) => raiseCount++;

        using var noop = control.AcquireRestriction(PVXControlCapabilities.All);
        Assert.Equal(0, raiseCount);

        using var restricting = control.AcquireRestriction(PVXControlCapabilities.Look);
        Assert.Equal(1, raiseCount);
    }

    [Fact]
    public void Mode_None_IsDisabled()
    {
        var control = new PVXControlService();
        using var restriction = control.AcquireRestriction(PVXControlCapabilities.None);

        Assert.Equal(PVXControlMode.Disabled, control.Mode);
    }
}
