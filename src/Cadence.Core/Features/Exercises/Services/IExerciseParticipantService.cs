using Cadence.Core.Features.Exercises.Models.DTOs;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service for managing exercise participants and their exercise-specific roles.
/// </summary>
public interface IExerciseParticipantService
{
    /// <summary>
    /// Get all participants for an exercise.
    /// </summary>
    /// <param name="exerciseId">Exercise ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of participants with role information</returns>
    Task<List<ExerciseParticipantDto>> GetParticipantsAsync(
        Guid exerciseId,
        CancellationToken ct = default);

    /// <summary>
    /// Get a specific participant for an exercise.
    /// </summary>
    /// <param name="exerciseId">Exercise ID</param>
    /// <param name="userId">User ID (ApplicationUser.Id string)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Participant information or null if not found</returns>
    Task<ExerciseParticipantDto?> GetParticipantAsync(
        Guid exerciseId,
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Get the effective role for a user in an exercise.
    /// Returns the exercise-specific role if set, otherwise the user's system role.
    /// </summary>
    /// <param name="exerciseId">Exercise ID</param>
    /// <param name="userId">User ID (ApplicationUser.Id string)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Effective role name</returns>
    Task<string> GetEffectiveRoleAsync(
        Guid exerciseId,
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Add a participant to an exercise with a specific HSEEP role.
    /// If the user is already a participant (even if soft-deleted), reactivates them.
    /// </summary>
    /// <param name="exerciseId">Exercise ID</param>
    /// <param name="request">Add participant request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created or reactivated participant</returns>
    /// <exception cref="KeyNotFoundException">User not found</exception>
    /// <exception cref="InvalidOperationException">User is already an active participant</exception>
    Task<ExerciseParticipantDto> AddParticipantAsync(
        Guid exerciseId,
        AddParticipantRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Update a participant's exercise-specific HSEEP role.
    /// </summary>
    /// <param name="exerciseId">Exercise ID</param>
    /// <param name="userId">User ID (ApplicationUser.Id string)</param>
    /// <param name="request">Update role request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated participant</returns>
    /// <exception cref="KeyNotFoundException">Participant not found</exception>
    Task<ExerciseParticipantDto> UpdateParticipantRoleAsync(
        Guid exerciseId,
        string userId,
        UpdateParticipantRoleRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Remove a participant from an exercise (soft delete).
    /// </summary>
    /// <param name="exerciseId">Exercise ID</param>
    /// <param name="userId">User ID (ApplicationUser.Id string)</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="KeyNotFoundException">Participant not found</exception>
    Task RemoveParticipantAsync(
        Guid exerciseId,
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Bulk update participants for an exercise.
    /// Adds or updates participants based on the request.
    /// </summary>
    /// <param name="exerciseId">Exercise ID</param>
    /// <param name="request">Bulk update request</param>
    /// <param name="ct">Cancellation token</param>
    Task BulkUpdateParticipantsAsync(
        Guid exerciseId,
        BulkUpdateParticipantsRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get all exercise assignments for a user.
    /// Returns all active exercises where the user has a role assignment.
    /// Excludes deleted exercises and deleted participant records.
    /// </summary>
    /// <param name="userId">User ID (ApplicationUser.Id string)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of exercise assignments ordered by most recently assigned first</returns>
    Task<IEnumerable<ExerciseAssignmentDto>> GetUserExerciseAssignmentsAsync(
        string userId,
        CancellationToken ct = default);
}
