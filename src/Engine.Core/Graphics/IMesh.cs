// src/Engine.Core/Graphics/IMesh.cs
namespace AntigravityEngine.Core.Graphics;

/// <summary>
/// Абстракция загруженной геометрии на GPU (vertex buffer + index buffer).
/// Создаётся через <see cref="IGraphicsDevice.CreateMesh"/> и должна быть явно освобождена.
/// </summary>
public interface IMesh : IDisposable
{
    /// <summary>Количество индексов (= количество вершин при Draw).</summary>
    uint IndexCount { get; }
}
