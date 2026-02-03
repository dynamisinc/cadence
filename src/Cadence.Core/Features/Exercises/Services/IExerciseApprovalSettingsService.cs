using Cadence.Core.Features.Exercises.Models.DTOs;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service for managing exercise-level inject approval settings.
/// Part of S02: Exercise Approval Configuration.
/// </summary>
public interface IExerciseApprovalSettingsService
{
    /// <summary>
    /// Gets the approval settings for an exercise, including organization policy context.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Approval settings DTO</returns>
    /// <exception cref="KeyNotFoundException">Exercise not found</exception>
    Task<ApprovalSettingsDto> GetApprovalSettingsAsync(
        Guid exerciseId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates exercise approval settings, respecting organization policy constraints.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="request">Update request with new settings</param>
    /// <param name="userId">ID of user making the change (for override tracking)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated approval settings</returns>
    /// <exception cref="KeyNotFoundException">Exercise not found</exception>
    /// <exception cref="InvalidOperationException">Update violates organization policy</exception>
    Task<ApprovalSettingsDto> UpdateApprovalSettingsAsync(
        Guid exerciseId,
        UpdateApprovalSettingsRequest request,
        string userId,
        CancellationToken ct = default);
}
