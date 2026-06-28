// src/Engine.Core/Graphics/VertexPositionNormalTexture.cs
using System.Numerics;
using System.Runtime.InteropServices;

namespace AntigravityEngine.Core.Graphics;

/// <summary>
/// Vertex with 3D position, surface normal, and UV texture coordinates.
/// Sequential layout: [Position: 12B] [Normal: 12B] [TexCoord: 8B] = 32 bytes total.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct VertexPositionNormalTexture
{
    public readonly Vector3 Position;
    public readonly Vector3 Normal;
    public readonly Vector2 TexCoord;

    /// <summary>Size of one vertex in bytes (32).</summary>
    public const uint SizeInBytes = 32u;

    public VertexPositionNormalTexture(Vector3 position, Vector3 normal, Vector2 texCoord)
    {
        Position = position;
        Normal   = normal;
        TexCoord = texCoord;
    }
}
