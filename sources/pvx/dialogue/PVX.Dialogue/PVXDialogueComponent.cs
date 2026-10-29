using PVX.Interaction;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace PVX.Dialogue;

/// <summary>
/// An interactable that plays an authored line sequence: "Talk" starts it,
/// and re-triggering the same action (another "Talk"/"Continue") advances
/// it one line at a time via its own <see cref="PVXDialogueRunner"/>.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately out of scope for 0.1: branching/choices, conditions
/// (gating a line on a narrative flag), speaker portraits/voice-over, and
/// localization keys — a line is just <see cref="PVXDialogueLine.Speaker"/>/<see cref="PVXDialogueLine.Text"/>,
/// nothing else. A future module can layer branching on top of
/// <see cref="PVXDialogueRunner"/> the same way <see cref="PVX.Persistence.PVXNarrativeFlags"/>
/// layers on top of <see cref="PVX.Persistence.PVXSaveStore"/>.
/// </para>
/// <para>
/// Each component owns its own runner instance — dialogue is not shared
/// global state the way focus/interaction is, so there is no cross-component
/// wiring needed the way <c>PVX.FirstPerson</c>'s carry component needs for
/// pickups: a game's UI reacts to <em>this</em> component's
/// <see cref="Runner"/> events once it starts a conversation.
/// </para>
/// </remarks>
[DataContract("PVXDialogueComponent")]
[Display("PVX Dialogue")]
[DefaultEntityComponentProcessor(typeof(PVXDialogueProcessor))]
public sealed class PVXDialogueComponent : ActivableEntityComponent, IPVXInteractable
{
    public static readonly PVXInteractionActionId TalkActionId = new("talk");

    private readonly TalkInteractionAction action;
    private readonly PVXDialogueRunner runner = new();

    public PVXDialogueComponent()
    {
        action = new TalkInteractionAction(this);
    }

    /// <summary>
    /// The authored line sequence, played in order from the start every
    /// time a new conversation begins.
    /// </summary>
    [DataMember(10)]
    public List<PVXDialogueLine> Lines { get; set; } = new();

    /// <summary>
    /// This component's own dialogue runner. A game's UI subscribes to its
    /// <see cref="PVXDialogueRunner.LineChanged"/>/<see cref="PVXDialogueRunner.Completed"/>
    /// to actually present lines.
    /// </summary>
    [DataMemberIgnore]
    public PVXDialogueRunner Runner => runner;

    /// <inheritdoc />
    /// <remarks>
    /// Offers no action at all if <see cref="Lines"/> is empty — an
    /// authoring mistake (a dialogue component with nothing to say) must
    /// not silently show a "Talk" prompt that does nothing.
    /// </remarks>
    public IReadOnlyList<PVXInteractionDescriptor> GetActions(PVXInteractionContext context)
    {
        if (Lines.Count == 0)
            return [];

        var label = runner.IsActive ? "Continue" : "Talk";
        return [new PVXInteractionDescriptor(TalkActionId, action, priority: 100, label: label)];
    }

    private sealed class TalkInteractionAction(PVXDialogueComponent dialogue) : IPVXInteractionAction
    {
        public bool CanExecute(PVXInteractionContext context) => dialogue.Lines.Count > 0;

        public void Begin(PVXInteractionContext context)
        {
        }

        public bool Execute(PVXInteractionContext context)
        {
            if (!dialogue.runner.IsActive)
            {
                return dialogue.runner.Start(dialogue.Lines);
            }

            dialogue.runner.Advance();
            return true;
        }

        public void Cancel(PVXInteractionContext context)
        {
        }
    }
}
