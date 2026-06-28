// src/Engine.Core/Graphics/IRenderContext.cs
using System.Numerics;

namespace AntigravityEngine.Core.Graphics;

/// <summary>
/// Per-frame render command recorder.
/// Obtained via <see cref="IGraphicsDevice.BeginFrame"/> and completed with
/// <see cref="IGraphicsDevice.EndFrame"/>.
/// </summary>
public interface IRenderContext
{
    /// <summary>Clears the colour (and depth) buffer to the given colour.</summary>
    void ClearColorTarget(RgbaColor color);

    /// <summary>
    /// Draws <paramref name="mesh"/> using <paramref name="pipeline"/>,
    /// applying <paramref name="modelMatrix"/> for the object's world-space transform
    /// and sampling <paramref name="texture"/> in the fragment stage.
    /// </summary>
    void DrawMesh(
        IPipeline  pipeline,
        IMesh      mesh,
        Matrix4x4  modelMatrix,
        ITexture2D texture);
}
