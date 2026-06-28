// src/Engine.Core/Graphics/GeometryFactory.cs
using System.Numerics;

namespace AntigravityEngine.Core.Graphics;

/// <summary>
/// Static factory for common CPU-side geometry primitives.
/// All meshes are centered at the origin, unit scale.
/// </summary>
public static class GeometryFactory
{
    /// <summary>
    /// Creates a unit cube (1×1×1) centred at origin.
    /// 24 vertices (4 per face, so each face has its own solid color),
    /// 36 indices (6 per face × 6 faces).
    /// </summary>
    public static MeshData CreateCube()
    {
        // Each face is built from 4 unique vertices so colors are flat per-face.
        // Winding order: counter-clockwise for front faces.

        var p = new Vector3[]
        {
            // +Z  (front,  red)
            new(-0.5f, -0.5f,  0.5f),
            new( 0.5f, -0.5f,  0.5f),
            new( 0.5f,  0.5f,  0.5f),
            new(-0.5f,  0.5f,  0.5f),

            // -Z  (back,   cyan)
            new( 0.5f, -0.5f, -0.5f),
            new(-0.5f, -0.5f, -0.5f),
            new(-0.5f,  0.5f, -0.5f),
            new( 0.5f,  0.5f, -0.5f),

            // +X  (right,  green)
            new( 0.5f, -0.5f,  0.5f),
            new( 0.5f, -0.5f, -0.5f),
            new( 0.5f,  0.5f, -0.5f),
            new( 0.5f,  0.5f,  0.5f),

            // -X  (left,   magenta)
            new(-0.5f, -0.5f, -0.5f),
            new(-0.5f, -0.5f,  0.5f),
            new(-0.5f,  0.5f,  0.5f),
            new(-0.5f,  0.5f, -0.5f),

            // +Y  (top,    blue)
            new(-0.5f,  0.5f,  0.5f),
            new( 0.5f,  0.5f,  0.5f),
            new( 0.5f,  0.5f, -0.5f),
            new(-0.5f,  0.5f, -0.5f),

            // -Y  (bottom, yellow)
            new(-0.5f, -0.5f, -0.5f),
            new( 0.5f, -0.5f, -0.5f),
            new( 0.5f, -0.5f,  0.5f),
            new(-0.5f, -0.5f,  0.5f),
        };

        var red     = new Vector4(0.95f, 0.25f, 0.25f, 1f);
        var cyan    = new Vector4(0.20f, 0.85f, 0.85f, 1f);
        var green   = new Vector4(0.25f, 0.90f, 0.30f, 1f);
        var magenta = new Vector4(0.90f, 0.25f, 0.90f, 1f);
        var blue    = new Vector4(0.25f, 0.45f, 0.95f, 1f);
        var yellow  = new Vector4(0.95f, 0.85f, 0.15f, 1f);

        var c = new Vector4[24];
        for (int i =  0; i <  4; i++) c[i]      = red;
        for (int i =  4; i <  8; i++) c[i]      = cyan;
        for (int i =  8; i < 12; i++) c[i]      = green;
        for (int i = 12; i < 16; i++) c[i]      = magenta;
        for (int i = 16; i < 20; i++) c[i]      = blue;
        for (int i = 20; i < 24; i++) c[i]      = yellow;

        // Two triangles per face (quad split). CCW winding.
        var indices = new uint[36];
        for (uint f = 0; f < 6; f++)
        {
            uint b = f * 4;
            uint o = f * 6;
            indices[o + 0] = b + 0;
            indices[o + 1] = b + 1;
            indices[o + 2] = b + 2;
            indices[o + 3] = b + 0;
            indices[o + 4] = b + 2;
            indices[o + 5] = b + 3;
        }

        return new MeshData(p, c, indices);
    }
}
