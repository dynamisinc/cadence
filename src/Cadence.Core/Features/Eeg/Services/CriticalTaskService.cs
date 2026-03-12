using Cadence.Core.Data;
using Cadence.Core.Features.Eeg.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Eeg.Services;

/// <summary>
/// Service implementation for critical task operations.
/// </summary>
public class CriticalTaskService : ICriticalTaskService
{
    private readonly AppDbContext _context;

    public CriticalTaskService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CriticalTaskListResponse> GetByCapabilityTargetAsync(Guid capabilityTargetId)
    {
        var tasks = await _context.CriticalTasks
            .Include(ct => ct.LinkedInjects)
            .Include(ct => ct.EegEntries)
            .Where(ct => ct.CapabilityTargetId == capabilityTargetId)
            .OrderBy(ct => ct.SortOrder)
            .ToListAsync();

        var items = tasks.Select(ToDto).ToList();
        return new CriticalTaskListResponse(items, items.Count);
    }

    public async Task<CriticalTaskListResponse> GetByExerciseAsync(Guid exerciseId, bool? hasInjects = null, bool? hasEegEntries = null)
    {
        var query = _context.CriticalTasks
            .Include(ct => ct.LinkedInjects)
            .Include(ct => ct.EegEntries)
            .Include(ct => ct.CapabilityTarget)
            .Where(ct => ct.CapabilityTarget.ExerciseId == exerciseId);

        if (hasInjects.HasValue)
        {
            query = hasInjects.Value
                ? query.Where(ct => ct.LinkedInjects.Any())
                : query.Where(ct => !ct.LinkedInjects.Any());
        }

        if (hasEegEntries.HasValue)
        {
            query = hasEegEntries.Value
                ? query.Where(ct => ct.EegEntries.Any(e => !e.IsDeleted))
                : query.Where(ct => !ct.EegEntries.Any(e => !e.IsDeleted));
        }

        var tasks = await query
            .OrderBy(ct => ct.CapabilityTarget.SortOrder)
            .ThenBy(ct => ct.SortOrder)
            .ToListAsync();

        var items = tasks.Select(ToDto).ToList();
        return new CriticalTaskListResponse(items, items.Count);
    }

    public async Task<CriticalTaskDto?> GetByIdAsync(Guid id)
    {
        var task = await _context.CriticalTasks
            .Include(ct => ct.LinkedInjects)
            .Include(ct => ct.EegEntries)
            .FirstOrDefaultAsync(ct => ct.Id == id);

        return task == null ? null : ToDto(task);
    }

    public async Task<CriticalTaskDto> CreateAsync(Guid capabilityTargetId, CreateCriticalTaskRequest request, string createdBy)
    {
        // Validate capability target exists
        var capabilityTarget = await _context.CapabilityTargets
            .FirstOrDefaultAsync(ct => ct.Id == capabilityTargetId);

        if (capabilityTarget == null)
            throw new InvalidOperationException($"Capability target {capabilityTargetId} not found");

        // Determine sort order
        var sortOrder = request.SortOrder ?? await GetNextSortOrderAsync(capabilityTargetId);

        var task = new CriticalTask
        {
            Id = Guid.NewGuid(),
            OrganizationId = capabilityTarget.OrganizationId,
            CapabilityTargetId = capabilityTargetId,
            TaskDescription = request.TaskDescription,
            Standard = request.Standard,
            SortOrder = sortOrder,
            CreatedBy = createdBy,
            ModifiedBy = createdBy
        };

        _context.CriticalTasks.Add(task);
        await _context.SaveChangesAsync();

        // Initialize empty collections
        task.LinkedInjects = new List<InjectCriticalTask>();
        task.EegEntries = new List<EegEntry>();

        return ToDto(task);
    }

    public async Task<CriticalTaskDto?> UpdateAsync(Guid id, UpdateCriticalTaskRequest request, string modifiedBy)
    {
        var task = await _context.CriticalTasks
            .Include(ct => ct.LinkedInjects)
            .Include(ct => ct.EegEntries)
            .FirstOrDefaultAsync(ct => ct.Id == id);

        if (task == null)
            return null;

        task.TaskDescription = request.TaskDescription;
        task.Standard = request.Standard;
        if (request.SortOrder.HasValue)
            task.SortOrder = request.SortOrder.Value;
        task.ModifiedBy = modifiedBy;

        await _context.SaveChangesAsync();

        return ToDto(task);
    }

