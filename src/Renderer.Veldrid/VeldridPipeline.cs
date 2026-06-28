// src/Renderer.Veldrid/VeldridPipeline.cs
using AntigravityEngine.Core.Graphics;
using Veldrid;
using Veldrid.SPIRV;

namespace AntigravityEngine.Renderer.Veldrid;

/// <summary>
/// Concrete implementation of <see cref="IPipeline"/>.
/// Compiles GLSL → SPIR-V → backend shaders, then assembles a Veldrid GraphicsPipeline.
/// Requires a <see cref="ResourceLayout"/> for the MVP UBO (set=0, binding=0).
/// </summary>
internal sealed class VeldridPipeline : IPipeline
{
    private bool _disposed;

    internal Pipeline NativePipeline { get; }

    public VeldridPipeline(
        ResourceFactory   factory,
        OutputDescription outputDescription,
        PipelineDescription desc,
        ResourceLayout    mvpLayout)
    {
        // ── 1. GLSL → SPIR-V ──────────────────────────────────────────────────
        var vertSpirv = SpirvCompilation.CompileGlslToSpirv(
            desc.VertexGlsl, "vertex.glsl", ShaderStages.Vertex,
            new GlslCompileOptions(debug: false));

        var fragSpirv = SpirvCompilation.CompileGlslToSpirv(
            desc.FragmentGlsl, "fragment.glsl", ShaderStages.Fragment,
            new GlslCompileOptions(debug: false));

        // ── 2. SPIR-V → backend shaders ──────────────────────────────────────
        var shaders = factory.CreateFromSpirv(
            new ShaderDescription(ShaderStages.Vertex,   vertSpirv.SpirvBytes,   "main"),
            new ShaderDescription(ShaderStages.Fragment, fragSpirv.SpirvBytes,   "main"));

        // ── 3. Vertex layout: GpuVertex = Position (Float3, 12B) + Color (Float4, 16B)
        // TextureCoordinate semantics work for both OpenGL and Vulkan backends
        // — they map to attribute locations 0 and 1 respectively.
        var vertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("Color",    VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

        // ── 4. Assemble pipeline ──────────────────────────────────────────────
        // CullNone: eliminates winding-order ambiguity (depth test handles occlusion).
        // DepthOnlyLessEqual: standard depth test, clear to 1.0 → fragments at z<1 pass.
        var pipelineDesc = new GraphicsPipelineDescription(
            blendState:                  BlendStateDescription.SingleOverrideBlend,
            depthStencilStateDescription: DepthStencilStateDescription.DepthOnlyLessEqual,
            rasterizerState:             RasterizerStateDescription.CullNone,
            primitiveTopology:           PrimitiveTopology.TriangleList,
            shaderSet:                   new ShaderSetDescription(new[] { vertexLayout }, shaders),
            resourceLayouts:             new[] { mvpLayout },
            outputs:                     outputDescription);

        NativePipeline = factory.CreateGraphicsPipeline(ref pipelineDesc);

        foreach (var s in shaders) s.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        NativePipeline.Dispose();
    }
}
