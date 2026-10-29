using PVX.Persistence;
using Xunit;

namespace PVX.Persistence.Tests;

public class SaveStoreTests
{
    [Fact]
    public void GetString_UnsetKey_ReturnsDefault()
    {
        var store = new PVXSaveStore();

        Assert.Null(store.GetString("missing"));
        Assert.Equal("fallback", store.GetString("missing", "fallback"));
    }

    [Fact]
    public void SetString_ThenGetString_RoundTrips()
    {
        var store = new PVXSaveStore();

        store.SetString("name", "Shadowless");

        Assert.Equal("Shadowless", store.GetString("name"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetBool_ThenGetBool_RoundTrips(bool value)
    {
        var store = new PVXSaveStore();

        store.SetBool("flag", value);

        Assert.Equal(value, store.GetBool("flag"));
    }

    [Fact]
    public void GetBool_UnsetKey_ReturnsDefault()
    {
        var store = new PVXSaveStore();

        Assert.False(store.GetBool("missing"));
        Assert.True(store.GetBool("missing", true));
    }

    [Fact]
    public void SetInt_ThenGetInt_RoundTrips()
    {
        var store = new PVXSaveStore();

        store.SetInt("count", 42);

        Assert.Equal(42, store.GetInt("count"));
    }

    [Fact]
    public void SetFloat_ThenGetFloat_RoundTrips()
    {
        var store = new PVXSaveStore();

        store.SetFloat("ratio", 3.5f);

        Assert.Equal(3.5f, store.GetFloat("ratio"));
    }

    [Fact]
    public void HasKey_ReflectsPresence()
    {
        var store = new PVXSaveStore();

        Assert.False(store.HasKey("key"));

        store.SetString("key", "value");

        Assert.True(store.HasKey("key"));
    }

    [Fact]
    public void Remove_ExistingKey_RemovesIt()
    {
        var store = new PVXSaveStore();
        store.SetString("key", "value");

        var result = store.Remove("key");

        Assert.True(result);
        Assert.False(store.HasKey("key"));
    }

    [Fact]
    public void Remove_MissingKey_ReturnsFalse()
    {
        var store = new PVXSaveStore();

        Assert.False(store.Remove("missing"));
    }

    [Fact]
    public void Clear_RemovesEveryKey()
    {
        var store = new PVXSaveStore();
        store.SetString("a", "1");
        store.SetString("b", "2");

        store.Clear();

        Assert.False(store.HasKey("a"));
        Assert.False(store.HasKey("b"));
    }

    [Fact]
    public void ValueChanged_RaisedOnNewValue()
    {
        var store = new PVXSaveStore();
        PVXSaveValueChangedEventArgs? captured = null;
        store.ValueChanged += (_, e) => captured = e;

        store.SetString("key", "value");

        Assert.NotNull(captured);
        Assert.Equal("key", captured!.Key);
        Assert.Null(captured.Previous);
        Assert.Equal("value", captured.Current);
    }

    [Fact]
    public void ValueChanged_NotRaisedForNoOpSet()
    {
        var store = new PVXSaveStore();
        store.SetString("key", "value");
        var raiseCount = 0;
        store.ValueChanged += (_, _) => raiseCount++;

        store.SetString("key", "value");

        Assert.Equal(0, raiseCount);
    }

    [Fact]
    public void ValueChanged_RaisedOnRemove()
    {
        var store = new PVXSaveStore();
        store.SetString("key", "value");
        PVXSaveValueChangedEventArgs? captured = null;
        store.ValueChanged += (_, e) => captured = e;

        store.Remove("key");

        Assert.NotNull(captured);
        Assert.Equal("value", captured!.Previous);
        Assert.Null(captured.Current);
    }

    [Fact]
    public void Serialize_ThenDeserialize_RoundTrips()
    {
        var store = new PVXSaveStore();
        store.SetString("name", "Shadowless");
        store.SetBool("flag", true);
        store.SetInt("count", 7);

        var json = store.Serialize();

        var restored = new PVXSaveStore();
        restored.Deserialize(json);

        Assert.Equal("Shadowless", restored.GetString("name"));
        Assert.True(restored.GetBool("flag"));
        Assert.Equal(7, restored.GetInt("count"));
    }

    [Fact]
    public void Deserialize_RemovesKeysAbsentFromIncomingData()
    {
        var store = new PVXSaveStore();
        store.SetString("stale", "value");

        var empty = new PVXSaveStore().Serialize();
        store.Deserialize(empty);

        Assert.False(store.HasKey("stale"));
    }
}
