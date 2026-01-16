using Cadence.Core.Data;
using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Injects.Services;

/// <summary>
/// Service for inject conduct operations (firing, skipping, resetting).
/// </summary>
public class InjectService : IInjectService
{
    private readonly AppDbContext _context;
    private readonly IExerciseHubContext _hubContext;

    public InjectService(AppDbContext context, IExerciseHubContext hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public async Task<InjectDto> FireInjectAsync(Guid exerciseId, Guid injectId, Guid userId, CancellationToken cancellationToken = default)
    {
        var inject = await GetInjectForExerciseAsync(exerciseId, injectId, cancellationToken);

        if (inject.Status != InjectStatus.Pending)
        {
            throw new InvalidOperationException($"Inject is already {inject.Status}. Only pending injects can be fired.");
        }

        inject.Status = InjectStatus.Fired;
        inject.FiredAt = DateTime.UtcNow;
        inject.FiredBy = userId;
        inject.SkippedAt = null;
        inject.SkippedBy = null;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = inject.ToDto();
        await _hubContext.NotifyInjectFired(exerciseId, dto);

        return dto;
    }

    /// <inheritdoc />
    public async Task<InjectDto> SkipInjectAsync(Guid exerciseId, Guid injectId, Guid userId, CancellationToken cancellationToken = default)
    {
        var inject = await GetInjectForExerciseAsync(exerciseId, injectId, cancellationToken);

        if (inject.Status != InjectStatus.Pending)
        {
            throw new InvalidOperationException($"Inject is already {inject.Status}. Only pending injects can be skipped.");
        }

        inject.Status = InjectStatus.Skipped;
        inject.SkippedAt = DateTime.UtcNow;
        inject.SkippedBy = userId;
        inject.FiredAt = null;
        inject.FiredBy = null;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = inject.ToDto();
        await _hubContext.NotifyInjectSkipped(exerciseId, dto);

        return dto;
    }

    /// <inheritdoc />
    public async Task<InjectDto> ResetInjectAsync(Guid exerciseId, Guid injectId, CancellationToken cancellationToken = default)
    {
        var inject = await GetInjectForExerciseAsync(exerciseId, injectId, cancellationToken);

        inject.Status = InjectStatus.Pending;
        inject.FiredAt = null;
        inject.FiredBy = null;
        inject.SkippedAt = null;
        inject.SkippedBy = null;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = inject.ToDto();
        await _hubContext.NotifyInjectReset(exerciseId, dto);

        return dto;
    }

    private async Task<Inject> GetInjectForExerciseAsync(Guid exerciseId, Guid injectId, CancellationToken cancellationToken)
    {
        var exercise = await _context.Exercises.FindAsync(new object[] { exerciseId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Exercise {exerciseId} not found.");

        if (exercise.ActiveMselId == null)
        {
            throw new InvalidOperationException("Exercise has no active MSEL.");
        }

        var inject = await _context.Injects
            .Include(i => i.Phase)
            .Include(i => i.FiredByUser)
            .Include(i => i.SkippedByUser)
            .Include(i => i.InjectObjectives)
            .FirstOrDefaultAsync(i => i.Id == injectId && i.MselId == exercise.ActiveMselId, cancellationToken)
            ?? throw new KeyNotFoundException($"Inject {injectId} not found in exercise's active MSEL.");

        return inject;
    }
}
