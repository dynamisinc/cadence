using Cadence.Core.Features.ExpectedOutcomes.Models.DTOs;

namespace Cadence.Core.Features.ExpectedOutcomes.Services;

/// <summary>
/// Service interface for managing expected outcomes.
/// </summary>
public interface IExpectedOutcomeService
{
    /// <summary>
    /// Gets all expected outcomes for an inject.
    /// </summary>
    Task<List<ExpectedOutcomeDto>> GetByInjectIdAsync(Guid injectId);

    /// <summary>
    /// Gets a single expected outcome by ID.
    /// </summary>
    Task<ExpectedOutcomeDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new expected outcome for an inject.
    /// </summary>
    Task<ExpectedOutcomeDto> CreateAsync(Guid injectId, CreateExpectedOutcomeRequest request, string userId);

    /// <summary>
    /// Updates an expected outcome's description.
    /// </summary>
    Task<ExpectedOutcomeDto?> UpdateAsync(Guid id, UpdateExpectedOutcomeRequest request, string userId);

    /// <summary>
    /// Evaluates an expected outcome (sets WasAchieved and EvaluatorNotes).
    /// </summary>
    Task<ExpectedOutcomeDto?> EvaluateAsync(Guid id, EvaluateExpectedOutcomeRequest request, string userId);

    /// <summary>
    /// Reorders expected outcomes for an inject.
    /// </summary>
    Task<bool> ReorderAsync(Guid injectId, ReorderExpectedOutcomesRequest request, string userId);

    /// <summary>
    /// Deletes an expected outcome.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, string userId);
}
