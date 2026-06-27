// src/Platform.SDL/SdlWindow.cs
using AntigravityEngine.Core.Platform;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace AntigravityEngine.Platform.SDL;

/// <summary>
/// Конкретная реализация <see cref="IWindow"/> поверх <see cref="Sdl2Window"/>.
/// Владеет нативным окном SDL2 и отвечает за его жизненный цикл.
/// </summary>
public sealed class SdlWindow : IWindow
{
    private readonly Sdl2Window _sdlWindow;
    private bool _disposed;

    /// <summary>
    /// Создаёт окно SDL2 по центру экрана с заданными параметрами.
    /// </summary>
    public SdlWindow(string title, int width, int height)
    {
        var windowCreateInfo = new WindowCreateInfo(
            x: 100,
            y: 100,
            windowWidth: width,
            windowHeight: height,
            windowInitialState: WindowState.Normal,
            windowTitle: title
        );

        _sdlWindow = VeldridStartup.CreateWindow(ref windowCreateInfo);
    }

    /// <inheritdoc/>
    public string Title => _sdlWindow.Title;

    /// <inheritdoc/>
    public int Width => _sdlWindow.Width;

    /// <inheritdoc/>
    public int Height => _sdlWindow.Height;

    /// <inheritdoc/>
    public bool Exists => _sdlWindow.Exists;

    /// <summary>
    /// Предоставляет доступ к нативному SDL2-окну для слоя рендеринга.
    /// Намеренно не является частью IWindow — используется только внутри движка.
    /// </summary>
    public Sdl2Window NativeWindow => _sdlWindow;

    /// <inheritdoc/>
    public void PumpEvents()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _sdlWindow.PumpEvents();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Sdl2Window не реализует IDisposable напрямую,
        // но при Close() освобождает нативный SDL_Window.
        if (_sdlWindow.Exists)
            _sdlWindow.Close();
    }
}
