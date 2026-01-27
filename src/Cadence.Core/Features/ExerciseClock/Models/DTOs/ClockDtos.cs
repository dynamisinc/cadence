using Cadence.Core.Data;
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
    /// ApplicationUser ID who last started the clock.
    /// </summary>
    string? StartedBy,

    /// <summary>
    /// Display name of the user who started the clock.
    /// </summary>
    string? StartedByName,

    /// <summary>
    /// UTC timestamp of when this state was captured.
    /// </summary>
    DateTime CapturedAt,

    /// <summary>
    /// Planned start time for the exercise (e.g., 09:00).
    /// Used by frontend to calculate inject scheduled offsets.
    /// Null if not set on the exercise.
    /// </summary>
    TimeOnly? ExerciseStartTime
);

/// <summary>
/// Extension methods for clock state.
/// </summary>
public static class ClockMapper
{
    /// <summary>
    /// Calculate the current clock state DTO from an exercise entity.
    /// Does not include StartedByName (returns null).
    /// </summary>
    public static ClockStateDto ToClockStateDto(this Exercise exercise)
    {
        return exercise.ToClockStateDto(startedByName: null);
    }

    /// <summary>
    /// Calculate the current clock state DTO from an exercise entity.
    /// Looks up the ApplicationUser to get the display name.
    /// </summary>
    public static ClockStateDto ToClockStateDto(this Exercise exercise, AppDbContext context)
    {
        string? startedByName = null;
        if (!string.IsNullOrEmpty(exercise.ClockStartedBy))
        {
            var user = context.ApplicationUsers.Find(exercise.ClockStartedBy);
            startedByName = user?.DisplayName;
        }

        return exercise.ToClockStateDto(startedByName);
    }

    /// <summary>
    /// Calculate the current clock state DTO from an exercise entity with explicit display name.
    /// </summary>
    private static ClockStateDto ToClockStateDto(this Exercise exercise, string? startedByName)
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
            startedByName,
            DateTime.UtcNow,
            exercise.StartTime
        );
    }
}
