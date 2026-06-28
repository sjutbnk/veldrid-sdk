// src/Renderer.Veldrid/VeldridRenderContext.cs
using AntigravityEngine.Core.Graphics;
using System.Numerics;
using Veldrid;

namespace AntigravityEngine.Renderer.Veldrid;

/// <summary>
/// Per-frame render command recorder.
/// Wraps a <see cref="CommandList"/> and binds scene-level and per-object resource sets.
/// </summary>
internal sealed class VeldridRenderContext : IRenderContext
{
    private readonly CommandList  _commandList;
    private readonly GraphicsDevice _gd;
    private readonly DeviceBuffer   _modelBuffer;
    private readonly ResourceSet    _sceneResourceSet;   // set=0
    private readonly ResourceSet    _modelResourceSet;   // set=1

    internal VeldridRenderContext(
        CommandList    commandList,
        Framebuffer    framebuffer,
        GraphicsDevice gd,
        DeviceBuffer   modelBuffer,
        ResourceSet    sceneResourceSet,
        ResourceSet    modelResourceSet)
    {
        _commandList      = commandList;
        _gd               = gd;
        _modelBuffer      = modelBuffer;
        _sceneResourceSet = sceneResourceSet;
        _modelResourceSet = modelResourceSet;

        _commandList.Begin();
        _commandList.SetFramebuffer(framebuffer);
        _commandList.SetViewport(0, new Viewport(
            x: 0, y: 0,
            width:    framebuffer.Width,
            height:   framebuffer.Height,
            minDepth: 0f, maxDepth: 1f));
        _commandList.SetScissorRect(0, 0, 0, framebuffer.Width, framebuffer.Height);
    }

    /// <inheritdoc/>
    public void ClearColorTarget(RgbaColor color)
    {
        _commandList.ClearColorTarget(0, new RgbaFloat(color.R, color.G, color.B, color.A));
        _commandList.ClearDepthStencil(1f);
    }

    /// <inheritdoc/>
    public void DrawMesh(
        IPipeline  pipeline,
        IMesh      mesh,
        Matrix4x4  modelMatrix,
        ITexture2D texture)
    {
        var vPipeline = (VeldridPipeline)pipeline;
        var vMesh     = (VeldridMesh)mesh;
        var vTexture  = (VeldridTexture2D)texture;

        // Upload per-object model matrix into the shared model buffer.
        // Using gd.UpdateBuffer (immediate) before the draw is safe in both
        // OpenGL and Vulkan for single-object-per-frame scenarios.
        _gd.UpdateBuffer(_modelBuffer, 0u, modelMatrix);

        _commandList.SetPipeline(vPipeline.NativePipeline);
        _commandList.SetGraphicsResourceSet(0, _sceneResourceSet);   // SceneGlobals
        _commandList.SetGraphicsResourceSet(1, _modelResourceSet);   // ModelBlock
        _commandList.SetGraphicsResourceSet(2, vTexture.ResourceSet); // Texture + Sampler
        _commandList.SetVertexBuffer(0, vMesh.VertexBuffer);
        _commandList.SetIndexBuffer(vMesh.IndexBuffer, IndexFormat.UInt32);
        _commandList.DrawIndexed(vMesh.IndexCount);
    }

    internal void End() => _commandList.End();
}
