// src/Engine.Core/Graphics/MeshData.cs
using System.Numerics;

namespace AntigravityEngine.Core.Graphics;

/// <summary>
/// CPU-side geometry: separate arrays for positions, normals, UV coordinates and indices.
/// Does not own any GPU resources — pure managed data.
/// Upload to GPU via <see cref="IGraphicsDevice.CreateMesh"/>.
/// </summary>
public sealed class MeshData
{
    /// <summary>Vertex positions in object-local space.</summary>
    public Vector3[] Positions { get; }

    /// <summary>Per-vertex surface normals (unit vectors). Length must equal Positions.Length.</summary>
    public Vector3[] Normals { get; }

    /// <summary>Per-vertex UV texture coordinates in [0,1]. Length must equal Positions.Length.</summary>
    public Vector2[] TexCoords { get; }

    /// <summary>Index buffer (triples form triangles). uint32 supports more than 65535 vertices.</summary>
    public uint[] Indices { get; }

    public MeshData(Vector3[] positions, Vector3[] normals, Vector2[] texCoords, uint[] indices)
    {
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(normals);
        ArgumentNullException.ThrowIfNull(texCoords);
        ArgumentNullException.ThrowIfNull(indices);

        if (positions.Length != normals.Length || positions.Length != texCoords.Length)
            throw new ArgumentException(
                $"Positions ({positions.Length}), Normals ({normals.Length}), " +
                $"and TexCoords ({texCoords.Length}) must all have equal length.");

        Positions = positions;
        Normals   = normals;
        TexCoords = texCoords;
        Indices   = indices;
    }
}
