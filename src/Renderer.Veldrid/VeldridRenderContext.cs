// src/Renderer.Veldrid/VeldridRenderContext.cs
using AntigravityEngine.Core.Graphics;
using Veldrid;

namespace AntigravityEngine.Renderer.Veldrid;

/// <summary>
/// Конкретная реализация <see cref="IRenderContext"/> для одного фрейма.
/// Оборачивает <see cref="CommandList"/> Veldrid и предоставляет
/// движковый API без утечки Veldrid-типов наружу.
/// </summary>
internal sealed class VeldridRenderContext : IRenderContext
{
    private readonly CommandList _commandList;

    internal VeldridRenderContext(CommandList commandList, Framebuffer framebuffer)
    {
        _commandList = commandList;
        _commandList.Begin();
        _commandList.SetFramebuffer(framebuffer);

        // Устанавливаем viewport и scissor на весь framebuffer
        _commandList.SetViewport(0, new Viewport(
            x: 0, y: 0,
            width:    framebuffer.Width,
            height:   framebuffer.Height,
            minDepth: 0f,
            maxDepth: 1f));

        _commandList.SetScissorRect(
            index:  0,
            x:      0,
            y:      0,
            width:  framebuffer.Width,
            height: framebuffer.Height);
    }

    /// <inheritdoc/>
    public void ClearColorTarget(RgbaColor color)
        => _commandList.ClearColorTarget(
               index:      0,
               clearColor: new RgbaFloat(color.R, color.G, color.B, color.A));

    /// <inheritdoc/>
    public void DrawMesh(IPipeline pipeline, IMesh mesh)
    {
        // Безопасное приведение — оба типа создаются только внутри этой сборки
        var vPipeline = (VeldridPipeline)pipeline;
        var vMesh     = (VeldridMesh)mesh;

        _commandList.SetPipeline(vPipeline.NativePipeline);
        _commandList.SetVertexBuffer(0, vMesh.VertexBuffer);
        _commandList.SetIndexBuffer(vMesh.IndexBuffer, IndexFormat.UInt16);
        _commandList.DrawIndexed(vMesh.IndexCount);
    }

    /// <summary>
    /// Завершает запись команд. Вызывается из <see cref="VeldridGraphicsDevice.EndFrame"/>.
    /// </summary>
    internal void End() => _commandList.End();
}
