using System.Security.Claims;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Features.Organizations.Models.DTOs;
using Cadence.Core.Features.Organizations.Services;
using Cadence.Core.Hubs;
using Cadence.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for organization management by OrgAdmins.
/// Operates on the current organization context from JWT claims.
/// </summary>
[ApiController]
[Route("api/organizations")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly IMembershipService _membershipService;
    private readonly IApprovalPermissionService _approvalPermissionService;
    private readonly ICurrentOrganizationContext _orgContext;
    private readonly ILogger<OrganizationsController> _logger;

    public OrganizationsController(
        IOrganizationService organizationService,
        IMembershipService membershipService,
        IApprovalPermissionService approvalPermissionService,
        ICurrentOrganizationContext orgContext,
        ILogger<OrganizationsController> logger)
    {
        _organizationService = organizationService;
        _membershipService = membershipService;
        _approvalPermissionService = approvalPermissionService;
        _orgContext = orgContext;
        _logger = logger;
    }

    // =========================================================================
    // Current Organization Endpoints (OrgAdmin)
    // =========================================================================

    /// <summary>
    /// Get the current organization details.
    /// </summary>
    [HttpGet("current")]
    [AuthorizeOrgAdmin]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentOrganization()
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
        {
            return NotFound(new { message = "No organization context" });
        }

        var organization = await _organizationService.GetByIdAsync(orgId.Value);
        if (organization == null)
        {
            return NotFound(new { message = "Organization not found" });
        }

        return Ok(organization);
    }

    /// <summary>
    /// Update the current organization details.
    /// </summary>
    [HttpPut("current")]
    [AuthorizeOrgAdmin]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCurrentOrganization([FromBody] UpdateOrganizationRequest request)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
        {
            return NotFound(new { message = "No organization context" });
        }

        try
        {
            var organization = await _organizationService.UpdateAsync(orgId.Value, request);
            if (organization == null)
            {
                return NotFound(new { message = "Organization not found" });
            }

            _logger.LogInformation(
                "OrgAdmin {UserId} updated organization {OrgId}",
                GetCurrentUserId(), orgId);

            return Ok(organization);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "validation_error", message = ex.Message });
        }
    }

    // =========================================================================
    // Current Organization Member Management (OrgAdmin)
    // =========================================================================

    /// <summary>
    /// Get all members of the current organization.
    /// </summary>
    [HttpGet("current/members")]
    [AuthorizeOrgAdmin]
    [ProducesResponseType(typeof(IEnumerable<OrgMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentOrganizationMembers()
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
        {
            return NotFound(new { message = "No organization context" });
        }

        var members = await _membershipService.GetOrganizationMembersAsync(orgId.Value);
        return Ok(members);
    }

    /// <summary>
    /// Add a user to the current organization by email.
    /// </summary>
    [HttpPost("current/members")]
    [AuthorizeOrgAdmin]
    [ProducesResponseType(typeof(OrgMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddCurrentOrganizationMember([FromBody] AddMemberRequest request)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
        {
            return NotFound(new { message = "No organization context" });
        }

        var currentUserId = GetCurrentUserId();

        try
        {
            var member = await _membershipService.AddMemberByEmailAsync(
                orgId.Value, request, currentUserId);

            _logger.LogInformation(
                "OrgAdmin {AdminId} added user {Email} to organization {OrgId} with role {Role}",
                currentUserId, request.Email, orgId, request.Role);

            return CreatedAtAction(
                nameof(GetCurrentOrganizationMembers),
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
    /// Update a member's role in the current organization.
    /// </summary>
    [HttpPut("current/members/{membershipId:guid}")]
    [AuthorizeOrgAdmin]
    [ProducesResponseType(typeof(MembershipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCurrentOrganizationMemberRole(
        Guid membershipId,
        [FromBody] UpdateMembershipRequest request)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
        {
            return NotFound(new { message = "No organization context" });
        }

        try
        {
            var membership = await _membershipService.UpdateMembershipRoleAsync(membershipId, request);

            _logger.LogInformation(
                "OrgAdmin {AdminId} updated membership {MembershipId} role to {Role} in organization {OrgId}",
                GetCurrentUserId(), membershipId, request.Role, orgId);

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
    /// Remove a member from the current organization.
    /// </summary>
    [HttpDelete("current/members/{membershipId:guid}")]
    [AuthorizeOrgAdmin]
    [ProducesResponseType(typeof(RemoveMembershipResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveCurrentOrganizationMember(Guid membershipId)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
        {
            return NotFound(new { message = "No organization context" });
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _membershipService.RemoveMembershipAsync(membershipId, currentUserId);

            _logger.LogInformation(
                "OrgAdmin {AdminId} removed membership {MembershipId} from organization {OrgId}",
                currentUserId, membershipId, orgId);

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

    // =========================================================================
    // Current Organization Approval Permissions (OrgAdmin)
    // =========================================================================

    /// <summary>
    /// Get approval permission settings for the current organization.
    /// </summary>
    [HttpGet("current/settings/approval-permissions")]
    [AuthorizeOrgAdmin]
    [ProducesResponseType(typeof(ApprovalPermissionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentOrganizationApprovalPermissions()
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
        {
            return NotFound(new { message = "No organization context" });
        }

        try
        {
            var permissions = await _approvalPermissionService.GetApprovalPermissionsAsync(orgId.Value);
            return Ok(permissions);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Organization not found" });
        }
    }

    /// <summary>
    /// Update approval permission settings for the current organization.
    /// </summary>
    [HttpPut("current/settings/approval-permissions")]
    [AuthorizeOrgAdmin]
    [ProducesResponseType(typeof(ApprovalPermissionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCurrentOrganizationApprovalPermissions(
        [FromBody] UpdateApprovalPermissionsRequest request)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
        {
            return NotFound(new { message = "No organization context" });
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var permissions = await _approvalPermissionService.UpdateApprovalPermissionsAsync(
                orgId.Value,
                request,
                currentUserId);

            _logger.LogInformation(
                "OrgAdmin {AdminId} updated organization {OrgId} approval permissions: Roles={Roles}, SelfApproval={SelfApproval}",
                currentUserId, orgId, request.AuthorizedRoles, request.SelfApprovalPolicy);

            return Ok(permissions);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Organization not found" });
        }
    }

    // =========================================================================
    // Helper Methods
    // =========================================================================

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

    /// <summary>
    /// Get the current organization ID from the organization context.
    /// </summary>
    private Guid? GetCurrentOrganizationId()
    {
        return _orgContext.CurrentOrganizationId;
    }
}
