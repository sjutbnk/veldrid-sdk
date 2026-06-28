// src/Engine.Core/Timing/Time.cs
namespace AntigravityEngine.Core.Timing;

/// <summary>
/// Global engine time state. Updated once per frame via <see cref="Advance"/>.
/// Read <see cref="DeltaTime"/> anywhere in game code to get frame-rate independent movement.
/// </summary>
public static class Time
{
    /// <summary>Seconds elapsed between the last two frames.</summary>
    public static float DeltaTime { get; private set; }

    /// <summary>Total seconds elapsed since engine start.</summary>
    public static float TotalTime { get; private set; }

    /// <summary>
    /// Advances the global time counters. Call exactly once at the top of each game-loop iteration.
    /// </summary>
    /// <param name="deltaSeconds">Time in seconds elapsed since the previous frame.</param>
    public static void Advance(float deltaSeconds)
    {
        DeltaTime  = deltaSeconds;
        TotalTime += deltaSeconds;
    }
}
