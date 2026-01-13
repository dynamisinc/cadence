using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.ExerciseClock.Models.DTOs;

/// <summary>
/// DTO for exercise clock state.
/// </summary>
public record ClockStateDto(
    /// <summary>
    /// The exercise ID this clock belongs to.
    /// </summary>
    Guid ExerciseId,

    /// <summary>
    /// Current clock state (Stopped, Running, Paused).
    /// </summary>
    ExerciseClockState State,

    /// <summary>
    /// UTC timestamp when the clock was last started.
    /// Null if never started.
    /// </summary>
    DateTime? StartedAt,

    /// <summary>
    /// Total elapsed time in the exercise.
    /// Includes time from previous running periods.
    /// </summary>
    TimeSpan ElapsedTime,

    /// <summary>
    /// User who last started the clock.
    /// </summary>
    Guid? StartedBy,

    /// <summary>
    /// Display name of the user who started the clock.
    /// </summary>
    string? StartedByName,

    /// <summary>
    /// UTC timestamp of when this state was captured.
    /// </summary>
    DateTime CapturedAt
);

/// <summary>
/// Extension methods for clock state.
/// </summary>
public static class ClockMapper
{
    /// <summary>
    /// Calculate the current clock state DTO from an exercise entity.
    /// </summary>
    public static ClockStateDto ToClockStateDto(this Exercise exercise)
    {
        var elapsed = exercise.ClockElapsedBeforePause ?? TimeSpan.Zero;

        // If currently running, add time since start
        if (exercise.ClockState == ExerciseClockState.Running && exercise.ClockStartedAt.HasValue)
        {
            elapsed += DateTime.UtcNow - exercise.ClockStartedAt.Value;
        }

        return new ClockStateDto(
            exercise.Id,
            exercise.ClockState,
            exercise.ClockStartedAt,
            elapsed,
            exercise.ClockStartedBy,
            exercise.ClockStartedByUser?.DisplayName,
            DateTime.UtcNow
        );
    }
}
