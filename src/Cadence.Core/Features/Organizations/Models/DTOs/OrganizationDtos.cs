namespace Cadence.Core.Features.Organizations.Models.DTOs;

/// <summary>
/// Organization list item DTO for table/grid display.
/// Includes aggregate counts for quick reference.
/// </summary>
public record OrganizationListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    int UserCount,
    int ExerciseCount,
    DateTime CreatedAt
);

/// <summary>
/// Complete organization details DTO.
/// Used for detail view and edit forms.
/// </summary>
public record OrganizationDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ContactEmail,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Request DTO for creating a new organization.
/// Includes first administrator designation.
/// </summary>
public record CreateOrganizationRequest(
    string Name,
    string Slug,
    string? Description,
    string? ContactEmail,
    string FirstAdminEmail
);

/// <summary>
/// Request DTO for updating an existing organization.
/// Slug is immutable after creation.
/// </summary>
public record UpdateOrganizationRequest(
    string Name,
    string? Description,
    string? ContactEmail
);

/// <summary>
/// Response DTO for slug availability check.
/// Includes suggestion if slug is taken.
/// </summary>
public record SlugCheckResponse(
    bool Available,
    string? Suggestion
);
