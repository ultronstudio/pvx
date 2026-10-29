using PVX.Gameplay;
using PVX.Interaction;
using PVX.Runtime;
using PVX.World;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

namespace PVX.FirstPerson;

/// <summary>
/// Wires <see cref="PVXCarryComponent"/> to the running
/// <c>PVXInteractionService</c> (begins carrying whatever
/// <see cref="PVXPickupComponent"/> the player just picked up) and drives
/// drop/throw/inspect from real input each frame.
/// </summary>
/// <remarks>
/// Subscribes to <c>PVXInteractionService.InteractionCompleted</c> rather
/// than polling <see cref="PVXPickupComponent.State"/> every frame: the
/// event already tells us exactly which action completed on which target,
/// so there is no need to re-discover "was something just picked up, and
/// if so, what" by other means.
/// </remarks>
public sealed class PVXCarryProcessor
    : EntityProcessor<PVXCarryComponent, PVXCarryProcessor.AssociatedData>
{
    protected override AssociatedData GenerateComponentData(Entity entity, PVXCarryComponent component)
    {
        if (component.Camera is null)
        {
            throw new InvalidOperationException(
                $"PVXCarryComponent on entity '{entity.Name}' requires Camera to be set.");
        }

        var runtime = Services.GetService<PVXRuntime>()
            ?? throw new InvalidOperationException(
                "PVXCarryComponent requires a PVXRuntime registered in the Stride service registry (PVXGame registers it automatically).");

        var session = runtime.Services.GetRequired<PVXGameplaySession>();

        PVXInteractionFeature? interactionFeature = null;
        foreach (var feature in runtime.Features)
        {
            if (feature is PVXInteractionFeature found)
            {
                interactionFeature = found;
                break;
            }
        }

        var service = interactionFeature?.Service
            ?? throw new InvalidOperationException(
                "PVXCarryComponent requires a PVXInteractionFeature (with its PVXInteractionService initialized) registered on the PVXRuntime.");

        var data = new AssociatedData(entity, session, service);

        // Captured by value in the closure: each PVXCarryComponent instance
        // owns its own subscription, scoped to its own entity/component.
        data.Handler = (_, e) =>
        {
            if (e.Descriptor.ActionId != PVXPickupComponent.PickUpActionId)
                return;

            if (e.Context.Target is PVXPickupComponent pickup && pickup.Entity is { } pickupEntity)
            {
                component.TryBeginCarry(pickupEntity);
            }
        };
        service.InteractionCompleted += data.Handler;

        return data;
    }

    protected override bool IsAssociatedDataValid(Entity entity, PVXCarryComponent component, AssociatedData associatedData) =>
        ReferenceEquals(associatedData.Entity, entity);

    protected override void OnEntityComponentRemoved(Entity entity, PVXCarryComponent component, AssociatedData data)
    {
        if (data.Handler is { } handler)
        {
            data.Service.InteractionCompleted -= handler;
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

            if (!component.Enabled || component.CarriedEntity is null)
                continue;

            if ((data.Session.Control.EffectiveCapabilities & PVXControlCapabilities.Interact) == 0)
                continue;

            if (input.IsKeyPressed(component.DropKey))
            {
                component.EndCarry(throwIt: false);
                continue;
            }

            if (input.IsMouseButtonPressed(component.ThrowButton))
            {
                component.EndCarry(throwIt: true);
                continue;
            }

            component.IsInspecting = input.IsMouseButtonDown(component.InspectButton);

            if (component.IsInspecting)
            {
                component.AccumulateInspectRotation(input.MouseDelta);
            }
        }
    }

    public sealed class AssociatedData(Entity entity, PVXGameplaySession session, PVXInteractionService service)
    {
        public Entity Entity { get; } = entity;

        public PVXGameplaySession Session { get; } = session;

        public PVXInteractionService Service { get; } = service;

        public EventHandler<PVXInteractionEventArgs>? Handler { get; set; }
    }
}
