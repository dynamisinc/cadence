using Cadence.Core.Features.Msel.Models.DTOs;

namespace Cadence.Core.Features.Msel.Services;

/// <summary>
/// Service interface for MSEL operations.
/// </summary>
public interface IMselService
{
    /// <summary>
    /// Get the summary of the active MSEL for an exercise.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <returns>MSEL summary or null if no active MSEL exists</returns>
    Task<MselSummaryDto?> GetActiveMselSummaryAsync(Guid exerciseId);

    /// <summary>
    /// Get all MSELs for an exercise.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <returns>List of MSELs</returns>
    Task<IReadOnlyList<MselDto>> GetMselsForExerciseAsync(Guid exerciseId);

    /// <summary>
    /// Get a specific MSEL by ID.
    /// </summary>
    /// <param name="mselId">The MSEL ID</param>
    /// <returns>MSEL summary or null if not found</returns>
    Task<MselSummaryDto?> GetMselSummaryAsync(Guid mselId);
}
