using Cadence.Core.Features.Phases.Models.DTOs;

namespace Cadence.Core.Features.Phases.Services;

/// <summary>
/// Service interface for managing exercise phases.
/// Phases organize injects into logical time segments within an exercise.
/// </summary>
public interface IPhaseService
{
    /// <summary>
    /// Gets all phases for an exercise, ordered by sequence.
    /// Includes inject counts per phase.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of phase DTOs, or null if exercise not found</returns>
    Task<List<PhaseDto>?> GetPhasesAsync(Guid exerciseId, CancellationToken ct = default);

    /// <summary>
    /// Gets a single phase by ID within an exercise.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="phaseId">The phase ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Phase DTO, or null if not found</returns>
    Task<PhaseDto?> GetPhaseAsync(Guid exerciseId, Guid phaseId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new phase for an exercise.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="request">Phase creation request</param>
    /// <param name="userId">The creating user's ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created phase DTO</returns>
    /// <exception cref="KeyNotFoundException">Exercise not found</exception>
    /// <exception cref="ArgumentException">Validation failed</exception>
    /// <exception cref="InvalidOperationException">Exercise is archived</exception>
    Task<PhaseDto> CreatePhaseAsync(Guid exerciseId, CreatePhaseRequest request, string userId, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing phase.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="phaseId">The phase ID</param>
    /// <param name="request">Phase update request</param>
    /// <param name="userId">The modifying user's ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated phase DTO, or null if not found</returns>
    /// <exception cref="ArgumentException">Validation failed</exception>
    /// <exception cref="InvalidOperationException">Exercise is archived</exception>
    Task<PhaseDto?> UpdatePhaseAsync(Guid exerciseId, Guid phaseId, UpdatePhaseRequest request, string userId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a phase from an exercise.
    /// Only allowed when the phase has no assigned injects.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="phaseId">The phase ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    /// <exception cref="InvalidOperationException">Exercise is archived or phase has injects</exception>
    Task<bool> DeletePhaseAsync(Guid exerciseId, Guid phaseId, CancellationToken ct = default);

    /// <summary>
    /// Reorders phases for an exercise by providing the new sequence of phase IDs.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="request">Reorder request with ordered phase IDs</param>
    /// <param name="userId">The modifying user's ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Reordered list of phase DTOs, or null if exercise not found</returns>
    /// <exception cref="ArgumentException">Phase IDs invalid or do not belong to this exercise</exception>
    /// <exception cref="InvalidOperationException">Exercise is archived</exception>
    Task<List<PhaseDto>?> ReorderPhasesAsync(Guid exerciseId, ReorderPhasesRequest request, string userId, CancellationToken ct = default);
}
