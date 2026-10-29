using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;

namespace PVX.Cinematics;

/// <summary>
/// One stop along a <see cref="PVXCinematicComponent"/>'s path: a pose to
/// reach, and how long the previous leg of the path takes to reach it.
/// </summary>
[DataContract("PVXCinematicWaypoint")]
public sealed class PVXCinematicWaypoint
{
    [DataMember(10)]
    public Vector3 Position { get; set; }

    [DataMember(20)]
    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    /// <summary>
    /// Time, in seconds, to travel from the previous pose (the entity's
    /// pose when <see cref="PVXCinematicComponent.Play"/> was called, for
    /// the first waypoint; the previous waypoint's pose otherwise) to this
    /// one. Non-positive or non-finite values make this leg instantaneous.
    /// </summary>
    [DataMember(30)]
    [DefaultValue(1f)]
    public float Duration { get; set; } = 1f;
}
