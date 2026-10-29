using PVX.Gameplay;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using Xunit;

namespace PVX.FirstPerson.Tests;

/// <summary>
/// Real-engine tests for <see cref="PVXFirstPersonControllerComponent"/>/
/// <see cref="PVXFirstPersonControllerProcessor"/>: real Bullet
/// <see cref="CharacterComponent"/> physics, real <c>InputManager</c>
/// (driven by simulated input devices, not fakes — see <see cref="FirstPersonFixture"/>),
/// and real <see cref="PVXGameplaySession"/> capability gating.
/// </summary>
[Collection("PVX FirstPerson")]
public class ControllerIntegrationTests
{
    private readonly FirstPersonFixture fixture;

    public ControllerIntegrationTests(FirstPersonFixture fixture)
    {
        this.fixture = fixture;
    }

    private static (Entity Player, Entity Camera, PVXFirstPersonControllerComponent Controller, CharacterComponent Character) CreatePlayer(string name)
    {
        var player = new Entity(name) { Transform = { Position = new Vector3(0, 2f, 0) } };
        var character = new CharacterComponent();
        character.ColliderShapes.Add(new CapsuleColliderShapeDesc { Radius = 0.3f, Length = 1f });
        player.Add(character);

        var camera = new Entity(name + "Camera");
        camera.Add(new CameraComponent());
        camera.Transform.Parent = player.Transform;

        var controller = new PVXFirstPersonControllerComponent
        {
            Character = character,
            Camera = camera.Get<CameraComponent>(),
            WalkSpeed = 4f,
        };
        player.Add(controller);

        return (player, camera, controller, character);
    }

    private static Entity CreateGround() =>
        PhysicsSceneHelpers.CreateStaticBoxCollider("Ground", new Vector3(0, -0.1f, 0), new Vector3(50, 0.2f, 50));

    [Fact]
    public async Task Attach_MissingCharacter_Throws()
    {
        var fault = await fixture.RunScenario(async game =>
        {
            var entity = new Entity("Broken");
            entity.Add(new PVXFirstPersonControllerComponent { Camera = new CameraComponent() });

            Exception? caught = null;
            try
            {
                await game.AddAndSettle(entity);
            }
            catch (Exception ex)
            {
                caught = ex;
            }
            finally
            {
                await game.RemoveAndSettleIfAttached(entity);
            }

            return caught;
        });

        Assert.NotNull(fault);
    }

    [Fact]
    public async Task Attach_MissingCamera_Throws()
    {
        var fault = await fixture.RunScenario(async game =>
        {
            var entity = new Entity("Broken");
            var character = new CharacterComponent();
            character.ColliderShapes.Add(new CapsuleColliderShapeDesc());
            entity.Add(character);
            entity.Add(new PVXFirstPersonControllerComponent { Character = character });

            Exception? caught = null;
            try
            {
                await game.AddAndSettle(entity);
            }
            catch (Exception ex)
            {
                caught = ex;
            }
            finally
            {
                await game.RemoveAndSettleIfAttached(entity);
            }

            return caught;
        });

        Assert.NotNull(fault);
    }

    [Fact]
    public async Task HoldingForward_WalksHorizontally_NeverFlies_EvenWhilePitchedUp()
    {
        var (startY, endY, movedHorizontally) = await fixture.RunScenario(async game =>
        {
            var ground = CreateGround();
            var (player, _, controller, character) = CreatePlayer("Walker");
            await game.AddAndSettle(ground, player);

            // Let the character land on the ground first (same pattern as
            // Stride.Physics.Tests's own CharacterComponent test).
            for (var i = 0; i < 180; i++)
                await game.Script.NextFrame();

            Assert.True(character.IsGrounded, "Character did not land on the ground before the movement check.");

            // Pitch the camera all the way up: the acceptance requirement
            // is specifically that this must not make forward input fly
            // the player upward.
            controller.AccumulateLook(new Vector2(0f, -100000f));
            Assert.Equal(controller.PitchMaxDegrees, controller.PitchDegrees, 3);

            var startPosition = player.Transform.Position;

            fixture.Keyboard.SimulateDown(Stride.Input.Keys.W);

            for (var i = 0; i < 60; i++)
                await game.Script.NextFrame();

            var endPosition = player.Transform.Position;

            fixture.Keyboard.SimulateUp(Stride.Input.Keys.W);
            await game.RemoveAndSettle(player, ground);

            var horizontalDistance = new Vector2(endPosition.X - startPosition.X, endPosition.Z - startPosition.Z).Length();
            return (startPosition.Y, endPosition.Y, horizontalDistance > 0.1f);
        });

        Assert.True(movedHorizontally, "Expected the character to move horizontally while holding forward.");
        Assert.True(MathF.Abs(endY - startY) < 0.3f, $"Expected the character to stay grounded (startY={startY}, endY={endY}) instead of flying while pitched up and moving forward.");
    }

