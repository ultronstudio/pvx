using Stride.Core.Mathematics;
using Stride.Engine;

namespace PVX.Interaction.Stride;

/// <summary>
/// <see cref="IPVXStrideRayInteractionSource"/> adapter for a center-screen,
/// first-person style ray cast straight down the camera's forward axis.
/// </summary>
/// <remarks>
/// Origin and Direction are read fresh from <see cref="CameraComponent.Entity"/>'s
/// world transform on every access rather than cached, so this stays correct
/// across frames without needing its own per-frame update call.
///
/// Stride's world-space camera forward is <c>Entity.Transform.WorldMatrix.Forward</c>
/// (<c>(-M31, -M32, -M33)</c>, matching the engine's right-handed projection —
/// see <see cref="Matrix.Forward"/> and <see cref="CameraComponent.Update(float?)"/>'s
/// use of <c>PerspectiveFovRH</c>/<c>OrthoRH</c>), not an assumed axis.
///
/// This is deliberately center-screen/forward-ray only: mouse cursor
/// unprojection and other screen-space picking are a different ray source,
/// out of scope for this bridge.
/// </remarks>
public sealed class PVXStrideCameraInteractionSource : IPVXStrideRayInteractionSource
{
    private readonly CameraComponent camera;

    public PVXStrideCameraInteractionSource(CameraComponent camera, float maxDistance, Entity? ownerEntity = null)
    {
        this.camera = camera ?? throw new ArgumentNullException(nameof(camera));
        MaxDistance = PVXStrideRayInteractionSource.ValidateMaxDistance(maxDistance);
        OwnerEntity = ownerEntity;
    }

    public Vector3 Origin => PVXStrideRayInteractionSource.ValidateOrigin(camera.Entity.Transform.WorldMatrix.TranslationVector);

    public Vector3 Direction => PVXStrideRayInteractionSource.ValidateAndNormalize(camera.Entity.Transform.WorldMatrix.Forward);

    public float MaxDistance { get; }

    public Entity? OwnerEntity { get; }
}
