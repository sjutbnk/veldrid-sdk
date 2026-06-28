// src/Renderer.Veldrid/VeldridMesh.cs
using AntigravityEngine.Core.Graphics;
using System.Runtime.InteropServices;
using Veldrid;

namespace AntigravityEngine.Renderer.Veldrid;

/// <summary>
/// Concrete implementation of <see cref="IMesh"/>.
/// Interleaves <see cref="MeshData"/> into a tightly packed
/// VertexPositionNormalTexture GPU vertex buffer and a UInt32 index buffer.
/// </summary>
internal sealed class VeldridMesh : IMesh
{
    private bool _disposed;

    /// <inheritdoc/>
    public uint IndexCount { get; }

    internal DeviceBuffer VertexBuffer { get; }
    internal DeviceBuffer IndexBuffer  { get; }

    // Internal GPU vertex that exactly mirrors VertexPositionNormalTexture (32 bytes).
    [StructLayout(LayoutKind.Sequential)]
    private struct GpuVertex
    {
        public System.Numerics.Vector3 Position;   // offset  0, 12 bytes
        public System.Numerics.Vector3 Normal;     // offset 12, 12 bytes
        public System.Numerics.Vector2 TexCoord;   // offset 24,  8 bytes
    }                                               // total: 32 bytes

    public VeldridMesh(GraphicsDevice gd, MeshData data)
    {
        IndexCount = (uint)data.Indices.Length;

        // Interleave into GPU vertex array
        var verts = new GpuVertex[data.Positions.Length];
        for (int i = 0; i < verts.Length; i++)
            verts[i] = new GpuVertex
            {
                Position = data.Positions[i],
                Normal   = data.Normals[i],
                TexCoord = data.TexCoords[i],
            };

        uint vbSize = (uint)(verts.Length * Marshal.SizeOf<GpuVertex>());
        VertexBuffer = gd.ResourceFactory.CreateBuffer(
            new BufferDescription(vbSize, BufferUsage.VertexBuffer));
        gd.UpdateBuffer(VertexBuffer, 0u, verts);

        uint ibSize = (uint)(data.Indices.Length * sizeof(uint));
        IndexBuffer = gd.ResourceFactory.CreateBuffer(
            new BufferDescription(ibSize, BufferUsage.IndexBuffer));
        gd.UpdateBuffer(IndexBuffer, 0u, data.Indices);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        VertexBuffer.Dispose();
        IndexBuffer.Dispose();
    }
}
