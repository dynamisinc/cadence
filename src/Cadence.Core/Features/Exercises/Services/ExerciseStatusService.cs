using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service for exercise status transitions.
/// Handles the exercise lifecycle: Draft → Active → Paused → Completed → Archived.
/// </summary>
public class ExerciseStatusService : IExerciseStatusService
{
    private readonly AppDbContext _context;
    private readonly IExerciseHubContext _hubContext;
    private readonly ILogger<ExerciseStatusService> _logger;

    /// <summary>
    /// Valid status transitions map.
    /// Key: current status, Value: allowed target statuses.
    /// Note: Archiving can be done from any non-archived status via the dedicated ArchiveAsync method.
    /// </summary>
    private static readonly Dictionary<ExerciseStatus, ExerciseStatus[]> ValidTransitions = new()
    {
        [ExerciseStatus.Draft] = [ExerciseStatus.Active, ExerciseStatus.Archived],
        [ExerciseStatus.Active] = [ExerciseStatus.Paused, ExerciseStatus.Completed, ExerciseStatus.Archived],
        [ExerciseStatus.Paused] = [ExerciseStatus.Active, ExerciseStatus.Completed, ExerciseStatus.Draft, ExerciseStatus.Archived],
        [ExerciseStatus.Completed] = [ExerciseStatus.Archived],
        [ExerciseStatus.Archived] = [], // Restore via UnarchiveAsync - target status depends on PreviousStatus
    };

    public ExerciseStatusService(
        AppDbContext context,
        IExerciseHubContext hubContext,
        ILogger<ExerciseStatusService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool CanTransition(ExerciseStatus from, ExerciseStatus to)
    {
        return ValidTransitions.TryGetValue(from, out var allowedTargets)
               && allowedTargets.Contains(to);
    }

    /// <inheritdoc />
    public IReadOnlyList<ExerciseStatus> GetAvailableTransitions(ExerciseStatus currentStatus)
    {
        return ValidTransitions.TryGetValue(currentStatus, out var transitions)
            ? transitions
            : Array.Empty<ExerciseStatus>();
    }

    /// <inheritdoc />
    public async Task<StatusTransitionResult> ActivateAsync(Guid exerciseId, Guid userId)
    {
        var exercise = await GetExerciseAsync(exerciseId);
        if (exercise == null)
            return StatusTransitionResult.Failed("Exercise not found", ExerciseStatus.Draft);

        if (exercise.Status != ExerciseStatus.Draft)
            return StatusTransitionResult.Failed(
                $"Cannot activate exercise. Current status is {exercise.Status}, expected Draft.",
                exercise.Status);

        // Validate: at least one inject required
        var hasInjects = await _context.Injects
            .AnyAsync(i => i.Msel!.ExerciseId == exerciseId && !i.IsDeleted);

        if (!hasInjects)
            return StatusTransitionResult.Failed(
                "Cannot activate exercise without at least one inject in the MSEL.",
                exercise.Status);

        // Perform transition
        exercise.Status = ExerciseStatus.Active;
        exercise.ActivatedAt = DateTime.UtcNow;
        exercise.ActivatedBy = userId;
        exercise.ModifiedBy = userId;

        // Mark as published - once true, never set back to false
        exercise.HasBeenPublished = true;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Exercise {ExerciseId} activated by user {UserId}",
            exerciseId, userId);

        var dto = exercise.ToDto();
        await _hubContext.NotifyExerciseStatusChanged(exerciseId, dto);

        return StatusTransitionResult.Succeeded(dto);
    }

    /// <inheritdoc />
    public async Task<StatusTransitionResult> PauseAsync(Guid exerciseId, Guid userId)
    {
        var exercise = await GetExerciseAsync(exerciseId);
        if (exercise == null)
            return StatusTransitionResult.Failed("Exercise not found", ExerciseStatus.Active);

        if (exercise.Status != ExerciseStatus.Active)
            return StatusTransitionResult.Failed(
                $"Cannot pause exercise. Current status is {exercise.Status}, expected Active.",
                exercise.Status);

        // Pause the clock if running
        if (exercise.ClockState == ExerciseClockState.Running && exercise.ClockStartedAt.HasValue)
        {
            var elapsedSinceStart = DateTime.UtcNow - exercise.ClockStartedAt.Value;
            exercise.ClockElapsedBeforePause = (exercise.ClockElapsedBeforePause ?? TimeSpan.Zero) + elapsedSinceStart;
            exercise.ClockState = ExerciseClockState.Paused;
            exercise.ClockStartedAt = null;
        }

        // Perform transition
        exercise.Status = ExerciseStatus.Paused;
        exercise.ModifiedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Exercise {ExerciseId} paused by user {UserId}",
            exerciseId, userId);

