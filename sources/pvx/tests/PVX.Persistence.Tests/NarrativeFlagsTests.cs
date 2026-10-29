using PVX.Persistence;
using Xunit;

namespace PVX.Persistence.Tests;

public class NarrativeFlagsTests
{
    [Fact]
    public void IsSet_UnsetFlag_ReturnsFalse()
    {
        var flags = new PVXNarrativeFlags(new PVXSaveStore());

        Assert.False(flags.IsSet("met_bob"));
    }

    [Fact]
    public void Set_ThenIsSet_ReturnsTrue()
    {
        var flags = new PVXNarrativeFlags(new PVXSaveStore());

        flags.Set("met_bob");

        Assert.True(flags.IsSet("met_bob"));
    }

    [Fact]
    public void Unset_ClearsFlag()
    {
        var flags = new PVXNarrativeFlags(new PVXSaveStore());
        flags.Set("met_bob");

        flags.Unset("met_bob");

        Assert.False(flags.IsSet("met_bob"));
    }

    [Fact]
    public void FlagChanged_RaisedOnlyOnActualChange()
    {
        var flags = new PVXNarrativeFlags(new PVXSaveStore());
        var transitions = new List<(string Flag, bool Previous, bool Current)>();
        flags.FlagChanged += (_, e) => transitions.Add((e.FlagName, e.Previous, e.Current));

        flags.Set("met_bob");
        flags.Set("met_bob"); // already set: must not raise again
        flags.Unset("met_bob");

        Assert.Equal(
            [("met_bob", false, true), ("met_bob", true, false)],
            transitions);
    }

    [Fact]
    public void FlagsShareUnderlyingStore_SoFlagsPersistWithEverythingElse()
    {
        var store = new PVXSaveStore();
        var flags = new PVXNarrativeFlags(store);
        flags.Set("found_key");

        var json = store.Serialize();
        var restoredStore = new PVXSaveStore();
        restoredStore.Deserialize(json);
        var restoredFlags = new PVXNarrativeFlags(restoredStore);

        Assert.True(restoredFlags.IsSet("found_key"));
    }

    [Fact]
    public void Dispose_StopsReactingToStoreChanges()
    {
        var store = new PVXSaveStore();
        var flags = new PVXNarrativeFlags(store);
        var raised = false;
        flags.FlagChanged += (_, _) => raised = true;

        flags.Dispose();
        flags.Set("met_bob");

        Assert.False(raised);
    }

    [Fact]
    public void UnrelatedStoreKeys_DoNotRaiseFlagChanged()
    {
        var store = new PVXSaveStore();
        var flags = new PVXNarrativeFlags(store);
        var raised = false;
        flags.FlagChanged += (_, _) => raised = true;

        store.SetInt("unrelated.counter", 5);

        Assert.False(raised);
    }
}
