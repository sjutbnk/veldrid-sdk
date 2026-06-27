// src/Engine.Core/Diagnostics/EngineLogger.cs
namespace AntigravityEngine.Core.Diagnostics;

/// <summary>
/// Минималистичный статический логгер движка.
/// В Phase 1 пишет в консоль с цветовой маркировкой по уровню.
/// В будущем можно заменить sink без изменения call-site.
/// </summary>
public static class EngineLogger
{
    public static void Info(string message)
    {
        WriteColored("[INFO ] ", ConsoleColor.Cyan, message);
    }

    public static void Warn(string message)
    {
        WriteColored("[WARN ] ", ConsoleColor.Yellow, message);
    }

    public static void Error(string message)
    {
        WriteColored("[ERROR] ", ConsoleColor.Red, message);
    }

    public static void Critical(string message, Exception? ex = null)
    {
        WriteColored("[CRIT ] ", ConsoleColor.DarkRed, message);
        if (ex is not null)
            WriteColored("        ", ConsoleColor.DarkRed, ex.ToString());
    }

    private static void WriteColored(string prefix, ConsoleColor color, string message)
    {
        var timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff");
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write($"{timestamp} {prefix}");
        Console.ForegroundColor = previousColor;
        Console.WriteLine(message);
    }
}
