// src/Renderer.Veldrid/VeldridRenderContext.cs
using AntigravityEngine.Core.Graphics;
using Veldrid;

namespace AntigravityEngine.Renderer.Veldrid;

/// <summary>
/// Concrete implementation of <see cref="IRenderContext"/> for one frame.
/// Wraps a <see cref="CommandList"/> and binds the MVP resource set.
/// </summary>
internal sealed class VeldridRenderContext : IRenderContext
{
    private readonly CommandList _commandList;
    private readonly ResourceSet _mvpResourceSet;

    internal VeldridRenderContext(
        CommandList  commandList,
        Framebuffer  framebuffer,
        ResourceSet  mvpResourceSet)
    {
        _commandList    = commandList;
        _mvpResourceSet = mvpResourceSet;

        _commandList.Begin();
        _commandList.SetFramebuffer(framebuffer);

        _commandList.SetViewport(0, new Viewport(
            x: 0, y: 0,
            width:    framebuffer.Width,
            height:   framebuffer.Height,
            minDepth: 0f,
            maxDepth: 1f));

        _commandList.SetScissorRect(0, 0, 0, framebuffer.Width, framebuffer.Height);
    }

    /// <inheritdoc/>
    public void ClearColorTarget(RgbaColor color)
    {
        _commandList.ClearColorTarget(0, new RgbaFloat(color.R, color.G, color.B, color.A));
        _commandList.ClearDepthStencil(1f);
    }

    /// <inheritdoc/>
    public void DrawMesh(IPipeline pipeline, IMesh mesh)
    {
        var vPipeline = (VeldridPipeline)pipeline;
        var vMesh     = (VeldridMesh)mesh;

        _commandList.SetPipeline(vPipeline.NativePipeline);
        _commandList.SetGraphicsResourceSet(0, _mvpResourceSet);
        _commandList.SetVertexBuffer(0, vMesh.VertexBuffer);
        _commandList.SetIndexBuffer(vMesh.IndexBuffer, IndexFormat.UInt32);
        _commandList.DrawIndexed(vMesh.IndexCount);
    }

    /// <summary>Finalises command recording. Called from <see cref="VeldridGraphicsDevice.EndFrame"/>.</summary>
    internal void End() => _commandList.End();
}
