using PVX.Gameplay;
using Xunit;

namespace PVX.Gameplay.Tests;

public class PlayerContextTests
{
    [Fact]
    public void NoProvider_PrimaryPlayerIsNull()
    {
        var context = new PVXPlayerContext();

        Assert.Null(context.PrimaryPlayer);
    }

    [Fact]
    public void RegisterProvider_ResolvesPrimaryPlayer()
    {
        var context = new PVXPlayerContext();
        var player = new FakePlayer();
        var provider = new FakePlayerProvider { Player = player };

        context.RegisterProvider(provider);

        Assert.Same(player, context.PrimaryPlayer);
    }

    [Fact]
    public void RegisterProvider_RaisesPrimaryPlayerChanged_WhenPlayerActuallyChanges()
    {
        var context = new PVXPlayerContext();
        var player = new FakePlayer();
        var provider = new FakePlayerProvider { Player = player };
        PVXPrimaryPlayerChangedEventArgs? args = null;
        context.PrimaryPlayerChanged += (_, e) => args = e;

        context.RegisterProvider(provider);

        Assert.NotNull(args);
        Assert.Null(args!.Previous);
        Assert.Same(player, args.Current);
    }

    [Fact]
    public void Refresh_WhenProviderReturnsDifferentPlayer_UpdatesAndRaisesEvent()
    {
        var context = new PVXPlayerContext();
        var playerA = new FakePlayer();
        var playerB = new FakePlayer();
        var provider = new FakePlayerProvider { Player = playerA };
        context.RegisterProvider(provider);

        var raiseCount = 0;
        context.PrimaryPlayerChanged += (_, _) => raiseCount++;
        provider.Player = playerB;
        context.Refresh();

        Assert.Same(playerB, context.PrimaryPlayer);
        Assert.Equal(1, raiseCount);
    }

    [Fact]
    public void Refresh_WhenProviderReturnsSamePlayer_DoesNotRaiseEvent()
    {
        var context = new PVXPlayerContext();
        var player = new FakePlayer();
        var provider = new FakePlayerProvider { Player = player };
        context.RegisterProvider(provider);

        var raiseCount = 0;
        context.PrimaryPlayerChanged += (_, _) => raiseCount++;
        context.Refresh();

        Assert.Equal(0, raiseCount);
    }

    [Fact]
    public void Refresh_WhenProviderReturnsNull_ClearsPrimaryPlayerAndRaisesEvent()
    {
        var context = new PVXPlayerContext();
        var provider = new FakePlayerProvider { Player = new FakePlayer() };
        context.RegisterProvider(provider);

        var raiseCount = 0;
        context.PrimaryPlayerChanged += (_, _) => raiseCount++;
        provider.Player = null;
        context.Refresh();

        Assert.Null(context.PrimaryPlayer);
        Assert.Equal(1, raiseCount);
    }

    [Fact]
    public void UnregisterProvider_ClearsPrimaryPlayer()
    {
        var context = new PVXPlayerContext();
        var provider = new FakePlayerProvider { Player = new FakePlayer() };
        context.RegisterProvider(provider);

        context.UnregisterProvider();

        Assert.Null(context.PrimaryPlayer);
    }

    [Fact]
    public void UnregisterProvider_WhenAlreadyNull_DoesNotRaiseEvent()
    {
        var context = new PVXPlayerContext();
        var raiseCount = 0;
        context.PrimaryPlayerChanged += (_, _) => raiseCount++;

        context.UnregisterProvider();

        Assert.Equal(0, raiseCount);
    }
}
