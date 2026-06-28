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
///
/// Owns three resource layouts and their associated GPU buffers/sampler:
/// <list type="bullet">
///   <item>set=0  SceneGlobals  (ViewProj + lighting params)</item>
///   <item>set=1  ModelBlock    (per-object Model matrix)</item>
///   <item>set=2  Texture2D + linear Sampler</item>
/// </list>
/// </summary>
public sealed class VeldridGraphicsDevice : IGraphicsDevice
{
    private readonly GraphicsDevice _gd;
    private readonly CommandList    _commandList;

    // ── Scene globals (set=0) ─────────────────────────────────────────────────
    // Layout:  [ViewProjection: 64B][LightDirection: 16B][LightColor: 16B][AmbientColor: 16B]
    //           = 112 bytes total, std140-compatible.
    [StructLayout(LayoutKind.Sequential)]
    private struct SceneGlobalsGpu   // 112 bytes
    {
        public Matrix4x4 ViewProjection;  // 64 bytes
        public Vector4   LightDirection;  // 16 bytes (xyz = dir, w = 0)
        public Vector4   LightColor;      // 16 bytes
        public Vector4   AmbientColor;    // 16 bytes
    }

    private readonly ResourceLayout _sceneLayout;
    private readonly DeviceBuffer   _sceneBuffer;
    private readonly ResourceSet    _sceneResourceSet;

    // ── Model matrix (set=1) ──────────────────────────────────────────────────
    private readonly ResourceLayout _modelLayout;
    private readonly DeviceBuffer   _modelBuffer;       // 64 bytes (mat4)
    private readonly ResourceSet    _modelResourceSet;

    // ── Texture + Sampler (set=2) ─────────────────────────────────────────────
    private readonly ResourceLayout _textureLayout;
    private readonly Sampler        _linearSampler;

    private VeldridRenderContext? _activeContext;
    private bool                  _disposed;

    /// <inheritdoc/>
    public string BackendName { get; }

    // ─────────────────────────────────────────────────────────────────────────

