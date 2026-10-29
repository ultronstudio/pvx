using PVX.World;
using Stride.Engine;
using Xunit;

namespace PVX.World.Tests;

public class NpcTests
{
    private static (Entity Entity, PVXNpcComponent Npc) CreateNpc()
    {
        var entity = new Entity("Npc");
        var npc = new PVXNpcComponent();
        entity.Add(npc);
        return (entity, npc);
    }

    [Fact]
    public void Npc_DefaultState_IsIdle()
    {
        var (_, npc) = CreateNpc();

        Assert.Equal(PVXNpcState.Idle, npc.State);
    }

    [Fact]
    public void Npc_SetAlertTrue_BecomesAlert()
    {
        var (_, npc) = CreateNpc();

        npc.SetAlert(true);

        Assert.Equal(PVXNpcState.Alert, npc.State);
    }

    [Fact]
    public void Npc_SetAlertFalse_AfterAlert_BecomesIdle()
    {
        var (_, npc) = CreateNpc();
        npc.SetAlert(true);

        npc.SetAlert(false);

        Assert.Equal(PVXNpcState.Idle, npc.State);
    }

    [Fact]
    public void Npc_StateChanged_FiresOncePerActualChange()
    {
        var (_, npc) = CreateNpc();
        var transitions = new List<(PVXNpcState Previous, PVXNpcState Current)>();
        npc.StateChanged += (_, e) => transitions.Add((e.Previous, e.Current));

        npc.SetAlert(true);
        npc.SetAlert(true); // already Alert: no-op
        npc.SetAlert(false);

        Assert.Equal(
            [(PVXNpcState.Idle, PVXNpcState.Alert), (PVXNpcState.Alert, PVXNpcState.Idle)],
            transitions);
    }

    [Fact]
    public void Npc_StartsAlert_InitialStateIsAlert_WithoutRaisingStateChanged()
    {
        var entity = new Entity("Npc");
        var npc = new PVXNpcComponent { StartsAlert = true };
        var raised = false;
        npc.StateChanged += (_, _) => raised = true;

        entity.Add(npc);

        Assert.Equal(PVXNpcState.Alert, npc.State);
        Assert.False(raised);
    }

    [Fact]
    public void Npc_DisplayName_DefaultsToEmpty()
    {
        var (_, npc) = CreateNpc();

        Assert.Equal(string.Empty, npc.DisplayName);
    }

    [Fact]
    public void Npc_DisplayName_RoundTrips()
    {
        var (_, npc) = CreateNpc();

        npc.DisplayName = "Bob";

        Assert.Equal("Bob", npc.DisplayName);
    }
}
