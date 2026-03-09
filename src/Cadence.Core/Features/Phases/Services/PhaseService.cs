using Cadence.Core.Data;
using Cadence.Core.Features.Phases.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Phases.Services;

/// <summary>
/// Service for managing exercise phases.
/// Phases organize injects into logical time segments within an exercise.
/// </summary>
public class PhaseService : IPhaseService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PhaseService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PhaseService"/>.
    /// </summary>
    public PhaseService(AppDbContext context, ILogger<PhaseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<PhaseDto>?> GetPhasesAsync(Guid exerciseId, CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync([exerciseId], ct);
        if (exercise == null)
            return null;

        var phases = await _context.Phases
            .Where(p => p.ExerciseId == exerciseId)
            .OrderBy(p => p.Sequence)
            .ToListAsync(ct);

        // Get inject counts per phase in a single query
        var injectCounts = await _context.Injects
            .Where(i => i.Msel!.ExerciseId == exerciseId && i.PhaseId != null)
            .GroupBy(i => i.PhaseId)
            .Select(g => new { PhaseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PhaseId!.Value, x => x.Count, ct);

        return phases.Select(p => p.ToDto(injectCounts.GetValueOrDefault(p.Id, 0))).ToList();
    }

    /// <inheritdoc />
    public async Task<PhaseDto?> GetPhaseAsync(Guid exerciseId, Guid phaseId, CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync([exerciseId], ct);
        if (exercise == null)
            return null;

        var phase = await _context.Phases
            .FirstOrDefaultAsync(p => p.Id == phaseId && p.ExerciseId == exerciseId, ct);

        if (phase == null)
            return null;

        var injectCount = await _context.Injects
            .CountAsync(i => i.PhaseId == phaseId, ct);

        return phase.ToDto(injectCount);
    }

    /// <inheritdoc />
    public async Task<PhaseDto> CreatePhaseAsync(
        Guid exerciseId,
        CreatePhaseRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync([exerciseId], ct)
            ?? throw new KeyNotFoundException($"Exercise {exerciseId} not found.");

        // Validate request fields
        ValidatePhaseRequest(request.Name, request.Description);

        // Check status-based restrictions
        if (exercise.Status == ExerciseStatus.Archived)
            throw new InvalidOperationException("Archived exercises cannot be modified.");

        // Get next sequence number
        var maxSequence = await _context.Phases
            .Where(p => p.ExerciseId == exerciseId)
            .MaxAsync(p => (int?)p.Sequence, ct) ?? 0;

        var phase = request.ToEntity(exerciseId, maxSequence + 1, userId);

        // Set OrganizationId from the parent exercise for data isolation
        phase.OrganizationId = exercise.OrganizationId;

        _context.Phases.Add(phase);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created phase {PhaseId}: {PhaseName} for exercise {ExerciseId}",
            phase.Id, phase.Name, exerciseId);

        return phase.ToDto(0);
    }

    /// <inheritdoc />
    public async Task<PhaseDto?> UpdatePhaseAsync(
        Guid exerciseId,
        Guid phaseId,
        UpdatePhaseRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync([exerciseId], ct);
        if (exercise == null)
            return null;

        var phase = await _context.Phases
            .FirstOrDefaultAsync(p => p.Id == phaseId && p.ExerciseId == exerciseId, ct);

        if (phase == null)
            return null;

        // Validate request fields
        ValidatePhaseRequest(request.Name, request.Description);

        // Check status-based restrictions
        if (exercise.Status == ExerciseStatus.Archived)
            throw new InvalidOperationException("Archived exercises cannot be modified.");

        phase.UpdateFromRequest(request, userId);
        await _context.SaveChangesAsync(ct);

        var injectCount = await _context.Injects
            .CountAsync(i => i.PhaseId == phaseId, ct);

        _logger.LogInformation("Updated phase {PhaseId}: {PhaseName}", phase.Id, phase.Name);

        return phase.ToDto(injectCount);
    }

    /// <inheritdoc />
    public async Task<bool> DeletePhaseAsync(Guid exerciseId, Guid phaseId, CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync([exerciseId], ct);
        if (exercise == null)
            return false;

        if (exercise.Status == ExerciseStatus.Archived)
            throw new InvalidOperationException("Archived exercises cannot be modified.");

        var phase = await _context.Phases
            .FirstOrDefaultAsync(p => p.Id == phaseId && p.ExerciseId == exerciseId, ct);

        if (phase == null)
            return false;

        // Check if phase has injects
        var injectCount = await _context.Injects
            .CountAsync(i => i.PhaseId == phaseId, ct);

        if (injectCount > 0)
            throw new InvalidOperationException(
                $"Cannot delete phase with {injectCount} inject(s). Move or delete the injects first.");

        // Hard delete (phases don't need soft delete since they're organizational)
        _context.Phases.Remove(phase);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted phase {PhaseId}: {PhaseName}", phase.Id, phase.Name);

        return true;
    }

    /// <inheritdoc />
    public async Task<List<PhaseDto>?> ReorderPhasesAsync(
        Guid exerciseId,
        ReorderPhasesRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync([exerciseId], ct);
        if (exercise == null)
            return null;

        if (exercise.Status == ExerciseStatus.Archived)
            throw new InvalidOperationException("Archived exercises cannot be modified.");

        if (request.PhaseIds.Count == 0)
            throw new ArgumentException("PhaseIds list is required.");

        // Get all phases for this exercise
        var phases = await _context.Phases
            .Where(p => p.ExerciseId == exerciseId)
            .ToListAsync(ct);

        // Validate all IDs are valid and belong to this exercise
        var phaseDict = phases.ToDictionary(p => p.Id);
        foreach (var phaseId in request.PhaseIds)
        {
            if (!phaseDict.ContainsKey(phaseId))
                throw new ArgumentException($"Phase {phaseId} not found in this exercise.");
        }

        for (int i = 0; i < request.PhaseIds.Count; i++)
        {
            var phase = phaseDict[request.PhaseIds[i]];
            phase.Sequence = i + 1;
            phase.ModifiedBy = userId;
        }

        await _context.SaveChangesAsync(ct);

        // Get inject counts for updated phase list
        var injectCounts = await _context.Injects
            .Where(i => i.Msel!.ExerciseId == exerciseId && i.PhaseId != null)
            .GroupBy(i => i.PhaseId)
            .Select(g => new { PhaseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PhaseId!.Value, x => x.Count, ct);

        _logger.LogInformation("Reordered {Count} phases for exercise {ExerciseId}",
            request.PhaseIds.Count, exerciseId);

        return phases
            .OrderBy(p => p.Sequence)
            .Select(p => p.ToDto(injectCounts.GetValueOrDefault(p.Id, 0)))
            .ToList();
    }

    /// <summary>
    /// Validates phase name and description fields.
    /// </summary>
    /// <exception cref="ArgumentException">Validation failed</exception>
    private static void ValidatePhaseRequest(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.");

        if (name.Length < 3)
            throw new ArgumentException("Name must be at least 3 characters.");

        if (name.Length > 100)
            throw new ArgumentException("Name must be 100 characters or less.");

        if (description?.Length > 500)
            throw new ArgumentException("Description must be 500 characters or less.");
    }
}
