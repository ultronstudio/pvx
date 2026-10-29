using PVX.Dialogue;
using Stride.Engine;
using Xunit;

namespace PVX.Dialogue.Tests;

public class DialogueComponentTests
{
    private static PVXDialogueLine Line(string speaker, string text) => new() { Speaker = speaker, Text = text };

    private static (Entity Entity, PVXDialogueComponent Dialogue) CreateDialogue(params PVXDialogueLine[] lines)
    {
        var entity = new Entity("NPC");
        var dialogue = new PVXDialogueComponent { Lines = lines.ToList() };
        entity.Add(dialogue);
        return (entity, dialogue);
    }

    [Fact]
    public void NoLines_OffersNoActions()
    {
        var (_, dialogue) = CreateDialogue();

        Assert.Empty(dialogue.GetActions(default));
    }

    [Fact]
    public void WithLines_OffersTalk()
    {
        var (_, dialogue) = CreateDialogue(Line("Bob", "Hello."));

        var descriptor = Assert.Single(dialogue.GetActions(default));
        Assert.Equal(PVXDialogueComponent.TalkActionId, descriptor.ActionId);
        Assert.Equal("Talk", descriptor.Label);
        Assert.True(descriptor.Availability.IsAvailable);
    }

    [Fact]
    public void WhileActive_OffersContinue()
    {
        var (_, dialogue) = CreateDialogue(Line("Bob", "Hello."), Line("Bob", "Goodbye."));
        dialogue.Runner.Start(dialogue.Lines);

        var descriptor = Assert.Single(dialogue.GetActions(default));
        Assert.Equal("Continue", descriptor.Label);
    }

    [Fact]
    public void ExecuteAction_FirstTime_StartsRunner()
    {
        var (_, dialogue) = CreateDialogue(Line("Bob", "Hello."), Line("Bob", "Goodbye."));
        var descriptor = dialogue.GetActions(default)[0];

        var result = descriptor.Action.Execute(default);

        Assert.True(result);
        Assert.True(dialogue.Runner.IsActive);
        Assert.Equal("Hello.", dialogue.Runner.CurrentLine!.Text);
    }

    [Fact]
    public void ExecuteAction_SecondTime_AdvancesRunner()
    {
        var (_, dialogue) = CreateDialogue(Line("Bob", "Hello."), Line("Bob", "Goodbye."));
        dialogue.GetActions(default)[0].Action.Execute(default);

        dialogue.GetActions(default)[0].Action.Execute(default);

        Assert.Equal("Goodbye.", dialogue.Runner.CurrentLine!.Text);
    }

    [Fact]
    public void ExecuteAction_PastLastLine_RestartsOnNextTalk()
    {
        var (_, dialogue) = CreateDialogue(Line("Bob", "Only line."));
        dialogue.GetActions(default)[0].Action.Execute(default); // start, shows the only line
        dialogue.GetActions(default)[0].Action.Execute(default); // advance past it, completes

        Assert.False(dialogue.Runner.IsActive);

        dialogue.GetActions(default)[0].Action.Execute(default); // "Talk" again: restarts

        Assert.True(dialogue.Runner.IsActive);
        Assert.Equal("Only line.", dialogue.Runner.CurrentLine!.Text);
    }
}
