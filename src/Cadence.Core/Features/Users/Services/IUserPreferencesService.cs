using Cadence.Core.Features.Users.Models.DTOs;

namespace Cadence.Core.Features.Users.Services;

/// <summary>
/// Service for user preferences operations.
/// Users can read and update their own preferences.
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Get preferences for a user.
    /// Creates default preferences if none exist.
    /// </summary>
    /// <param name="userId">User ID (ApplicationUser.Id).</param>
    /// <returns>User preferences DTO.</returns>
    Task<UserPreferencesDto> GetPreferencesAsync(string userId);

    /// <summary>
    /// Update preferences for a user.
    /// Creates preferences if none exist, then applies updates.
    /// </summary>
    /// <param name="userId">User ID (ApplicationUser.Id).</param>
    /// <param name="request">Update request with new values.</param>
    /// <returns>Updated preferences DTO.</returns>
    Task<UserPreferencesDto> UpdatePreferencesAsync(string userId, UpdateUserPreferencesRequest request);

    /// <summary>
    /// Reset preferences to defaults for a user.
    /// </summary>
    /// <param name="userId">User ID (ApplicationUser.Id).</param>
    /// <returns>Reset preferences DTO with default values.</returns>
    Task<UserPreferencesDto> ResetPreferencesAsync(string userId);
}
