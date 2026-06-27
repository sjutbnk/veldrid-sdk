// src/Engine.Core/Graphics/PipelineDescription.cs
namespace AntigravityEngine.Core.Graphics;

/// <summary>
/// Описание графического конвейера, передаваемое в <see cref="IGraphicsDevice.CreatePipeline"/>.
/// <para>
/// <see cref="VertexGlsl"/> и <see cref="FragmentGlsl"/> — исходный код шейдеров на GLSL #version 450.
/// Рендерер сам компилирует их в SPIR-V и выполняет кросс-компиляцию под нужный бэкенд.
/// </para>
/// </summary>
public sealed record PipelineDescription(string VertexGlsl, string FragmentGlsl);
