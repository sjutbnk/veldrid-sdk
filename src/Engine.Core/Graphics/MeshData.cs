// src/Engine.Core/Graphics/MeshData.cs
using System.Numerics;

namespace AntigravityEngine.Core.Graphics;

/// <summary>
/// CPU-side geometry: separate arrays for positions, colors and indices.
/// Does not own any GPU resources — pure managed data.
/// Upload to GPU via <see cref="IGraphicsDevice.CreateMesh(MeshData)"/>.
/// </summary>
public sealed class MeshData
{
    /// <summary>Vertex positions in object-local space.</summary>
    public Vector3[] Positions { get; }

    /// <summary>Per-vertex RGBA colors (0.0–1.0). Length must equal Positions.Length.</summary>
    public Vector4[] Colors { get; }

    /// <summary>Index buffer (triples form triangles). uint32 supports meshes with more than 65535 vertices.</summary>
    public uint[] Indices { get; }

    public MeshData(Vector3[] positions, Vector4[] colors, uint[] indices)
    {
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(colors);
        ArgumentNullException.ThrowIfNull(indices);

        if (positions.Length != colors.Length)
            throw new ArgumentException(
                $"Positions ({positions.Length}) and Colors ({colors.Length}) must have equal length.");

        Positions = positions;
        Colors    = colors;
        Indices   = indices;
    }
}
