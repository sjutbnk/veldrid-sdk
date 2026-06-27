// src/Renderer.Veldrid/VeldridGraphicsDevice.cs
using AntigravityEngine.Core.Diagnostics;
using AntigravityEngine.Core.Graphics;
using AntigravityEngine.Core.Platform;
using AntigravityEngine.Platform.SDL;
using Veldrid;
using Veldrid.StartupUtilities;

namespace AntigravityEngine.Renderer.Veldrid;

/// <summary>
/// Конкретная реализация <see cref="IGraphicsDevice"/> поверх Veldrid.
/// Предпочитает Vulkan; при ошибке инициализации откатывается на OpenGL.
/// </summary>
public sealed class VeldridGraphicsDevice : IGraphicsDevice
{
    private readonly GraphicsDevice _gd;
    private readonly CommandList _commandList;
    private bool _disposed;

    /// <inheritdoc/>
    public string BackendName { get; }

    /// <summary>
    /// Инициализирует GPU-устройство, привязанное к переданному <see cref="SdlWindow"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Если передан не <see cref="SdlWindow"/>.</exception>
    public VeldridGraphicsDevice(IWindow window)
    {
        if (window is not SdlWindow sdlWindow)
            throw new ArgumentException(
                $"VeldridGraphicsDevice требует {nameof(SdlWindow)}, получен {window.GetType().Name}.",
                nameof(window));

        var graphicsDeviceOptions = new GraphicsDeviceOptions(
            debug: false,
            swapchainDepthFormat: null,
            syncToVerticalBlank: true,
            resourceBindingModel: ResourceBindingModel.Improved,
            preferDepthRangeZeroToOne: true,
            preferStandardClipSpaceYDirection: true
        );

        _gd = TryCreateVulkan(sdlWindow.NativeWindow, graphicsDeviceOptions)
            ?? CreateOpenGL(sdlWindow.NativeWindow, graphicsDeviceOptions);

        BackendName = _gd.BackendType.ToString();
        EngineLogger.Info($"GPU-бэкенд инициализирован: {BackendName} | Устройство: {_gd.DeviceName}");

        _commandList = _gd.ResourceFactory.CreateCommandList();
    }

    /// <inheritdoc/>
    public IRenderContext BeginFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new VeldridRenderContext(_commandList, _gd.SwapchainFramebuffer);
    }

    /// <inheritdoc/>
    public void EndFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Завершаем запись команд (BeginFrame создал VeldridRenderContext, который уже вызвал Begin())
        _commandList.End();
        _gd.SubmitCommands(_commandList);
        _gd.SwapBuffers();
        _gd.WaitForIdle();
    }

    /// <inheritdoc/>
    public IMesh CreateMesh(Vertex[] vertices, ushort[] indices)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new VeldridMesh(_gd, vertices, indices);
    }

    /// <inheritdoc/>
    public IPipeline CreatePipeline(PipelineDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new VeldridPipeline(
            _gd.ResourceFactory,
            _gd.SwapchainFramebuffer.OutputDescription,
            description);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _commandList.Dispose();
        _gd.Dispose();

        EngineLogger.Info("VeldridGraphicsDevice освобождён.");
    }

    // ─── Приватные вспомогательные методы ───────────────────────────────────

    private static GraphicsDevice? TryCreateVulkan(
        global::Veldrid.Sdl2.Sdl2Window nativeWindow,
        GraphicsDeviceOptions options)
    {
        try
        {
            EngineLogger.Info("Попытка инициализации Vulkan...");
            return VeldridStartup.CreateGraphicsDevice(nativeWindow, options, GraphicsBackend.Vulkan);
        }
        catch (Exception ex)
        {
            EngineLogger.Warn($"Vulkan недоступен: {ex.Message}. Откат на OpenGL.");
            return null;
        }
    }

    private static GraphicsDevice CreateOpenGL(
        global::Veldrid.Sdl2.Sdl2Window nativeWindow,
        GraphicsDeviceOptions options)
    {
        EngineLogger.Info("Инициализация OpenGL...");
        return VeldridStartup.CreateGraphicsDevice(nativeWindow, options, GraphicsBackend.OpenGL);
    }
}
