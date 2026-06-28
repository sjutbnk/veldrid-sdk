// samples/Sandbox/Program.cs
//
// Antigravity Engine SDK — Phase 4: Texture Pipeline, Normals & Directional Lighting
//
// Architectural constraint enforced: zero Veldrid types or usings at compile time.
// All GPU interaction goes through Engine.Core abstractions.

using AntigravityEngine.Core.Diagnostics;
using AntigravityEngine.Core.Graphics;
using AntigravityEngine.Core.Math;
using AntigravityEngine.Core.Platform;
using AntigravityEngine.Core.Timing;
using System.Numerics;

// ── GLSL Shaders ─────────────────────────────────────────────────────────────
//
// RESOURCE LAYOUT:
//   set=0  SceneGlobals  (ViewProjection mat4, LightDirection vec4, LightColor vec4, AmbientColor vec4)
//   set=1  ModelBlock    (Model mat4)
//   set=2  Tex  (texture2D) + Samp (sampler)
//
// MATRIX CONVENTION:
//   C# System.Numerics is row-major (v * M).
//   GLSL mat4 is column-major, so bytes uploaded as-is give:
//     GLSL(M * v) == C#(v * M)
//   Therefore: compose C# matrices as  model * view * proj
//   and the GLSL shader can do:  scene.ViewProjection * model.Model * vec4(pos, 1)

const string VertexShader = """
    #version 450

    layout(set = 0, binding = 0) uniform SceneGlobals {
        mat4 ViewProjection;
        vec4 LightDirection;
        vec4 LightColor;
        vec4 AmbientColor;
    } scene;

    layout(set = 1, binding = 0) uniform ModelBlock {
        mat4 Model;
    } model;

    layout(location = 0) in vec3 Position;
    layout(location = 1) in vec3 Normal;
    layout(location = 2) in vec2 TexCoord;

    layout(location = 0) out vec3 fsin_WorldNormal;
    layout(location = 1) out vec2 fsin_TexCoord;

    void main()
    {
        // Full MVP: ViewProjection is already View*Proj; Model is the object transform.
        gl_Position    = scene.ViewProjection * model.Model * vec4(Position, 1.0);
        // For rotation-only transforms, mat3(Model) is correct for normals.
        fsin_WorldNormal = mat3(model.Model) * Normal;
        fsin_TexCoord    = TexCoord;
    }
    """;

const string FragmentShader = """
    #version 450

    layout(set = 0, binding = 0) uniform SceneGlobals {
        mat4 ViewProjection;
        vec4 LightDirection;   // xyz = direction FROM light, w unused
        vec4 LightColor;
        vec4 AmbientColor;
    } scene;

    layout(set = 2, binding = 0) uniform texture2D Tex;
    layout(set = 2, binding = 1) uniform sampler   Samp;

    layout(location = 0) in  vec3 fsin_WorldNormal;
    layout(location = 1) in  vec2 fsin_TexCoord;
    layout(location = 0) out vec4 fsout_Color;

    void main()
    {
        vec3 normal   = normalize(fsin_WorldNormal);
        vec3 lightDir = normalize(scene.LightDirection.xyz);

        // Lambertian diffuse: brightness proportional to cos(angle between normal and light)
        float NdotL = max(dot(normal, -lightDir), 0.0);

        vec4 texColor = texture(sampler2D(Tex, Samp), fsin_TexCoord);
        vec4 diffuse  = NdotL * scene.LightColor;
        vec4 ambient  = scene.AmbientColor;

        fsout_Color = (diffuse + ambient) * texColor;
    }
    """;

// ── Logging header ────────────────────────────────────────────────────────────
EngineLogger.Info("═══════════════════════════════════════════════════════════");
EngineLogger.Info("   Antigravity Engine SDK — Phase 4: Textures & Lighting   ");
EngineLogger.Info("═══════════════════════════════════════════════════════════");

// ── Engine boot ───────────────────────────────────────────────────────────────
IEngineFactory factory = EngineBootstrapper.LoadFactory();

const int Width  = 1280;
const int Height = 720;

using IWindow window = factory.CreateWindow(
    title: "Antigravity SDK — Phase 4 | Textured Lit Cube",
    width: Width, height: Height);

using IGraphicsDevice gpu = factory.CreateGraphicsDevice(window);
EngineLogger.Info($"GPU backend: {gpu.BackendName}");

// ── GPU resources ─────────────────────────────────────────────────────────────
using IMesh    cube     = gpu.CreateMesh(GeometryFactory.CreateCube());
using IPipeline pipeline = gpu.CreatePipeline(new PipelineDescription(VertexShader, FragmentShader));

// Procedural 256×256 checkerboard (16×16 tiles: Magenta #FF00FF / Black #000000)
const uint TexSize = 256u;
const int  Tile    = 16;
var pixels = new byte[TexSize * TexSize * 4];
for (int y = 0; y < (int)TexSize; y++)
{
    for (int x = 0; x < (int)TexSize; x++)
    {
        bool magenta = ((x / Tile) + (y / Tile)) % 2 == 0;
        int idx = (y * (int)TexSize + x) * 4;
        pixels[idx + 0] = magenta ? (byte)0xFF : (byte)0x00;  // R
        pixels[idx + 1] = (byte)0x00;                          // G
        pixels[idx + 2] = magenta ? (byte)0xFF : (byte)0x00;  // B
        pixels[idx + 3] = (byte)0xFF;                          // A
    }
}
using ITexture2D checkerTex = gpu.CreateTexture2D(TexSize, TexSize, pixels);

EngineLogger.Info("Shaders compiled, cube uploaded, checkerboard texture created.");
EngineLogger.Info("Close the window to exit.");

// ── Camera & lighting ─────────────────────────────────────────────────────────
var camera = new Camera(position: new Vector3(0f, 0.8f, 3.5f), pitch: -0.22f);

var lightDir    = Vector3.Normalize(new Vector3(-0.5f, -1.0f, -0.5f));
var lightColor  = new Vector4(1.00f, 0.95f, 0.88f, 1f);   // warm white
var ambientColor = new Vector4(0.12f, 0.12f, 0.18f, 1f);  // cool dark grey

// ── Game loop ─────────────────────────────────────────────────────────────────
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
var clearColor = new RgbaColor(0.06f, 0.06f, 0.10f, 1f);

const float RotSpeedX = 0.35f;  // radians per second
const float RotSpeedY = 0.60f;

float angleX = 0f;
float angleY = 0f;

while (window.Exists)
{
    float deltaTime = (float)stopwatch.Elapsed.TotalSeconds;
    stopwatch.Restart();

    window.PumpEvents();
    if (!window.Exists) break;

    // ── Update rotation (strictly DeltaTime-driven) ───────────────────────
    angleX += RotSpeedX * deltaTime;
    angleY += RotSpeedY * deltaTime;

    Matrix4x4 model =
        Matrix4x4.CreateRotationX(angleX) *
        Matrix4x4.CreateRotationY(angleY);

    // ── Upload scene-level uniforms ───────────────────────────────────────
    float aspect = (float)window.Width / window.Height;
    Matrix4x4 vp = camera.GetViewMatrix() * camera.GetProjectionMatrix(aspect);
    gpu.SetSceneGlobals(vp, lightDir, lightColor, ambientColor);

    // ── Record and submit frame ───────────────────────────────────────────
    IRenderContext ctx = gpu.BeginFrame();
    ctx.ClearColorTarget(clearColor);
    ctx.DrawMesh(pipeline, cube, model, checkerTex);
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
