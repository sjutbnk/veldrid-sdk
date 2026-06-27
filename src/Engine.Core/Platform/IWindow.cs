// src/Engine.Core/Platform/IWindow.cs
namespace AntigravityEngine.Core.Platform;

/// <summary>
/// Абстракция окна операционной системы.
/// Реализация живёт в Platform.SDL; Sandbox ссылается только на этот интерфейс.
/// </summary>
public interface IWindow : IDisposable
{
    /// <summary>Заголовок окна.</summary>
    string Title { get; }

    /// <summary>Ширина клиентской области в пикселях.</summary>
    int Width { get; }

    /// <summary>Высота клиентской области в пикселях.</summary>
    int Height { get; }

    /// <summary>
    /// <c>true</c>, пока окно не закрыто пользователем.
    /// Используется как условие главного игрового цикла.
    /// </summary>
    bool Exists { get; }

    /// <summary>
    /// Перекачивает системную очередь событий (нажатия, изменения окна и т.д.).
    /// Вызывается один раз в начале каждого тика игрового цикла.
    /// </summary>
    void PumpEvents();
}
