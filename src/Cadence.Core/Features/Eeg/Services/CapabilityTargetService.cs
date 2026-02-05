using Cadence.Core.Data;
using Cadence.Core.Features.Eeg.Models.DTOs;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Eeg.Services;

/// <summary>
/// Service implementation for capability target operations.
/// </summary>
public class CapabilityTargetService : ICapabilityTargetService
{
    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationContext _orgContext;

    public CapabilityTargetService(AppDbContext context, ICurrentOrganizationContext orgContext)
    {
        _context = context;
        _orgContext = orgContext;
    }

    public async Task<CapabilityTargetListResponse> GetByExerciseAsync(Guid exerciseId)
    {
        var targets = await _context.CapabilityTargets
            .Include(ct => ct.Capability)
            .Include(ct => ct.CriticalTasks)
            .Where(ct => ct.ExerciseId == exerciseId)
            .OrderBy(ct => ct.SortOrder)
            .ToListAsync();

        var items = targets.Select(ToDto).ToList();
        return new CapabilityTargetListResponse(items, items.Count);
    }

    public async Task<CapabilityTargetDto?> GetByIdAsync(Guid id)
    {
        var target = await _context.CapabilityTargets
            .Include(ct => ct.Capability)
            .Include(ct => ct.CriticalTasks)
            .FirstOrDefaultAsync(ct => ct.Id == id);

        return target == null ? null : ToDto(target);
    }

    public async Task<CapabilityTargetDto> CreateAsync(Guid exerciseId, CreateCapabilityTargetRequest request, string createdBy)
    {
        // Validate exercise exists and belongs to current organization
        var exercise = await _context.Exercises
            .FirstOrDefaultAsync(e => e.Id == exerciseId);

        if (exercise == null)
            throw new InvalidOperationException($"Exercise {exerciseId} not found");

        // Validate capability exists and belongs to current organization
        var capability = await _context.Capabilities
            .FirstOrDefaultAsync(c => c.Id == request.CapabilityId);

        if (capability == null)
            throw new InvalidOperationException($"Capability {request.CapabilityId} not found");

        if (capability.OrganizationId != exercise.OrganizationId)
            throw new InvalidOperationException("Capability does not belong to the same organization as the exercise");

        // Determine sort order
        var sortOrder = request.SortOrder ?? await GetNextSortOrderAsync(exerciseId);

        var target = new CapabilityTarget
        {
            Id = Guid.NewGuid(),
            ExerciseId = exerciseId,
            OrganizationId = exercise.OrganizationId,
            CapabilityId = request.CapabilityId,
            TargetDescription = request.TargetDescription,
            SortOrder = sortOrder,
            CreatedBy = createdBy,
            ModifiedBy = createdBy
        };

        _context.CapabilityTargets.Add(target);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(target).Reference(ct => ct.Capability).LoadAsync();
        target.CriticalTasks = new List<CriticalTask>();

        return ToDto(target);
    }

    public async Task<CapabilityTargetDto?> UpdateAsync(Guid id, UpdateCapabilityTargetRequest request, string modifiedBy)
    {
        var target = await _context.CapabilityTargets
            .Include(ct => ct.Capability)
            .Include(ct => ct.CriticalTasks)
            .FirstOrDefaultAsync(ct => ct.Id == id);

        if (target == null)
            return null;

        target.TargetDescription = request.TargetDescription;
        if (request.SortOrder.HasValue)
            target.SortOrder = request.SortOrder.Value;
        target.ModifiedBy = modifiedBy;

        await _context.SaveChangesAsync();

        return ToDto(target);
    }

    public async Task<bool> DeleteAsync(Guid id, string deletedBy)
    {
        var target = await _context.CapabilityTargets.FindAsync(id);
        if (target == null)
            return false;

        // Soft delete
        target.IsDeleted = true;
        target.DeletedAt = DateTime.UtcNow;
        target.DeletedBy = deletedBy;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderAsync(Guid exerciseId, IEnumerable<Guid> orderedIds)
    {
        var targets = await _context.CapabilityTargets
            .Where(ct => ct.ExerciseId == exerciseId)
            .ToListAsync();

        var orderedIdsList = orderedIds.ToList();

        for (var i = 0; i < orderedIdsList.Count; i++)
        {
            var target = targets.FirstOrDefault(ct => ct.Id == orderedIdsList[i]);
            if (target != null)
                target.SortOrder = i;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<int> GetNextSortOrderAsync(Guid exerciseId)
    {
        var maxSortOrder = await _context.CapabilityTargets
            .Where(ct => ct.ExerciseId == exerciseId)
            .MaxAsync(ct => (int?)ct.SortOrder) ?? -1;

        return maxSortOrder + 1;
    }

    private static CapabilityTargetDto ToDto(CapabilityTarget target) => new(
        target.Id,
        target.ExerciseId,
        target.CapabilityId,
        new CapabilitySummaryDto(
            target.Capability.Id,
            target.Capability.Name,
            target.Capability.Category
        ),
        target.TargetDescription,
        target.SortOrder,
        target.CriticalTasks.Count(ct => !ct.IsDeleted),
        target.CreatedAt,
        target.UpdatedAt
    );
}