        var dto = exercise.ToDto();
        await _hubContext.NotifyExerciseStatusChanged(exerciseId, dto);

        return StatusTransitionResult.Succeeded(dto);
    }

    /// <inheritdoc />
    public async Task<StatusTransitionResult> ResumeAsync(Guid exerciseId, Guid userId)
    {
        var exercise = await GetExerciseAsync(exerciseId);
        if (exercise == null)
            return StatusTransitionResult.Failed("Exercise not found", ExerciseStatus.Paused);

        if (exercise.Status != ExerciseStatus.Paused)
            return StatusTransitionResult.Failed(
                $"Cannot resume exercise. Current status is {exercise.Status}, expected Paused.",
                exercise.Status);

        // Resume the clock
        exercise.ClockState = ExerciseClockState.Running;
        exercise.ClockStartedAt = DateTime.UtcNow;
        exercise.ClockStartedBy = userId;

        // Perform transition
        exercise.Status = ExerciseStatus.Active;
        exercise.ModifiedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Exercise {ExerciseId} resumed by user {UserId}",
            exerciseId, userId);

        var dto = exercise.ToDto();
        await _hubContext.NotifyExerciseStatusChanged(exerciseId, dto);

        return StatusTransitionResult.Succeeded(dto);
    }

    /// <inheritdoc />
    public async Task<StatusTransitionResult> CompleteAsync(Guid exerciseId, Guid userId)
    {
        var exercise = await GetExerciseAsync(exerciseId);
        if (exercise == null)
            return StatusTransitionResult.Failed("Exercise not found", ExerciseStatus.Active);

        if (exercise.Status != ExerciseStatus.Active && exercise.Status != ExerciseStatus.Paused)
            return StatusTransitionResult.Failed(
                $"Cannot complete exercise. Current status is {exercise.Status}, expected Active or Paused.",
                exercise.Status);

        // Finalize clock if running
        if (exercise.ClockState == ExerciseClockState.Running && exercise.ClockStartedAt.HasValue)
        {
            var elapsedSinceStart = DateTime.UtcNow - exercise.ClockStartedAt.Value;
            exercise.ClockElapsedBeforePause = (exercise.ClockElapsedBeforePause ?? TimeSpan.Zero) + elapsedSinceStart;
        }

        // Stop clock
        exercise.ClockState = ExerciseClockState.Stopped;
        exercise.ClockStartedAt = null;

        // Perform transition
        exercise.Status = ExerciseStatus.Completed;
        exercise.CompletedAt = DateTime.UtcNow;
        exercise.CompletedBy = userId;
        exercise.ModifiedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Exercise {ExerciseId} completed by user {UserId}. Final elapsed time: {Elapsed}",
            exerciseId, userId, exercise.ClockElapsedBeforePause);

        var dto = exercise.ToDto();
        await _hubContext.NotifyExerciseStatusChanged(exerciseId, dto);

        return StatusTransitionResult.Succeeded(dto);
    }

    /// <inheritdoc />
    public async Task<StatusTransitionResult> ArchiveAsync(Guid exerciseId, Guid userId)
    {
        var exercise = await GetExerciseAsync(exerciseId);
        if (exercise == null)
            return StatusTransitionResult.Failed("Exercise not found", ExerciseStatus.Draft);

        if (exercise.Status == ExerciseStatus.Archived)
            return StatusTransitionResult.Failed(
                "Exercise is already archived.",
                exercise.Status);

        // Store the current status before archiving so we can restore to it later
        var previousStatus = exercise.Status;

        // Stop the clock if running (for Active or Paused exercises)
        if (exercise.ClockState == ExerciseClockState.Running && exercise.ClockStartedAt.HasValue)
        {
            var elapsedSinceStart = DateTime.UtcNow - exercise.ClockStartedAt.Value;
            exercise.ClockElapsedBeforePause = (exercise.ClockElapsedBeforePause ?? TimeSpan.Zero) + elapsedSinceStart;
        }
        exercise.ClockState = ExerciseClockState.Stopped;
        exercise.ClockStartedAt = null;

        // Perform transition
        exercise.PreviousStatus = previousStatus;
        exercise.Status = ExerciseStatus.Archived;
        exercise.ArchivedAt = DateTime.UtcNow;
        exercise.ArchivedBy = userId;
        exercise.ModifiedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Exercise {ExerciseId} archived by user {UserId}. Previous status was {PreviousStatus}",
            exerciseId, userId, previousStatus);

        var dto = exercise.ToDto();
        await _hubContext.NotifyExerciseStatusChanged(exerciseId, dto);

        return StatusTransitionResult.Succeeded(dto);
    }

    /// <inheritdoc />
    public async Task<StatusTransitionResult> UnarchiveAsync(Guid exerciseId, Guid userId)
    {
        var exercise = await GetExerciseAsync(exerciseId);
        if (exercise == null)
            return StatusTransitionResult.Failed("Exercise not found", ExerciseStatus.Archived);

        if (exercise.Status != ExerciseStatus.Archived)
            return StatusTransitionResult.Failed(
                $"Cannot restore exercise. Current status is {exercise.Status}, expected Archived.",
                exercise.Status);

        // Restore to previous status if available, otherwise default to Draft
        var restoredStatus = exercise.PreviousStatus ?? ExerciseStatus.Draft;

        // Clear archive tracking fields
        exercise.Status = restoredStatus;
        exercise.PreviousStatus = null;
        exercise.ArchivedAt = null;
        exercise.ArchivedBy = null;
        exercise.ModifiedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Exercise {ExerciseId} restored by user {UserId} to status {RestoredStatus}",
            exerciseId, userId, restoredStatus);

        var dto = exercise.ToDto();
        await _hubContext.NotifyExerciseStatusChanged(exerciseId, dto);

        return StatusTransitionResult.Succeeded(dto);
    }

    /// <inheritdoc />
    public async Task<StatusTransitionResult> RevertToDraftAsync(Guid exerciseId, Guid userId)
    {
        var exercise = await GetExerciseAsync(exerciseId);
        if (exercise == null)
            return StatusTransitionResult.Failed("Exercise not found", ExerciseStatus.Paused);

        if (exercise.Status != ExerciseStatus.Paused)
            return StatusTransitionResult.Failed(
                $"Cannot revert to draft. Current status is {exercise.Status}, expected Paused.",
                exercise.Status);

        // Clear all conduct data
        // 1. Reset all inject statuses to Pending
        var injects = await _context.Injects
            .Where(i => i.Msel!.ExerciseId == exerciseId && !i.IsDeleted)
            .ToListAsync();

        foreach (var inject in injects)
        {
            inject.Status = InjectStatus.Pending;
            inject.FiredAt = null;
            inject.FiredBy = null;
            inject.SkippedAt = null;
            inject.SkippedBy = null;
            inject.ModifiedBy = userId;
        }

        // 2. Delete all observations (permanent - they're conduct-time data)
        var observations = await _context.Observations
            .Where(o => o.ExerciseId == exerciseId && !o.IsDeleted)
            .ToListAsync();

        foreach (var obs in observations)
        {
            obs.IsDeleted = true;
            obs.DeletedAt = DateTime.UtcNow;
            obs.DeletedBy = userId;
        }

        _logger.LogWarning(
            "Reverting exercise {ExerciseId} to draft. Resetting {InjectCount} injects and soft-deleting {ObservationCount} observations.",
            exerciseId, injects.Count, observations.Count);

        // 3. Reset clock
        exercise.ClockState = ExerciseClockState.Stopped;
        exercise.ClockStartedAt = null;
        exercise.ClockElapsedBeforePause = null;
        exercise.ClockStartedBy = null;

        // 4. Clear activation data
        exercise.ActivatedAt = null;
        exercise.ActivatedBy = null;

        // Perform transition
        exercise.Status = ExerciseStatus.Draft;
        exercise.ModifiedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Exercise {ExerciseId} reverted to Draft by user {UserId}",
            exerciseId, userId);

        var dto = exercise.ToDto();
        await _hubContext.NotifyExerciseStatusChanged(exerciseId, dto);

        return StatusTransitionResult.Succeeded(dto);
    }

    private async Task<Exercise?> GetExerciseAsync(Guid exerciseId)
    {
        return await _context.Exercises
            .Include(e => e.ClockStartedByUser)
            .Include(e => e.ActivatedByUser)
            .Include(e => e.CompletedByUser)
            .Include(e => e.ArchivedByUser)
            .FirstOrDefaultAsync(e => e.Id == exerciseId && !e.IsDeleted);
    }
}
