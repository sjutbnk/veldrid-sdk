// src/Engine.Core/Graphics/Vertex.cs
using System.Numerics;
using System.Runtime.InteropServices;

namespace AntigravityEngine.Core.Graphics;

/// <summary>
/// Вершина с трёхмерной позицией и цветом RGBA.
/// Layout (sequential): [Position: 3×float = 12 байт] [Color: 4×float = 16 байт] = 28 байт.
/// StructLayout.Sequential гарантирует корректную передачу в GPU-буфер.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Vertex
{
    /// <summary>Позиция в пространстве (NDC для Phase 2, мировые координаты позже).</summary>
    public readonly Vector3 Position;

    /// <summary>Цвет вершины в формате RGBA (0.0–1.0).</summary>
    public readonly Vector4 Color;

    /// <summary>Размер одной вершины в байтах.</summary>
    public const uint SizeInBytes = 28u;

    public Vertex(Vector3 position, Vector4 color)
    {
        Position = position;
        Color    = color;
    }
}
