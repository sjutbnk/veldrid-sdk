// src/Engine.Core/Platform/IEngineFactory.cs
using AntigravityEngine.Core.Graphics;

namespace AntigravityEngine.Core.Platform;

/// <summary>
/// Фабрика для создания платформенно-зависимых объектов.
/// Sandbox получает реализацию через рефлексию/DI и работает только с этим интерфейсом.
/// </summary>
public interface IEngineFactory
{
    /// <summary>
    /// Создаёт окно операционной системы.
    /// </summary>
    IWindow CreateWindow(string title, int width, int height);

    /// <summary>
    /// Создаёт GPU-устройство, привязанное к переданному окну.
    /// </summary>
    IGraphicsDevice CreateGraphicsDevice(IWindow window);
}
