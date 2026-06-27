// src/Engine.Core/Timing/GameTimer.cs
using System.Diagnostics;

namespace AntigravityEngine.Core.Timing;

/// <summary>
/// Высокоточный таймер игрового цикла на базе <see cref="Stopwatch"/>.
/// Измеряет реальное прошедшее время (delta time) между тиками.
/// </summary>
public sealed class GameTimer
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private long _lastTicks;

    /// <summary>Полное время с момента создания таймера.</summary>
    public TimeSpan TotalTime => _stopwatch.Elapsed;

    /// <summary>Время, прошедшее с предыдущего тика (delta time).</summary>
    public TimeSpan DeltaTime { get; private set; }

    /// <summary>
    /// Фиксирует текущий момент и вычисляет <see cref="DeltaTime"/>.
    /// Должен вызываться один раз в начале каждого тика игрового цикла.
    /// </summary>
    public void Tick()
    {
        var currentTicks = _stopwatch.ElapsedTicks;
        DeltaTime = TimeSpan.FromTicks(currentTicks - _lastTicks);
        _lastTicks = currentTicks;
    }
}
