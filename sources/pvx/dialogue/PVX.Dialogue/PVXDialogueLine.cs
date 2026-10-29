using System.ComponentModel;
using Stride.Core;

namespace PVX.Dialogue;

/// <summary>
/// A single authored line of dialogue: who says it, and what they say.
/// Deliberately has no conditions, branching, choices, or localization
/// key — see <see cref="PVXDialogueComponent"/>'s own remarks for why
/// those are explicitly out of scope for 0.1.
/// </summary>
[DataContract("PVXDialogueLine")]
public sealed class PVXDialogueLine
{
    [DataMember(10)]
    [DefaultValue("")]
    public string Speaker { get; set; } = string.Empty;

    [DataMember(20)]
    [DefaultValue("")]
    public string Text { get; set; } = string.Empty;
}
