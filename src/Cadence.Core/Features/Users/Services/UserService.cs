using Cadence.Core.Features.Authentication.Services;
using Cadence.Core.Features.Users.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Users.Services;

/// <summary>
/// Service for user management operations.
/// Implements administrative user management: viewing, editing, deactivating, and role assignment.
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly ILogger<UserService> _logger;

    private static readonly string[] ValidSystemRoles = new[]
    {
        nameof(SystemRole.Admin),
        nameof(SystemRole.Manager),
        nameof(SystemRole.User)
    };

    public UserService(
        UserManager<ApplicationUser> userManager,
        IRefreshTokenStore refreshTokenStore,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _refreshTokenStore = refreshTokenStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserListResponse> GetUsersAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? role = null)
    {
        // Enforce pagination limits
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Start with all users
        var query = _userManager.Users.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(u =>
                u.DisplayName.ToLower().Contains(searchLower) ||
                u.Email!.ToLower().Contains(searchLower));
        }

        // Apply role filter
        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<SystemRole>(role, out var systemRole))
        {
            query = query.Where(u => u.SystemRole == systemRole);
        }

        // Get total count
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Get page of users
        var users = await query
            .OrderBy(u => u.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new UserListResponse
        {
            Users = users.Select(u => u.ToDto()).ToList(),
            Pagination = new PaginationInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            }
        };
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        return user?.ToDto();
    }

    /// <inheritdoc />
    public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            throw new KeyNotFoundException($"User {id} not found");
        }

        // Update display name if provided
        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            user.DisplayName = request.DisplayName;
        }

        // Update email if provided
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            user.Email = request.Email;
            user.NormalizedEmail = request.Email.ToUpperInvariant();
            user.UserName = request.Email;
            user.NormalizedUserName = request.Email.ToUpperInvariant();
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user: {errors}");
        }

        _logger.LogInformation("Updated user {UserId}: DisplayName={DisplayName}, Email={Email}",
            id, request.DisplayName, request.Email);

        return user.ToDto();
    }

    /// <inheritdoc />
    public async Task<UserDto> ChangeRoleAsync(Guid id, string newRole, Guid changedById)
    {
        // Validate and parse role
        if (!Enum.TryParse<SystemRole>(newRole, out var systemRole))
        {
            throw new ArgumentException($"Invalid system role: {newRole}. Must be one of: {string.Join(", ", ValidSystemRoles)}");
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            throw new KeyNotFoundException($"User {id} not found");
        }

        // Last admin protection - only check if demoting FROM Admin
        if (user.SystemRole == SystemRole.Admin && systemRole != SystemRole.Admin)
        {
            var adminCount = await _userManager.Users
                .Where(u => u.SystemRole == SystemRole.Admin && u.Id != id.ToString())
                .CountAsync();

            if (adminCount == 0)
            {
                throw new InvalidOperationException("Cannot remove the last Admin. Assign another Admin first.");
            }
        }

        var oldRole = user.SystemRole.ToString();
        user.SystemRole = systemRole;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user role: {errors}");
        }

        // Revoke all tokens to force re-authentication with new role
        await _refreshTokenStore.RevokeAllForUserAsync(id);

        _logger.LogInformation("Changed system role for user {UserId} from {OldRole} to {NewRole} by admin {AdminId}",
            id, oldRole, newRole, changedById);

        return user.ToDto();
    }

    /// <inheritdoc />
    public async Task<UserDto> DeactivateUserAsync(Guid id, string? reason, Guid deactivatedById)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            throw new KeyNotFoundException($"User {id} not found");
        }

        user.Status = UserStatus.Deactivated;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to deactivate user: {errors}");
        }

        // Revoke all refresh tokens so user can't use existing sessions
        await _refreshTokenStore.RevokeAllForUserAsync(id);

        _logger.LogInformation("Deactivated user {UserId} by admin {AdminId}. Reason: {Reason}",
            id, deactivatedById, reason ?? "Not specified");

        return user.ToDto();
    }

    /// <inheritdoc />
    public async Task<UserDto> ReactivateUserAsync(Guid id, Guid reactivatedById)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            throw new KeyNotFoundException($"User {id} not found");
        }

        user.Status = UserStatus.Active;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to reactivate user: {errors}");
        }

        _logger.LogInformation("Reactivated user {UserId} by admin {AdminId}",
            id, reactivatedById);

        return user.ToDto();
    }
}
