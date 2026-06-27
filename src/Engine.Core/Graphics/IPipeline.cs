// src/Engine.Core/Graphics/IPipeline.cs
namespace AntigravityEngine.Core.Graphics;

/// <summary>
/// Абстракция графического конвейера (шейдеры, rasterizerState, blendState и т.д.).
/// Создаётся через <see cref="IGraphicsDevice.CreatePipeline"/> и должна быть явно освобождена.
/// </summary>
public interface IPipeline : IDisposable { }
