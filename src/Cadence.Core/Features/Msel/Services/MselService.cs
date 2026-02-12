using Cadence.Core.Data;
using Cadence.Core.Features.Msel.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Msel.Services;

/// <summary>
/// Service for MSEL operations.
/// </summary>
public class MselService : IMselService
{
    private readonly AppDbContext _context;

    public MselService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<MselSummaryDto?> GetActiveMselSummaryAsync(Guid exerciseId)
    {
        var exercise = await _context.Exercises
            .Include(e => e.Phases)
            .Include(e => e.Objectives)
            .FirstOrDefaultAsync(e => e.Id == exerciseId && !e.IsDeleted);

        if (exercise == null)
            return null;

        // Get the active MSEL
        var msel = await _context.Msels
            .Include(m => m.Injects.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(m => m.ExerciseId == exerciseId && m.IsActive && !m.IsDeleted);

        if (msel == null)
            return null;

        return BuildMselSummary(msel, exercise);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MselDto>> GetMselsForExerciseAsync(Guid exerciseId)
    {
        var msels = await _context.Msels
            .Where(m => m.ExerciseId == exerciseId && !m.IsDeleted)
            .OrderByDescending(m => m.Version)
            .Select(m => new MselDto
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                Version = m.Version,
                IsActive = m.IsActive,
                ExerciseId = m.ExerciseId,
                InjectCount = m.Injects.Count(i => !i.IsDeleted),
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
            })
            .ToListAsync();

        return msels;
    }

    /// <inheritdoc />
    public async Task<MselSummaryDto?> GetMselSummaryAsync(Guid mselId)
    {
        var msel = await _context.Msels
            .Include(m => m.Exercise)
                .ThenInclude(e => e.Phases)
            .Include(m => m.Exercise)
                .ThenInclude(e => e.Objectives)
            .Include(m => m.Injects.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(m => m.Id == mselId && !m.IsDeleted);

        if (msel == null)
            return null;

        return BuildMselSummary(msel, msel.Exercise);
    }

    private MselSummaryDto BuildMselSummary(Cadence.Core.Models.Entities.Msel msel, Exercise exercise)
    {
        var injects = msel.Injects.ToList();
        var totalInjects = injects.Count;
        var draftCount = injects.Count(i => i.Status == InjectStatus.Draft);
        var releasedCount = injects.Count(i => i.Status == InjectStatus.Released);
        var deferredCount = injects.Count(i => i.Status == InjectStatus.Deferred);

        // Calculate completion percentage
        var completionPercentage = totalInjects > 0
            ? (int)Math.Round((double)(releasedCount + deferredCount) / totalInjects * 100)
            : 0;

        // Find last modified inject
        var lastModifiedInject = injects
            .OrderByDescending(i => i.UpdatedAt)
            .FirstOrDefault();

        // Note: LastModifiedByName is not populated here due to type mismatch between
        // BaseEntity.ModifiedBy (Guid) and ApplicationUser.Id (string).
        // This avoids an N+1 query that was causing performance issues.
        // Future: Consider denormalizing the user name or fixing the FK type.

        return new MselSummaryDto
        {
            Id = msel.Id,
            Name = msel.Name,
            Description = msel.Description,
            Version = msel.Version,
            IsActive = msel.IsActive,
            ExerciseId = msel.ExerciseId,
            TotalInjects = totalInjects,
            DraftCount = draftCount,
            ReleasedCount = releasedCount,
            DeferredCount = deferredCount,
            CompletionPercentage = completionPercentage,
            PhaseCount = exercise.Phases.Count(p => !p.IsDeleted),
            ObjectiveCount = exercise.Objectives.Count(o => !o.IsDeleted),
            LastModifiedAt = lastModifiedInject?.UpdatedAt,
            LastModifiedByName = null, // Not populated - see comment above
            CreatedAt = msel.CreatedAt,
            UpdatedAt = msel.UpdatedAt,
        };
    }
}
