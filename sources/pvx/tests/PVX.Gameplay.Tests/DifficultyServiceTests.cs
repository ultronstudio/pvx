using PVX.Gameplay;
using Xunit;

namespace PVX.Gameplay.Tests;

public class DifficultyServiceTests
{
    [Fact]
    public void InitiallyUnset_CurrentIsNull()
    {
        var service = new PVXDifficultyService();

        Assert.Null(service.Current);
    }

    [Fact]
    public void SetDifficulty_UpdatesCurrent_AndRaisesEvent()
    {
        var service = new PVXDifficultyService();
        var id = new PVXDifficultyId("sandbox.normal");
        PVXDifficultyChangedEventArgs? args = null;
        service.DifficultyChanged += (_, e) => args = e;

        service.SetDifficulty(id);

        Assert.Equal(id, service.Current);
        Assert.NotNull(args);
        Assert.Null(args!.Previous);
        Assert.Equal(id, args.Current);
    }

    [Fact]
    public void SetDifficulty_SameValueTwice_DoesNotRaiseEventSecondTime()
    {
        var service = new PVXDifficultyService();
        var id = new PVXDifficultyId("sandbox.normal");
        service.SetDifficulty(id);

        var raiseCount = 0;
        service.DifficultyChanged += (_, _) => raiseCount++;
        service.SetDifficulty(id);

        Assert.Equal(0, raiseCount);
    }

    [Fact]
    public void SetDifficulty_DifferentValue_RaisesEventWithPreviousAndCurrent()
    {
        var service = new PVXDifficultyService();
        var easy = new PVXDifficultyId("sandbox.easy");
        var hard = new PVXDifficultyId("sandbox.hard");
        service.SetDifficulty(easy);

        PVXDifficultyChangedEventArgs? args = null;
        service.DifficultyChanged += (_, e) => args = e;
        service.SetDifficulty(hard);

        Assert.Equal(hard, service.Current);
        Assert.NotNull(args);
        Assert.Equal(easy, args!.Previous);
        Assert.Equal(hard, args.Current);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PVXDifficultyId_EmptyOrWhitespace_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new PVXDifficultyId(value));
    }

    [Fact]
    public void SetDifficulty_DefaultId_Throws()
    {
        // default(PVXDifficultyId) bypasses the constructor's own
        // validation (its Value is null), so the service boundary must
        // reject it too.
        var service = new PVXDifficultyService();

        Assert.Throws<ArgumentException>(() => service.SetDifficulty(default));
        Assert.Null(service.Current);
    }

    [Fact]
    public void PVXDifficultyId_SameValue_AreEqual()
    {
        var a = new PVXDifficultyId("shadowless.standard");
        var b = new PVXDifficultyId("shadowless.standard");

        Assert.Equal(a, b);
    }
}
