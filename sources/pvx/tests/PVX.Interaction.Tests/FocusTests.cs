using PVX.Interaction;
using Xunit;

namespace PVX.Interaction.Tests;

public class FocusTests
{
    [Fact]
    public void NoCandidates_NoFocus()
    {
        var (session, _) = TestSessions.CreateActive();
        var service = new PVXInteractionService(session) { TargetProvider = new FakeTargetProvider() };

        service.Update(new FakeInteractionSource(), TimeSpan.Zero);

        Assert.Null(service.CurrentFocus);
    }

    [Fact]
    public void OneCandidate_BecomesFocus()
    {
        var (session, _) = TestSessions.CreateActive();
        var provider = new FakeTargetProvider();
        var target = new FakeInteractable();
        provider.Candidates.Add(new PVXInteractionCandidate(target));
        var service = new PVXInteractionService(session) { TargetProvider = provider };

        service.Update(new FakeInteractionSource(), TimeSpan.Zero);

        Assert.NotNull(service.CurrentFocus);
        Assert.Same(target, service.CurrentFocus!.Value.Target);
    }

    [Fact]
    public void CandidateChanges_RaisesFocusChanged()
    {
        var (session, _) = TestSessions.CreateActive();
        var provider = new FakeTargetProvider();
        var targetA = new FakeInteractable();
        provider.Candidates.Add(new PVXInteractionCandidate(targetA));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);

        var raiseCount = 0;
        PVXFocusChangedEventArgs? lastArgs = null;
        service.FocusChanged += (_, e) => { raiseCount++; lastArgs = e; };

        var targetB = new FakeInteractable();
        provider.Candidates.Clear();
        provider.Candidates.Add(new PVXInteractionCandidate(targetB));
        service.Update(source, TimeSpan.Zero);

        Assert.Equal(1, raiseCount);
        Assert.Same(targetA, lastArgs!.Previous!.Value.Target);
        Assert.Same(targetB, lastArgs.Current!.Value.Target);
        Assert.Same(targetB, service.CurrentFocus!.Value.Target);
    }

    [Fact]
    public void SameCandidateRepeatedly_DoesNotRaiseDuplicateFocusChanged()
    {
        var (session, _) = TestSessions.CreateActive();
        var provider = new FakeTargetProvider();
        var target = new FakeInteractable();
        provider.Candidates.Add(new PVXInteractionCandidate(target));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);

        var raiseCount = 0;
        service.FocusChanged += (_, _) => raiseCount++;

        service.Update(source, TimeSpan.Zero);
        service.Update(source, TimeSpan.Zero);

        Assert.Equal(0, raiseCount);
    }

    [Fact]
    public void CandidateDisappears_FocusCleared_AndRaisesFocusChanged()
    {
        var (session, _) = TestSessions.CreateActive();
        var provider = new FakeTargetProvider();
        var target = new FakeInteractable();
        provider.Candidates.Add(new PVXInteractionCandidate(target));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);

        var raiseCount = 0;
        service.FocusChanged += (_, _) => raiseCount++;

        provider.Candidates.Clear();
        service.Update(source, TimeSpan.Zero);

        Assert.Null(service.CurrentFocus);
        Assert.Equal(1, raiseCount);
    }

    [Fact]
    public void MultipleCandidates_HigherPriorityWinsFocus()
    {
        var (session, _) = TestSessions.CreateActive();
        var provider = new FakeTargetProvider();
        var low = new FakeInteractable();
        var high = new FakeInteractable();
        provider.Candidates.Add(new PVXInteractionCandidate(low, priority: 1));
        provider.Candidates.Add(new PVXInteractionCandidate(high, priority: 5));
        var service = new PVXInteractionService(session) { TargetProvider = provider };

        service.Update(new FakeInteractionSource(), TimeSpan.Zero);

        Assert.Same(high, service.CurrentFocus!.Value.Target);
    }

    [Fact]
    public void TiePriority_FirstProviderOrderWins()
    {
        var (session, _) = TestSessions.CreateActive();
        var provider = new FakeTargetProvider();
        var first = new FakeInteractable();
        var second = new FakeInteractable();
        provider.Candidates.Add(new PVXInteractionCandidate(first, priority: 3));
        provider.Candidates.Add(new PVXInteractionCandidate(second, priority: 3));
        var service = new PVXInteractionService(session) { TargetProvider = provider };

        service.Update(new FakeInteractionSource(), TimeSpan.Zero);

        Assert.Same(first, service.CurrentFocus!.Value.Target);
    }

    [Fact]
    public void DifferentPointOnSameTarget_CountsAsFocusChange()
    {
        var (session, _) = TestSessions.CreateActive();
        var provider = new FakeTargetProvider();
        var target = new FakeInteractable();
        var bottom = new PVXInteractionPoint("BottomEntry");
        var top = new PVXInteractionPoint("TopEntry");
        provider.Candidates.Add(new PVXInteractionCandidate(target, bottom));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);

        var raiseCount = 0;
        service.FocusChanged += (_, _) => raiseCount++;

        provider.Candidates.Clear();
        provider.Candidates.Add(new PVXInteractionCandidate(target, top));
        service.Update(source, TimeSpan.Zero);

        Assert.Equal(1, raiseCount);
        Assert.Equal(top, service.CurrentFocus!.Value.Point);
    }
}
