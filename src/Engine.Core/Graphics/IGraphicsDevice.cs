// src/Engine.Core/Graphics/IGraphicsDevice.cs
using System.Numerics;

namespace AntigravityEngine.Core.Graphics;

/// <summary>
/// Abstraction of a GPU device. Owns the swapchain and command buffers.
/// Must be explicitly released via <see cref="IDisposable.Dispose"/>.
/// </summary>
public interface IGraphicsDevice : IDisposable
{
    /// <summary>Name of the active graphics backend (e.g. "Vulkan", "OpenGL").</summary>
    string BackendName { get; }

    /// <summary>
    /// Begins a new frame and returns a context for recording per-frame draw commands.
    /// Every BeginFrame MUST be paired with a corresponding EndFrame.
    /// </summary>
    IRenderContext BeginFrame();

    /// <summary>Submits recorded commands and presents the swapchain image.</summary>
    void EndFrame();

    /// <summary>
    /// Uploads <see cref="MeshData"/> (positions, normals, UVs, indices) to GPU buffers.
    /// The caller must call <see cref="IDisposable.Dispose"/> on the returned object.
    /// </summary>
    IMesh CreateMesh(MeshData data);

    /// <summary>
    /// Compiles GLSL vertex + fragment shaders (via SPIR-V) into a graphics pipeline.
    /// The caller must call <see cref="IDisposable.Dispose"/> on the returned object.
    /// </summary>
    IPipeline CreatePipeline(PipelineDescription description);

    /// <summary>
    /// Uploads an RGBA8 image to a GPU texture and returns an opaque handle.
    /// <paramref name="rgbaPixelData"/> must be exactly <c>width × height × 4</c> bytes.
    /// The caller must call <see cref="IDisposable.Dispose"/> on the returned object.
    /// </summary>
    ITexture2D CreateTexture2D(uint width, uint height, byte[] rgbaPixelData);

    /// <summary>
    /// Updates the scene-level uniform buffer used by all subsequent draw calls this frame.
    /// Call once per frame, before <see cref="BeginFrame"/>.
    /// </summary>
    /// <param name="viewProjection">Combined View × Projection matrix.</param>
    /// <param name="lightDirection">Normalized world-space direction FROM the light source.</param>
    /// <param name="lightColor">Diffuse light colour (RGBA, linear).</param>
    /// <param name="ambientColor">Ambient / fill light colour (RGBA, linear).</param>
    void SetSceneGlobals(
        Matrix4x4 viewProjection,
        Vector3   lightDirection,
        Vector4   lightColor,
        Vector4   ambientColor);
}
