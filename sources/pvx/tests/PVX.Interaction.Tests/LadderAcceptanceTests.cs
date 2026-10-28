using PVX.Interaction;
using Xunit;

namespace PVX.Interaction.Tests;

/// <summary>
/// Ladder acceptance case (spec section 29): the ladder offers a different
/// primary action depending on which interaction point is focused, and
/// executing it only hands off to a fake traversal handler. PVX.Interaction
/// never implements the actual climbing/movement itself.
/// </summary>
public class LadderAcceptanceTests
{
    private sealed class LadderInteractable : IPVXInteractable
    {
        public static readonly PVXInteractionPoint BottomEntry = new("BottomEntry");
        public static readonly PVXInteractionPoint TopEntry = new("TopEntry");

        private readonly FakeTraversalHandler traversal;

        public LadderInteractable(FakeTraversalHandler traversal)
        {
            this.traversal = traversal;
        }

        public IReadOnlyList<PVXInteractionDescriptor> GetActions(PVXInteractionContext context)
        {
            if (context.Point == BottomEntry)
            {
                var climbUp = new FakeAction { OnExecute = _ => traversal.EnterLadder("Up") };
                return [new PVXInteractionDescriptor(new PVXInteractionActionId("climb_up"), climbUp, priority: 100)];
            }

            if (context.Point == TopEntry)
            {
                var climbDown = new FakeAction { OnExecute = _ => traversal.EnterLadder("Down") };
                return [new PVXInteractionDescriptor(new PVXInteractionActionId("climb_down"), climbDown, priority: 100)];
            }

            return Array.Empty<PVXInteractionDescriptor>();
        }
    }

    [Fact]
    public void BottomEntry_OffersClimbUp()
    {
        var traversal = new FakeTraversalHandler();
        var ladder = new LadderInteractable(traversal);
        var (session, _) = TestSessions.CreateActive();
        var provider = new FakeTargetProvider();
        provider.Candidates.Add(new PVXInteractionCandidate(ladder, LadderInteractable.BottomEntry));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);

        Assert.Equal("climb_up", service.PrimaryAction!.ActionId.Value);
    }

    [Fact]
    public void BottomEntry_Execute_DelegatesToFakeTraversalHandler_EnteringUp()
    {
        var traversal = new FakeTraversalHandler();
        var ladder = new LadderInteractable(traversal);
        var (session, _) = TestSessions.CreateActive();
        var provider = new FakeTargetProvider();
        provider.Candidates.Add(new PVXInteractionCandidate(ladder, LadderInteractable.BottomEntry));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);

        var result = service.BeginPrimaryInteraction(source);

        Assert.True(result);
        Assert.Equal(["Up"], traversal.EnteredLadders);
    }

    [Fact]
    public void TopEntry_OffersClimbDown_AndDelegatesEnteringDown()
    {
        var traversal = new FakeTraversalHandler();
        var ladder = new LadderInteractable(traversal);
        var (session, _) = TestSessions.CreateActive();
        var provider = new FakeTargetProvider();
        provider.Candidates.Add(new PVXInteractionCandidate(ladder, LadderInteractable.TopEntry));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);

        Assert.Equal("climb_down", service.PrimaryAction!.ActionId.Value);

        var result = service.BeginPrimaryInteraction(source);

        Assert.True(result);
        Assert.Equal(["Down"], traversal.EnteredLadders);
    }

    [Fact]
    public void MovingFromBottomToTopEntry_CancelsActiveHold_AndDoesNotAutoSwitchAction()
    {
        // A ladder with a Hold-mode climb to exercise focus-change
        // cancellation (spec section 24) on a target that itself matches
        // the ladder acceptance case, not just a generic fake.
        var bottom = LadderInteractable.BottomEntry;
        var top = LadderInteractable.TopEntry;
        var climbAction = new FakeAction();
        var holdLadder = new FakeInteractable
        {
            ActionsFactory = ctx => ctx.Point == bottom
                ? [new PVXInteractionDescriptor(new PVXInteractionActionId("climb_up"), climbAction, priority: 100, mode: PVXInteractionMode.Hold, holdDuration: TimeSpan.FromSeconds(2))]
                : Array.Empty<PVXInteractionDescriptor>()
        };

        var (session, _) = TestSessions.CreateActive();
        var provider = new FakeTargetProvider();
        provider.Candidates.Add(new PVXInteractionCandidate(holdLadder, bottom));
        var service = new PVXInteractionService(session) { TargetProvider = provider };
        var source = new FakeInteractionSource();
        service.Update(source, TimeSpan.Zero);
        service.BeginPrimaryInteraction(source);
        Assert.NotNull(service.ActiveInteraction);

        provider.Candidates.Clear();
        provider.Candidates.Add(new PVXInteractionCandidate(holdLadder, top));
        service.Update(source, TimeSpan.Zero);

        Assert.Null(service.ActiveInteraction);
        Assert.Equal(1, climbAction.CancelCount);
        Assert.Equal(0, climbAction.ExecuteCount);
    }
}