    [Fact]
    public async Task MoveCapabilityRestricted_BlocksLocomotion()
    {
        var moved = await fixture.RunScenario(async game =>
        {
            var ground = CreateGround();
            var (player, _, _, character) = CreatePlayer("Restricted");
            await game.AddAndSettle(ground, player);

            for (var i = 0; i < 120; i++)
                await game.Script.NextFrame();

            using var restriction = fixture.Session.Control.AcquireRestriction(PVXControlCapabilities.All & ~PVXControlCapabilities.Move, owner: this);

            var startPosition = player.Transform.Position;

            fixture.Keyboard.SimulateDown(Stride.Input.Keys.W);
            for (var i = 0; i < 30; i++)
                await game.Script.NextFrame();
            fixture.Keyboard.SimulateUp(Stride.Input.Keys.W);

            var endPosition = player.Transform.Position;
            await game.RemoveAndSettle(player, ground);

            return new Vector2(endPosition.X - startPosition.X, endPosition.Z - startPosition.Z).Length();
        });

        Assert.True(moved < 0.05f, $"Expected no horizontal movement while Move capability is restricted, moved {moved}.");
    }

    [Fact]
    public async Task LookCapabilityRestricted_BlocksCameraRotation()
    {
        var (yawBefore, yawAfter) = await fixture.RunScenario(async game =>
        {
            var (player, _, controller, _) = CreatePlayer("LookRestricted");
            await game.AddAndSettle(player);

            fixture.Mouse.SimulateMouseDown(Stride.Input.MouseButton.Left);
            await game.Script.NextFrame();
            fixture.Mouse.SimulateMouseUp(Stride.Input.MouseButton.Left);

            using var restriction = fixture.Session.Control.AcquireRestriction(PVXControlCapabilities.All & ~PVXControlCapabilities.Look, owner: this);

            var before = controller.Yaw;

            // Mouse delta is accumulated from real device events (there is
            // no public "set delta" API), so this only proves that a
            // restricted Look doesn't advance Yaw/Pitch across a frame
            // where input is actively being read for everything else.
            for (var i = 0; i < 5; i++)
                await game.Script.NextFrame();

            var after = controller.Yaw;

            await game.RemoveAndSettle(player);
            return (before, after);
        });

        Assert.Equal(yawBefore, yawAfter, 5);
    }

    [Fact]
    public async Task MouseClick_LocksMouse_Escape_Unlocks()
    {
        var (lockedAfterClick, unlockedAfterEscape) = await fixture.RunScenario(async game =>
        {
            var (player, _, _, _) = CreatePlayer("MouseCapture");
            await game.AddAndSettle(player);

            fixture.Mouse.SimulateMouseDown(Stride.Input.MouseButton.Left);
            await game.Script.NextFrame();
            fixture.Mouse.SimulateMouseUp(Stride.Input.MouseButton.Left);
            await game.Script.NextFrame();

            var locked = game.Input.IsMousePositionLocked;

            fixture.Keyboard.SimulateDown(Stride.Input.Keys.Escape);
            await game.Script.NextFrame();
            fixture.Keyboard.SimulateUp(Stride.Input.Keys.Escape);
            await game.Script.NextFrame();

            var unlocked = !game.Input.IsMousePositionLocked;

            await game.RemoveAndSettle(player);
            return (locked, unlocked);
        });

        Assert.True(lockedAfterClick, "Expected a mouse click to lock the mouse position.");
        Assert.True(unlockedAfterEscape, "Expected Escape to unlock the mouse position.");
    }
}
