using Cadence.Core.Features.Exercises.Models.DTOs;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service for calculating exercise setup progress
/// </summary>
public interface ISetupProgressService
{
    /// <summary>
    /// Get the setup progress for an exercise
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <returns>Setup progress or null if exercise not found</returns>
    Task<SetupProgressDto?> GetSetupProgressAsync(Guid exerciseId);
}
