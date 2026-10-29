using PVX.Gameplay;
using PVX.Runtime;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

namespace PVX.FirstPerson;

/// <summary>
/// Drives <see cref="PVXFirstPersonControllerComponent"/> from real input
/// and physics each frame: mouse capture, gameplay-capability-gated
/// look/move, and pushing the computed yaw/pitch/velocity onto the entity
/// transforms and <see cref="CharacterComponent"/>.
/// </summary>
/// <remarks>
/// <para>
/// Mouse capture: locks and hides the cursor on the first mouse-button
/// press while not already locked (so a click anywhere on the game view
/// captures it — no held button required afterward, matching the FPS
/// template's own capture gesture), and releases it on Escape. While
/// captured, <c>InputManager.MouseDelta</c> drives look every frame with
/// no extra gesture needed.
/// </para>
/// <para>
/// Capability gating reads <c>PVXGameplaySession.Control.EffectiveCapabilities</c>
/// every frame (the same authority <see cref="PVX.Interaction.Stride.PVXStrideInteractionControllerComponent"/>
/// already uses for <see cref="PVXControlCapabilities.Interact"/>): without
/// <see cref="PVXControlCapabilities.Move"/>, the character's velocity is
/// forced to zero every frame rather than left at its last value;
/// without <see cref="PVXControlCapabilities.Look"/>, mouse delta is not
/// read at all, so yaw/pitch simply stop changing.
/// </para>
/// </remarks>
public sealed class PVXFirstPersonControllerProcessor
    : EntityProcessor<PVXFirstPersonControllerComponent, PVXFirstPersonControllerProcessor.AssociatedData>
{
    protected override AssociatedData GenerateComponentData(Entity entity, PVXFirstPersonControllerComponent component)
    {
        if (component.Character is null)
        {
            throw new InvalidOperationException(
                $"PVXFirstPersonControllerComponent on entity '{entity.Name}' requires Character to be set.");
        }

        if (component.Camera is null)
        {
            throw new InvalidOperationException(
                $"PVXFirstPersonControllerComponent on entity '{entity.Name}' requires Camera to be set.");
        }

        var runtime = Services.GetService<PVXRuntime>()
            ?? throw new InvalidOperationException(
                "PVXFirstPersonControllerComponent requires a PVXRuntime registered in the Stride service registry (PVXGame registers it automatically).");

        var session = runtime.Services.GetRequired<PVXGameplaySession>();

        return new AssociatedData(entity, session);
    }

    protected override bool IsAssociatedDataValid(Entity entity, PVXFirstPersonControllerComponent component, AssociatedData associatedData) =>
        ReferenceEquals(associatedData.Entity, entity);

    public override void Update(GameTime time)
    {
        var input = Services.GetService<InputManager>();
        var game = Services.GetService<IGame>();

        foreach (var pair in ComponentDatas)
        {
            var component = pair.Key;
            var data = pair.Value;

            if (!component.Enabled)
                continue;

            var character = component.Character!;
            var camera = component.Camera!;
            var capabilities = data.Session.Control.EffectiveCapabilities;

            if (input is not null)
            {
                UpdateMouseCapture(input, game);

                if (input.IsMousePositionLocked && (capabilities & PVXControlCapabilities.Look) != 0)
                {
                    component.AccumulateLook(input.MouseDelta);
                }
            }

            data.Entity.Transform.Rotation = component.YawRotation;
            camera.Entity.Transform.Rotation = component.PitchRotation;

            var moveCapable = (capabilities & PVXControlCapabilities.Move) != 0;
            var localInput = moveCapable && input is not null ? ReadMoveInput(input) : Vector2.Zero;
            var velocity = component.ComputeHorizontalVelocity(localInput);

            character.SetVelocity(velocity);
        }
    }

    /// <summary>
    /// Captures on the first mouse-button press while not already locked
    /// (click/focus the game view — no held button after that); releases
    /// on Escape.
    /// </summary>
    private static void UpdateMouseCapture(InputManager input, IGame? game)
    {
        if (!input.IsMousePositionLocked && input.HasMouse && AnyMouseButtonPressed(input))
        {
            input.LockMousePosition(true);
            if (game is not null)
                game.IsMouseVisible = false;
        }
        else if (input.IsMousePositionLocked && input.IsKeyPressed(Keys.Escape))
        {
            input.UnlockMousePosition();
            if (game is not null)
                game.IsMouseVisible = true;
        }
    }

    private static bool AnyMouseButtonPressed(InputManager input) =>
        input.IsMouseButtonPressed(MouseButton.Left)
        || input.IsMouseButtonPressed(MouseButton.Right)
        || input.IsMouseButtonPressed(MouseButton.Middle);

    private static Vector2 ReadMoveInput(InputManager input)
    {
        var move = Vector2.Zero;

        if (input.IsKeyDown(Keys.W))
            move.Y += 1f;
        if (input.IsKeyDown(Keys.S))
            move.Y -= 1f;
        if (input.IsKeyDown(Keys.D))
            move.X += 1f;
        if (input.IsKeyDown(Keys.A))
            move.X -= 1f;

        return move;
    }

    public sealed class AssociatedData(Entity entity, PVXGameplaySession session)
    {
        public Entity Entity { get; } = entity;

        public PVXGameplaySession Session { get; } = session;
    }
}
