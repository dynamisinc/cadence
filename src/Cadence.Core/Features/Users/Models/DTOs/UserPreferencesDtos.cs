using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Users.Models.DTOs;

/// <summary>
/// DTO for user display and behavior preferences.
/// Used for both GET and response after PUT operations.
/// </summary>
public record UserPreferencesDto
{
    /// <summary>
    /// Theme preference for UI appearance.
    /// Values: "Light", "Dark", "System"
    /// Default: "System"
    /// </summary>
    public string Theme { get; init; } = "System";

    /// <summary>
    /// Display density for UI spacing.
    /// Values: "Comfortable", "Compact"
    /// Default: "Comfortable"
    /// </summary>
    public string DisplayDensity { get; init; } = "Comfortable";

    /// <summary>
    /// Time format for displaying times throughout the application.
    /// Values: "TwentyFourHour", "TwelveHour"
    /// Default: "TwentyFourHour" (military time - EM standard)
    /// </summary>
    public string TimeFormat { get; init; } = "TwentyFourHour";

    /// <summary>
    /// When preferences were last updated.
    /// </summary>
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Request to update user preferences.
/// All fields are optional - only provided fields will be updated.
/// </summary>
public record UpdateUserPreferencesRequest
{
    /// <summary>
    /// New theme preference. If null, theme is not changed.
    /// Values: "Light", "Dark", "System"
    /// </summary>
    public string? Theme { get; init; }

    /// <summary>
    /// New display density. If null, density is not changed.
    /// Values: "Comfortable", "Compact"
    /// </summary>
    public string? DisplayDensity { get; init; }

    /// <summary>
    /// New time format. If null, format is not changed.
    /// Values: "TwentyFourHour", "TwelveHour"
    /// </summary>
    public string? TimeFormat { get; init; }
}

/// <summary>
/// Extension methods for mapping between UserPreferences entity and DTOs.
/// </summary>
public static class UserPreferencesMapper
{
    /// <summary>
    /// Map UserPreferences entity to UserPreferencesDto.
    /// </summary>
    public static UserPreferencesDto ToDto(this UserPreferences preferences) => new()
    {
        Theme = preferences.Theme.ToString(),
        DisplayDensity = preferences.DisplayDensity.ToString(),
        TimeFormat = preferences.TimeFormat.ToString(),
        UpdatedAt = preferences.UpdatedAt
    };

    /// <summary>
    /// Create default UserPreferences for a user.
    /// </summary>
    public static UserPreferences CreateDefault(string userId) => new()
    {
        UserId = userId,
        Theme = ThemePreference.System,
        DisplayDensity = Cadence.Core.Models.Entities.DisplayDensity.Comfortable,
        TimeFormat = Cadence.Core.Models.Entities.TimeFormat.TwentyFourHour,
        UpdatedAt = DateTime.UtcNow
    };

    /// <summary>
    /// Apply update request to existing preferences.
    /// Only updates fields that are provided (non-null).
    /// </summary>
    public static void ApplyUpdate(this UserPreferences preferences, UpdateUserPreferencesRequest request)
    {
        if (request.Theme != null && Enum.TryParse<ThemePreference>(request.Theme, true, out var theme))
        {
            preferences.Theme = theme;
        }

        if (request.DisplayDensity != null && Enum.TryParse<Cadence.Core.Models.Entities.DisplayDensity>(request.DisplayDensity, true, out var density))
        {
            preferences.DisplayDensity = density;
        }

        if (request.TimeFormat != null && Enum.TryParse<Cadence.Core.Models.Entities.TimeFormat>(request.TimeFormat, true, out var timeFormat))
        {
            preferences.TimeFormat = timeFormat;
        }

        preferences.UpdatedAt = DateTime.UtcNow;
    }
}
