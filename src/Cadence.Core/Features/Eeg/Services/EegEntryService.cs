using Cadence.Core.Data;
using Cadence.Core.Features.Eeg.Models.DTOs;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Eeg.Services;

/// <summary>
/// Service implementation for EEG entry operations.
/// </summary>
public class EegEntryService : IEegEntryService
{
    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationContext _orgContext;

    public EegEntryService(AppDbContext context, ICurrentOrganizationContext orgContext)
    {
        _context = context;
        _orgContext = orgContext;
    }

    public async Task<EegEntryListResponse> GetByExerciseAsync(Guid exerciseId)
    {
        var entries = await _context.EegEntries
            .Include(e => e.CriticalTask)
                .ThenInclude(ct => ct.CapabilityTarget)
                    .ThenInclude(cpt => cpt.Capability)
            .Include(e => e.Evaluator)
            .Include(e => e.TriggeringInject)
            .Where(e => e.CriticalTask.CapabilityTarget.ExerciseId == exerciseId)
            .OrderByDescending(e => e.ObservedAt)
            .ToListAsync();

        var items = entries.Select(ToDto).ToList();
        return new EegEntryListResponse(items, items.Count);
    }

    public async Task<EegEntryListResponse> GetByCriticalTaskAsync(Guid criticalTaskId)
    {
        var entries = await _context.EegEntries
            .Include(e => e.CriticalTask)
                .ThenInclude(ct => ct.CapabilityTarget)
                    .ThenInclude(cpt => cpt.Capability)
            .Include(e => e.Evaluator)
            .Include(e => e.TriggeringInject)
            .Where(e => e.CriticalTaskId == criticalTaskId)
            .OrderByDescending(e => e.ObservedAt)
            .ToListAsync();

        var items = entries.Select(ToDto).ToList();
        return new EegEntryListResponse(items, items.Count);
    }

    public async Task<EegEntryDto?> GetByIdAsync(Guid id)
    {
        var entry = await _context.EegEntries
            .Include(e => e.CriticalTask)
                .ThenInclude(ct => ct.CapabilityTarget)
                    .ThenInclude(cpt => cpt.Capability)
            .Include(e => e.Evaluator)
            .Include(e => e.TriggeringInject)
            .FirstOrDefaultAsync(e => e.Id == id);

        return entry == null ? null : ToDto(entry);
    }

    public async Task<EegEntryDto> CreateAsync(CreateEegEntryRequest request, string evaluatorId)
    {
        // Validate critical task exists
        var criticalTask = await _context.CriticalTasks
            .Include(ct => ct.CapabilityTarget)
            .FirstOrDefaultAsync(ct => ct.Id == request.CriticalTaskId);

        if (criticalTask == null)
            throw new InvalidOperationException($"Critical task {request.CriticalTaskId} not found");

        // Get organization ID from capability target
        var organizationId = criticalTask.CapabilityTarget.OrganizationId;

        // Validate triggering inject if provided
        if (request.TriggeringInjectId.HasValue)
        {
            var inject = await _context.Injects.FindAsync(request.TriggeringInjectId.Value);
            if (inject == null)
                throw new InvalidOperationException($"Inject {request.TriggeringInjectId} not found");
        }

        var now = DateTime.UtcNow;
        var entry = new EegEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            CriticalTaskId = request.CriticalTaskId,
            ObservationText = request.ObservationText,
            Rating = request.Rating,
            ObservedAt = request.ObservedAt ?? now,
            RecordedAt = now,
            EvaluatorId = evaluatorId,
            TriggeringInjectId = request.TriggeringInjectId,
            CreatedBy = evaluatorId,
            ModifiedBy = evaluatorId
        };

