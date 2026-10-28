using System.ComponentModel;
using Stride.Core;
using Stride.Engine;

namespace PVX.Interaction.Stride;

/// <summary>
/// Authoring component marking an entity as one named interaction point
/// on whichever ancestor entity (including itself) owns an
/// <see cref="IPVXInteractable"/> component.
/// </summary>
/// <remarks>
/// Pure data: it has no runtime behavior of its own and needs no
/// processor. <see cref="PVXStridePhysicsTargetProvider"/> reads it
/// directly off the hit entity (or its ancestors, up to the resolved
/// interactable) via <see cref="Entity.Get{T}()"/>.
///
/// Deliberately carries only identity and selection metadata (point id,
/// priority) — not action metadata like a prompt, action id or hold
/// duration, which belong to the interactable/action layer, not to the
/// authoring of a point in space.
/// </remarks>
[DataContract("PVXInteractionPointComponent")]
[Display("PVX Interaction Point")]
public sealed class PVXInteractionPointComponent : EntityComponent
{
    /// <summary>
    /// Game-defined identity of this point, e.g. <c>"bottom_entry"</c>.
    /// Blank/whitespace is valid and is treated by the target provider as
    /// "no explicit point" rather than a validation error, so this stays
    /// authoring-friendly (a freshly added component starts blank).
    /// </summary>
    [DataMember(10)]
    [Display("Point Id")]
    [DefaultValue("")]
    public string PointId { get; set; } = string.Empty;

    /// <summary>
    /// Candidate selection priority contributed to the
    /// <see cref="PVXInteractionCandidate"/> built from this point.
    /// </summary>
    [DataMember(20)]
    [Display("Priority")]
    [DefaultValue(0)]
    public int Priority { get; set; }
}
