using Cadence.Core.Constants;
using Cadence.Core.Data;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Authentication.Services;
using Cadence.Core.Features.Users.Models.DTOs;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Users.Services;

/// <summary>
/// Service for user management operations.
/// Implements administrative user management: viewing, editing, deactivating, and role assignment.
/// Organization-scoped: OrgAdmins can only see/manage users within their organization.
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly ILogger<UserService> _logger;
    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationContext _orgContext;

    private static readonly string[] ValidSystemRoles = new[]
    {
        nameof(SystemRole.Admin),
        nameof(SystemRole.Manager),
        nameof(SystemRole.User)
    };

    public UserService(
        UserManager<ApplicationUser> userManager,
        IRefreshTokenStore refreshTokenStore,
        ILogger<UserService> logger,
        AppDbContext context,
        ICurrentOrganizationContext orgContext)
    {
        _userManager = userManager;
        _refreshTokenStore = refreshTokenStore;
        _logger = logger;
        _context = context;
        _orgContext = orgContext;
    }

    /// <inheritdoc />
    public async Task<UserListResponse> GetUsersAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? role = null,
        string? status = null,
        Guid? organizationId = null)
    {
        // Enforce pagination limits
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Start with all users
        var query = _userManager.Users.AsQueryable();

        // Organization filtering: Non-SysAdmins can only see users in their organization
        if (!_orgContext.IsSysAdmin && _orgContext.CurrentOrganizationId.HasValue)
        {
            var currentOrgId = _orgContext.CurrentOrganizationId.Value;

            // Get user IDs who are members of the current organization
            var orgMemberUserIds = _context.OrganizationMemberships
                .Where(m => m.OrganizationId == currentOrgId && m.Status == MembershipStatus.Active)
                .Select(m => m.UserId);

            query = query.Where(u => orgMemberUserIds.Contains(u.Id));
        }
        else if (!_orgContext.IsSysAdmin && !_orgContext.CurrentOrganizationId.HasValue)
        {
            // User without org context can only see themselves (edge case)
            // Return empty list for safety
            return new UserListResponse
            {
                Users = new List<UserDto>(),
                Pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    TotalPages = 0
                }
            };
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(u =>
                u.DisplayName.ToLower().Contains(searchLower) ||
                u.Email!.ToLower().Contains(searchLower));
        }

        // Apply role filter (supports comma-separated values like "Admin,Manager")
        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleNames = role.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var validRoles = roleNames
                .Where(r => Enum.TryParse<SystemRole>(r, out _))
                .Select(r => Enum.Parse<SystemRole>(r))
                .ToList();

            if (validRoles.Count > 0)
            {
                query = query.Where(u => validRoles.Contains(u.SystemRole));
            }
        }

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<UserStatus>(status, ignoreCase: true, out var userStatus))
            {
                query = query.Where(u => u.Status == userStatus);
            }
        }

        // Apply organization membership filter (SysAdmin only feature)
        if (organizationId.HasValue && _orgContext.IsSysAdmin)
        {
            var orgMemberUserIds = _context.OrganizationMemberships
                .Where(m => m.OrganizationId == organizationId.Value && m.Status == MembershipStatus.Active)
                .Select(m => m.UserId);

            query = query.Where(u => orgMemberUserIds.Contains(u.Id));
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
    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, string createdById, bool isCreatorAdmin)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new ArgumentException("Display name is required");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email is required");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Password is required");
        }

        // Check for duplicate email
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("A user with this email already exists");
        }

        // Determine system role: Admins can set any role, non-admins default to User
        var systemRole = SystemRole.User;
        if (isCreatorAdmin && request.SystemRole.HasValue)
        {
            systemRole = request.SystemRole.Value;
        }

        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            DisplayName = request.DisplayName.Trim(),
            SystemRole = systemRole,
            Status = UserStatus.Active,
            EmailConfirmed = true, // MVP: Skip email verification
            OrganizationId = SystemConstants.DefaultOrganizationId,
            CreatedAt = DateTime.UtcNow,
            CreatedById = createdById
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToList();

            // Check for duplicate email error from Identity
            if (errors.Any(e => e.Code == "DuplicateEmail" || e.Code == "DuplicateUserName"))
            {
                throw new InvalidOperationException("A user with this email already exists");
            }

            // Return password validation errors
            var errorMessages = string.Join("; ", errors.Select(e => e.Description));
            throw new ArgumentException($"Failed to create user: {errorMessages}");
        }

        _logger.LogInformation(
            "User {NewUserId} created by {CreatorId} via inline creation. Email: {Email}, Role: {Role}",
            user.Id, createdById, user.Email, user.SystemRole);

        return user.ToDto();
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
    public async Task<UserDto> ChangeRoleAsync(Guid id, string newRole, string changedById)
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

        // Re-check admin count after update to catch race conditions
        // If we just demoted someone and there are now 0 admins, revert the change
        if (oldRole == nameof(SystemRole.Admin) && systemRole != SystemRole.Admin)
        {
            var remainingAdmins = await _userManager.Users
                .Where(u => u.SystemRole == SystemRole.Admin)
                .CountAsync();

            if (remainingAdmins == 0)
            {
                // Revert the change
                user.SystemRole = SystemRole.Admin;
                await _userManager.UpdateAsync(user);
                throw new InvalidOperationException("Cannot remove the last Admin. Assign another Admin first.");
            }
        }
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user role: {errors}");
        }

        // Revoke all tokens to force re-authentication with new role
        await _refreshTokenStore.RevokeAllForUserAsync(id.ToString());

        _logger.LogInformation("Changed system role for user {UserId} from {OldRole} to {NewRole} by admin {AdminId}",
            id, oldRole, newRole, changedById);

        return user.ToDto();
    }

    /// <inheritdoc />
    public async Task<UserDto> DeactivateUserAsync(Guid id, string? reason, string deactivatedById)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            throw new KeyNotFoundException($"User {id} not found");
        }

        user.Status = UserStatus.Disabled;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to deactivate user: {errors}");
        }

        // Revoke all refresh tokens so user can't use existing sessions
        await _refreshTokenStore.RevokeAllForUserAsync(id.ToString());

        _logger.LogInformation("Deactivated user {UserId} by admin {AdminId}. Reason: {Reason}",
            id, deactivatedById, reason ?? "Not specified");

        return user.ToDto();
    }

    /// <inheritdoc />
    public async Task<UserDto> ReactivateUserAsync(Guid id, string reactivatedById)
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

    /// <inheritdoc />
    public async Task UpdateCurrentOrganizationAsync(string userId, Guid organizationId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User {userId} not found");
        }

        user.CurrentOrganizationId = organizationId;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update current organization: {errors}");
        }

        _logger.LogInformation("Updated current organization for user {UserId} to {OrgId}",
            userId, organizationId);
    }

    /// <inheritdoc />
    public async Task<UserInfo> GetUserInfoAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User {userId} not found");
        }

        return new UserInfo
        {
            Id = Guid.Parse(user.Id),
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName ?? user.Email ?? string.Empty,
            Role = user.SystemRole.ToString(),
            Status = user.Status.ToString(),
            LastLoginAt = user.LastLoginAt
        };
    }

    /// <inheritdoc />
    public async Task<Guid?> GetCurrentOrganizationIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.CurrentOrganizationId;
    }
}
