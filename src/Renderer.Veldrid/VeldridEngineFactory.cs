// src/Renderer.Veldrid/VeldridEngineFactory.cs
using AntigravityEngine.Core.Graphics;
using AntigravityEngine.Core.Platform;
using AntigravityEngine.Platform.SDL;

namespace AntigravityEngine.Renderer.Veldrid;

/// <summary>
/// Конкретная реализация <see cref="IEngineFactory"/>.
/// Связывает Platform.SDL и Renderer.Veldrid в единую точку входа.
/// Загружается Sandbox-ом через рефлексию — ни один тип из этой сборки
/// не упоминается напрямую в Sandbox.csproj на этапе компиляции.
/// </summary>
public sealed class VeldridEngineFactory : IEngineFactory
{
    /// <inheritdoc/>
    public IWindow CreateWindow(string title, int width, int height)
        => new SdlWindow(title, width, height);

    /// <inheritdoc/>
    public IGraphicsDevice CreateGraphicsDevice(IWindow window)
        => new VeldridGraphicsDevice(window);
}
