using System.Security.Claims;
using Cadence.Core.Features.Authentication.Services;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Features.Organizations.Models.DTOs;
using Cadence.Core.Features.Organizations.Services;
using Cadence.Core.Features.Users.Models.DTOs;
using Cadence.Core.Features.Users.Services;
using Cadence.Core.Models.Entities;
using Cadence.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for user management.
/// Administrative functions for managing user accounts, roles, and status.
/// </summary>
[ApiController]
[Route("api/users")]
[AuthorizeAdmin]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IExerciseParticipantService _exerciseParticipantService;
    private readonly IMembershipService _membershipService;
    private readonly IOrganizationService _organizationService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        IExerciseParticipantService exerciseParticipantService,
        IMembershipService membershipService,
        IOrganizationService organizationService,
        ITokenService tokenService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _exerciseParticipantService = exerciseParticipantService;
        _membershipService = membershipService;
        _organizationService = organizationService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of users with optional filtering.
    /// Managers can access this to select Exercise Directors.
    /// </summary>
    /// <param name="page">Page number (1-indexed, default 1).</param>
    /// <param name="pageSize">Items per page (default 20, max 100).</param>
    /// <param name="search">Optional search term (filters by name or email).</param>
    /// <param name="role">Optional role filter.</param>
    [HttpGet]
    [AuthorizeManager] // Override class-level AuthorizeAdmin to allow Managers to list users
    [ProducesResponseType(typeof(UserListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null)
    {
        var result = await _userService.GetUsersAsync(page, pageSize, search, role);
        return Ok(result);
    }

    /// <summary>
    /// Get a single user by ID.
    /// </summary>
    /// <param name="id">User ID.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = $"User {id} not found" });
        }
        return Ok(user);
    }

    /// <summary>
    /// Create a new user account.
    /// Used for inline user creation from exercise participants dialog.
    /// Admins and Managers (Exercise Directors) can create users.
    /// Non-admin creators can only create users with User (Observer) role.
    /// </summary>
    /// <param name="request">User creation request.</param>
    [HttpPost]
    [AuthorizeManager] // Override class-level AuthorizeAdmin
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var currentUserId = GetCurrentUserId();
        var isAdmin = User.Claims.Any(c =>
            c.Type == "role" && c.Value == SystemRole.Admin.ToString());

        try
        {
            var result = await _userService.CreateUserAsync(request, currentUserId.ToString(), isAdmin);

            _logger.LogInformation(
                "User {NewUserId} created by {CreatorId} (Admin: {IsAdmin})",
                result.Id, currentUserId, isAdmin);

            return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return Conflict(new { error = "duplicate_email", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "validation_error", message = ex.Message });
        }
    }

    /// <summary>
    /// Update user details (display name, email).
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">Update request.</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var result = await _userService.UpdateUserAsync(id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Change a user's global role.
    /// Enforces last-administrator protection.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">Role change request.</param>
    [HttpPatch("{id:guid}/role")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest request)
    {
        var currentUserId = GetCurrentUserId();

        try
        {
            var result = await _userService.ChangeRoleAsync(id, request.SystemRole, currentUserId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "invalid_role", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "last_administrator", message = ex.Message });
        }
    }

    /// <summary>
    /// Deactivate a user account.
    /// Deactivated users cannot login.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">Optional deactivation reason.</param>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(Guid id, [FromBody] DeactivateUserRequest? request = null)
    {
        var currentUserId = GetCurrentUserId();

        try
        {
            var result = await _userService.DeactivateUserAsync(id, request?.Reason, currentUserId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reactivate a deactivated user account.
    /// </summary>
    /// <param name="id">User ID.</param>
    [HttpPost("{id:guid}/reactivate")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateUser(Guid id)
    {
        var currentUserId = GetCurrentUserId();

        try
        {
            var result = await _userService.ReactivateUserAsync(id, currentUserId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get a user's exercise assignments (exercises where they have a role).
    /// Users can get their own assignments, Admins can get any user's assignments.
    /// </summary>
    /// <param name="userId">User ID (GUID as string path parameter).</param>
    [HttpGet("{userId:guid}/exercise-assignments")]
    [Authorize] // Override controller-level RequireAdmin policy
    [ProducesResponseType(typeof(IEnumerable<ExerciseAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserExerciseAssignments(Guid userId)
    {
        // Check if user is authenticated
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Unauthorized();
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized();
        }

        var currentUserId = Guid.Parse(userIdClaim);

        // Check authorization: User can get their own assignments, Admin can get any user's
        var isAdmin = User.Claims.Any(c =>
            c.Type == "role" && c.Value == SystemRole.Admin.ToString());

        if (currentUserId != userId && !isAdmin)
        {
            _logger.LogWarning(
                "User {CurrentUserId} attempted to access exercise assignments for user {RequestedUserId} without authorization",
                currentUserId, userId);
            return Forbid();
        }

        // Get the user's exercise assignments
        var assignments = await _exerciseParticipantService.GetUserExerciseAssignmentsAsync(userId.ToString());

        _logger.LogInformation(
            "Retrieved {Count} exercise assignments for user {UserId}",
            assignments.Count(), userId);

        return Ok(assignments);
    }

    /// <summary>
    /// Get the current user's organization memberships.
    /// Any authenticated user can access their own memberships.
    /// </summary>
    [HttpGet("me/organizations")]
    [Authorize] // Override class-level AuthorizeAdmin - any authenticated user
    [ProducesResponseType(typeof(UserOrganizationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrganizations()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var memberships = await _membershipService.GetUserMembershipsAsync(userId);

        // Get current org from JWT claim first, fallback to database
        var currentOrgIdClaim = User.FindFirst("org_id")?.Value;
        Guid? currentOrgId = !string.IsNullOrEmpty(currentOrgIdClaim) && Guid.TryParse(currentOrgIdClaim, out var id)
            ? id
            : null;

        // If no current org in JWT, check the database (user may have just switched)
        if (!currentOrgId.HasValue)
        {
            var userCurrentOrgId = await _userService.GetCurrentOrganizationIdAsync(userId);
            currentOrgId = userCurrentOrgId;
        }

        // Set IsCurrent flag on the membership matching the current org
        var membershipList = memberships.Select(m => m with
        {
            IsCurrent = currentOrgId.HasValue && m.OrganizationId == currentOrgId.Value
        }).ToList();

        _logger.LogInformation(
            "User {UserId} retrieved {Count} organization memberships (current org: {CurrentOrgId})",
            userId, membershipList.Count, currentOrgId);

        return Ok(new UserOrganizationsResponse(currentOrgId, membershipList));
    }

    /// <summary>
    /// Switch the current user's organization context.
    /// Returns a new JWT with updated org_id and org_role claims.
    /// SysAdmins can switch to any organization (they get OrgAdmin access).
    /// </summary>
    [HttpPost("current-organization")]
    [Authorize] // Override class-level AuthorizeAdmin - any authenticated user
    [ProducesResponseType(typeof(SwitchOrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SwitchOrganization([FromBody] SwitchOrganizationRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Check if user is a SysAdmin
        var systemRoleClaim = User.FindFirst("SystemRole")?.Value;
        var isSysAdmin = systemRoleClaim == SystemRole.Admin.ToString();

        // Verify user has membership in target organization (or is SysAdmin)
        var role = await _membershipService.GetUserRoleInOrganizationAsync(userId, request.OrganizationId);

        if (role == null && !isSysAdmin)
        {
            _logger.LogWarning(
                "User {UserId} attempted to switch to organization {OrgId} without membership",
                userId, request.OrganizationId);
            return Forbid();
        }

        // SysAdmins without membership get OrgAdmin access
        var effectiveRole = role ?? OrgRole.OrgAdmin;

        // Get organization details for response
        var org = await _organizationService.GetByIdAsync(request.OrganizationId);
        if (org == null)
        {
            return NotFound(new { message = "Organization not found" });
        }

        // Update user's current organization
        await _userService.UpdateCurrentOrganizationAsync(userId, request.OrganizationId);

        // Generate new token with updated org claims
        var userInfo = await _userService.GetUserInfoAsync(userId);
        var (token, _) = _tokenService.GenerateAccessToken(userInfo, request.OrganizationId, org.Name, org.Slug, effectiveRole.ToString());

        _logger.LogInformation(
            "User {UserId} switched to organization {OrgId} ({OrgName}) with role {Role} (SysAdmin: {IsSysAdmin})",
            userId, request.OrganizationId, org.Name, effectiveRole, isSysAdmin);

        return Ok(new SwitchOrganizationResponse(
            request.OrganizationId,
            org.Name,
            effectiveRole.ToString(),
            token
        ));
    }

    /// <summary>
    /// Get a specific user's organization memberships.
    /// Admin-only endpoint for user management page.
    /// </summary>
    /// <param name="userId">User ID to get memberships for.</param>
    [HttpGet("{userId:guid}/memberships")]
    [ProducesResponseType(typeof(IEnumerable<MembershipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserMemberships(Guid userId)
    {
        // Verify user exists
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = $"User {userId} not found" });
        }

        var memberships = await _membershipService.GetUserMembershipsAsync(userId.ToString());

        _logger.LogInformation(
            "Admin retrieved {Count} memberships for user {UserId}",
            memberships.Count(), userId);

        return Ok(memberships);
    }

    /// <summary>
    /// Get current authenticated user's ID from JWT claims.
    /// </summary>
    private string GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }
        return userIdClaim;
    }
}
