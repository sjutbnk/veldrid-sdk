// src/Engine.Core/Graphics/GeometryFactory.cs
using System.Numerics;

namespace AntigravityEngine.Core.Graphics;

/// <summary>
/// Static factory for common CPU-side geometry primitives.
/// All meshes are centred at the origin at unit scale.
/// </summary>
public static class GeometryFactory
{
    /// <summary>
    /// Creates a unit cube (1×1×1) centred at the origin with hard (flat) face normals
    /// and standard [0,1]² UV coordinates on each face.
    /// 24 vertices (4 per face), 36 indices (2 triangles × 6 faces).
    /// </summary>
    public static MeshData CreateCube()
    {
        // ── Helper to build one face ──────────────────────────────────────────
        // Each face is CCW when viewed from the outside.
        // UVs: bottom-left=(0,1), bottom-right=(1,1), top-right=(1,0), top-left=(0,0)

        var positions = new Vector3[24];
        var normals   = new Vector3[24];
        var uvs       = new Vector2[24];

        void Face(int start, Vector3 n,
                  Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl)
        {
            positions[start + 0] = bl; positions[start + 1] = br;
            positions[start + 2] = tr; positions[start + 3] = tl;

            normals[start + 0] = n; normals[start + 1] = n;
            normals[start + 2] = n; normals[start + 3] = n;

            uvs[start + 0] = new Vector2(0f, 1f);
            uvs[start + 1] = new Vector2(1f, 1f);
            uvs[start + 2] = new Vector2(1f, 0f);
            uvs[start + 3] = new Vector2(0f, 0f);
        }

        // +Z  front
        Face(0,  new Vector3( 0,  0,  1),
            new(-0.5f, -0.5f,  0.5f), new( 0.5f, -0.5f,  0.5f),
            new( 0.5f,  0.5f,  0.5f), new(-0.5f,  0.5f,  0.5f));

        // -Z  back
        Face(4,  new Vector3( 0,  0, -1),
            new( 0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f, -0.5f),
            new(-0.5f,  0.5f, -0.5f), new( 0.5f,  0.5f, -0.5f));

        // +X  right
        Face(8,  new Vector3( 1,  0,  0),
            new( 0.5f, -0.5f,  0.5f), new( 0.5f, -0.5f, -0.5f),
            new( 0.5f,  0.5f, -0.5f), new( 0.5f,  0.5f,  0.5f));

        // -X  left
        Face(12, new Vector3(-1,  0,  0),
            new(-0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f,  0.5f),
            new(-0.5f,  0.5f,  0.5f), new(-0.5f,  0.5f, -0.5f));

        // +Y  top
        Face(16, new Vector3( 0,  1,  0),
            new(-0.5f,  0.5f,  0.5f), new( 0.5f,  0.5f,  0.5f),
            new( 0.5f,  0.5f, -0.5f), new(-0.5f,  0.5f, -0.5f));

        // -Y  bottom
        Face(20, new Vector3( 0, -1,  0),
            new(-0.5f, -0.5f, -0.5f), new( 0.5f, -0.5f, -0.5f),
            new( 0.5f, -0.5f,  0.5f), new(-0.5f, -0.5f,  0.5f));

        // Two triangles per face, CCW winding: (0,1,2) and (0,2,3)
        var indices = new uint[36];
        for (uint f = 0; f < 6; f++)
        {
            uint b = f * 4, o = f * 6;
            indices[o + 0] = b + 0; indices[o + 1] = b + 1; indices[o + 2] = b + 2;
            indices[o + 3] = b + 0; indices[o + 4] = b + 2; indices[o + 5] = b + 3;
        }

        return new MeshData(positions, normals, uvs, indices);
    }
}
