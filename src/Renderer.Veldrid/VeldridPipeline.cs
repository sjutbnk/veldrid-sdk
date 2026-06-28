// src/Renderer.Veldrid/VeldridPipeline.cs
using AntigravityEngine.Core.Graphics;
using Veldrid;
using Veldrid.SPIRV;

namespace AntigravityEngine.Renderer.Veldrid;

/// <summary>
/// Concrete implementation of <see cref="IPipeline"/>.
/// Compiles GLSL → SPIR-V → backend shaders and creates a GraphicsPipeline
/// with three resource layouts:
/// <list type="bullet">
///   <item>set=0  SceneGlobals UBO  (ViewProjection + lighting)</item>
///   <item>set=1  ModelBlock UBO    (per-object Model matrix)</item>
///   <item>set=2  Texture + Sampler (per-material albedo)</item>
/// </list>
/// </summary>
internal sealed class VeldridPipeline : IPipeline
{
    private bool _disposed;

    internal Pipeline NativePipeline { get; }

    public VeldridPipeline(
        ResourceFactory   factory,
        OutputDescription outputDescription,
        PipelineDescription desc,
        ResourceLayout    sceneLayout,
        ResourceLayout    modelLayout,
        ResourceLayout    textureLayout)
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
            new ShaderDescription(ShaderStages.Vertex,   vertSpirv.SpirvBytes, "main"),
            new ShaderDescription(ShaderStages.Fragment, fragSpirv.SpirvBytes, "main"));

        // ── 3. Vertex layout: Position (Float3) | Normal (Float3) | TexCoord (Float2)
        //       = 32 bytes per vertex, matching VeldridMesh.GpuVertex.
        var vertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("Normal",   VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

        // ── 4. Assemble pipeline ──────────────────────────────────────────────
        // Fallback: If OutputDescription lacks a depth attachment, ensure we configure one,
        // otherwise the pipeline completely disables depth testing!
        if (!outputDescription.DepthAttachment.HasValue)
        {
            outputDescription.DepthAttachment = new OutputAttachmentDescription(PixelFormat.D24_UNorm_S8_UInt);
        }

        var pipelineDesc = new GraphicsPipelineDescription(
            blendState:                  BlendStateDescription.SingleOverrideBlend,
            depthStencilStateDescription: DepthStencilStateDescription.DepthOnlyLessEqual,
            rasterizerState:             new RasterizerStateDescription(
                cullMode: FaceCullMode.Back,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.CounterClockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false),
            primitiveTopology:           PrimitiveTopology.TriangleList,
            shaderSet:                   new ShaderSetDescription(new[] { vertexLayout }, shaders),
            resourceLayouts:             new[] { sceneLayout, modelLayout, textureLayout },
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
