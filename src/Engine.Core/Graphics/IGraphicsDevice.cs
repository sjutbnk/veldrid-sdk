// src/Engine.Core/Graphics/IGraphicsDevice.cs
namespace AntigravityEngine.Core.Graphics;

/// <summary>
/// Абстракция GPU-устройства. Владеет свайп-чейном и командными буферами.
/// Должна быть явно освобождена через <see cref="IDisposable.Dispose"/>.
/// </summary>
public interface IGraphicsDevice : IDisposable
{
    /// <summary>Имя активного графического бэкенда (например, "Vulkan", "OpenGL").</summary>
    string BackendName { get; }

    /// <summary>
    /// Начинает новый фрейм, возвращает контекст для записи команд.
    /// Каждый вызов BeginFrame ДОЛЖЕН быть завершён вызовом EndFrame.
    /// </summary>
    IRenderContext BeginFrame();

    /// <summary>
    /// Сабмитит записанные команды и выполняет SwapBuffers (Present).
    /// </summary>
    void EndFrame();

    /// <summary>
    /// Загружает вершины и индексы в GPU-буфер и возвращает <see cref="IMesh"/>.
    /// Потребитель обязан вызвать Dispose() на возвращённом объекте.
    /// </summary>
    IMesh CreateMesh(Vertex[] vertices, ushort[] indices);

    /// <summary>
    /// Компилирует GLSL-шейдеры (через SPIR-V) и создаёт <see cref="IPipeline"/>.
    /// Потребитель обязан вызвать Dispose() на возвращённом объекте.
    /// </summary>
    IPipeline CreatePipeline(PipelineDescription description);
}
