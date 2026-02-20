using Cadence.Core.Data;
using Cadence.Core.Features.ExerciseClock.Models.DTOs;
using Cadence.Core.Features.Injects.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.ExerciseClock.Services;

/// <summary>
/// Service for exercise clock operations.
/// </summary>
public class ExerciseClockService : IExerciseClockService
{
    /// <summary>Default max duration when none is configured (72 hours).</summary>
    public static readonly TimeSpan DefaultMaxDuration = TimeSpan.FromHours(72);

    /// <summary>Absolute maximum duration that can never be exceeded (2 weeks / 336 hours).</summary>
    public static readonly TimeSpan AbsoluteMaxDuration = TimeSpan.FromDays(14);

    private readonly AppDbContext _context;
    private readonly IExerciseHubContext _hubContext;
    private readonly IInjectReadinessService _injectReadinessService;
    private readonly ILogger<ExerciseClockService> _logger;

    public ExerciseClockService(
        AppDbContext context,
        IExerciseHubContext hubContext,
        IInjectReadinessService injectReadinessService,
        ILogger<ExerciseClockService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _injectReadinessService = injectReadinessService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ClockStateDto?> GetClockStateAsync(Guid exerciseId)
    {
        var exercise = await _context.Exercises
                        .FirstOrDefaultAsync(e => e.Id == exerciseId);

        return exercise?.ToClockStateDto();
    }

    /// <inheritdoc />
    public async Task<ClockStateDto> StartClockAsync(Guid exerciseId, string startedBy)
    {
        var exercise = await _context.Exercises
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

        // Validate max duration not already exceeded (when resuming from pause)
        var effectiveMax = GetEffectiveMaxDuration(exercise);
        var currentElapsed = exercise.ClockElapsedBeforePause ?? TimeSpan.Zero;
        if (currentElapsed >= effectiveMax)
        {
            throw new InvalidOperationException(
                $"Exercise has reached its maximum duration of {effectiveMax.TotalHours:F0} hours. Reset the clock or increase the max duration to continue.");
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

        var clockState = exercise.ToClockStateDto(_context);

        // Log the clock event for timeline tracking
        await LogClockEventAsync(exerciseId, ClockEventType.Started, startedBy, clockState.ElapsedTime);

        // Broadcast clock started event to all connected clients
        await _hubContext.NotifyClockStarted(exerciseId, clockState);

        // Immediately evaluate for any past-due injects
        await _injectReadinessService.EvaluateExerciseAsync(exerciseId);

        return clockState;
    }

    /// <inheritdoc />
    public async Task<ClockStateDto> PauseClockAsync(Guid exerciseId, string pausedBy)
    {
        var exercise = await _context.Exercises
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

        var totalElapsed = (exercise.ClockElapsedBeforePause ?? TimeSpan.Zero) + elapsedSinceStart;

        // Clamp to max duration to prevent overshoot
        var effectiveMax = GetEffectiveMaxDuration(exercise);
        if (totalElapsed > effectiveMax)
        {
            _logger.LogWarning(
                "Exercise {ExerciseId} elapsed time {Elapsed} exceeded max duration {MaxDuration}. Clamping.",
                exerciseId, totalElapsed, effectiveMax);
            totalElapsed = effectiveMax;
        }

        exercise.ClockElapsedBeforePause = totalElapsed;
        exercise.ClockState = ExerciseClockState.Paused;
        exercise.ClockStartedAt = null; // Clear start time when paused

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Paused clock for exercise {ExerciseId}. Total elapsed: {Elapsed}",
            exerciseId, exercise.ClockElapsedBeforePause);

        var clockState = exercise.ToClockStateDto();

        // Log the clock event for timeline tracking
        await LogClockEventAsync(exerciseId, ClockEventType.Paused, pausedBy.ToString(), clockState.ElapsedTime);

        // Broadcast clock paused event to all connected clients
        await _hubContext.NotifyClockPaused(exerciseId, clockState);

        return clockState;
    }

    /// <inheritdoc />
    public async Task<ClockStateDto> StopClockAsync(Guid exerciseId, string stoppedBy)
    {
        var exercise = await _context.Exercises
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

        var clockState = exercise.ToClockStateDto();

        // Log the clock event for timeline tracking
        await LogClockEventAsync(exerciseId, ClockEventType.Stopped, stoppedBy.ToString(), clockState.ElapsedTime);

        // Broadcast clock stopped event to all connected clients
        await _hubContext.NotifyClockStopped(exerciseId, clockState);

        return clockState;
    }

    /// <inheritdoc />
    public async Task<ClockStateDto> ResetClockAsync(Guid exerciseId, string resetBy)
    {
        var exercise = await _context.Exercises
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

        var clockState = exercise.ToClockStateDto();

        // Broadcast clock stopped event (reset is a form of stop with zeroed state)
        await _hubContext.NotifyClockStopped(exerciseId, clockState);

        return clockState;
    }

    /// <inheritdoc />
    public async Task<ClockStateDto> SetClockTimeAsync(Guid exerciseId, TimeSpan elapsedTime, string setBy)
    {
        var exercise = await _context.Exercises
                        .FirstOrDefaultAsync(e => e.Id == exerciseId);

        if (exercise == null)
        {
            throw new InvalidOperationException($"Exercise {exerciseId} not found");
        }

        // Can only set time when clock is paused
        if (exercise.ClockState != ExerciseClockState.Paused)
        {
            throw new InvalidOperationException(
                $"Can only set clock time when paused. Current state: {exercise.ClockState}");
        }

        // Validate elapsed time is non-negative
        if (elapsedTime < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Elapsed time cannot be negative.");
        }

        // Validate against max duration
        var effectiveMax = GetEffectiveMaxDuration(exercise);
        if (elapsedTime > effectiveMax)
        {
            throw new InvalidOperationException(
                $"Elapsed time cannot exceed the maximum duration of {effectiveMax.TotalHours:F0} hours.");
        }

        var previousElapsed = exercise.ClockElapsedBeforePause ?? TimeSpan.Zero;

        // Set the new elapsed time (wall clock time, before multiplier)
        exercise.ClockElapsedBeforePause = elapsedTime;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Clock time manually set for exercise {ExerciseId}. Previous: {Previous}, New: {New}, SetBy: {SetBy}",
            exerciseId, previousElapsed, elapsedTime, setBy);

        var clockState = exercise.ToClockStateDto();

        // Log the clock event
        await LogClockEventAsync(
            exerciseId,
            ClockEventType.TimeSet,
            setBy,
            clockState.ElapsedTime,
            $"Manual time set from {previousElapsed} to {elapsedTime}");

        // Broadcast updated state (clock remains paused)
        await _hubContext.NotifyClockPaused(exerciseId, clockState);

        return clockState;
    }

