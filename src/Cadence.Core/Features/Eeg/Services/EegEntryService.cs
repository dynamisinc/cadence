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

    public async Task<EegEntryListResponse> GetByExerciseAsync(Guid exerciseId, EegEntryQueryParams? queryParams = null)
    {
        queryParams ??= new EegEntryQueryParams();

        // Enforce pagination limits
        var page = Math.Max(1, queryParams.Page);
        var pageSize = Math.Clamp(queryParams.PageSize, 1, 100);

        var query = _context.EegEntries
            .Include(e => e.CriticalTask)
                .ThenInclude(ct => ct.CapabilityTarget)
                    .ThenInclude(cpt => cpt.Capability)
            .Include(e => e.Evaluator)
            .Include(e => e.TriggeringInject)
            .Where(e => e.CriticalTask.CapabilityTarget.ExerciseId == exerciseId
                && e.OrganizationId == _orgContext.CurrentOrganizationId);

        // Apply rating filter
        if (!string.IsNullOrWhiteSpace(queryParams.Rating))
        {
            var ratingStrings = queryParams.Rating.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var ratings = ratingStrings
                .Where(r => Enum.TryParse<PerformanceRating>(r, ignoreCase: true, out _))
                .Select(r => Enum.Parse<PerformanceRating>(r, ignoreCase: true))
                .ToList();

            if (ratings.Count > 0)
                query = query.Where(e => ratings.Contains(e.Rating));
        }

        // Apply evaluator filter
        if (!string.IsNullOrWhiteSpace(queryParams.EvaluatorId))
        {
            var evaluatorIds = queryParams.EvaluatorId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            query = query.Where(e => evaluatorIds.Contains(e.EvaluatorId));
        }

        // Apply capability target filter
        if (queryParams.CapabilityTargetId.HasValue)
            query = query.Where(e => e.CriticalTask.CapabilityTargetId == queryParams.CapabilityTargetId.Value);

        // Apply critical task filter
        if (queryParams.CriticalTaskId.HasValue)
            query = query.Where(e => e.CriticalTaskId == queryParams.CriticalTaskId.Value);

        // Apply date range filters
        if (queryParams.FromDate.HasValue)
            query = query.Where(e => e.ObservedAt >= queryParams.FromDate.Value);

        if (queryParams.ToDate.HasValue)
            query = query.Where(e => e.ObservedAt <= queryParams.ToDate.Value);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var searchLower = queryParams.Search.ToLowerInvariant();
            query = query.Where(e => e.ObservationText.ToLower().Contains(searchLower));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Apply sorting
        query = queryParams.SortBy?.ToLowerInvariant() switch
        {
            "recordedat" => queryParams.SortOrder?.ToLowerInvariant() == "asc"
                ? query.OrderBy(e => e.RecordedAt)
                : query.OrderByDescending(e => e.RecordedAt),
            "rating" => queryParams.SortOrder?.ToLowerInvariant() == "asc"
                ? query.OrderBy(e => e.Rating)
                : query.OrderByDescending(e => e.Rating),
            _ => queryParams.SortOrder?.ToLowerInvariant() == "asc"
                ? query.OrderBy(e => e.ObservedAt)
                : query.OrderByDescending(e => e.ObservedAt)
        };

        // Apply pagination
        var entries = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Get modifier users for wasEdited display
        var modifierIds = entries
            .Where(e => WasEdited(e) && e.ModifiedBy != e.EvaluatorId)
            .Select(e => e.ModifiedBy)
            .Distinct()
            .ToList();

        var modifiers = modifierIds.Count > 0
            ? await _context.Users
                .Where(u => modifierIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.DisplayName)
            : new Dictionary<string, string>();

        var items = entries.Select(e => ToDto(e, modifiers)).ToList();
        return new EegEntryListResponse(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<EegEntryListResponse> GetByCriticalTaskAsync(Guid criticalTaskId)
    {
        // Validate critical task exists and belongs to current organization
        var criticalTask = await _context.CriticalTasks
            .Include(ct => ct.CapabilityTarget)
            .FirstOrDefaultAsync(ct => ct.Id == criticalTaskId
                && ct.CapabilityTarget.OrganizationId == _orgContext.CurrentOrganizationId);

        if (criticalTask == null)
            throw new InvalidOperationException($"Critical task {criticalTaskId} not found");

        var entries = await _context.EegEntries
            .Include(e => e.CriticalTask)
                .ThenInclude(ct => ct.CapabilityTarget)
                    .ThenInclude(cpt => cpt.Capability)
            .Include(e => e.Evaluator)
            .Include(e => e.TriggeringInject)
            .Where(e => e.CriticalTaskId == criticalTaskId
                && e.OrganizationId == _orgContext.CurrentOrganizationId)
            .OrderByDescending(e => e.ObservedAt)
            .ToListAsync();

        // Get modifier users for wasEdited display
        var modifierIds = entries
            .Where(e => WasEdited(e) && e.ModifiedBy != e.EvaluatorId)
            .Select(e => e.ModifiedBy)
            .Distinct()
            .ToList();

        var modifiers = modifierIds.Count > 0
            ? await _context.Users
                .Where(u => modifierIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.DisplayName)
            : new Dictionary<string, string>();

        var items = entries.Select(e => ToDto(e, modifiers)).ToList();
        return new EegEntryListResponse(items, items.Count, 1, items.Count, 1);
    }

    public async Task<EegEntryDto?> GetByIdAsync(Guid id)
    {
        var entry = await _context.EegEntries
            .Include(e => e.CriticalTask)
                .ThenInclude(ct => ct.CapabilityTarget)
                    .ThenInclude(cpt => cpt.Capability)
            .Include(e => e.Evaluator)
            .Include(e => e.TriggeringInject)
            .FirstOrDefaultAsync(e => e.Id == id
                && e.OrganizationId == _orgContext.CurrentOrganizationId);

        if (entry == null)
            return null;

        // Get modifier user if needed
        Dictionary<string, string> modifiers = new();
        if (WasEdited(entry) && entry.ModifiedBy != entry.EvaluatorId)
        {
            var modifier = await _context.Users.FirstOrDefaultAsync(u => u.Id == entry.ModifiedBy);
            if (modifier != null)
                modifiers[modifier.Id] = modifier.DisplayName;
        }

        return ToDto(entry, modifiers);
    }

    public async Task<EegEntryDto> CreateAsync(CreateEegEntryRequest request, string evaluatorId)
    {
        // Validate critical task exists and belongs to current organization
        var criticalTask = await _context.CriticalTasks
            .Include(ct => ct.CapabilityTarget)
            .FirstOrDefaultAsync(ct => ct.Id == request.CriticalTaskId
                && ct.CapabilityTarget.OrganizationId == _orgContext.CurrentOrganizationId);

        if (criticalTask == null)
            throw new InvalidOperationException($"Critical task {request.CriticalTaskId} not found");

        // Get organization ID from capability target (already validated)
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
        var entry = await _context.EegEntries
            .FirstOrDefaultAsync(e => e.Id == id
                && e.OrganizationId == _orgContext.CurrentOrganizationId);
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
        var entry = await _context.EegEntries
            .FirstOrDefaultAsync(e => e.Id == id
                && e.OrganizationId == _orgContext.CurrentOrganizationId);
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
        // Get all critical tasks for the exercise - filter by organization
        var tasks = await _context.CriticalTasks
            .Include(ct => ct.CapabilityTarget)
                .ThenInclude(cpt => cpt.Capability)
            .Include(ct => ct.EegEntries.Where(e => !e.IsDeleted))
            .Where(ct => ct.CapabilityTarget.ExerciseId == exerciseId
                && ct.CapabilityTarget.OrganizationId == _orgContext.CurrentOrganizationId
                && !ct.IsDeleted)
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

    /// <summary>
    /// Determines if an entry has been edited since creation.
    /// Uses a 5-second tolerance to account for timestamp differences during creation.
    /// </summary>
    private static bool WasEdited(EegEntry entry)
    {
        var tolerance = TimeSpan.FromSeconds(5);
        return entry.UpdatedAt - entry.CreatedAt > tolerance;
    }

    private static EegEntryDto ToDto(EegEntry entry, Dictionary<string, string> modifiers)
    {
        var wasEdited = WasEdited(entry);
        UserSummaryDto? updatedBy = null;

        if (wasEdited)
        {
            // If ModifiedBy is different from evaluator, try to get from modifiers dictionary
            if (entry.ModifiedBy != entry.EvaluatorId && modifiers.TryGetValue(entry.ModifiedBy, out var modifierName))
            {
                updatedBy = new UserSummaryDto(entry.ModifiedBy, modifierName);
            }
            else if (entry.ModifiedBy == entry.EvaluatorId)
            {
                // Edited by the original evaluator
                updatedBy = new UserSummaryDto(entry.EvaluatorId, entry.Evaluator?.DisplayName ?? "Unknown");
            }
            else
            {
                // Fallback if modifier not found
                updatedBy = new UserSummaryDto(entry.ModifiedBy, "Unknown User");
            }
        }

        return new EegEntryDto(
            entry.Id,
            entry.CriticalTaskId,
            new CriticalTaskSummaryDto(
                entry.CriticalTask.Id,
                entry.CriticalTask.TaskDescription,
                entry.CriticalTask.Standard,
                entry.CriticalTask.CapabilityTargetId,
                entry.CriticalTask.CapabilityTarget.TargetDescription,
                entry.CriticalTask.CapabilityTarget.Sources,
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
            entry.UpdatedAt,
            wasEdited,
            updatedBy
        );
    }
}
