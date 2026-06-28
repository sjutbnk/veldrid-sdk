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
    /// Begins a new frame, returns a context for recording commands.
    /// Every BeginFrame MUST be paired with EndFrame.
    /// </summary>
    IRenderContext BeginFrame();

    /// <summary>
    /// Submits recorded commands and performs SwapBuffers (Present).
    /// </summary>
    void EndFrame();

    /// <summary>
    /// Uploads <see cref="MeshData"/> to GPU buffers and returns an <see cref="IMesh"/>.
    /// The caller is responsible for calling Dispose() on the returned object.
    /// </summary>
    IMesh CreateMesh(MeshData data);

    /// <summary>
    /// Compiles GLSL shaders (via SPIR-V) and creates an <see cref="IPipeline"/>.
    /// The caller is responsible for calling Dispose() on the returned object.
    /// </summary>
    IPipeline CreatePipeline(PipelineDescription description);

    /// <summary>
    /// Updates the MVP (Model-View-Projection) uniform buffer with the given matrix.
    /// Call this once per frame before DrawMesh.
    /// </summary>
    void SetMvp(Matrix4x4 mvp);
}
