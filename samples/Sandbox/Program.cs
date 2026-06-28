// samples/Sandbox/Program.cs
//
// Antigravity Engine SDK — Phase 3: Camera & Spinning 3D Cube
//
// Architectural constraint enforced: no Veldrid types referenced at compile time.
// Sandbox works exclusively through Engine.Core abstractions.

using AntigravityEngine.Core.Diagnostics;
using AntigravityEngine.Core.Graphics;
using AntigravityEngine.Core.Math;
using AntigravityEngine.Core.Platform;
using AntigravityEngine.Core.Timing;
using System.Numerics;

// ── GLSL shaders ──────────────────────────────────────────────────────────────
// The vertex shader reads the MVP matrix from a UBO at set=0, binding=0.
// TextureCoordinate semantics are used here because Veldrid.SPIRV cross-compiles
// them correctly to attribute locations 0 and 1 for both OpenGL and Vulkan.
const string VertexShader = """
    #version 450

    layout(set = 0, binding = 0) uniform MvpBlock {
        mat4 Mvp;
    } u;

    layout(location = 0) in vec3 Position;
    layout(location = 1) in vec4 Color;

    layout(location = 0) out vec4 fsin_Color;

    void main()
    {
        gl_Position = u.Mvp * vec4(Position, 1.0);
        fsin_Color  = Color;
    }
    """;

const string FragmentShader = """
    #version 450

    layout(location = 0) in  vec4 fsin_Color;
    layout(location = 0) out vec4 fsout_Color;

    void main()
    {
        fsout_Color = fsin_Color;
    }
    """;

// ── Engine boot ───────────────────────────────────────────────────────────────
EngineLogger.Info("═══════════════════════════════════════════════════════");
EngineLogger.Info("   Antigravity Engine SDK — Phase 3: Cube & Camera     ");
EngineLogger.Info("═══════════════════════════════════════════════════════");

IEngineFactory factory = EngineBootstrapper.LoadFactory();

const int Width  = 1280;
const int Height = 720;

using IWindow window = factory.CreateWindow(
    title:  "Antigravity SDK — Phase 3 | Spinning Cube",
    width:  Width,
    height: Height);

using IGraphicsDevice gpu = factory.CreateGraphicsDevice(window);
EngineLogger.Info($"GPU backend: {gpu.BackendName}");

// ── Resources ─────────────────────────────────────────────────────────────────
MeshData cubeData = GeometryFactory.CreateCube();
using IMesh    cube     = gpu.CreateMesh(cubeData);
using IPipeline pipeline = gpu.CreatePipeline(new PipelineDescription(VertexShader, FragmentShader));

EngineLogger.Info("Shaders compiled, cube uploaded to GPU.");
EngineLogger.Info("Controls: arrow keys — orbit camera. Close window to quit.");

// ── Camera ────────────────────────────────────────────────────────────────────
var camera = new Camera(position: new Vector3(0f, 0f, 4f));

// ── Game loop ─────────────────────────────────────────────────────────────────
var timer      = new GameTimer();
var clearColor = new RgbaColor(0.07f, 0.07f, 0.12f, 1f);

float totalTime = 0f;

while (window.Exists)
{
    timer.Tick();
    float dt = (float)timer.DeltaTime.TotalSeconds;
    totalTime += dt;

    window.PumpEvents();
    if (!window.Exists) break;

    // Slowly auto-rotate the cube around X and Y axes
    float angleX = totalTime * 0.4f;   // ~23 °/s
    float angleY = totalTime * 0.7f;   // ~40 °/s

    Matrix4x4 model =
        Matrix4x4.CreateRotationX(angleX) *
        Matrix4x4.CreateRotationY(angleY);

    float aspect = (float)Width / Height;
    Matrix4x4 mvp = model * camera.GetViewMatrix() * camera.GetProjectionMatrix(aspect);

    // Upload MVP to GPU
    gpu.SetMvp(mvp);

    // Record and submit
    IRenderContext ctx = gpu.BeginFrame();
    ctx.ClearColorTarget(clearColor);
    ctx.DrawMesh(pipeline, cube);
    gpu.EndFrame();
}

EngineLogger.Info("Exit. All GPU resources released.");

// ── Factory loader via reflection (Thin RHI) ──────────────────────────────────
static class EngineBootstrapper
{
    private const string FactoryAssembly = "Renderer.Veldrid";
    private const string FactoryTypeName = "AntigravityEngine.Renderer.Veldrid.VeldridEngineFactory";

    public static IEngineFactory LoadFactory()
    {
        EngineLogger.Info($"Loading factory from '{FactoryAssembly}'...");
        var asm     = System.Reflection.Assembly.Load(FactoryAssembly);
        var type    = asm.GetType(FactoryTypeName)
            ?? throw new InvalidOperationException($"Type '{FactoryTypeName}' not found.");
        var factory = Activator.CreateInstance(type) as IEngineFactory
            ?? throw new InvalidOperationException($"Could not create '{FactoryTypeName}'.");
        EngineLogger.Info("Factory loaded.");
        return factory;
    }
}
