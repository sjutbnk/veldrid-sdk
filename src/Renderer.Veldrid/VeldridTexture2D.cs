// src/Renderer.Veldrid/VeldridTexture2D.cs
using AntigravityEngine.Core.Graphics;
using Veldrid;

namespace AntigravityEngine.Renderer.Veldrid;

/// <summary>
/// Concrete implementation of <see cref="ITexture2D"/>.
/// Owns a Veldrid Texture, TextureView, and the ResourceSet that binds them
/// together with the shared linear sampler for use in the fragment stage.
/// </summary>
internal sealed class VeldridTexture2D : ITexture2D
{
    private readonly global::Veldrid.Texture _texture;
    private readonly TextureView             _textureView;
    private bool                             _disposed;

    /// <summary>
    /// Pre-built ResourceSet at set=2 (Texture + Sampler).
    /// Bound by <see cref="VeldridRenderContext.DrawMesh"/> before each draw call.
    /// </summary>
    internal ResourceSet ResourceSet { get; }

    /// <summary>
    /// Creates a GPU texture from raw RGBA8 pixel data and builds the ResourceSet
    /// using the device's shared linear sampler and the texture resource layout.
    /// </summary>
    public VeldridTexture2D(
        GraphicsDevice gd,
        ResourceLayout textureLayout,
        Sampler        linearSampler,
        uint           width,
        uint           height,
        byte[]         rgbaPixelData)
    {
        // ── Create the GPU texture ────────────────────────────────────────────
        _texture = gd.ResourceFactory.CreateTexture(
            TextureDescription.Texture2D(
                width, height,
                mipLevels:   1,
                arrayLayers: 1,
                format:      PixelFormat.R8_G8_B8_A8_UNorm,
                usage:       TextureUsage.Sampled));

        // Upload pixels: gd.UpdateTexture uses glTexSubImage2D internally on OpenGL.
        gd.UpdateTexture(_texture, rgbaPixelData, 0, 0, 0, width, height, 1, 0, 0);

        // ── Create view ───────────────────────────────────────────────────────
        _textureView = gd.ResourceFactory.CreateTextureView(_texture);

        // ── Create ResourceSet for set=2 ─────────────────────────────────────
        // Binding order must match the ResourceLayout element order:
        //   binding 0 → TextureView ("Tex")
        //   binding 1 → Sampler    ("Samp")
        ResourceSet = gd.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(textureLayout, _textureView, linearSampler));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ResourceSet.Dispose();
        _textureView.Dispose();
        _texture.Dispose();
    }
}
