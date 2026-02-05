using Cadence.Core.Features.Eeg.Models.DTOs;

namespace Cadence.Core.Features.Eeg.Services;

/// <summary>
/// Service interface for critical task operations.
/// </summary>
public interface ICriticalTaskService
{
    /// <summary>
    /// Get all critical tasks for a capability target.
    /// </summary>
    Task<CriticalTaskListResponse> GetByCapabilityTargetAsync(Guid capabilityTargetId);

    /// <summary>
    /// Get all critical tasks for an exercise (across all capability targets).
    /// </summary>
    Task<CriticalTaskListResponse> GetByExerciseAsync(Guid exerciseId, bool? hasInjects = null, bool? hasEegEntries = null);

    /// <summary>
    /// Get a single critical task by ID.
    /// </summary>
    Task<CriticalTaskDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Create a new critical task.
    /// </summary>
    Task<CriticalTaskDto> CreateAsync(Guid capabilityTargetId, CreateCriticalTaskRequest request, string createdBy);

    /// <summary>
    /// Update an existing critical task.
    /// </summary>
    Task<CriticalTaskDto?> UpdateAsync(Guid id, UpdateCriticalTaskRequest request, string modifiedBy);

    /// <summary>
    /// Delete a critical task (cascades to EEG entries and inject links).
    /// </summary>
    Task<bool> DeleteAsync(Guid id, string deletedBy);

    /// <summary>
    /// Reorder critical tasks within a capability target.
    /// </summary>
    Task<bool> ReorderAsync(Guid capabilityTargetId, IEnumerable<Guid> orderedIds);

    /// <summary>
    /// Set the linked injects for a critical task.
    /// </summary>
    /// <param name="criticalTaskId">The critical task ID.</param>
    /// <param name="injectIds">The inject IDs to link.</param>
    /// <param name="createdBy">User ID creating the linkages (for audit).</param>
    Task<bool> SetLinkedInjectsAsync(Guid criticalTaskId, IEnumerable<Guid> injectIds, string createdBy);

    /// <summary>
    /// Get linked inject IDs for a critical task.
    /// </summary>
    Task<IEnumerable<Guid>> GetLinkedInjectIdsAsync(Guid criticalTaskId);
}
