using Cadence.Core.Features.Objectives.Models.DTOs;

namespace Cadence.Core.Features.Objectives.Services;

/// <summary>
/// Service interface for objective operations.
/// </summary>
public interface IObjectiveService
{
    /// <summary>
    /// Get all objectives for an exercise.
    /// </summary>
    Task<IEnumerable<ObjectiveDto>> GetObjectivesByExerciseAsync(Guid exerciseId);

    /// <summary>
    /// Get objective summaries for dropdowns/selection.
    /// </summary>
    Task<IEnumerable<ObjectiveSummaryDto>> GetObjectiveSummariesAsync(Guid exerciseId);

    /// <summary>
    /// Get a single objective by ID.
    /// </summary>
    Task<ObjectiveDto?> GetObjectiveAsync(Guid exerciseId, Guid id);

    /// <summary>
    /// Create a new objective.
    /// </summary>
    Task<ObjectiveDto> CreateObjectiveAsync(Guid exerciseId, CreateObjectiveRequest request, Guid createdBy);

    /// <summary>
    /// Update an existing objective.
    /// </summary>
    Task<ObjectiveDto?> UpdateObjectiveAsync(Guid exerciseId, Guid id, UpdateObjectiveRequest request, Guid modifiedBy);

    /// <summary>
    /// Soft delete an objective.
    /// </summary>
    Task<bool> DeleteObjectiveAsync(Guid exerciseId, Guid id, Guid deletedBy);

    /// <summary>
    /// Check if an objective number is unique within an exercise.
    /// </summary>
    Task<bool> IsObjectiveNumberUniqueAsync(Guid exerciseId, string objectiveNumber, Guid? excludeId = null);
}