    public async Task<bool> DeleteAsync(Guid id, string deletedBy)
    {
        var task = await _context.CriticalTasks.FindAsync(id);
        if (task == null)
            return false;

        // Soft delete
        task.IsDeleted = true;
        task.DeletedAt = DateTime.UtcNow;
        task.DeletedBy = deletedBy;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderAsync(Guid capabilityTargetId, IEnumerable<Guid> orderedIds)
    {
        var tasks = await _context.CriticalTasks
            .Where(ct => ct.CapabilityTargetId == capabilityTargetId)
            .ToListAsync();

        var orderedIdsList = orderedIds.ToList();

        for (var i = 0; i < orderedIdsList.Count; i++)
        {
            var task = tasks.FirstOrDefault(ct => ct.Id == orderedIdsList[i]);
            if (task != null)
                task.SortOrder = i;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetLinkedInjectsAsync(Guid criticalTaskId, IEnumerable<Guid> injectIds, string createdBy)
    {
        var task = await _context.CriticalTasks
            .Include(ct => ct.LinkedInjects)
            .FirstOrDefaultAsync(ct => ct.Id == criticalTaskId);

        if (task == null)
            return false;

        // Remove existing links
        _context.InjectCriticalTasks.RemoveRange(task.LinkedInjects);

        // Add new links with audit fields
        var injectIdsList = injectIds.ToList();
        var now = DateTime.UtcNow;
        foreach (var injectId in injectIdsList)
        {
            _context.InjectCriticalTasks.Add(new InjectCriticalTask
            {
                InjectId = injectId,
                CriticalTaskId = criticalTaskId,
                CreatedAt = now,
                CreatedBy = createdBy
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Guid>> GetLinkedInjectIdsAsync(Guid criticalTaskId)
    {
        return await _context.InjectCriticalTasks
            .Where(ict => ict.CriticalTaskId == criticalTaskId)
            .Select(ict => ict.InjectId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Guid>> GetLinkedCriticalTaskIdsForInjectAsync(Guid exerciseId, Guid injectId)
    {
        // Validate that the inject belongs to the specified exercise
        var injectBelongsToExercise = await _context.Injects
            .AnyAsync(i => i.Id == injectId && i.Msel.ExerciseId == exerciseId);

        if (!injectBelongsToExercise)
            throw new KeyNotFoundException($"Inject {injectId} not found in exercise {exerciseId}");

        return await _context.InjectCriticalTasks
            .Where(ict => ict.InjectId == injectId)
            .Select(ict => ict.CriticalTaskId)
            .ToListAsync();
    }

    public async Task<List<CriticalTaskDto>> SetLinkedCriticalTasksForInjectAsync(
        Guid exerciseId,
        Guid injectId,
        IEnumerable<Guid> criticalTaskIds,
        string userId)
    {
        // Validate that the inject belongs to the specified exercise
        var injectBelongsToExercise = await _context.Injects
            .AnyAsync(i => i.Id == injectId && i.Msel.ExerciseId == exerciseId);

        if (!injectBelongsToExercise)
            throw new KeyNotFoundException($"Inject {injectId} not found in exercise {exerciseId}");

        var criticalTaskIdsList = criticalTaskIds.ToList();

        // Validate all task IDs belong to this exercise
        var validTaskIds = await _context.CriticalTasks
            .Where(ct => criticalTaskIdsList.Contains(ct.Id))
            .Where(ct => ct.CapabilityTarget.ExerciseId == exerciseId)
            .Select(ct => ct.Id)
            .ToListAsync();

        var invalidTaskIds = criticalTaskIdsList.Except(validTaskIds).ToList();
        if (invalidTaskIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Invalid or cross-exercise task IDs: {string.Join(", ", invalidTaskIds)}");
        }

        // Load existing links for this inject
        var existingLinks = await _context.InjectCriticalTasks
            .Where(ict => ict.InjectId == injectId)
            .ToListAsync();

        // Clear existing links
        _context.InjectCriticalTasks.RemoveRange(existingLinks);

        // Add new links with audit fields
        var now = DateTime.UtcNow;
        foreach (var taskId in validTaskIds)
        {
            _context.InjectCriticalTasks.Add(new InjectCriticalTask
            {
                InjectId = injectId,
                CriticalTaskId = taskId,
                CreatedAt = now,
                CreatedBy = userId
            });
        }

        await _context.SaveChangesAsync();

        // Return the linked tasks with full details
        var linkedTasks = await _context.CriticalTasks
            .Include(ct => ct.LinkedInjects)
            .Include(ct => ct.EegEntries)
            .Where(ct => validTaskIds.Contains(ct.Id))
            .OrderBy(ct => ct.SortOrder)
            .ToListAsync();

        return linkedTasks.Select(ToDto).ToList();
    }

    private async Task<int> GetNextSortOrderAsync(Guid capabilityTargetId)
    {
        var maxSortOrder = await _context.CriticalTasks
            .Where(ct => ct.CapabilityTargetId == capabilityTargetId)
            .MaxAsync(ct => (int?)ct.SortOrder) ?? -1;

        return maxSortOrder + 1;
    }

    private static CriticalTaskDto ToDto(CriticalTask task) => new(
        task.Id,
        task.CapabilityTargetId,
        task.TaskDescription,
        task.Standard,
        task.SortOrder,
        task.LinkedInjects.Count,
        task.EegEntries.Count(e => !e.IsDeleted),
        task.CreatedAt,
        task.UpdatedAt
    );
}
