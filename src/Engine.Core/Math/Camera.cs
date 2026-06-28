// src/Engine.Core/Math/Camera.cs
using System.Numerics;

namespace AntigravityEngine.Core.Math;

/// <summary>
/// Simple FPS-style camera. Produces View and Projection matrices
/// compatible with System.Numerics row-major convention and Veldrid's
/// D3D/Vulkan clip-space (Z in [0,1], Y up).
/// </summary>
public sealed class Camera
{
    private const float DegToRad = MathF.PI / 180f;

    public Vector3 Position { get; set; }
    /// <summary>Horizontal rotation in radians (0 = looking toward -Z).</summary>
    public float Yaw   { get; set; }
    /// <summary>Vertical rotation in radians (positive = looking up).</summary>
    public float Pitch { get; set; }

    public float FovDegrees { get; set; } = 60f;
    public float NearPlane  { get; set; } = 0.1f;
    public float FarPlane   { get; set; } = 1000f;

    public Camera(Vector3 position, float yaw = 0f, float pitch = 0f)
    {
        Position = position;
        Yaw      = yaw;
        Pitch    = pitch;
    }

    public Vector3 Forward
    {
        get
        {
            float cosP = MathF.Cos(Pitch);
            return Vector3.Normalize(new Vector3(
                cosP * MathF.Sin(Yaw),
               -MathF.Sin(Pitch),
               -cosP * MathF.Cos(Yaw)));
        }
    }

    /// <summary>
    /// Returns a View matrix using <see cref="Matrix4x4.CreateLookAt"/>.
    /// System.Numerics uses row-vector convention, so vectors transform as
    /// <c>v_view = v_world × ViewMatrix</c>.
    /// </summary>
    public Matrix4x4 GetViewMatrix()
        => Matrix4x4.CreateLookAt(Position, Position + Forward, Vector3.UnitY);

    /// <summary>
    /// Returns a Perspective Projection matrix.
    /// <para>
    /// <see cref="Matrix4x4.CreatePerspectiveFieldOfView"/> already produces a
    /// D3D-style matrix with clip-space Z mapped to [0, 1] — matching Veldrid's
    /// <c>preferDepthRangeZeroToOne = true</c>.  No additional remapping needed.
    /// </para>
    /// </summary>
    public Matrix4x4 GetProjectionMatrix(float aspectRatio)
        => Matrix4x4.CreatePerspectiveFieldOfView(
            FovDegrees * DegToRad, aspectRatio, NearPlane, FarPlane);
}
