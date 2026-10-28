using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace PVX.World.Playground;

/// <summary>
/// Diagnostic/sample-only free-fly camera for manually testing the
/// playground: mouse-look while holding the right mouse button, WASD to
/// move. This is explicitly NOT a stand-in for a production PVX
/// first-person character controller, which remains a future milestone —
/// see the playground's own remarks.
/// </summary>
internal sealed class PlaygroundFreeFlyCameraScript : SyncScript
{
    private const float MaxPitch = MathUtil.PiOverTwo * 0.99f;

    private float yaw;
    private float pitch;

    public float MoveSpeed { get; set; } = 4f;

    public float LookSpeed { get; set; } = 2.5f;

    public override void Update()
    {
        var elapsed = (float)Game.UpdateTime.Elapsed.TotalSeconds;

        if (Input.HasMouse && Input.IsMouseButtonDown(MouseButton.Right))
        {
            Input.LockMousePosition();
            Game.IsMouseVisible = false;

            yaw -= Input.MouseDelta.X * LookSpeed;
            pitch = MathUtil.Clamp(pitch - Input.MouseDelta.Y * LookSpeed, -MaxPitch, MaxPitch);
        }
        else
        {
            Input.UnlockMousePosition();
            Game.IsMouseVisible = true;
        }

        Entity.Transform.Rotation = Quaternion.RotationYawPitchRoll(yaw, pitch, 0f);

        var move = Vector3.Zero;
        if (Input.IsKeyDown(Keys.W)) move.Z -= 1f;
        if (Input.IsKeyDown(Keys.S)) move.Z += 1f;
        if (Input.IsKeyDown(Keys.A)) move.X -= 1f;
        if (Input.IsKeyDown(Keys.D)) move.X += 1f;

        if (move.LengthSquared() > 0f)
        {
            move.Normalize();
            var rotationMatrix = Matrix.RotationQuaternion(Entity.Transform.Rotation);
            var worldMove = Vector3.TransformNormal(move, rotationMatrix) * MoveSpeed * elapsed;

            // Move the player root (this camera's parent), not the camera
            // itself, so OwnerEntity-based self-ignore on the interaction
            // controller stays meaningful.
            var root = Entity.Transform.Parent?.Entity ?? Entity;
            root.Transform.Position += worldMove;
        }
    }
}
