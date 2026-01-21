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
        var (inject, exercise) = await GetInjectAndExerciseAsync(exerciseId, injectId, cancellationToken);

        // Validate exercise is active
        if (exercise.Status != ExerciseStatus.Active)
        {
            throw new InvalidOperationException($"Cannot fire inject. Exercise is {exercise.Status}. Injects can only be fired during an active exercise.");
        }

        // Validate inject can be fired based on delivery mode
        // In clock-driven mode, inject must be Ready
        // In facilitator-paced mode, inject can be Pending or Ready
        if (exercise.DeliveryMode == DeliveryMode.ClockDriven)
        {
            if (inject.Status != InjectStatus.Ready)
            {
                throw new InvalidOperationException($"Inject must be Ready to fire in clock-driven mode. Current status: {inject.Status}");
            }
        }
        else // FacilitatorPaced
        {
            if (inject.Status != InjectStatus.Pending && inject.Status != InjectStatus.Ready)
            {
                throw new InvalidOperationException($"Inject is already {inject.Status}. Only Pending or Ready injects can be fired.");
            }
        }

        inject.Status = InjectStatus.Fired;
        inject.FiredAt = DateTime.UtcNow;
        inject.FiredBy = userId;
        inject.ModifiedBy = userId;
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
        var (inject, exercise) = await GetInjectAndExerciseAsync(exerciseId, injectId, cancellationToken);

        // Validate exercise is active
        if (exercise.Status != ExerciseStatus.Active)
        {
            throw new InvalidOperationException($"Cannot skip inject. Exercise is {exercise.Status}. Injects can only be skipped during an active exercise.");
        }

        // Injects can be skipped from Pending or Ready status
        if (inject.Status != InjectStatus.Pending && inject.Status != InjectStatus.Ready)
        {
            throw new InvalidOperationException($"Inject is already {inject.Status}. Only Pending or Ready injects can be skipped.");
        }

        inject.Status = InjectStatus.Skipped;
        inject.SkippedAt = DateTime.UtcNow;
        inject.SkippedBy = userId;
        inject.ModifiedBy = userId;
        inject.FiredAt = null;
        inject.FiredBy = null;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = inject.ToDto();
        await _hubContext.NotifyInjectSkipped(exerciseId, dto);

        return dto;
    }

    /// <inheritdoc />
    public async Task<InjectDto> ResetInjectAsync(Guid exerciseId, Guid injectId, Guid userId, CancellationToken cancellationToken = default)
    {
        var (inject, exercise) = await GetInjectAndExerciseAsync(exerciseId, injectId, cancellationToken);

        // Validate exercise is active
        if (exercise.Status != ExerciseStatus.Active)
        {
            throw new InvalidOperationException($"Cannot reset inject. Exercise is {exercise.Status}. Injects can only be reset during an active exercise.");
        }

        inject.Status = InjectStatus.Pending;
        inject.ReadyAt = null;
        inject.FiredAt = null;
        inject.FiredBy = null;
        inject.SkippedAt = null;
        inject.SkippedBy = null;
        inject.ModifiedBy = userId;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = inject.ToDto();
        await _hubContext.NotifyInjectReset(exerciseId, dto);

        return dto;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<InjectDto>> ReorderInjectsAsync(Guid exerciseId, IEnumerable<Guid> injectIds, CancellationToken cancellationToken = default)
    {
        var exercise = await _context.Exercises.FindAsync(new object[] { exerciseId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Exercise {exerciseId} not found.");

        if (exercise.ActiveMselId == null)
        {
            throw new InvalidOperationException("Exercise has no active MSEL.");
        }

        // Block reordering for archived exercises
        if (exercise.Status == ExerciseStatus.Archived)
        {
            throw new InvalidOperationException("Cannot reorder injects in an archived exercise.");
        }

        var injectIdsList = injectIds.ToList();
        if (injectIdsList.Count == 0)
        {
            throw new ArgumentException("InjectIds cannot be empty.", nameof(injectIds));
        }

        // Get all injects for this MSEL
        var injects = await _context.Injects
            .Include(i => i.Phase)
            .Include(i => i.FiredByUser)
            .Include(i => i.SkippedByUser)
            .Include(i => i.InjectObjectives)
            .Where(i => i.MselId == exercise.ActiveMselId)
            .ToListAsync(cancellationToken);

        // Verify all provided IDs exist in this MSEL
        var injectDict = injects.ToDictionary(i => i.Id);
        foreach (var id in injectIdsList)
        {
            if (!injectDict.ContainsKey(id))
            {
                throw new KeyNotFoundException($"Inject {id} not found in this exercise.");
            }
        }

        // Update sequence values based on the new order
        for (int i = 0; i < injectIdsList.Count; i++)
        {
            var inject = injectDict[injectIdsList[i]];
            inject.Sequence = i + 1;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Broadcast SignalR notification for inject reorder
        await _hubContext.NotifyInjectsReordered(exerciseId, injectIdsList);

        // Return the updated injects in the new order
        return injectIdsList.Select(id => injectDict[id].ToDto());
    }

    private async Task<(Inject inject, Exercise exercise)> GetInjectAndExerciseAsync(Guid exerciseId, Guid injectId, CancellationToken cancellationToken)
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

        return (inject, exercise);
    }
}