        _context.EegEntries.Add(entry);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        return (await GetByIdAsync(entry.Id))!;
    }

    public async Task<EegEntryDto?> UpdateAsync(Guid id, UpdateEegEntryRequest request, string modifiedBy)
    {
        var entry = await _context.EegEntries.FindAsync(id);
        if (entry == null)
            return null;

        entry.ObservationText = request.ObservationText;
        entry.Rating = request.Rating;
        if (request.ObservedAt.HasValue)
            entry.ObservedAt = request.ObservedAt.Value;
        entry.TriggeringInjectId = request.TriggeringInjectId;
        entry.ModifiedBy = modifiedBy;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id, string deletedBy)
    {
        var entry = await _context.EegEntries.FindAsync(id);
        if (entry == null)
            return false;

        // Soft delete
        entry.IsDeleted = true;
        entry.DeletedAt = DateTime.UtcNow;
        entry.DeletedBy = deletedBy;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<EegCoverageDto> GetCoverageAsync(Guid exerciseId)
    {
        // Get all critical tasks for the exercise
        var tasks = await _context.CriticalTasks
            .Include(ct => ct.CapabilityTarget)
                .ThenInclude(cpt => cpt.Capability)
            .Include(ct => ct.EegEntries.Where(e => !e.IsDeleted))
            .Where(ct => ct.CapabilityTarget.ExerciseId == exerciseId && !ct.IsDeleted)
            .ToListAsync();

        var totalTasks = tasks.Count;
        var evaluatedTasks = tasks.Count(t => t.EegEntries.Any());
        var coveragePercentage = totalTasks > 0
            ? Math.Round((decimal)evaluatedTasks / totalTasks * 100, 1)
            : 0;

        // Rating distribution
        var allEntries = tasks.SelectMany(t => t.EegEntries).ToList();
        var ratingDistribution = new Dictionary<PerformanceRating, int>
        {
            { PerformanceRating.Performed, allEntries.Count(e => e.Rating == PerformanceRating.Performed) },
            { PerformanceRating.SomeChallenges, allEntries.Count(e => e.Rating == PerformanceRating.SomeChallenges) },
            { PerformanceRating.MajorChallenges, allEntries.Count(e => e.Rating == PerformanceRating.MajorChallenges) },
            { PerformanceRating.UnableToPerform, allEntries.Count(e => e.Rating == PerformanceRating.UnableToPerform) }
        };

        // By capability target
        var byCapabilityTarget = tasks
            .GroupBy(t => t.CapabilityTarget)
            .Select(g => new CapabilityTargetCoverageDto(
                g.Key.Id,
                g.Key.TargetDescription,
                g.Key.Capability.Name,
                g.Count(),
                g.Count(t => t.EegEntries.Any()),
                g.Select(t => new TaskRatingDto(
                    t.Id,
                    t.TaskDescription,
                    t.EegEntries.OrderByDescending(e => e.ObservedAt).FirstOrDefault()?.Rating
                ))
            ))
            .ToList();

        // Unevaluated tasks
        var unevaluatedTasks = tasks
            .Where(t => !t.EegEntries.Any())
            .Select(t => new UnevaluatedTaskDto(
                t.Id,
                t.TaskDescription,
                t.CapabilityTargetId,
                t.CapabilityTarget.TargetDescription
            ))
            .ToList();

        return new EegCoverageDto(
            totalTasks,
            evaluatedTasks,
            coveragePercentage,
            ratingDistribution,
            byCapabilityTarget,
            unevaluatedTasks
        );
    }

    private static EegEntryDto ToDto(EegEntry entry) => new(
        entry.Id,
        entry.CriticalTaskId,
        new CriticalTaskSummaryDto(
            entry.CriticalTask.Id,
            entry.CriticalTask.TaskDescription,
            entry.CriticalTask.CapabilityTargetId,
            entry.CriticalTask.CapabilityTarget.TargetDescription,
            entry.CriticalTask.CapabilityTarget.Capability.Name
        ),
        entry.ObservationText,
        entry.Rating,
        entry.Rating.ToDisplayString(),
        entry.ObservedAt,
        entry.RecordedAt,
        entry.EvaluatorId,
        entry.Evaluator?.DisplayName,
        entry.TriggeringInjectId,
        entry.TriggeringInject == null ? null : new InjectSummaryDto(
            entry.TriggeringInject.Id,
            entry.TriggeringInject.InjectNumber,
            entry.TriggeringInject.Title
        ),
        entry.CreatedAt,
        entry.UpdatedAt
    );
}
