using PVX.Interaction;
using Xunit;

namespace PVX.Interaction.Tests;

public class ValueObjectsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PVXInteractionActionId_EmptyOrWhitespace_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new PVXInteractionActionId(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PVXInteractionPoint_EmptyOrWhitespace_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new PVXInteractionPoint(value));
    }

    [Fact]
    public void PVXInteractionActionId_SameValue_AreEqual()
    {
        var a = new PVXInteractionActionId("open");
        var b = new PVXInteractionActionId("open");

        Assert.Equal(a, b);
    }

    [Fact]
    public void PVXInteractionAvailability_Available_IsAvailable()
    {
        Assert.True(PVXInteractionAvailability.Available.IsAvailable);
        Assert.Null(PVXInteractionAvailability.Available.Reason);
    }

    [Fact]
    public void PVXInteractionAvailability_Unavailable_CarriesReason()
    {
        var unavailable = PVXInteractionAvailability.Unavailable("locked");

        Assert.False(unavailable.IsAvailable);
        Assert.Equal("locked", unavailable.Reason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PVXInteractionAvailability_Unavailable_EmptyReason_Throws(string reason)
    {
        Assert.Throws<ArgumentException>(() => PVXInteractionAvailability.Unavailable(reason));
    }

    [Fact]
    public void Descriptor_DefaultActionId_Throws()
    {
        // default(PVXInteractionActionId) bypasses the id's own constructor
        // validation (its Value is null), so the descriptor boundary must
        // reject it too.
        var action = new FakeAction();

        Assert.Throws<ArgumentException>(() =>
            new PVXInteractionDescriptor(default, action));
    }

    [Fact]
    public void Descriptor_NullAction_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PVXInteractionDescriptor(new PVXInteractionActionId("open"), null!));
    }

    [Fact]
    public void Descriptor_HoldMode_WithoutHoldDuration_Throws()
    {
        var action = new FakeAction();

        Assert.Throws<ArgumentException>(() =>
            new PVXInteractionDescriptor(new PVXInteractionActionId("hold"), action, mode: PVXInteractionMode.Hold));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Descriptor_HoldMode_WithNonPositiveHoldDuration_Throws(int seconds)
    {
        var action = new FakeAction();

        Assert.Throws<ArgumentException>(() =>
            new PVXInteractionDescriptor(
                new PVXInteractionActionId("hold"),
                action,
                mode: PVXInteractionMode.Hold,
                holdDuration: TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void Descriptor_InstantMode_IgnoresHoldDuration()
    {
        var action = new FakeAction();

        var descriptor = new PVXInteractionDescriptor(
            new PVXInteractionActionId("open"),
            action,
            mode: PVXInteractionMode.Instant,
            holdDuration: TimeSpan.FromSeconds(5));

        Assert.Null(descriptor.HoldDuration);
    }

    [Fact]
    public void Descriptor_DefaultsToAvailable()
    {
        var descriptor = new PVXInteractionDescriptor(new PVXInteractionActionId("open"), new FakeAction());

        Assert.True(descriptor.Availability.IsAvailable);
    }
}
