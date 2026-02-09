using Cadence.Core.Features.Email.Models;

namespace Cadence.Core.Features.Email.Services;

/// <summary>
/// Service for managing user email preferences.
/// Checks whether a user has opted in/out of specific email categories.
/// </summary>
public interface IEmailPreferenceService
{
    /// <summary>
    /// Check if an email of the given category can be sent to the user.
    /// Returns true for mandatory categories regardless of preference.
    /// </summary>
    Task<bool> CanSendAsync(string userId, EmailCategory category, CancellationToken ct = default);

    /// <summary>
    /// Get all email preferences for a user.
    /// Returns defaults for categories without explicit preference.
    /// </summary>
    Task<IReadOnlyDictionary<EmailCategory, bool>> GetPreferencesAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Update a user's preference for a specific category.
    /// Throws if attempting to disable a mandatory category.
    /// </summary>
    Task UpdatePreferenceAsync(string userId, EmailCategory category, bool isEnabled, CancellationToken ct = default);

    /// <summary>
    /// Initialize default preferences for a new user.
    /// </summary>
    Task InitializeDefaultsAsync(string userId, CancellationToken ct = default);
}
