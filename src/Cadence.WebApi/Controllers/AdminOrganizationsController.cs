using System.Security.Claims;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Features.Organizations.Models.DTOs;
using Cadence.Core.Features.Organizations.Services;
using Cadence.Core.Models.Entities;
using Cadence.WebApi.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for system administrator organization management.
/// All endpoints require SysAdmin role.
/// </summary>
[ApiController]
[Route("api/admin/organizations")]
[AuthorizeAdmin]
public class AdminOrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly IMembershipService _membershipService;
    private readonly IApprovalPermissionService _approvalPermissionService;
    private readonly ILogger<AdminOrganizationsController> _logger;

    public AdminOrganizationsController(
        IOrganizationService organizationService,
        IMembershipService membershipService,
        IApprovalPermissionService approvalPermissionService,
        ILogger<AdminOrganizationsController> logger)
    {
        _organizationService = organizationService;
        _membershipService = membershipService;
        _approvalPermissionService = approvalPermissionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all organizations with optional filtering, searching, and sorting.
    /// </summary>
    /// <param name="search">Search term for name or slug</param>
    /// <param name="status">Filter by organization status</param>
    /// <param name="sortBy">Sort column (name, slug, status, createdAt)</param>
    /// <param name="sortDir">Sort direction (asc, desc)</param>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrganizations(
        [FromQuery] string? search = null,
        [FromQuery] OrgStatus? status = null,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDir = "asc")
    {
        var (items, totalCount) = await _organizationService.GetOrganizationsAsync(
            search, status, sortBy, sortDir);

        return Ok(new { items, totalCount });
    }

    /// <summary>
    /// Get a single organization by ID.
    /// </summary>
    /// <param name="id">Organization ID</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganization(Guid id)
    {
        var organization = await _organizationService.GetByIdAsync(id);

        if (organization == null)
        {
            return NotFound(new { message = $"Organization {id} not found" });
        }

        return Ok(organization);
    }

    /// <summary>
    /// Create a new organization with its first administrator.
    /// </summary>
    /// <param name="request">Organization creation request</param>
    [HttpPost]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        var currentUserId = GetCurrentUserId();

        try
        {
            var organization = await _organizationService.CreateAsync(request, currentUserId);

            _logger.LogInformation(
                "Organization {OrgId} created by SysAdmin {UserId}",
                organization.Id, currentUserId);

            return CreatedAtAction(
                nameof(GetOrganization),
                new { id = organization.Id },
                organization);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("slug"))
        {
            return Conflict(new { error = "duplicate_slug", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "validation_error", message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing organization.
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Update request</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateOrganization(Guid id, [FromBody] UpdateOrganizationRequest request)
    {
        try
        {
            var organization = await _organizationService.UpdateAsync(id, request);

            if (organization == null)
            {
                return NotFound(new { message = $"Organization {id} not found" });
            }

            _logger.LogInformation(
                "Organization {OrgId} updated by SysAdmin {UserId}",
                id, GetCurrentUserId());

            return Ok(organization);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "validation_error", message = ex.Message });
        }
    }

    /// <summary>
    /// Check if a slug is available.
    /// </summary>
    /// <param name="slug">Slug to check</param>
    /// <param name="excludeId">Optional organization ID to exclude from check</param>
    [HttpGet("check-slug")]
    [ProducesResponseType(typeof(SlugCheckResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckSlug(
        [FromQuery] string slug,
        [FromQuery] Guid? excludeId = null)
    {
        var result = await _organizationService.CheckSlugAsync(slug, excludeId);
        return Ok(result);
    }

    /// <summary>
    /// Archive an organization (make read-only).
    /// </summary>
    /// <param name="id">Organization ID</param>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveOrganization(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        var organization = await _organizationService.ArchiveAsync(id, currentUserId);

        if (organization == null)
        {
            return NotFound(new { message = $"Organization {id} not found" });
        }

        _logger.LogInformation(
            "Organization {OrgId} archived by SysAdmin {UserId}",
            id, currentUserId);

        return Ok(organization);
    }

    /// <summary>
    /// Deactivate an organization (soft delete).
    /// </summary>
    /// <param name="id">Organization ID</param>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateOrganization(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        var organization = await _organizationService.DeactivateAsync(id, currentUserId);

        if (organization == null)
        {
            return NotFound(new { message = $"Organization {id} not found" });
        }

        _logger.LogInformation(
            "Organization {OrgId} deactivated by SysAdmin {UserId}",
            id, currentUserId);

        return Ok(organization);
    }

    /// <summary>
    /// Restore an archived or inactive organization to active status.
    /// </summary>
    /// <param name="id">Organization ID</param>
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreOrganization(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        var organization = await _organizationService.RestoreAsync(id, currentUserId);

        if (organization == null)
        {
            return NotFound(new { message = $"Organization {id} not found" });
        }

        _logger.LogInformation(
            "Organization {OrgId} restored to Active by SysAdmin {UserId}",
            id, currentUserId);

        return Ok(organization);
    }

    // =========================================================================
    // Member Management Endpoints
    // =========================================================================

    /// <summary>
    /// Get all members of an organization.
    /// </summary>
    /// <param name="id">Organization ID</param>
    [HttpGet("{id:guid}/members")]
    [ProducesResponseType(typeof(IEnumerable<OrgMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationMembers(Guid id)
    {
        // Verify organization exists
        var org = await _organizationService.GetByIdAsync(id);
        if (org == null)
        {
            return NotFound(new { message = $"Organization {id} not found" });
        }

        var members = await _membershipService.GetOrganizationMembersAsync(id);
        return Ok(members);
    }

    /// <summary>
    /// Add a user to an organization by email.
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Email and role for the new member</param>
    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(typeof(OrgMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddOrganizationMember(Guid id, [FromBody] AddMemberRequest request)
    {
        var currentUserId = GetCurrentUserId();

        try
        {
            var member = await _membershipService.AddMemberByEmailAsync(
                id, request, currentUserId.ToString());

            _logger.LogInformation(
                "SysAdmin {AdminId} added user {Email} to organization {OrgId} with role {Role}",
                currentUserId, request.Email, id, request.Role);

            return CreatedAtAction(
                nameof(GetOrganizationMembers),
                new { id },
                member);
        }
        catch (Core.Exceptions.NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Core.Exceptions.ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update a member's role in an organization.
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="membershipId">Membership ID</param>
    /// <param name="request">New role</param>
    [HttpPut("{id:guid}/members/{membershipId:guid}")]
    [ProducesResponseType(typeof(MembershipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMemberRole(
        Guid id,
        Guid membershipId,
        [FromBody] UpdateMembershipRequest request)
    {
        try
        {
            var membership = await _membershipService.UpdateMembershipRoleAsync(membershipId, request);

            _logger.LogInformation(
                "SysAdmin {AdminId} updated membership {MembershipId} role to {Role} in organization {OrgId}",
                GetCurrentUserId(), membershipId, request.Role, id);

            return Ok(membership);
        }
        catch (Core.Exceptions.NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Core.Exceptions.BusinessRuleException ex)
        {
            return BadRequest(new { error = "business_rule_violation", message = ex.Message });
        }
    }

    /// <summary>
    /// Remove a member from an organization.
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="membershipId">Membership ID</param>
    [HttpDelete("{id:guid}/members/{membershipId:guid}")]
    [ProducesResponseType(typeof(RemoveMembershipResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveOrganizationMember(Guid id, Guid membershipId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _membershipService.RemoveMembershipAsync(membershipId, currentUserId);

            _logger.LogInformation(
                "SysAdmin {AdminId} removed membership {MembershipId} from organization {OrgId}",
                GetCurrentUserId(), membershipId, id);

            return Ok(result);
        }
        catch (Core.Exceptions.NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Core.Exceptions.BusinessRuleException ex)
        {
            return BadRequest(new { error = "business_rule_violation", message = ex.Message });
        }
    }

    /// <summary>
    /// Update organization approval policy.
    /// Only administrators can configure the inject approval workflow policy.
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Approval policy update request</param>
    [HttpPut("{id:guid}/settings/approval-policy")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateApprovalPolicy(
        Guid id,
        [FromBody] UpdateApprovalPolicyRequest request)
    {
        try
        {
            var organization = await _organizationService.UpdateApprovalPolicyAsync(
                id,
                request.InjectApprovalPolicy);

            if (organization == null)
            {
                return NotFound(new { message = $"Organization {id} not found" });
            }

            _logger.LogInformation(
                "SysAdmin {AdminId} updated organization {OrgId} approval policy to {Policy}",
                GetCurrentUserId(), id, request.InjectApprovalPolicy);

            return Ok(organization);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "validation_error", message = ex.Message });
        }
    }

    // =========================================================================
    // Approval Permission Settings (S11)
    // =========================================================================

    /// <summary>
    /// Get organization approval permission settings.
    /// Includes which roles can approve and self-approval policy.
    /// </summary>
    /// <param name="id">Organization ID</param>
    [HttpGet("{id:guid}/settings/approval-permissions")]
    [ProducesResponseType(typeof(ApprovalPermissionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApprovalPermissions(Guid id)
    {
        try
        {
            var permissions = await _approvalPermissionService.GetApprovalPermissionsAsync(id);
            return Ok(permissions);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Organization {id} not found" });
        }
    }

    /// <summary>
    /// Update organization approval permission settings.
    /// Configures which exercise roles can approve injects and self-approval policy.
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Approval permissions update request</param>
    [HttpPut("{id:guid}/settings/approval-permissions")]
    [ProducesResponseType(typeof(ApprovalPermissionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateApprovalPermissions(
        Guid id,
        [FromBody] UpdateApprovalPermissionsRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var permissions = await _approvalPermissionService.UpdateApprovalPermissionsAsync(
                id,
                request,
                currentUserId);

            _logger.LogInformation(
                "SysAdmin {AdminId} updated organization {OrgId} approval permissions: Roles={Roles}, SelfApproval={SelfApproval}",
                currentUserId, id, request.AuthorizedRoles, request.SelfApprovalPolicy);

            return Ok(permissions);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Organization {id} not found" });
        }
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
