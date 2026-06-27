// src/Renderer.Veldrid/VeldridPipeline.cs
using AntigravityEngine.Core.Graphics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace AntigravityEngine.Renderer.Veldrid;

/// <summary>
/// Конкретная реализация <see cref="IPipeline"/>.
/// <para>
/// Флоу: GLSL source → <see cref="SpirvCompilation.CompileGlslToSpirv"/> → SPIR-V bytes
///       → <see cref="ResourceFactoryExtensions.CreateFromSpirv"/> → кросс-компиляция под
///       текущий бэкенд (Vulkan/OpenGL) → <see cref="Shader"/>[] → <see cref="Pipeline"/>.
/// </para>
/// </summary>
internal sealed class VeldridPipeline : IPipeline
{
    private bool _disposed;

    /// <summary>Нативный Veldrid-пайплайн. Используется из VeldridRenderContext.</summary>
    internal Pipeline NativePipeline { get; }

    public VeldridPipeline(
        ResourceFactory     factory,
        OutputDescription   outputDescription,
        PipelineDescription desc)
    {
        // ── 1. Компилируем GLSL → SPIR-V (через shaderc) ─────────────────────
        var vertexSpirv = SpirvCompilation.CompileGlslToSpirv(
            desc.VertexGlsl,
            "vertex_shader.glsl",
            ShaderStages.Vertex,
            new GlslCompileOptions(debug: false));

        var fragmentSpirv = SpirvCompilation.CompileGlslToSpirv(
            desc.FragmentGlsl,
            "fragment_shader.glsl",
            ShaderStages.Fragment,
            new GlslCompileOptions(debug: false));

        // ── 2. SPIR-V → бэкенд-специфичные шейдеры (Veldrid.SPIRV кросс-компилятор) ──
        var vertexDesc   = new ShaderDescription(ShaderStages.Vertex,   vertexSpirv.SpirvBytes,   "main");
        var fragmentDesc = new ShaderDescription(ShaderStages.Fragment, fragmentSpirv.SpirvBytes, "main");
        var shaders      = factory.CreateFromSpirv(vertexDesc, fragmentDesc);

        // ── 3. Описываем layout вершин, совпадающий со struct Vertex ──────────
        // Position: Float3 (12 байт), Color: Float4 (16 байт) — offset 0 и 12
        var vertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("Color",    VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

        // ── 4. Собираем GraphicsPipeline ──────────────────────────────────────
        var pipelineDesc = new GraphicsPipelineDescription(
            BlendStateDescription.SingleAlphaBlend,
            DepthStencilStateDescription.Disabled,
            RasterizerStateDescription.CullNone,
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription(new[] { vertexLayout }, shaders),
            Array.Empty<ResourceLayout>(),
            outputDescription);

        NativePipeline = factory.CreateGraphicsPipeline(ref pipelineDesc);

        // Шейдеры можно освободить сразу — пайплайн держит свои копии
        foreach (var shader in shaders)
            shader.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        NativePipeline.Dispose();
    }
}
