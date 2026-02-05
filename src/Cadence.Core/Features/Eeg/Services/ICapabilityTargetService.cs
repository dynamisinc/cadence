using Cadence.Core.Features.Eeg.Models.DTOs;

namespace Cadence.Core.Features.Eeg.Services;

/// <summary>
/// Service interface for capability target operations.
/// </summary>
public interface ICapabilityTargetService
{
    /// <summary>
    /// Get all capability targets for an exercise.
    /// </summary>
    Task<CapabilityTargetListResponse> GetByExerciseAsync(Guid exerciseId);

    /// <summary>
    /// Get a single capability target by ID.
    /// </summary>
    Task<CapabilityTargetDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Create a new capability target.
    /// </summary>
    Task<CapabilityTargetDto> CreateAsync(Guid exerciseId, CreateCapabilityTargetRequest request, string createdBy);

    /// <summary>
    /// Update an existing capability target.
    /// </summary>
    Task<CapabilityTargetDto?> UpdateAsync(Guid id, UpdateCapabilityTargetRequest request, string modifiedBy);

    /// <summary>
    /// Delete a capability target (cascades to critical tasks).
    /// </summary>
    Task<bool> DeleteAsync(Guid id, string deletedBy);

    /// <summary>
    /// Reorder capability targets within an exercise.
    /// </summary>
    Task<bool> ReorderAsync(Guid exerciseId, IEnumerable<Guid> orderedIds);
}
