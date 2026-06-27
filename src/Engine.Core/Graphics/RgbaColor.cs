// src/Engine.Core/Graphics/RgbaColor.cs
namespace AntigravityEngine.Core.Graphics;

/// <summary>
/// Нормализованный цвет RGBA (значения от 0.0f до 1.0f).
/// Используется везде, где нужен цвет без привязки к конкретному бэкенду.
/// </summary>
public readonly record struct RgbaColor(float R, float G, float B, float A)
{
    /// <summary>Непрозрачный чёрный.</summary>
    public static readonly RgbaColor Black = new(0f, 0f, 0f, 1f);

    /// <summary>Тёмный сине-серый фон — цвет очистки буфера по умолчанию.</summary>
    public static readonly RgbaColor DarkSlateBlue = new(0.08f, 0.08f, 0.12f, 1.0f);

    /// <summary>Непрозрачный белый.</summary>
    public static readonly RgbaColor White = new(1f, 1f, 1f, 1f);
}
