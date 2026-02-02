using Cadence.Core.Features.ExerciseClock.Models.DTOs;

namespace Cadence.Core.Features.ExerciseClock.Services;

/// <summary>
/// Service interface for exercise clock operations.
/// </summary>
public interface IExerciseClockService
{
    /// <summary>
    /// Get the current clock state for an exercise.
    /// </summary>
    Task<ClockStateDto?> GetClockStateAsync(Guid exerciseId);

    /// <summary>
    /// Start the exercise clock.
    /// Exercise must be in Active status and clock must be Stopped or Paused.
    /// </summary>
    Task<ClockStateDto> StartClockAsync(Guid exerciseId, string startedBy);

    /// <summary>
    /// Pause the exercise clock.
    /// Clock must be currently Running.
    /// </summary>
    Task<ClockStateDto> PauseClockAsync(Guid exerciseId, string pausedBy);

    /// <summary>
    /// Stop the exercise clock and complete the exercise.
    /// Clock must be Running or Paused.
    /// </summary>
    Task<ClockStateDto> StopClockAsync(Guid exerciseId, string stoppedBy);

    /// <summary>
    /// Reset the exercise clock to zero.
    /// Only allowed when exercise is in Draft status or clock is Stopped.
    /// </summary>
    Task<ClockStateDto> ResetClockAsync(Guid exerciseId, string resetBy);
}