    public VeldridGraphicsDevice(IWindow window)
    {
        if (window is not SdlWindow sdlWindow)
            throw new ArgumentException(
                $"VeldridGraphicsDevice requires {nameof(SdlWindow)}, got {window.GetType().Name}.",
                nameof(window));

        // Must be called before any Veldrid or Vulkan type is touched.
        RegisterNativeLibraryResolver();

        var options = new GraphicsDeviceOptions(
            debug:                             false,
            swapchainDepthFormat:              PixelFormat.D24_UNorm_S8_UInt,
            syncToVerticalBlank:               true,   // VSync on
            resourceBindingModel:              ResourceBindingModel.Improved,
            preferDepthRangeZeroToOne:         true,
            preferStandardClipSpaceYDirection: true);

        _gd = TryCreateVulkan(sdlWindow.NativeWindow, options)
           ?? CreateOpenGL(sdlWindow.NativeWindow, options);

        BackendName = _gd.BackendType.ToString();
        EngineLogger.Info($"GPU backend: {BackendName} | Device: {_gd.DeviceName}");

        var factory = _gd.ResourceFactory;

        // ── set=0  SceneGlobals ───────────────────────────────────────────────
        _sceneLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription(
                "SceneGlobals", ResourceKind.UniformBuffer,
                ShaderStages.Vertex | ShaderStages.Fragment)));

        _sceneBuffer = factory.CreateBuffer(
            new BufferDescription(112u, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

        _sceneResourceSet = factory.CreateResourceSet(
            new ResourceSetDescription(_sceneLayout, _sceneBuffer));

        // Initialise buffer with identity VP so the first frame is safe.
        _gd.UpdateBuffer(_sceneBuffer, 0u, new SceneGlobalsGpu
        {
            ViewProjection = Matrix4x4.Identity,
            LightDirection = Vector4.UnitZ,
            LightColor     = Vector4.One,
            AmbientColor   = new Vector4(0.15f, 0.15f, 0.15f, 1f),
        });

        // ── set=1  ModelBlock ─────────────────────────────────────────────────
        _modelLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription(
                "ModelBlock", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

        _modelBuffer = factory.CreateBuffer(
            new BufferDescription(64u, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

        _modelResourceSet = factory.CreateResourceSet(
            new ResourceSetDescription(_modelLayout, _modelBuffer));

        _gd.UpdateBuffer(_modelBuffer, 0u, Matrix4x4.Identity);

        // ── set=2  Texture + Sampler layout ──────────────────────────────────
        _textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription(
                "Tex",  ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription(
                "Samp", ResourceKind.Sampler,         ShaderStages.Fragment)));

        _linearSampler = factory.CreateSampler(new SamplerDescription(
            addressModeU:      SamplerAddressMode.Wrap,
            addressModeV:      SamplerAddressMode.Wrap,
            addressModeW:      SamplerAddressMode.Wrap,
            filter:            SamplerFilter.MinLinear_MagLinear_MipLinear,
            comparisonKind:    null,
            maximumAnisotropy: 0,
            minimumLod:        0u,
            maximumLod:        uint.MaxValue,
            lodBias:           0,
            borderColor:       SamplerBorderColor.TransparentBlack));

        // ── Command list ──────────────────────────────────────────────────────
        _commandList = factory.CreateCommandList();
        EngineLogger.Info("SceneGlobals, ModelBlock, Texture layouts and CommandList initialised.");
    }

    // ── IGraphicsDevice ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public IRenderContext BeginFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _activeContext = new VeldridRenderContext(
            _commandList,
            _gd.SwapchainFramebuffer,
            _gd,
            _modelBuffer,
            _sceneResourceSet,
            _modelResourceSet);
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
            _sceneLayout,
            _modelLayout,
            _textureLayout);
    }

    /// <inheritdoc/>
    public ITexture2D CreateTexture2D(uint width, uint height, byte[] rgbaPixelData)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(rgbaPixelData);
        if (rgbaPixelData.Length != (int)(width * height * 4))
            throw new ArgumentException(
                $"Expected {width * height * 4} bytes, got {rgbaPixelData.Length}.");

        return new VeldridTexture2D(_gd, _textureLayout, _linearSampler, width, height, rgbaPixelData);
    }

    /// <inheritdoc/>
    public void SetSceneGlobals(
        Matrix4x4 viewProjection,
        Vector3   lightDirection,
        Vector4   lightColor,
        Vector4   ambientColor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // C# Matrix4x4 is row-major. GLSL mat4 is column-major.
        // When Veldrid uploads bytes directly, GLSL(M*v) == C#(v*M).
        // Do NOT transpose — the convention works correctly as-is.
        _gd.UpdateBuffer(_sceneBuffer, 0u, new SceneGlobalsGpu
        {
            ViewProjection = viewProjection,
            LightDirection = new Vector4(lightDirection, 0f),
            LightColor     = lightColor,
            AmbientColor   = ambientColor,
        });
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Dispose in reverse creation order
        _commandList.Dispose();
        _linearSampler.Dispose();
        _textureLayout.Dispose();
        _modelResourceSet.Dispose();
        _modelBuffer.Dispose();
        _modelLayout.Dispose();
        _sceneResourceSet.Dispose();
        _sceneBuffer.Dispose();
        _sceneLayout.Dispose();
        _gd.Dispose();

        EngineLogger.Info("VeldridGraphicsDevice disposed.");
    }

    // ── Backend creation ──────────────────────────────────────────────────────

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
    /// Registers a P/Invoke resolver on every current and future assembly so that:
    /// <list type="bullet">
    ///   <item><c>"libdl"</c>   → <c>libdl.so.2</c> / <c>libc.so.6</c> (needed by vk.NET's Libdl)</item>
    ///   <item><c>"vulkan"</c>  → <c>libvulkan.so.1</c> (Arch/CachyOS Vulkan loader name)</item>
    /// </list>
    /// Must run before any Veldrid or Vulkan code accesses native libraries.
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

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            TrySetResolver(asm, resolver);

        AppDomain.CurrentDomain.AssemblyLoad += (_, args) =>
            TrySetResolver(args.LoadedAssembly, resolver);
    }

    private static void TrySetResolver(Assembly asm, DllImportResolver resolver)
    {
        try { NativeLibrary.SetDllImportResolver(asm, resolver); }
        catch { /* Some assemblies don't support custom resolvers — ignore. */ }
    }

    private static IntPtr LinuxNativeResolver(
        string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // libdl: required by vk.NET (Vulkan.Libdl) for dlopen/dlerror.
        // On modern glibc (Arch/CachyOS) the symbols live in libc.so.6.
        if (libraryName is "libdl" or "dl" or "libdl.so")
        {
            if (NativeLibrary.TryLoad("libdl.so.2", assembly, searchPath, out var h)) return h;
            if (NativeLibrary.TryLoad("libc.so.6",  assembly, searchPath, out h))    return h;
            if (NativeLibrary.TryLoad("libc",        assembly, searchPath, out h))    return h;
        }

        // Vulkan loader: Arch/CachyOS installs libvulkan.so.1, not libvulkan.so.
        if (libraryName is "vulkan" or "libvulkan" or "libvulkan.so")
        {
            if (NativeLibrary.TryLoad("libvulkan.so.1", assembly, searchPath, out var h)) return h;
            if (NativeLibrary.TryLoad("libvulkan.so",   assembly, searchPath, out h))    return h;
        }

        return IntPtr.Zero;
    }
}
