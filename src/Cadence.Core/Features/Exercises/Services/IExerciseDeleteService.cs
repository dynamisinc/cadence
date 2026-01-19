using Cadence.Core.Features.Exercises.Models.DTOs;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service interface for permanently deleting exercises.
/// Handles delete eligibility checks and cascade deletion of related data.
/// </summary>
public interface IExerciseDeleteService
{
    /// <summary>
    /// Gets a summary of what would be deleted if the exercise is permanently deleted.
    /// Also determines whether the exercise can be deleted based on its status and the user's permissions.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="userId">The user requesting the deletion</param>
    /// <param name="isAdmin">Whether the user is an administrator</param>
    /// <returns>Delete summary response with eligibility and data counts</returns>
    Task<DeleteSummaryResponse?> GetDeleteSummaryAsync(Guid exerciseId, Guid userId, bool isAdmin);

    /// <summary>
    /// Permanently deletes an exercise and all related data.
    /// This action is irreversible.
    ///
    /// Delete eligibility rules:
    /// - Draft exercises that have never been published: Creator OR Administrator
    /// - Archived exercises: Administrator only
    /// - Published/Active/Completed exercises (not archived): Cannot delete - must archive first
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="userId">The user performing the deletion</param>
    /// <param name="isAdmin">Whether the user is an administrator</param>
    /// <returns>Delete result with success/failure and reason</returns>
    Task<DeleteExerciseResult> DeleteExerciseAsync(Guid exerciseId, Guid userId, bool isAdmin);
}
