using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service interface for exercise status transitions.
/// Handles the exercise lifecycle: Draft → Active → Paused → Completed → Archived.
/// </summary>
public interface IExerciseStatusService
{
    /// <summary>
    /// Activates an exercise (Draft → Active).
    /// Requires at least one inject in the MSEL.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="userId">The user performing the action</param>
    /// <returns>Status transition result</returns>
    Task<StatusTransitionResult> ActivateAsync(Guid exerciseId, string userId);

    /// <summary>
    /// Pauses an exercise (Active → Paused).
    /// Preserves clock elapsed time.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="userId">The user performing the action</param>
    /// <returns>Status transition result</returns>
    Task<StatusTransitionResult> PauseAsync(Guid exerciseId, string userId);

    /// <summary>
    /// Resumes a paused exercise (Paused → Active).
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="userId">The user performing the action</param>
    /// <returns>Status transition result</returns>
    Task<StatusTransitionResult> ResumeAsync(Guid exerciseId, string userId);

    /// <summary>
    /// Completes an exercise (Active/Paused → Completed).
    /// Permanently stops the clock.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="userId">The user performing the action</param>
    /// <returns>Status transition result</returns>
    Task<StatusTransitionResult> CompleteAsync(Guid exerciseId, string userId);

    /// <summary>
    /// Archives a completed exercise (Completed → Archived).
    /// Makes the exercise fully read-only.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="userId">The user performing the action</param>
    /// <returns>Status transition result</returns>
    Task<StatusTransitionResult> ArchiveAsync(Guid exerciseId, string userId);

    /// <summary>
    /// Unarchives an exercise (Archived → Completed).
    /// Restores the exercise to completed status.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="userId">The user performing the action</param>
    /// <returns>Status transition result</returns>
    Task<StatusTransitionResult> UnarchiveAsync(Guid exerciseId, string userId);

    /// <summary>
    /// Reverts a paused exercise to draft (Paused → Draft).
    /// WARNING: This clears all conduct data (fired times, observations).
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="userId">The user performing the action</param>
    /// <returns>Status transition result</returns>
    Task<StatusTransitionResult> RevertToDraftAsync(Guid exerciseId, string userId);

    /// <summary>
    /// Checks if a status transition is valid.
    /// </summary>
    /// <param name="from">Current status</param>
    /// <param name="to">Target status</param>
    /// <returns>True if the transition is allowed</returns>
    bool CanTransition(ExerciseStatus from, ExerciseStatus to);

    /// <summary>
    /// Gets the available status transitions for the current status.
    /// </summary>
    /// <param name="currentStatus">Current status</param>
    /// <returns>List of available target statuses</returns>
    IReadOnlyList<ExerciseStatus> GetAvailableTransitions(ExerciseStatus currentStatus);

    /// <summary>
    /// Validates whether an exercise can be published.
    /// When approval is enabled, checks for unapproved injects (Draft or Submitted).
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <returns>Validation result with counts and error/warning messages</returns>
    Task<PublishValidationResult> ValidatePublishAsync(Guid exerciseId);
}

/// <summary>
/// Result of a status transition operation.
/// </summary>
public record StatusTransitionResult
{
    /// <summary>
    /// Whether the transition was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if the transition failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The exercise's new status after transition.
    /// </summary>
    public ExerciseStatus NewStatus { get; init; }

    /// <summary>
    /// The updated exercise DTO.
    /// </summary>
    public ExerciseDto? Exercise { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static StatusTransitionResult Succeeded(ExerciseDto exercise) => new()
    {
        Success = true,
        NewStatus = exercise.Status,
        Exercise = exercise
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static StatusTransitionResult Failed(string errorMessage, ExerciseStatus currentStatus) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        NewStatus = currentStatus
    };
}
