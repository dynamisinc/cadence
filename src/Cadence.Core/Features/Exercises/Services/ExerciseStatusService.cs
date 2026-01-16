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
    /// </summary>
    private static readonly Dictionary<ExerciseStatus, ExerciseStatus[]> ValidTransitions = new()
    {
        [ExerciseStatus.Draft] = [ExerciseStatus.Active],
        [ExerciseStatus.Active] = [ExerciseStatus.Paused, ExerciseStatus.Completed],
        [ExerciseStatus.Paused] = [ExerciseStatus.Active, ExerciseStatus.Completed, ExerciseStatus.Draft],
        [ExerciseStatus.Completed] = [ExerciseStatus.Archived],
        [ExerciseStatus.Archived] = [ExerciseStatus.Completed], // Unarchive
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
            return StatusTransitionResult.Failed("Exercise not found", ExerciseStatus.Completed);

        if (exercise.Status != ExerciseStatus.Completed)
            return StatusTransitionResult.Failed(
                $"Cannot archive exercise. Current status is {exercise.Status}, expected Completed.",
                exercise.Status);

        // Perform transition
        exercise.Status = ExerciseStatus.Archived;
        exercise.ArchivedAt = DateTime.UtcNow;
        exercise.ArchivedBy = userId;
        exercise.ModifiedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Exercise {ExerciseId} archived by user {UserId}",
            exerciseId, userId);

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
                $"Cannot unarchive exercise. Current status is {exercise.Status}, expected Archived.",
                exercise.Status);

        // Perform transition - back to Completed
        exercise.Status = ExerciseStatus.Completed;
        exercise.ArchivedAt = null;
        exercise.ArchivedBy = null;
        exercise.ModifiedBy = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Exercise {ExerciseId} unarchived by user {UserId}",
            exerciseId, userId);

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
