// samples/Sandbox/Program.cs
//
// Antigravity Engine SDK — Phase 2: Vertex Buffers & Basic Shaders.
//
// Архитектурное ограничение соблюдено: ни один тип из Veldrid, Platform.SDL
// или Renderer.Veldrid не упоминается на этапе компиляции.
// Sandbox работает исключительно через интерфейсы Engine.Core.

using AntigravityEngine.Core.Diagnostics;
using AntigravityEngine.Core.Graphics;
using AntigravityEngine.Core.Platform;
using AntigravityEngine.Core.Timing;
using System.Numerics;

// ── GLSL шейдеры (raw string literals, C# 11+) ────────────────────────────
const string VertexShader = """
    #version 450

    layout(location = 0) in vec3 Position;
    layout(location = 1) in vec4 Color;

    layout(location = 0) out vec4 fsin_Color;

    void main()
    {
        gl_Position = vec4(Position, 1.0);
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

// ── Геометрия: радужный треугольник в NDC-координатах ─────────────────────
// NDC: X и Y в диапазоне [-1, +1]. Y растёт вверх.
var vertices = new Vertex[]
{
    new(new Vector3( 0.0f,  0.55f, 0f), new Vector4(1.00f, 0.25f, 0.50f, 1f)),  // Верх   — тёплый розовый
    new(new Vector3( 0.55f,-0.45f, 0f), new Vector4(0.20f, 0.70f, 1.00f, 1f)),  // Право  — неоновый голубой
    new(new Vector3(-0.55f,-0.45f, 0f), new Vector4(0.35f, 1.00f, 0.35f, 1f)),  // Лево   — кислотный зелёный
};
var indices = new ushort[] { 0, 1, 2 };

// ── Инициализация ──────────────────────────────────────────────────────────
EngineLogger.Info("═══════════════════════════════════════════════════════");
EngineLogger.Info("   Antigravity Engine SDK — Phase 2: Shaders & Mesh    ");
EngineLogger.Info("═══════════════════════════════════════════════════════");

IEngineFactory factory = EngineBootstrapper.LoadFactory();

using IWindow window = factory.CreateWindow(
    title:  "Antigravity SDK — Phase 2 | Радужный треугольник",
    width:  1280,
    height: 720);

using IGraphicsDevice gpu = factory.CreateGraphicsDevice(window);
EngineLogger.Info($"GPU-бэкенд: {gpu.BackendName}");

// Загружаем геометрию и компилируем шейдеры (GLSL → SPIR-V → бэкенд)
using IMesh    triangle = gpu.CreateMesh(vertices, indices);
using IPipeline pipeline = gpu.CreatePipeline(new PipelineDescription(VertexShader, FragmentShader));

EngineLogger.Info("Шейдеры скомпилированы, буферы загружены. Запуск цикла...");

// ── Игровой цикл ───────────────────────────────────────────────────────────
var timer      = new GameTimer();
var clearColor = RgbaColor.DarkSlateBlue;   // 0.08, 0.08, 0.12, 1.0

while (window.Exists)
{
    timer.Tick();
    window.PumpEvents();
    if (!window.Exists) break;

    IRenderContext ctx = gpu.BeginFrame();

    ctx.ClearColorTarget(clearColor);         // Тёмный фон
    ctx.DrawMesh(pipeline, triangle);          // Радужный треугольник

    gpu.EndFrame();
}

// using-блоки освобождают ресурсы в порядке LIFO:
//   pipeline.Dispose() → triangle.Dispose() → gpu.Dispose() → window.Dispose()
EngineLogger.Info("Выход. Все GPU-ресурсы освобождены.");

// ── Загрузчик фабрики через рефлексию (Thin RHI) ──────────────────────────
static class EngineBootstrapper
{
    private const string FactoryAssembly = "Renderer.Veldrid";
    private const string FactoryTypeName = "AntigravityEngine.Renderer.Veldrid.VeldridEngineFactory";

    public static IEngineFactory LoadFactory()
    {
        EngineLogger.Info($"Загрузка фабрики из {FactoryAssembly}...");
        var asm  = System.Reflection.Assembly.Load(FactoryAssembly);
        var type = asm.GetType(FactoryTypeName)
            ?? throw new InvalidOperationException($"Тип '{FactoryTypeName}' не найден.");
        var factory = Activator.CreateInstance(type) as IEngineFactory
            ?? throw new InvalidOperationException($"Не удалось создать '{FactoryTypeName}'.");
        EngineLogger.Info("Фабрика загружена.");
        return factory;
    }
}
