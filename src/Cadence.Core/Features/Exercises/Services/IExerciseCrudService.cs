using Cadence.Core.Features.Exercises.Models.DTOs;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service interface for core exercise CRUD and settings operations.
/// </summary>
public interface IExerciseCrudService
{
    /// <summary>
    /// Gets a filtered list of exercises with inject counts and clock state.
    /// Filters by organization context (SysAdmins see all, others see their org or membership orgs).
    /// </summary>
    Task<List<ExerciseDto>> GetExercisesAsync(
        string? userId,
        bool includeArchived = false,
        bool archivedOnly = false,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a single exercise by ID.
    /// </summary>
    Task<ExerciseDto?> GetExerciseAsync(Guid exerciseId, CancellationToken ct = default);

    /// <summary>
    /// Checks whether an exercise exists by ID.
    /// </summary>
    Task<bool> ExerciseExistsAsync(Guid exerciseId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new exercise with optional director assignment.
    /// </summary>
    /// <exception cref="FluentValidation.ValidationException">Request fails validation.</exception>
    /// <exception cref="InvalidOperationException">No organization context.</exception>
    Task<ExerciseDto> CreateExerciseAsync(
        CreateExerciseRequest request,
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing exercise with optional director reassignment.
    /// </summary>
    /// <returns>Updated exercise DTO, or null if not found.</returns>
    /// <exception cref="ArgumentException">Validation fails.</exception>
    /// <exception cref="InvalidOperationException">Exercise status blocks edit.</exception>
    Task<ExerciseDto?> UpdateExerciseAsync(
        Guid exerciseId,
        UpdateExerciseRequest request,
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Duplicates an exercise with all its configuration (phases, objectives, MSEL, injects).
    /// The new exercise starts in Draft status.
    /// </summary>
    /// <returns>New exercise DTO, or null if source not found.</returns>
    Task<ExerciseDto?> DuplicateExerciseAsync(
        Guid sourceExerciseId,
        DuplicateExerciseRequest? request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the settings for an exercise.
    /// </summary>
    Task<ExerciseSettingsDto?> GetExerciseSettingsAsync(Guid exerciseId, CancellationToken ct = default);

    /// <summary>
    /// Updates settings for an exercise.
    /// </summary>
    /// <exception cref="ArgumentException">Clock multiplier out of range or max duration invalid</exception>
    /// <exception cref="InvalidOperationException">Clock multiplier changed while clock is running</exception>
    Task<ExerciseSettingsDto?> UpdateExerciseSettingsAsync(
        Guid exerciseId,
        UpdateExerciseSettingsRequest request,
        CancellationToken ct = default);
}
