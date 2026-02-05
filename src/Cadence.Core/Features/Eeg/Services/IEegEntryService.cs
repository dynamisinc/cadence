using Cadence.Core.Features.Eeg.Models.DTOs;

namespace Cadence.Core.Features.Eeg.Services;

/// <summary>
/// Service interface for EEG entry operations.
/// </summary>
public interface IEegEntryService
{
    /// <summary>
    /// Get all EEG entries for an exercise.
    /// </summary>
    Task<EegEntryListResponse> GetByExerciseAsync(Guid exerciseId);

    /// <summary>
    /// Get all EEG entries for a critical task.
    /// </summary>
    Task<EegEntryListResponse> GetByCriticalTaskAsync(Guid criticalTaskId);

    /// <summary>
    /// Get a single EEG entry by ID.
    /// </summary>
    Task<EegEntryDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Create a new EEG entry.
    /// </summary>
    Task<EegEntryDto> CreateAsync(CreateEegEntryRequest request, string evaluatorId);

    /// <summary>
    /// Update an existing EEG entry.
    /// </summary>
    Task<EegEntryDto?> UpdateAsync(Guid id, UpdateEegEntryRequest request, string modifiedBy);

    /// <summary>
    /// Delete an EEG entry.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, string deletedBy);

    /// <summary>
    /// Get EEG coverage statistics for an exercise.
    /// </summary>
    Task<EegCoverageDto> GetCoverageAsync(Guid exerciseId);
}
