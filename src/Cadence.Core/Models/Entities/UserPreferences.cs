using Cadence.Core.Data;

namespace Cadence.Core.Models.Entities;

/// <summary>
/// User preferences for display and behavior settings.
/// One-to-one relationship with ApplicationUser.
/// These settings follow the user across all exercises.
/// </summary>
public class UserPreferences : IHasTimestamps
{
    /// <summary>
    /// Primary key - matches the associated ApplicationUser.Id.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    // =========================================================================
    // Display Settings (S01)
    // =========================================================================

    /// <summary>
    /// Theme preference for UI appearance.
    /// Default: System (follow OS preference).
    /// </summary>
    public ThemePreference Theme { get; set; } = ThemePreference.System;

    /// <summary>
    /// Display density for UI spacing.
    /// Default: Comfortable (standard spacing).
    /// </summary>
    public DisplayDensity DisplayDensity { get; set; } = DisplayDensity.Comfortable;

    // =========================================================================
    // Time Settings (S02)
    // =========================================================================

    /// <summary>
    /// Time format for displaying times throughout the application.
    /// Default: TwentyFourHour (military time - EM standard).
    /// </summary>
    public TimeFormat TimeFormat { get; set; } = TimeFormat.TwentyFourHour;

    // =========================================================================
    // IHasTimestamps
    // =========================================================================

    /// <summary>
    /// UTC timestamp when preferences were created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp when preferences were last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The user these preferences belong to.
    /// </summary>
    public ApplicationUser User { get; set; } = null!;
}
