// src/Renderer.Veldrid/VeldridMesh.cs
using AntigravityEngine.Core.Graphics;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace AntigravityEngine.Renderer.Veldrid;

/// <summary>
/// Concrete implementation of <see cref="IMesh"/>.
/// Holds vertex buffer and index buffer on the GPU and manages their lifetime.
/// Vertices are stored as interleaved Position (Float3) + Color (Float4) = 28 bytes.
/// Indices are UInt32.
/// </summary>
internal sealed class VeldridMesh : IMesh
{
    private bool _disposed;

    /// <inheritdoc/>
    public uint IndexCount { get; }

    internal DeviceBuffer VertexBuffer { get; }
    internal DeviceBuffer IndexBuffer  { get; }

    [StructLayout(LayoutKind.Sequential)]
    private struct GpuVertex
    {
        public Vector3 Position;
        public Vector4 Color;
    }

    /// <summary>
    /// Creates and immediately fills GPU buffers from <see cref="MeshData"/>.
    /// </summary>
    public VeldridMesh(GraphicsDevice gd, MeshData data)
    {
        IndexCount = (uint)data.Indices.Length;

        // Build interleaved vertex array
        var verts = new GpuVertex[data.Positions.Length];
        for (int i = 0; i < verts.Length; i++)
            verts[i] = new GpuVertex { Position = data.Positions[i], Color = data.Colors[i] };

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