    /// <summary>
    /// Gets the effective max duration for an exercise.
    /// Returns the configured MaxDuration if set, otherwise the default (72 hours).
    /// Clamped to the absolute maximum (2 weeks).
    /// </summary>
    private static TimeSpan GetEffectiveMaxDuration(Exercise exercise)
    {
        var max = exercise.MaxDuration ?? DefaultMaxDuration;
        return max > AbsoluteMaxDuration ? AbsoluteMaxDuration : max;
    }

    /// <summary>
    /// Logs a clock event for timeline tracking.
    /// </summary>
    private async Task LogClockEventAsync(
        Guid exerciseId,
        ClockEventType eventType,
        string? userId,
        TimeSpan elapsedTime,
        string? notes = null)
    {
        var clockEvent = new ClockEvent
        {
            Id = Guid.NewGuid(),
            ExerciseId = exerciseId,
            EventType = eventType,
            OccurredAt = DateTime.UtcNow,
            UserId = userId,
            ElapsedTimeAtEvent = elapsedTime,
            Notes = notes
        };

        _context.ClockEvents.Add(clockEvent);
        await _context.SaveChangesAsync();

        _logger.LogDebug(
            "Logged clock event {EventType} for exercise {ExerciseId} at elapsed {Elapsed}",
            eventType, exerciseId, elapsedTime);
    }
}
