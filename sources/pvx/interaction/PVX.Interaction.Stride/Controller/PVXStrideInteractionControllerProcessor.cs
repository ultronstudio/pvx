using PVX.Runtime;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Physics;

namespace PVX.Interaction.Stride;

/// <summary>
/// Resolves the running <c>PVXInteractionFeature</c>/<c>PVXInteractionService</c>,
/// binds a <see cref="PVXStrideInteractionControllerComponent"/>'s camera
/// and physics simulation into them, and turns a key-down edge into
/// <c>PVXInteractionService.BeginPrimaryInteraction</c>.
/// </summary>
/// <remarks>
/// <para>
/// Fails clearly (throws) if the PVX runtime, <c>PVXInteractionFeature</c>,
/// or the component's <see cref="PVXStrideInteractionControllerComponent.Camera"/>
/// are missing — a broken configuration is never silently ignored.
/// </para>
/// <para>
/// Ownership policy for <c>PVXInteractionFeature.Source</c>/<c>PVXInteractionService.TargetProvider</c>,
/// since 0.1 has no multiplayer/multi-controller arbitration: the first
/// controller to attach while they are unset claims them and throws if
/// one is already claimed by another controller. On detach, a controller
/// only clears them if it still owns them (<see langword="ReferenceEquals"/>) —
/// never a different controller's configuration that took over meanwhile.
/// </para>
/// </remarks>
public sealed class PVXStrideInteractionControllerProcessor
    : EntityProcessor<PVXStrideInteractionControllerComponent, PVXStrideInteractionControllerProcessor.AssociatedData>
{
    protected override AssociatedData GenerateComponentData(Entity entity, PVXStrideInteractionControllerComponent component)
    {
        var runtime = Services.GetService<PVXRuntime>()
            ?? throw new InvalidOperationException(
                "PVXStrideInteractionControllerComponent requires a PVXRuntime registered in the Stride service registry (PVXGame registers it automatically).");

        PVXInteractionFeature? interactionFeature = null;
        foreach (var feature in runtime.Features)
        {
            if (feature is PVXInteractionFeature found)
            {
                interactionFeature = found;
                break;
            }
        }

        if (interactionFeature is null)
        {
            throw new InvalidOperationException(
                "PVXStrideInteractionControllerComponent requires a PVXInteractionFeature registered on the PVXRuntime.");
        }

        var interactionService = interactionFeature.Service
            ?? throw new InvalidOperationException(
                "PVXInteractionFeature has not initialized its PVXInteractionService yet.");

        if (component.Camera is null)
        {
            throw new InvalidOperationException(
                $"PVXStrideInteractionControllerComponent on entity '{entity.Name}' requires Camera to be set.");
        }

        if (interactionFeature.Source is not null)
        {
            throw new InvalidOperationException(
                "Another PVXStrideInteractionControllerComponent is already driving this PVXInteractionFeature's Source. PVX 0.1 supports only one active interaction controller at a time.");
        }

        var source = new PVXStrideCameraInteractionSource(component.Camera, component.MaxDistance, component.OwnerEntity);
        var provider = new PVXStridePhysicsTargetProvider(() => GetSimulation(entity));

        interactionFeature.Source = source;
        interactionService.TargetProvider = provider;

        return new AssociatedData(source, provider, interactionFeature, interactionService);
    }

    protected override bool IsAssociatedDataValid(Entity entity, PVXStrideInteractionControllerComponent component, AssociatedData associatedData) => true;

    protected override void OnEntityComponentRemoved(Entity entity, PVXStrideInteractionControllerComponent component, AssociatedData data)
    {
        // Only release what this controller still owns: another
        // controller may have claimed Source/TargetProvider in the
        // meantime (e.g. after this one faulted during its own attach).
        if (ReferenceEquals(data.InteractionFeature.Source, data.Source))
        {
            data.InteractionFeature.Source = null;
        }

        if (ReferenceEquals(data.InteractionService.TargetProvider, data.Provider))
        {
            data.InteractionService.TargetProvider = null;
        }
    }

    public override void Update(GameTime time)
    {
        var input = Services.GetService<InputManager>();
        if (input is null)
            return;

        foreach (var pair in ComponentDatas)
        {
            var component = pair.Key;
            var data = pair.Value;

            if (!component.Enabled)
                continue;

            if (input.IsKeyPressed(component.PrimaryInteractionKey))
            {
                data.InteractionService.BeginPrimaryInteraction(data.Source);
            }
        }
    }

    private Simulation? GetSimulation(Entity entity) =>
        entity.EntityManager?.GetProcessor<PhysicsProcessor>()?.Simulation;

    public sealed class AssociatedData(
        PVXStrideCameraInteractionSource source,
        PVXStridePhysicsTargetProvider provider,
        PVXInteractionFeature interactionFeature,
        PVXInteractionService interactionService)
    {
        public PVXStrideCameraInteractionSource Source { get; } = source;

        public PVXStridePhysicsTargetProvider Provider { get; } = provider;

        public PVXInteractionFeature InteractionFeature { get; } = interactionFeature;

        public PVXInteractionService InteractionService { get; } = interactionService;
    }
}
