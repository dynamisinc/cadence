using Cadence.Core.Data;
using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Injects.Services;

/// <summary>
/// Service for evaluating and transitioning injects to Ready status when their
/// delivery time is reached in clock-driven exercises. Also handles auto-fire
/// when enabled for the exercise.
/// </summary>
public class InjectReadinessService : IInjectReadinessService
{
    private readonly AppDbContext _context;
    private readonly IExerciseHubContext _hubContext;
    private readonly ILogger<InjectReadinessService> _logger;
    private readonly IInjectService _injectService;

    public InjectReadinessService(
        AppDbContext context,
        IExerciseHubContext hubContext,
        IInjectService injectService,
        ILogger<InjectReadinessService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _injectService = injectService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task EvaluateAllExercisesAsync(CancellationToken ct = default)
    {
        // Find all active, clock-driven exercises with running clock
        var exercises = await _context.Exercises
            .AsNoTracking()
            .Where(e => e.Status == ExerciseStatus.Active)
            .Where(e => e.DeliveryMode == DeliveryMode.ClockDriven)
            .Where(e => e.ClockState == ExerciseClockState.Running)
            .Select(e => e.Id)
            .ToListAsync(ct);

        if (exercises.Count == 0)
        {
            return;
        }

        _logger.LogDebug(
            "Evaluating {Count} active clock-driven exercises for inject readiness",
            exercises.Count);

        foreach (var exerciseId in exercises)
        {
            try
            {
                await EvaluateExerciseAsync(exerciseId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error evaluating inject readiness for exercise {ExerciseId}",
                    exerciseId);
            }
        }
    }

    /// <inheritdoc />
    public async Task EvaluateExerciseAsync(Guid exerciseId, CancellationToken ct = default)
    {
        var exercise = await _context.Exercises
            .Include(e => e.ActiveMsel)
                .ThenInclude(m => m!.Injects.Where(i => i.Status == InjectStatus.Pending && i.DeliveryTime != null))
            .FirstOrDefaultAsync(e => e.Id == exerciseId, ct);

        if (exercise == null)
        {
            _logger.LogWarning("Exercise {ExerciseId} not found for readiness evaluation", exerciseId);
            return;
        }

        // Only evaluate clock-driven exercises with running clock
        if (exercise.DeliveryMode != DeliveryMode.ClockDriven)
        {
            _logger.LogDebug(
                "Skipping exercise {ExerciseId}: not clock-driven (mode: {Mode})",
                exerciseId,
                exercise.DeliveryMode);
            return;
        }

        if (exercise.ClockState != ExerciseClockState.Running)
        {
            _logger.LogDebug(
                "Skipping exercise {ExerciseId}: clock not running (state: {State})",
                exerciseId,
                exercise.ClockState);
            return;
        }

        if (exercise.ActiveMsel == null)
        {
            _logger.LogDebug("Exercise {ExerciseId} has no active MSEL", exerciseId);
            return;
        }

        // Calculate elapsed time
        var elapsedTime = CalculateElapsedTime(exercise);

        // Find injects that should be ready
        var candidateInjects = exercise.ActiveMsel.Injects
            .Where(i => i.Status == InjectStatus.Pending)
            .Where(i => i.DeliveryTime.HasValue)
            .Where(i => i.DeliveryTime!.Value <= elapsedTime)
            .ToList();

        if (candidateInjects.Count == 0)
        {
            return;
        }

        var injectIds = candidateInjects.Select(i => i.Id).ToList();
        var readyAt = DateTime.UtcNow;

        // Re-query with tracking to get fresh state and update
        // This ensures we work with the most current data
        var injectsToUpdate = await _context.Injects
            .Where(i => injectIds.Contains(i.Id))
            .Where(i => i.Status == InjectStatus.Pending) // Only get injects still Pending
            .ToListAsync(ct);

        if (injectsToUpdate.Count == 0)
        {
            _logger.LogDebug(
                "No injects transitioned to Ready for exercise {ExerciseId} (all candidates were modified concurrently)",
                exerciseId);
            return;
        }

        // Update the injects that are still Pending
        foreach (var inject in injectsToUpdate)
        {
            inject.Status = InjectStatus.Ready;
            inject.ReadyAt = readyAt;

            _logger.LogInformation(
                "Inject {InjectId} (#{InjectNumber}) transitioned to Ready in exercise {ExerciseId} " +
                "(DeliveryTime: {DeliveryTime}, Elapsed: {Elapsed})",
                inject.Id,
                inject.InjectNumber,
                exerciseId,
                inject.DeliveryTime,
                elapsedTime);
        }

        // Save changes - this will only update injects that were still Pending when we queried
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Transitioned {Count} injects to Ready for exercise {ExerciseId}",
            injectsToUpdate.Count,
            exerciseId);

        // Broadcast each Ready transition
        foreach (var inject in injectsToUpdate)
        {
            try
            {
                await _hubContext.NotifyInjectReadyToFire(exerciseId, inject.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error broadcasting InjectReadyToFire for inject {InjectId}",
                    inject.Id);
            }
        }

        // Auto-fire injects if AutoFireEnabled is true for this exercise
        if (exercise.AutoFireEnabled)
        {
            _logger.LogInformation(
                "Auto-fire enabled for exercise {ExerciseId}, firing {Count} ready injects",
                exerciseId,
                injectsToUpdate.Count);

            foreach (var inject in injectsToUpdate)
            {
                try
                {
                    // Use Guid.Empty to indicate system auto-fire (no specific user)
                    await _injectService.FireInjectAsync(exerciseId, inject.Id, Guid.Empty, ct);

                    _logger.LogInformation(
                        "Auto-fired inject {InjectId} (#{InjectNumber}) in exercise {ExerciseId}",
                        inject.Id,
                        inject.InjectNumber,
                        exerciseId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error auto-firing inject {InjectId} in exercise {ExerciseId}",
                        inject.Id,
                        exerciseId);
                }
            }
        }
    }

    /// <summary>
    /// Calculates the total elapsed scenario time for an exercise based on its clock state.
    /// Applies the clock multiplier to get scenario time from wall clock time.
    /// </summary>
    /// <param name="exercise">The exercise to calculate elapsed time for</param>
    /// <returns>Total elapsed scenario time (with clock multiplier applied)</returns>
    private TimeSpan CalculateElapsedTime(Exercise exercise)
    {
        // Calculate wall clock elapsed time
        var wallClockElapsed = exercise.ClockElapsedBeforePause ?? TimeSpan.Zero;

        // If clock is currently running, add time since last start
        if (exercise.ClockStartedAt.HasValue)
        {
            wallClockElapsed += DateTime.UtcNow - exercise.ClockStartedAt.Value;
        }

        // Apply clock multiplier to get scenario time
        // ClockMultiplier of 2.0 means scenario time runs 2x faster than wall clock
        var scenarioElapsed = TimeSpan.FromTicks((long)(wallClockElapsed.Ticks * (double)exercise.ClockMultiplier));

        return scenarioElapsed;
    }
}
