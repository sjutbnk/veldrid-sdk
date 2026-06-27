// src/Renderer.Veldrid/VeldridMesh.cs
using AntigravityEngine.Core.Graphics;
using System.Runtime.CompilerServices;
using Veldrid;

namespace AntigravityEngine.Renderer.Veldrid;

/// <summary>
/// Конкретная реализация <see cref="IMesh"/>.
/// Хранит vertex buffer и index buffer на GPU, управляет их жизненным циклом.
/// </summary>
internal sealed class VeldridMesh : IMesh
{
    private bool _disposed;

    /// <inheritdoc/>
    public uint IndexCount { get; }

    internal DeviceBuffer VertexBuffer { get; }
    internal DeviceBuffer IndexBuffer  { get; }

    /// <summary>
    /// Создаёт и немедленно заполняет GPU-буферы переданными данными.
    /// </summary>
    public VeldridMesh(GraphicsDevice gd, Vertex[] vertices, ushort[] indices)
    {
        IndexCount = (uint)indices.Length;

        // Vertex buffer
        var vertexBufferSize = (uint)(vertices.Length * Unsafe.SizeOf<Vertex>());
        VertexBuffer = gd.ResourceFactory.CreateBuffer(
            new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer));
        gd.UpdateBuffer(VertexBuffer, bufferOffsetInBytes: 0, source: vertices);

        // Index buffer
        var indexBufferSize = (uint)(indices.Length * sizeof(ushort));
        IndexBuffer = gd.ResourceFactory.CreateBuffer(
            new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer));
        gd.UpdateBuffer(IndexBuffer, bufferOffsetInBytes: 0, source: indices);
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
