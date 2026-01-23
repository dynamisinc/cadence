using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Users.Models.DTOs;

/// <summary>
/// DTO for user in list view.
/// Includes basic user information for administrative display.
/// </summary>
public record UserDto
{
    /// <summary>
    /// User's unique identifier (ASP.NET Core Identity uses string IDs).
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// User's email address (also login identifier).
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// User's display name shown in UI.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// System-level role for application permissions (Admin, Manager, User).
    /// </summary>
    public string SystemRole { get; init; } = string.Empty;

    /// <summary>
    /// Account status (Active or Deactivated).
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Most recent successful login timestamp.
    /// </summary>
    public DateTime? LastLoginAt { get; init; }

    /// <summary>
    /// When the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Response for paginated user list.
/// </summary>
public record UserListResponse
{
    /// <summary>
    /// List of users for current page.
    /// </summary>
    public List<UserDto> Users { get; init; } = new();

    /// <summary>
    /// Pagination metadata.
    /// </summary>
    public PaginationInfo Pagination { get; init; } = new();
}

/// <summary>
/// Pagination metadata for user list.
/// </summary>
public record PaginationInfo
{
    /// <summary>
    /// Current page number (1-indexed).
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages { get; init; }
}

/// <summary>
/// Request to update user details.
/// </summary>
public record UpdateUserRequest
{
    /// <summary>
    /// New display name. If null, display name is not changed.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// New email address. If null, email is not changed.
    /// </summary>
    public string? Email { get; init; }
}

/// <summary>
/// Request to change a user's system-level role.
/// </summary>
public record ChangeRoleRequest
{
    /// <summary>
    /// New system role. Must be one of: Admin, Manager, User.
    /// </summary>
    public string SystemRole { get; init; } = string.Empty;
}

/// <summary>
/// Request to deactivate a user account.
/// </summary>
public record DeactivateUserRequest
{
    /// <summary>
    /// Optional reason for deactivation (for audit purposes).
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// Request to create a new user account.
/// Used for inline user creation from exercise participants dialog.
/// </summary>
public record CreateUserRequest
{
    /// <summary>
    /// User's display name shown in UI.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// User's email address (also login identifier).
    /// Must be unique in the system.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Initial password for the account.
    /// Must meet password policy requirements.
    /// </summary>
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Extension methods for mapping between ApplicationUser entity and DTOs.
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// Map ApplicationUser entity to UserDto.
    /// </summary>
    public static UserDto ToDto(this ApplicationUser user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        DisplayName = user.DisplayName,
        SystemRole = user.SystemRole.ToString(),
        Status = user.Status.ToString(),
        LastLoginAt = user.LastLoginAt,
        CreatedAt = user.CreatedAt
    };
}
