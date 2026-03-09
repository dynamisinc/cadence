using Cadence.Core.Data;
using Cadence.Core.Features.ExpectedOutcomes.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.ExpectedOutcomes.Services;

/// <summary>
/// Service for managing expected outcomes on injects.
/// </summary>
public class ExpectedOutcomeService : IExpectedOutcomeService
{
    private readonly AppDbContext _context;

    public ExpectedOutcomeService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<InjectValidationResult> ValidateInjectAsync(Guid injectId, CancellationToken ct = default)
    {
        var result = await _context.Injects
            .Where(i => i.Id == injectId)
            .Select(i => new
            {
                ExerciseStatus = i.Msel!.Exercise.Status
            })
            .FirstOrDefaultAsync(ct);

        if (result == null)
            return new InjectValidationResult(InjectExists: false, ExerciseIsArchived: false);

        return new InjectValidationResult(
            InjectExists: true,
            ExerciseIsArchived: result.ExerciseStatus == ExerciseStatus.Archived);
    }

    /// <inheritdoc />
    public async Task<List<ExpectedOutcomeDto>> GetByInjectIdAsync(Guid injectId)
    {
        var outcomes = await _context.ExpectedOutcomes
            .Where(o => o.InjectId == injectId)
            .OrderBy(o => o.SortOrder)
            .ToListAsync();

        return outcomes.Select(o => o.ToDto()).ToList();
    }

    /// <inheritdoc />
    public async Task<ExpectedOutcomeDto?> GetByIdAsync(Guid id)
    {
        var outcome = await _context.ExpectedOutcomes.FindAsync(id);
        return outcome?.ToDto();
    }

    /// <inheritdoc />
    public async Task<ExpectedOutcomeDto> CreateAsync(Guid injectId, CreateExpectedOutcomeRequest request, string userId)
    {
        // Validate inject exists
        var inject = await _context.Injects.FindAsync(injectId)
            ?? throw new KeyNotFoundException($"Inject {injectId} not found.");

        // Validate description
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Description is required.");
        }

        if (request.Description.Length > 1000)
        {
            throw new ArgumentException("Description cannot exceed 1000 characters.");
        }

        // Get next sort order if not specified
        var nextSortOrder = request.SortOrder ?? await GetNextSortOrderAsync(injectId);

        var outcome = request.ToEntity(injectId, nextSortOrder, userId);
        _context.ExpectedOutcomes.Add(outcome);
        await _context.SaveChangesAsync();

        return outcome.ToDto();
    }

    /// <inheritdoc />
    public async Task<ExpectedOutcomeDto?> UpdateAsync(Guid id, UpdateExpectedOutcomeRequest request, string userId)
    {
        var outcome = await _context.ExpectedOutcomes.FindAsync(id);
        if (outcome == null) return null;

        // Validate description
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Description is required.");
        }

        if (request.Description.Length > 1000)
        {
            throw new ArgumentException("Description cannot exceed 1000 characters.");
        }

        outcome.UpdateFromRequest(request, userId);
        await _context.SaveChangesAsync();

        return outcome.ToDto();
    }

    /// <inheritdoc />
    public async Task<ExpectedOutcomeDto?> EvaluateAsync(Guid id, EvaluateExpectedOutcomeRequest request, string userId)
    {
        var outcome = await _context.ExpectedOutcomes.FindAsync(id);
        if (outcome == null) return null;

        // Validate evaluator notes length
        if (request.EvaluatorNotes?.Length > 2000)
        {
            throw new ArgumentException("Evaluator notes cannot exceed 2000 characters.");
        }

        outcome.EvaluateFromRequest(request, userId);
        await _context.SaveChangesAsync();

        return outcome.ToDto();
    }

    /// <inheritdoc />
    public async Task<bool> ReorderAsync(Guid injectId, ReorderExpectedOutcomesRequest request, string userId)
    {
        // Validate inject exists
        var inject = await _context.Injects.FindAsync(injectId);
        if (inject == null) return false;

        // Get all outcomes for this inject
        var outcomes = await _context.ExpectedOutcomes
            .Where(o => o.InjectId == injectId)
            .ToListAsync();

        // Validate all IDs belong to this inject
        var outcomeIds = outcomes.Select(o => o.Id).ToHashSet();
        if (!request.OutcomeIds.All(id => outcomeIds.Contains(id)))
        {
            throw new ArgumentException("One or more outcome IDs do not belong to this inject.");
        }

        if (request.OutcomeIds.Count != outcomes.Count)
        {
            throw new ArgumentException("The number of outcome IDs must match the total number of outcomes for this inject.");
        }

        // Update sort orders
        for (int i = 0; i < request.OutcomeIds.Count; i++)
        {
            var outcome = outcomes.First(o => o.Id == request.OutcomeIds[i]);
            outcome.SortOrder = i;
            outcome.ModifiedBy = userId;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, string userId)
    {
        var outcome = await _context.ExpectedOutcomes.FindAsync(id);
        if (outcome == null) return false;

        _context.ExpectedOutcomes.Remove(outcome);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<int> GetNextSortOrderAsync(Guid injectId)
    {
        var maxSortOrder = await _context.ExpectedOutcomes
            .Where(o => o.InjectId == injectId)
            .MaxAsync(o => (int?)o.SortOrder) ?? -1;

        return maxSortOrder + 1;
    }
}
