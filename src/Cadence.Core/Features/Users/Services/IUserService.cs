using Cadence.Core.Features.Users.Models.DTOs;

namespace Cadence.Core.Features.Users.Services;

/// <summary>
/// Service for user management operations.
/// Supports administrative user management: viewing, editing, deactivating, and role assignment.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Get paginated list of users with optional filtering.
    /// </summary>
    /// <param name="page">Page number (1-indexed).</param>
    /// <param name="pageSize">Number of users per page (default 20, max 100).</param>
    /// <param name="search">Optional search term (filters by name or email).</param>
    /// <param name="role">Optional role filter.</param>
    /// <returns>Paginated user list with metadata.</returns>
    Task<UserListResponse> GetUsersAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? role = null);

    /// <summary>
    /// Get a single user by ID.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <returns>User DTO if found, null otherwise.</returns>
    Task<UserDto?> GetUserByIdAsync(Guid id);

    /// <summary>
    /// Update user details (display name, email).
    /// </summary>
    /// <param name="id">User ID to update.</param>
    /// <param name="request">Update request with new values.</param>
    /// <returns>Updated user DTO.</returns>
    /// <exception cref="KeyNotFoundException">User not found.</exception>
    Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request);

    /// <summary>
    /// Change a user's global role.
    /// Enforces last-administrator protection.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="newRole">New global role.</param>
    /// <param name="changedById">Administrator making the change.</param>
    /// <returns>Updated user DTO.</returns>
    /// <exception cref="KeyNotFoundException">User not found.</exception>
    /// <exception cref="ArgumentException">Invalid role.</exception>
    /// <exception cref="InvalidOperationException">Cannot remove last administrator.</exception>
    Task<UserDto> ChangeRoleAsync(Guid id, string newRole, Guid changedById);

    /// <summary>
    /// Deactivate a user account.
    /// Deactivated users cannot login. All refresh tokens are revoked.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="reason">Optional reason for deactivation.</param>
    /// <param name="deactivatedById">Administrator performing deactivation.</param>
    /// <returns>Updated user DTO.</returns>
    /// <exception cref="KeyNotFoundException">User not found.</exception>
    Task<UserDto> DeactivateUserAsync(Guid id, string? reason, Guid deactivatedById);

    /// <summary>
    /// Reactivate a deactivated user account.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="reactivatedById">Administrator performing reactivation.</param>
    /// <returns>Updated user DTO.</returns>
    /// <exception cref="KeyNotFoundException">User not found.</exception>
    Task<UserDto> ReactivateUserAsync(Guid id, Guid reactivatedById);
}
