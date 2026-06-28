// src/Engine.Core/Graphics/ITexture2D.cs
namespace AntigravityEngine.Core.Graphics;

/// <summary>
/// Abstraction of a 2D GPU texture.
/// Created via <see cref="IGraphicsDevice.CreateTexture2D"/> and must be explicitly disposed.
/// </summary>
public interface ITexture2D : IDisposable { }
