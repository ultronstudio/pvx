using PVX.Dialogue;
using Xunit;

namespace PVX.Dialogue.Tests;

public class DialogueRunnerTests
{
    private static PVXDialogueLine Line(string speaker, string text) => new() { Speaker = speaker, Text = text };

    [Fact]
    public void DefaultState_IsNotActive()
    {
        var runner = new PVXDialogueRunner();

        Assert.False(runner.IsActive);
        Assert.Null(runner.CurrentLine);
    }

    [Fact]
    public void Start_WithLines_BecomesActiveAtFirstLine()
    {
        var runner = new PVXDialogueRunner();
        var lines = new[] { Line("Bob", "Hello."), Line("Bob", "Goodbye.") };

        var result = runner.Start(lines);

        Assert.True(result);
        Assert.True(runner.IsActive);
        Assert.Same(lines[0], runner.CurrentLine);
    }

    [Fact]
    public void Start_EmptyLines_Fails()
    {
        var runner = new PVXDialogueRunner();

        var result = runner.Start(Array.Empty<PVXDialogueLine>());

        Assert.False(result);
        Assert.False(runner.IsActive);
    }

    [Fact]
    public void Start_NullLines_Fails()
    {
        var runner = new PVXDialogueRunner();

        var result = runner.Start(null);

        Assert.False(result);
    }

    [Fact]
    public void Start_WhileAlreadyActive_Fails()
    {
        var runner = new PVXDialogueRunner();
        runner.Start([Line("Bob", "Hello.")]);

        var result = runner.Start([Line("Alice", "Hi.")]);

        Assert.False(result);
        Assert.Equal("Bob", runner.CurrentLine!.Speaker);
    }

    [Fact]
    public void Advance_MovesToNextLine()
    {
        var runner = new PVXDialogueRunner();
        var lines = new[] { Line("Bob", "First."), Line("Bob", "Second.") };
        runner.Start(lines);

        var result = runner.Advance();

        Assert.True(result);
        Assert.Same(lines[1], runner.CurrentLine);
    }

    [Fact]
    public void Advance_PastLastLine_Completes()
    {
        var runner = new PVXDialogueRunner();
        runner.Start([Line("Bob", "Only line.")]);

        var result = runner.Advance();

        Assert.False(result);
        Assert.False(runner.IsActive);
        Assert.Null(runner.CurrentLine);
    }

    [Fact]
    public void Advance_WhileNotActive_Fails()
    {
        var runner = new PVXDialogueRunner();

        Assert.False(runner.Advance());
    }

    [Fact]
    public void Stop_WhileActive_EndsSequence()
    {
        var runner = new PVXDialogueRunner();
        runner.Start([Line("Bob", "Hello."), Line("Bob", "Goodbye.")]);

        runner.Stop();

        Assert.False(runner.IsActive);
    }

    [Fact]
    public void Completed_RaisedOnceOnNaturalEnd()
    {
        var runner = new PVXDialogueRunner();
        runner.Start([Line("Bob", "Only line.")]);
        var raiseCount = 0;
        runner.Completed += (_, _) => raiseCount++;

        runner.Advance();

        Assert.Equal(1, raiseCount);
    }

    [Fact]
    public void Completed_RaisedOnExplicitStop()
    {
        var runner = new PVXDialogueRunner();
        runner.Start([Line("Bob", "Hello.")]);
        var raised = false;
        runner.Completed += (_, _) => raised = true;

        runner.Stop();

        Assert.True(raised);
    }

    [Fact]
    public void LineChanged_RaisedOnStartAndAdvance()
    {
        var runner = new PVXDialogueRunner();
        var lines = new[] { Line("Bob", "First."), Line("Bob", "Second.") };
        var seenLines = new List<PVXDialogueLine>();
        runner.LineChanged += (_, e) => seenLines.Add(e.CurrentLine);

        runner.Start(lines);
        runner.Advance();

        Assert.Equal(lines, seenLines);
    }

    [Fact]
    public void AfterCompletion_CanStartAgain()
    {
        var runner = new PVXDialogueRunner();
        runner.Start([Line("Bob", "First run.")]);
        runner.Advance(); // completes

        var result = runner.Start([Line("Bob", "Second run.")]);

        Assert.True(result);
        Assert.Equal("Second run.", runner.CurrentLine!.Text);
    }
}
