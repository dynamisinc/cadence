namespace Cadence.Core.Features.Email.Models.DTOs;

/// <summary>
/// A single email preference category with its current state.
/// </summary>
public record EmailPreferenceDto(
    string Category,
    string DisplayName,
    string Description,
    bool IsEnabled,
    bool IsMandatory
);

/// <summary>
/// Response containing all email preferences for the current user.
/// </summary>
public record EmailPreferencesResponse(
    IReadOnlyList<EmailPreferenceDto> Preferences
);

/// <summary>
/// Request to update a single email preference category.
/// </summary>
public record UpdateEmailPreferenceRequest(
    string Category,
    bool IsEnabled
);
