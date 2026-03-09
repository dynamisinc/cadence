using Cadence.Core.Features.ExpectedOutcomes.Models.DTOs;

namespace Cadence.Core.Features.ExpectedOutcomes.Services;

/// <summary>
/// Result of an inject validation lookup.
/// </summary>
public record InjectValidationResult(bool InjectExists, bool ExerciseIsArchived);

/// <summary>
/// Service interface for managing expected outcomes.
/// </summary>
public interface IExpectedOutcomeService
{
    /// <summary>
    /// Validates whether an inject exists and returns whether its parent exercise is archived.
    /// Used for pre-condition checks before mutating expected outcomes.
    /// </summary>
    /// <param name="injectId">The inject ID to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result indicating existence and archive status</returns>
    Task<InjectValidationResult> ValidateInjectAsync(Guid injectId, CancellationToken ct = default);

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
