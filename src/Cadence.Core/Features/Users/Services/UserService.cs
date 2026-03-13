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

        var query = _userManager.Users.AsQueryable();

        // Organization filtering: non-SysAdmins are restricted to their org's members.
        // Returns null when the caller should short-circuit with an empty response.
        var filteredQuery = ApplyOrganizationFilter(query);
        if (filteredQuery == null)
            return BuildEmptyResponse(page, pageSize);

        query = filteredQuery;
        query = ApplySearchFilter(query, search);
        query = ApplyRoleFilter(query, role);
        query = ApplyStatusFilter(query, status);
        query = ApplyAdminOrganizationFilter(query, organizationId);

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

    /// <summary>
    /// Restricts the query to members of the current organization for non-SysAdmins.
    /// Returns <c>null</c> when the caller has no organization context and should receive an empty result.
    /// SysAdmins receive the unmodified query.
    /// </summary>
    private IQueryable<ApplicationUser>? ApplyOrganizationFilter(IQueryable<ApplicationUser> query)
    {
        if (_orgContext.IsSysAdmin)
            return query;

        if (_orgContext.CurrentOrganizationId.HasValue)
        {
            var currentOrgId = _orgContext.CurrentOrganizationId.Value;
            var orgMemberUserIds = _context.OrganizationMemberships
                .Where(m => m.OrganizationId == currentOrgId && m.Status == MembershipStatus.Active)
                .Select(m => m.UserId);

            return query.Where(u => orgMemberUserIds.Contains(u.Id));
        }

        // Non-SysAdmin without org context — signal caller to return empty list
        return null;
    }

    /// <summary>
    /// Filters users by display name or email when a search term is provided.
    /// </summary>
    private static IQueryable<ApplicationUser> ApplySearchFilter(
        IQueryable<ApplicationUser> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        var searchLower = search.ToLowerInvariant();
        return query.Where(u =>
            u.DisplayName.ToLowerInvariant().Contains(searchLower) ||
            u.Email!.ToLowerInvariant().Contains(searchLower));
    }

    /// <summary>
    /// Filters users by one or more comma-separated system role names.
    /// Unrecognized role names are silently ignored.
    /// </summary>
    private static IQueryable<ApplicationUser> ApplyRoleFilter(
        IQueryable<ApplicationUser> query, string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return query;

        var roleNames = role.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var validRoles = roleNames
            .Where(r => Enum.TryParse<SystemRole>(r, out _))
            .Select(r => Enum.Parse<SystemRole>(r))
            .ToList();

        return validRoles.Count > 0
            ? query.Where(u => validRoles.Contains(u.SystemRole))
            : query;
    }

    /// <summary>
    /// Filters users by account status when a valid <see cref="UserStatus"/> value is provided.
    /// </summary>
    private static IQueryable<ApplicationUser> ApplyStatusFilter(
        IQueryable<ApplicationUser> query, string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return query;

        return Enum.TryParse<UserStatus>(status, ignoreCase: true, out var userStatus)
            ? query.Where(u => u.Status == userStatus)
            : query;
    }

    /// <summary>
    /// Restricts results to members of a specific organization. Only applied for SysAdmins.
    /// </summary>
    private IQueryable<ApplicationUser> ApplyAdminOrganizationFilter(
        IQueryable<ApplicationUser> query, Guid? organizationId)
    {
        if (!organizationId.HasValue || !_orgContext.IsSysAdmin)
            return query;

        var orgMemberUserIds = _context.OrganizationMemberships
            .Where(m => m.OrganizationId == organizationId.Value && m.Status == MembershipStatus.Active)
            .Select(m => m.UserId);

        return query.Where(u => orgMemberUserIds.Contains(u.Id));
    }

    /// <summary>
    /// Builds a <see cref="UserListResponse"/> with zero results for the given pagination parameters.
    /// </summary>
    private static UserListResponse BuildEmptyResponse(int page, int pageSize) =>
        new()
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

    /// <inheritdoc />
    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        return user?.ToDto();
    }

    /// <inheritdoc />
    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, string createdById, bool isCreatorAdmin)
    {
        ValidateCreateUserRequest(request);

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
        HandleCreateUserResult(result);

        _logger.LogInformation(
            "User {NewUserId} created by {CreatorId} via inline creation. Email: {Email}, Role: {Role}",
            user.Id, createdById, user.Email, user.SystemRole);

        return user.ToDto();
    }

    /// <summary>
    /// Validates that DisplayName, Email, and Password are all non-empty.
    /// Throws <see cref="ArgumentException"/> if any field is missing.
    /// </summary>
    private static void ValidateCreateUserRequest(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            throw new ArgumentException("Display name is required");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password is required");
    }

    /// <summary>
    /// Interprets an <see cref="IdentityResult"/> from <c>CreateAsync</c>.
    /// Maps duplicate-email/username errors to <see cref="InvalidOperationException"/>
    /// and all other failures to <see cref="ArgumentException"/>.
    /// Does nothing when the result succeeded.
    /// </summary>
    private static void HandleCreateUserResult(IdentityResult result)
    {
        if (result.Succeeded) return;

        var errors = result.Errors.ToList();

        // Check for duplicate email error from Identity
        if (errors.Any(e => e.Code == "DuplicateEmail" || e.Code == "DuplicateUserName"))
            throw new InvalidOperationException("A user with this email already exists");

        // Return password validation errors
        var errorMessages = string.Join("; ", errors.Select(e => e.Description));
        throw new ArgumentException($"Failed to create user: {errorMessages}");
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

    /// <inheritdoc />
    public async Task<CurrentUserProfileDto?> GetCurrentUserProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.ToProfileDto();
    }

    /// <inheritdoc />
    public async Task<UserContactDto> UpdatePhoneNumberAsync(string userId, string? phoneNumber)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User {userId} not found");
        }

        // Validate phone number length
        if (phoneNumber != null && phoneNumber.Length > 25)
        {
            throw new ArgumentException("Phone number cannot exceed 25 characters");
        }

        // Trim and normalize empty string to null
        var normalizedPhone = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

        user.PhoneNumber = normalizedPhone;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update phone number: {errors}");
        }

        _logger.LogInformation("Updated phone number for user {UserId}", userId);

        return user.ToContactDto(DateTime.UtcNow);
    }
}
