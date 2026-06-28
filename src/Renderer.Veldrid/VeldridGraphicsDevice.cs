// src/Renderer.Veldrid/VeldridGraphicsDevice.cs
using AntigravityEngine.Core.Diagnostics;
using AntigravityEngine.Core.Graphics;
using AntigravityEngine.Core.Platform;
using AntigravityEngine.Platform.SDL;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.StartupUtilities;

namespace AntigravityEngine.Renderer.Veldrid;

/// <summary>
/// Concrete implementation of <see cref="IGraphicsDevice"/> built on top of Veldrid.
/// Prefers Vulkan; falls back to OpenGL on failure.
/// Owns the swapchain, command list, and the global MVP UBO.
/// </summary>
public sealed class VeldridGraphicsDevice : IGraphicsDevice
{
    private readonly GraphicsDevice  _gd;
    private readonly CommandList     _commandList;

    // ── MVP Uniform Buffer Object ─────────────────────────────────────────────
    private readonly DeviceBuffer   _mvpBuffer;
    private readonly ResourceLayout _mvpLayout;
    private readonly ResourceSet    _mvpResourceSet;

    private VeldridRenderContext? _activeContext;
    private bool _disposed;

    /// <inheritdoc/>
    public string BackendName { get; }

    public VeldridGraphicsDevice(IWindow window)
    {
        if (window is not SdlWindow sdlWindow)
            throw new ArgumentException(
                $"VeldridGraphicsDevice requires {nameof(SdlWindow)}, got {window.GetType().Name}.",
                nameof(window));

        // Must be done BEFORE any Veldrid/Vulkan code is touched.
        RegisterNativeLibraryResolver();

        var options = new GraphicsDeviceOptions(
            debug:                          false,
            swapchainDepthFormat:           PixelFormat.D24_UNorm_S8_UInt,
            syncToVerticalBlank:            true,
            resourceBindingModel:           ResourceBindingModel.Improved,
            preferDepthRangeZeroToOne:      true,
            preferStandardClipSpaceYDirection: true);

        _gd = TryCreateVulkan(sdlWindow.NativeWindow, options)
           ?? CreateOpenGL(sdlWindow.NativeWindow, options);

        BackendName = _gd.BackendType.ToString();
        EngineLogger.Info($"GPU backend: {BackendName} | Device: {_gd.DeviceName}");

        // ── MVP UBO ───────────────────────────────────────────────────────────
        _mvpLayout = _gd.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "MvpBlock", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

        _mvpBuffer = _gd.ResourceFactory.CreateBuffer(
            new BufferDescription(64u, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

        _mvpResourceSet = _gd.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(_mvpLayout, _mvpBuffer));

        // Upload identity matrix as default so the buffer is never uninitialised
        _gd.UpdateBuffer(_mvpBuffer, 0u, Matrix4x4.Identity);

        _commandList = _gd.ResourceFactory.CreateCommandList();
        EngineLogger.Info("MVP UBO and CommandList initialised.");
    }

    // ── IGraphicsDevice ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public IRenderContext BeginFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _activeContext = new VeldridRenderContext(
            _commandList, _gd.SwapchainFramebuffer, _mvpResourceSet);
        return _activeContext;
    }

    /// <inheritdoc/>
    public void EndFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _activeContext?.End();
        _activeContext = null;
        _gd.SubmitCommands(_commandList);
        _gd.SwapBuffers();
        _gd.WaitForIdle();
    }

    /// <inheritdoc/>
    public IMesh CreateMesh(MeshData data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(data);
        return new VeldridMesh(_gd, data);
    }

    /// <inheritdoc/>
    public IPipeline CreatePipeline(PipelineDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(description);
        return new VeldridPipeline(
            _gd.ResourceFactory,
            _gd.SwapchainFramebuffer.OutputDescription,
            description,
            _mvpLayout);
    }

    /// <inheritdoc/>
    public void SetMvp(Matrix4x4 mvp)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // KEY INSIGHT: System.Numerics.Matrix4x4 is row-major in memory.
        // When GLSL reads it as column-major mat4, the computation:
        //     GLSL:  Mvp * vec4(pos, 1)
        // is mathematically equivalent to:
        //     C#:    Vector4.Transform(pos, mvp)   (i.e., v * M)
        //
        // which is exactly what we want — Model*View*Proj applied to the vertex.
        // DO NOT transpose: transposing would flip the meaning and break the transform.
        _gd.UpdateBuffer(_mvpBuffer, 0u, mvp);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _mvpResourceSet.Dispose();
        _mvpBuffer.Dispose();
        _mvpLayout.Dispose();
        _commandList.Dispose();
        _gd.Dispose();

        EngineLogger.Info("VeldridGraphicsDevice disposed.");
    }

    // ── Backend creation helpers ──────────────────────────────────────────────

    private static GraphicsDevice? TryCreateVulkan(
        global::Veldrid.Sdl2.Sdl2Window nativeWindow,
        GraphicsDeviceOptions options)
    {
        try
        {
            EngineLogger.Info("Attempting Vulkan initialisation...");
            var gd = VeldridStartup.CreateGraphicsDevice(nativeWindow, options, GraphicsBackend.Vulkan);
            EngineLogger.Info("Vulkan initialised successfully.");
            return gd;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.WriteLine("=== VULKAN INIT FAILED (full diagnostic) ===");
            Console.Error.WriteLine(ex.ToString());
            if (ex.InnerException is not null)
            {
                Console.Error.WriteLine("--- InnerException ---");
                Console.Error.WriteLine(ex.InnerException.ToString());
            }
            Console.ResetColor();
            EngineLogger.Warn($"Vulkan unavailable: {ex.Message}. Falling back to OpenGL.");
            return null;
        }
    }

    private static GraphicsDevice CreateOpenGL(
        global::Veldrid.Sdl2.Sdl2Window nativeWindow,
        GraphicsDeviceOptions options)
    {
        EngineLogger.Info("Initialising OpenGL...");
        return VeldridStartup.CreateGraphicsDevice(nativeWindow, options, GraphicsBackend.OpenGL);
    }

    // ── Native library resolver ───────────────────────────────────────────────

    private static bool _resolverRegistered;
    private static readonly object _resolverLock = new();

    /// <summary>
    /// Registers a custom P/Invoke resolver on EVERY assembly in the AppDomain,
    /// and hooks future assembly loads.  This intercepts:
    /// <list type="bullet">
    ///   <item><c>libdl</c> → <c>libdl.so.2</c> (Vulkan C# bindings need dlerror/dlopen)</item>
    ///   <item><c>vulkan</c> → <c>libvulkan.so.1</c> (actual Vulkan loader)</item>
    /// </list>
    /// Must be called before any Veldrid/Vulkan type is first accessed.
    /// </summary>
    private static void RegisterNativeLibraryResolver()
    {
        lock (_resolverLock)
        {
            if (_resolverRegistered) return;
            _resolverRegistered = true;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;

        DllImportResolver resolver = LinuxNativeResolver;

        // Register on all currently loaded assemblies
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            TryRegisterResolver(asm, resolver);

        // Register on all future assemblies (e.g. "vk" / "Vulkan" that load lazily)
        AppDomain.CurrentDomain.AssemblyLoad += (_, args) =>
            TryRegisterResolver(args.LoadedAssembly, resolver);
    }

    private static void TryRegisterResolver(Assembly asm, DllImportResolver resolver)
    {
        try { NativeLibrary.SetDllImportResolver(asm, resolver); }
        catch { /* Some assemblies don't support custom resolvers; ignore silently. */ }
    }

    private static IntPtr LinuxNativeResolver(
        string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // ── libdl: needed by the Vulkan C# binding (vk.NET) to call dlopen/dlerror
        // On modern glibc (Arch/CachyOS) these symbols live in libc.so.6.
        // There is usually also a libdl.so.2 compat stub.
        if (libraryName is "libdl" or "dl" or "libdl.so")
        {
            if (NativeLibrary.TryLoad("libdl.so.2", assembly, searchPath, out var h)) return h;
            if (NativeLibrary.TryLoad("libc.so.6",  assembly, searchPath, out h))    return h;
            if (NativeLibrary.TryLoad("libc",        assembly, searchPath, out h))    return h;
        }

        // ── Vulkan loader: Arch/CachyOS installs libvulkan.so.1 (not libvulkan.so)
        if (libraryName is "vulkan" or "libvulkan" or "libvulkan.so")
        {
            if (NativeLibrary.TryLoad("libvulkan.so.1", assembly, searchPath, out var h)) return h;
            if (NativeLibrary.TryLoad("libvulkan.so",   assembly, searchPath, out h))    return h;
        }

        return IntPtr.Zero; // fall back to default resolution
    }
}
