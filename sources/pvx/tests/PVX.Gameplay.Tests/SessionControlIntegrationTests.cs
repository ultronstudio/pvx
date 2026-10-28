using PVX.Gameplay;
using Xunit;

namespace PVX.Gameplay.Tests;

public class SessionControlIntegrationTests
{
    [Fact]
    public void Pause_WithNoRestrictions_EffectiveCapabilitiesBecomeNone()
    {
        var session = new PVXGameplaySession();
        session.Start();

        session.Pause();

        Assert.Equal(PVXControlCapabilities.None, session.Control.EffectiveCapabilities);
    }

    [Fact]
    public void Pause_WithActiveRestriction_EffectiveCapabilitiesBecomeNone()
    {
        var session = new PVXGameplaySession();
        session.Start();
        using var restriction = session.Control.AcquireRestriction(PVXControlCapabilities.Look);

        session.Pause();

        Assert.Equal(PVXControlCapabilities.None, session.Control.EffectiveCapabilities);
    }

    [Fact]
    public void Resume_RestoresCapabilitiesStillAllowedByOtherRestrictions()
    {
        // base = All; restriction = Look; Pause() -> None; Resume() -> Look (not All).
        var session = new PVXGameplaySession();
        session.Start();
        using var restriction = session.Control.AcquireRestriction(PVXControlCapabilities.Look);

        session.Pause();
        Assert.Equal(PVXControlCapabilities.None, session.Control.EffectiveCapabilities);

        session.Resume();
        Assert.Equal(PVXControlCapabilities.Look, session.Control.EffectiveCapabilities);
    }

    [Fact]
    public void Resume_WithNoOtherRestrictions_RestoresFullCapabilities()
    {
        var session = new PVXGameplaySession();
        session.Start();

        session.Pause();
        session.Resume();

        Assert.Equal(PVXControlCapabilities.All, session.Control.EffectiveCapabilities);
    }
}
