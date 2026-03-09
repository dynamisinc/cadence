using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service for permanently deleting exercises.
/// Handles delete eligibility checks and cascade deletion of related data.
/// </summary>
public class ExerciseDeleteService : IExerciseDeleteService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExerciseDeleteService> _logger;

    public ExerciseDeleteService(
        AppDbContext context,
        ILogger<ExerciseDeleteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DeleteSummaryResponse?> GetDeleteSummaryAsync(Guid exerciseId, string userId, bool isAdmin)
    {
        var exercise = await _context.Exercises
            .IgnoreQueryFilters() // Include soft-deleted for counts
            .FirstOrDefaultAsync(e => e.Id == exerciseId && !e.IsDeleted);

        if (exercise == null)
        {
            return null;
        }

        // Get data counts
        var summary = await GetDataSummaryAsync(exerciseId);

        // Determine delete eligibility
        var (canDelete, deleteReason, cannotDeleteReason) = GetDeleteEligibility(exercise, userId, isAdmin);

        return new DeleteSummaryResponse(
            exerciseId,
            exercise.Name,
            canDelete,
            deleteReason,
            cannotDeleteReason,
            summary
        );
    }

    /// <inheritdoc />
    public async Task<DeleteExerciseResult> DeleteExerciseAsync(Guid exerciseId, string userId, bool isAdmin)
    {
        var exercise = await _context.Exercises
            .FirstOrDefaultAsync(e => e.Id == exerciseId && !e.IsDeleted);

        if (exercise == null)
        {
            return DeleteExerciseResult.Failed("Exercise not found", CannotDeleteReason.NotFound);
        }

        // Check eligibility
        var (canDelete, deleteReason, cannotDeleteReason) = GetDeleteEligibility(exercise, userId, isAdmin);

        if (!canDelete)
        {
            var errorMessage = cannotDeleteReason switch
            {
                CannotDeleteReason.MustArchiveFirst =>
                    $"Cannot delete {exercise.Status} exercise. Archive the exercise first, then delete.",
                CannotDeleteReason.NotAuthorized =>
                    "You do not have permission to delete this exercise.",
                _ => "Cannot delete this exercise."
            };
            return DeleteExerciseResult.Failed(errorMessage, cannotDeleteReason!.Value);
        }

        // Get summary for audit logging before delete
        var summary = await GetDataSummaryAsync(exerciseId);

        _logger.LogWarning(
            "PERMANENT DELETE: Exercise {ExerciseId} '{ExerciseName}' being deleted by user {UserId}. " +
            "Data counts: {InjectCount} injects, {PhaseCount} phases, {ObservationCount} observations, " +
            "{ParticipantCount} participants, {ObjectiveCount} objectives, {MselCount} MSELs.",
            exerciseId, exercise.Name, userId,
            summary.InjectCount, summary.PhaseCount, summary.ObservationCount,
            summary.ParticipantCount, summary.ObjectiveCount, summary.MselCount);

        // Perform cascade delete using execution strategy to support retrying execution strategy
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Delete in order of dependencies (children first):
                //  1. ExpectedOutcomes (child of Inject)
                //  2. InjectObjectives (junction between Inject and Objective)
                //  3. InjectCriticalTasks (junction between Inject and CriticalTask)
                //  4. Observations (references Inject, Exercise, Objective)
                //  5. Injects (child of MSEL)
                //  6. Msels (child of Exercise) - also clears ActiveMselId
                //  7. ExerciseParticipants (child of Exercise)
                //  8. Objectives (child of Exercise)
                //  9. Phases (child of Exercise)
                // 10. EegEntries (child of CriticalTask, which is child of CapabilityTarget)
                // 11. CriticalTasks (child of CapabilityTarget)
                // 12. CapabilityTargets (child of Exercise)
                // 13. ExercisePhotos (child of Exercise)
                // 14. Exercise itself

                // Clear ActiveMselId to avoid FK constraint using bulk update (no change tracking)
                await _context.Exercises
                    .Where(e => e.Id == exerciseId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.ActiveMselId, (Guid?)null));

                // Get all MSEL IDs for this exercise
                var mselIds = await _context.Msels
                    .Where(m => m.ExerciseId == exerciseId)
                    .Select(m => m.Id)
                    .ToListAsync();

                // Get all Inject IDs for these MSELs
                var injectIds = await _context.Injects
                    .IgnoreQueryFilters()
                    .Where(i => mselIds.Contains(i.MselId))
                    .Select(i => i.Id)
                    .ToListAsync();

                // Get all CapabilityTarget IDs for this exercise
                var capabilityTargetIds = await _context.CapabilityTargets
                    .IgnoreQueryFilters()
                    .Where(ct => ct.ExerciseId == exerciseId)
                    .Select(ct => ct.Id)
                    .ToListAsync();

                // Get all CriticalTask IDs for these CapabilityTargets
                var criticalTaskIds = capabilityTargetIds.Count > 0
                    ? await _context.CriticalTasks
                        .IgnoreQueryFilters()
                        .Where(ct => capabilityTargetIds.Contains(ct.CapabilityTargetId))
                        .Select(ct => ct.Id)
                        .ToListAsync()
                    : [];

                // 1. Delete ExpectedOutcomes for all injects
                if (injectIds.Count > 0)
                {
                    await _context.ExpectedOutcomes
                        .IgnoreQueryFilters()
                        .Where(eo => injectIds.Contains(eo.InjectId))
                        .ExecuteDeleteAsync();

                    // 2. Delete InjectObjectives for all injects
                    await _context.InjectObjectives
                        .Where(io => injectIds.Contains(io.InjectId))
                        .ExecuteDeleteAsync();

                    // 3. Delete InjectCriticalTasks for all injects
                    // InjectCriticalTask has RESTRICT on the CriticalTask side to avoid cascade cycles,
                    // so must be deleted explicitly before CriticalTasks are removed.
                    await _context.InjectCriticalTasks
                        .Where(ict => injectIds.Contains(ict.InjectId))
                        .ExecuteDeleteAsync();
                }

                // 4. Delete Observations for this exercise
                await _context.Observations
                    .IgnoreQueryFilters()
                    .Where(o => o.ExerciseId == exerciseId)
                    .ExecuteDeleteAsync();

                // 5. Delete Injects for all MSELs
                if (mselIds.Count > 0)
                {
                    await _context.Injects
                        .IgnoreQueryFilters()
                        .Where(i => mselIds.Contains(i.MselId))
                        .ExecuteDeleteAsync();
                }

                // 6. Delete Msels for this exercise
                await _context.Msels
                    .IgnoreQueryFilters()
                    .Where(m => m.ExerciseId == exerciseId)
                    .ExecuteDeleteAsync();

                // 7. Delete ExerciseParticipants for this exercise
                await _context.ExerciseParticipants
                    .IgnoreQueryFilters()
                    .Where(p => p.ExerciseId == exerciseId)
                    .ExecuteDeleteAsync();

                // 8. Delete Objectives for this exercise
                await _context.Objectives
                    .IgnoreQueryFilters()
                    .Where(o => o.ExerciseId == exerciseId)
                    .ExecuteDeleteAsync();

                // 9. Delete Phases for this exercise
                await _context.Phases
                    .IgnoreQueryFilters()
                    .Where(p => p.ExerciseId == exerciseId)
                    .ExecuteDeleteAsync();

                // 10. Delete EegEntries for all CriticalTasks in this exercise
                if (criticalTaskIds.Count > 0)
                {
                    await _context.EegEntries
                        .IgnoreQueryFilters()
                        .Where(e => criticalTaskIds.Contains(e.CriticalTaskId))
                        .ExecuteDeleteAsync();

                    // 11. Delete CriticalTasks for all CapabilityTargets in this exercise
                    await _context.CriticalTasks
                        .IgnoreQueryFilters()
                        .Where(ct => capabilityTargetIds.Contains(ct.CapabilityTargetId))
                        .ExecuteDeleteAsync();
                }

                // 12. Delete CapabilityTargets for this exercise
                if (capabilityTargetIds.Count > 0)
                {
                    await _context.CapabilityTargets
                        .IgnoreQueryFilters()
                        .Where(ct => ct.ExerciseId == exerciseId)
                        .ExecuteDeleteAsync();
                }

                // 13. Delete ExercisePhotos for this exercise
                await _context.ExercisePhotos
                    .IgnoreQueryFilters()
                    .Where(p => p.ExerciseId == exerciseId)
                    .ExecuteDeleteAsync();

                // 14. Delete the Exercise itself
                await _context.Exercises
                    .Where(e => e.Id == exerciseId)
                    .ExecuteDeleteAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex,
                    "Failed to delete exercise {ExerciseId}. Transaction rolled back.",
                    exerciseId);

                throw;
            }
        });

        _logger.LogInformation(
            "Successfully deleted exercise {ExerciseId} '{ExerciseName}'. Reason: {DeleteReason}",
            exerciseId, exercise.Name, deleteReason);

        return DeleteExerciseResult.Succeeded();
    }

    /// <summary>
    /// Determines if an exercise can be deleted based on its status and user permissions.
    /// </summary>
    private static (bool canDelete, DeleteEligibilityReason? deleteReason, CannotDeleteReason? cannotDeleteReason)
        GetDeleteEligibility(Exercise exercise, string userId, bool isAdmin)
    {
        // Rule 1: Archived exercises can only be deleted by admins
        if (exercise.Status == ExerciseStatus.Archived)
        {
            if (isAdmin)
            {
                return (true, DeleteEligibilityReason.Archived, null);
            }
            return (false, null, CannotDeleteReason.NotAuthorized);
        }

        // Rule 2: Never-published (Draft only, HasBeenPublished = false) can be deleted by creator or admin
        if (!exercise.HasBeenPublished && exercise.Status == ExerciseStatus.Draft)
        {
            if (isAdmin || exercise.CreatedBy == userId)
            {
                return (true, DeleteEligibilityReason.NeverPublished, null);
            }
            return (false, null, CannotDeleteReason.NotAuthorized);
        }

        // Rule 3: Published/Active/Completed/Paused exercises cannot be deleted - must archive first
        return (false, null, CannotDeleteReason.MustArchiveFirst);
    }

    /// <summary>
    /// Gets counts of all data that would be deleted with the exercise.
    /// </summary>
    private async Task<DeleteDataSummary> GetDataSummaryAsync(Guid exerciseId)
    {
        // Get MSEL IDs
        var mselIds = await _context.Msels
            .IgnoreQueryFilters()
            .Where(m => m.ExerciseId == exerciseId)
            .Select(m => m.Id)
            .ToListAsync();

        // Get Inject IDs for expected outcome count
        var injectIds = await _context.Injects
            .IgnoreQueryFilters()
            .Where(i => mselIds.Contains(i.MselId))
            .Select(i => i.Id)
            .ToListAsync();

        var injectCount = injectIds.Count;

        var phaseCount = await _context.Phases
            .IgnoreQueryFilters()
            .CountAsync(p => p.ExerciseId == exerciseId);

        var observationCount = await _context.Observations
            .IgnoreQueryFilters()
            .CountAsync(o => o.ExerciseId == exerciseId);

        var participantCount = await _context.ExerciseParticipants
            .IgnoreQueryFilters()
            .CountAsync(p => p.ExerciseId == exerciseId);

        var expectedOutcomeCount = await _context.ExpectedOutcomes
            .IgnoreQueryFilters()
            .CountAsync(eo => injectIds.Contains(eo.InjectId));

        var objectiveCount = await _context.Objectives
            .IgnoreQueryFilters()
            .CountAsync(o => o.ExerciseId == exerciseId);

        return new DeleteDataSummary(
            injectCount,
            phaseCount,
            observationCount,
            participantCount,
            expectedOutcomeCount,
            objectiveCount,
            mselIds.Count
        );
    }
}
