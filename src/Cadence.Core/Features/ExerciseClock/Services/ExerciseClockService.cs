using Cadence.Core.Data;
using Cadence.Core.Features.ExerciseClock.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.ExerciseClock.Services;

/// <summary>
/// Service for exercise clock operations.
/// </summary>
public class ExerciseClockService : IExerciseClockService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExerciseClockService> _logger;

    public ExerciseClockService(AppDbContext context, ILogger<ExerciseClockService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ClockStateDto?> GetClockStateAsync(Guid exerciseId)
    {
        var exercise = await _context.Exercises
            .Include(e => e.ClockStartedByUser)
            .FirstOrDefaultAsync(e => e.Id == exerciseId);

        return exercise?.ToClockStateDto();
    }

    /// <inheritdoc />
    public async Task<ClockStateDto> StartClockAsync(Guid exerciseId, Guid startedBy)
    {
        var exercise = await _context.Exercises
            .Include(e => e.ClockStartedByUser)
            .FirstOrDefaultAsync(e => e.Id == exerciseId);

        if (exercise == null)
        {
            throw new InvalidOperationException($"Exercise {exerciseId} not found");
        }

        // Validate exercise status - can only run clock for Active exercises
        if (exercise.Status != ExerciseStatus.Active && exercise.Status != ExerciseStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Cannot start clock for {exercise.Status} exercise. Exercise must be Draft or Active.");
        }

        // Validate clock state
        if (exercise.ClockState == ExerciseClockState.Running)
        {
            throw new InvalidOperationException("Clock is already running");
        }

        // If starting from Draft, transition to Active
        if (exercise.Status == ExerciseStatus.Draft)
        {
            exercise.Status = ExerciseStatus.Active;
            _logger.LogInformation("Exercise {ExerciseId} transitioned from Draft to Active", exerciseId);
        }

        // Start the clock
        exercise.ClockState = ExerciseClockState.Running;
        exercise.ClockStartedAt = DateTime.UtcNow;
        exercise.ClockStartedBy = startedBy;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Started clock for exercise {ExerciseId} at {StartedAt}",
            exerciseId, exercise.ClockStartedAt);

        // Reload user navigation
        await _context.Entry(exercise).Reference(e => e.ClockStartedByUser).LoadAsync();

        return exercise.ToClockStateDto();
    }

    /// <inheritdoc />
    public async Task<ClockStateDto> PauseClockAsync(Guid exerciseId, Guid pausedBy)
    {
        var exercise = await _context.Exercises
            .Include(e => e.ClockStartedByUser)
            .FirstOrDefaultAsync(e => e.Id == exerciseId);

        if (exercise == null)
        {
            throw new InvalidOperationException($"Exercise {exerciseId} not found");
        }

        // Validate clock state
        if (exercise.ClockState != ExerciseClockState.Running)
        {
            throw new InvalidOperationException(
                $"Cannot pause clock that is not running. Current state: {exercise.ClockState}");
        }

        // Calculate elapsed time and store it
        var elapsedSinceStart = exercise.ClockStartedAt.HasValue
            ? DateTime.UtcNow - exercise.ClockStartedAt.Value
            : TimeSpan.Zero;

        exercise.ClockElapsedBeforePause = (exercise.ClockElapsedBeforePause ?? TimeSpan.Zero) + elapsedSinceStart;
        exercise.ClockState = ExerciseClockState.Paused;
        exercise.ClockStartedAt = null; // Clear start time when paused

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Paused clock for exercise {ExerciseId}. Total elapsed: {Elapsed}",
            exerciseId, exercise.ClockElapsedBeforePause);

        return exercise.ToClockStateDto();
    }

    /// <inheritdoc />
    public async Task<ClockStateDto> StopClockAsync(Guid exerciseId, Guid stoppedBy)
    {
        var exercise = await _context.Exercises
            .Include(e => e.ClockStartedByUser)
            .FirstOrDefaultAsync(e => e.Id == exerciseId);

        if (exercise == null)
        {
            throw new InvalidOperationException($"Exercise {exerciseId} not found");
        }

        // Validate clock state - can only stop if running or paused
        if (exercise.ClockState == ExerciseClockState.Stopped)
        {
            throw new InvalidOperationException("Clock is already stopped");
        }

        // If running, capture final elapsed time
        if (exercise.ClockState == ExerciseClockState.Running && exercise.ClockStartedAt.HasValue)
        {
            var elapsedSinceStart = DateTime.UtcNow - exercise.ClockStartedAt.Value;
            exercise.ClockElapsedBeforePause = (exercise.ClockElapsedBeforePause ?? TimeSpan.Zero) + elapsedSinceStart;
        }

        // Stop the clock and complete the exercise
        exercise.ClockState = ExerciseClockState.Stopped;
        exercise.ClockStartedAt = null;
        exercise.Status = ExerciseStatus.Completed;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Stopped clock for exercise {ExerciseId}. Exercise marked as Completed. Final elapsed: {Elapsed}",
            exerciseId, exercise.ClockElapsedBeforePause);

        return exercise.ToClockStateDto();
    }

    /// <inheritdoc />
    public async Task<ClockStateDto> ResetClockAsync(Guid exerciseId, Guid resetBy)
    {
        var exercise = await _context.Exercises
            .Include(e => e.ClockStartedByUser)
            .FirstOrDefaultAsync(e => e.Id == exerciseId);

        if (exercise == null)
        {
            throw new InvalidOperationException($"Exercise {exerciseId} not found");
        }

        // Can only reset in Draft status or when clock is Stopped
        if (exercise.Status != ExerciseStatus.Draft && exercise.ClockState != ExerciseClockState.Stopped)
        {
            throw new InvalidOperationException(
                "Can only reset clock when exercise is Draft or clock is Stopped");
        }

        // Reset clock state
        exercise.ClockState = ExerciseClockState.Stopped;
        exercise.ClockStartedAt = null;
        exercise.ClockElapsedBeforePause = null;
        exercise.ClockStartedBy = null;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reset clock for exercise {ExerciseId}", exerciseId);

        return exercise.ToClockStateDto();
    }
}
