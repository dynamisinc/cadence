using Cadence.Core.Features.Exercises.Models.DTOs;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service interface for core exercise CRUD and settings operations.
/// Handles exercise lookup, settings retrieval, and settings updates.
/// For complex operations (list with metrics, create, update, duplicate), see ExercisesController.
/// </summary>
public interface IExerciseCrudService
{
    /// <summary>
    /// Gets a single exercise by ID.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Exercise DTO, or null if not found</returns>
    Task<ExerciseDto?> GetExerciseAsync(Guid exerciseId, CancellationToken ct = default);

    /// <summary>
    /// Checks whether an exercise exists by ID.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if the exercise exists, false otherwise</returns>
    Task<bool> ExerciseExistsAsync(Guid exerciseId, CancellationToken ct = default);

    /// <summary>
    /// Gets the settings for an exercise.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Exercise settings DTO, or null if exercise not found</returns>
    Task<ExerciseSettingsDto?> GetExerciseSettingsAsync(Guid exerciseId, CancellationToken ct = default);

    /// <summary>
    /// Updates settings for an exercise.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="request">Settings update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated exercise settings DTO, or null if exercise not found</returns>
    /// <exception cref="ArgumentException">Clock multiplier out of range or max duration invalid</exception>
    /// <exception cref="InvalidOperationException">Clock multiplier changed while clock is running</exception>
    Task<ExerciseSettingsDto?> UpdateExerciseSettingsAsync(
        Guid exerciseId,
        UpdateExerciseSettingsRequest request,
        CancellationToken ct = default);
}
