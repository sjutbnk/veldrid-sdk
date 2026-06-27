// src/Engine.Core/Graphics/IRenderContext.cs
namespace AntigravityEngine.Core.Graphics;

/// <summary>
/// Контекст одного фрейма: команды отрисовки, очистки, сабмита.
/// Получается через <see cref="IGraphicsDevice.BeginFrame"/> и должен
/// быть завершён вызовом <see cref="IGraphicsDevice.EndFrame"/>.
/// </summary>
public interface IRenderContext
{
    /// <summary>
    /// Очищает цветовой буфер заданным цветом.
    /// </summary>
    void ClearColorTarget(RgbaColor color);

    /// <summary>
    /// Привязывает <see cref="IPipeline"/> и рисует геометрию <see cref="IMesh"/>.
    /// </summary>
    void DrawMesh(IPipeline pipeline, IMesh mesh);
}
