using PVX.Gameplay;
using PVX.Runtime;
using Stride.Engine;
using Stride.Games;

namespace PVX.Cinematics;

/// <summary>
/// Drives every <see cref="PVXCinematicComponent"/> in the scene: advances
/// playback by real elapsed time each frame, and acquires/releases a
/// <see cref="PVXControlRestriction"/> (stripping the active player's
/// control) for as long as a component with
/// <see cref="PVXCinematicComponent.RestrictPlayerControlWhilePlaying"/>
/// set is playing.
/// </summary>
public sealed class PVXCinematicProcessor
    : EntityProcessor<PVXCinematicComponent, PVXCinematicProcessor.AssociatedData>
{
    protected override AssociatedData GenerateComponentData(Entity entity, PVXCinematicComponent component)
    {
        var runtime = Services.GetService<PVXRuntime>()
            ?? throw new InvalidOperationException(
                "PVXCinematicComponent requires a PVXRuntime registered in the Stride service registry (PVXGame registers it automatically).");

        var session = runtime.Services.GetRequired<PVXGameplaySession>();

        return new AssociatedData(entity, session);
    }

    protected override bool IsAssociatedDataValid(Entity entity, PVXCinematicComponent component, AssociatedData associatedData) =>
        ReferenceEquals(associatedData.Entity, entity);

    protected override void OnEntityComponentRemoved(Entity entity, PVXCinematicComponent component, AssociatedData data)
    {
        data.Restriction?.Dispose();
        data.Restriction = null;
    }

    public override void Update(GameTime time)
    {
        var elapsed = (float)time.Elapsed.TotalSeconds;

        foreach (var pair in ComponentDatas)
        {
            var component = pair.Key;
            var data = pair.Value;

            if (!component.Enabled)
                continue;

            component.Advance(elapsed);

            var shouldRestrict = component.IsPlaying && component.RestrictPlayerControlWhilePlaying;

            if (shouldRestrict && data.Restriction is null)
            {
                data.Restriction = data.Session.Control.AcquireRestriction(PVXControlCapabilities.None, component);
            }
            else if (!shouldRestrict && data.Restriction is not null)
            {
                data.Restriction.Dispose();
                data.Restriction = null;
            }
        }
    }

    public sealed class AssociatedData(Entity entity, PVXGameplaySession session)
    {
        public Entity Entity { get; } = entity;

        public PVXGameplaySession Session { get; } = session;

        public PVXControlRestriction? Restriction { get; set; }
    }
}
