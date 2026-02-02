using Cadence.Core.Features.Authentication.Models.DTOs;
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
    /// Create a new user account.
    /// Used for inline user creation from exercise participants dialog.
    /// Per story S25, all inline-created users receive User (Observer) role.
    /// </summary>
    /// <param name="request">User creation request with display name, email, and password.</param>
    /// <param name="createdById">ID of the user creating this account.</param>
    /// <param name="isCreatorAdmin">Reserved for future use. Currently all users are created with User role per S25 scope.</param>
    /// <returns>Created user DTO.</returns>
    /// <exception cref="InvalidOperationException">Email already exists (409 Conflict).</exception>
    /// <exception cref="ArgumentException">Invalid request data or password requirements not met.</exception>
    Task<UserDto> CreateUserAsync(CreateUserRequest request, string createdById, bool isCreatorAdmin);

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
    Task<UserDto> ChangeRoleAsync(Guid id, string newRole, string changedById);

    /// <summary>
    /// Deactivate a user account.
    /// Deactivated users cannot login. All refresh tokens are revoked.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="reason">Optional reason for deactivation.</param>
    /// <param name="deactivatedById">Administrator performing deactivation.</param>
    /// <returns>Updated user DTO.</returns>
    /// <exception cref="KeyNotFoundException">User not found.</exception>
    Task<UserDto> DeactivateUserAsync(Guid id, string? reason, string deactivatedById);

    /// <summary>
    /// Reactivate a deactivated user account.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="reactivatedById">Administrator performing reactivation.</param>
    /// <returns>Updated user DTO.</returns>
    /// <exception cref="KeyNotFoundException">User not found.</exception>
    Task<UserDto> ReactivateUserAsync(Guid id, string reactivatedById);

    /// <summary>
    /// Update a user's current organization context.
    /// </summary>
    /// <param name="userId">User ID (string for Identity compatibility).</param>
    /// <param name="organizationId">New current organization ID.</param>
    /// <exception cref="KeyNotFoundException">User not found.</exception>
    Task UpdateCurrentOrganizationAsync(string userId, Guid organizationId);

    /// <summary>
    /// Get user information for token generation.
    /// </summary>
    /// <param name="userId">User ID (string for Identity compatibility).</param>
    /// <returns>User information for JWT claims.</returns>
    /// <exception cref="KeyNotFoundException">User not found.</exception>
    Task<UserInfo> GetUserInfoAsync(string userId);

    /// <summary>
    /// Get user's current organization ID from the database.
    /// </summary>
    /// <param name="userId">User ID (string for Identity compatibility).</param>
    /// <returns>Current organization ID, or null if not set.</returns>
    Task<Guid?> GetCurrentOrganizationIdAsync(string userId);
}
