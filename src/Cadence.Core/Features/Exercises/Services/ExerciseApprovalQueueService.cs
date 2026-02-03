using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service for exercise approval queue operations (S06: Approval Queue View).
/// </summary>
public class ExerciseApprovalQueueService : IExerciseApprovalQueueService
{
    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationContext _orgContext;
    private readonly ILogger<ExerciseApprovalQueueService> _logger;

    public ExerciseApprovalQueueService(
        AppDbContext context,
        ICurrentOrganizationContext orgContext,
        ILogger<ExerciseApprovalQueueService> logger)
    {
        _context = context;
        _orgContext = orgContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApprovalStatusDto> GetApprovalStatusAsync(
        Guid exerciseId,
        CancellationToken cancellationToken = default)
    {
        var exercise = await _context.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exerciseId, cancellationToken);

        if (exercise == null)
        {
            _logger.LogWarning("Exercise {ExerciseId} not found", exerciseId);
            throw new InvalidOperationException($"Exercise {exerciseId} not found");
        }

        // If no active MSEL, return empty status
        if (exercise.ActiveMselId == null)
        {
            _logger.LogDebug("Exercise {ExerciseId} has no active MSEL, returning empty approval status", exerciseId);
            return new ApprovalStatusDto(
                TotalInjects: 0,
                ApprovedCount: 0,
                PendingApprovalCount: 0,
                DraftCount: 0,
                ApprovalPercentage: 100,
                AllApproved: true
            );
        }

        // Get all injects for the active MSEL
        var injects = await _context.Injects
            .AsNoTracking()
            .Where(i => i.MselId == exercise.ActiveMselId)
            .Select(i => i.Status)
            .ToListAsync(cancellationToken);

        var totalInjects = injects.Count;
        var draftCount = injects.Count(s => s == InjectStatus.Draft);
        var submittedCount = injects.Count(s => s == InjectStatus.Submitted);

        // Approved count includes: Approved, Synchronized, Released, Deferred (and excludes Obsolete)
        // Essentially, anything that's >= Approved status except Obsolete
        var approvedCount = injects.Count(s =>
            s >= InjectStatus.Approved &&
            s != InjectStatus.Obsolete);

        var approvalPercentage = totalInjects > 0
            ? Math.Round((decimal)approvedCount / totalInjects * 100, 2)
            : 100m; // Vacuously true if no injects

        var allApproved = submittedCount == 0 && draftCount == 0;

        _logger.LogDebug(
            "Exercise {ExerciseId} approval status: Total={Total}, Approved={Approved}, Pending={Pending}, Draft={Draft}, AllApproved={AllApproved}",
            exerciseId, totalInjects, approvedCount, submittedCount, draftCount, allApproved);

        return new ApprovalStatusDto(
            TotalInjects: totalInjects,
            ApprovedCount: approvedCount,
            PendingApprovalCount: submittedCount,
            DraftCount: draftCount,
            ApprovalPercentage: approvalPercentage,
            AllApproved: allApproved
        );
    }
}
