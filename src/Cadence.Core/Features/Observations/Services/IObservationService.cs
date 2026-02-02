using Cadence.Core.Features.Observations.Models.DTOs;

namespace Cadence.Core.Features.Observations.Services;

/// <summary>
/// Service interface for observation operations.
/// </summary>
public interface IObservationService
{
    /// <summary>
    /// Get all observations for an exercise.
    /// </summary>
    Task<IEnumerable<ObservationDto>> GetObservationsByExerciseAsync(Guid exerciseId);

    /// <summary>
    /// Get all observations for a specific inject.
    /// </summary>
    Task<IEnumerable<ObservationDto>> GetObservationsByInjectAsync(Guid injectId);

    /// <summary>
    /// Get a single observation by ID.
    /// </summary>
    Task<ObservationDto?> GetObservationAsync(Guid id);

    /// <summary>
    /// Create a new observation.
    /// </summary>
    Task<ObservationDto> CreateObservationAsync(Guid exerciseId, CreateObservationRequest request, string createdBy);

    /// <summary>
    /// Update an existing observation.
    /// </summary>
    Task<ObservationDto?> UpdateObservationAsync(Guid id, UpdateObservationRequest request, string modifiedBy);

    /// <summary>
    /// Soft delete an observation.
    /// </summary>
    Task<bool> DeleteObservationAsync(Guid id, string deletedBy);
}
