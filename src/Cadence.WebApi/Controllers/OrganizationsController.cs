using System.Security.Claims;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Features.Organizations.Models.DTOs;
using Cadence.Core.Features.Organizations.Services;
using Cadence.Core.Hubs;
using Cadence.WebApi.Authorization;
using Cadence.WebApi.Extensions;
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
    private readonly IOrganizationInvitationService _invitationService;
    private readonly IApprovalPermissionService _approvalPermissionService;
    private readonly ICurrentOrganizationContext _orgContext;
    private readonly ILogger<OrganizationsController> _logger;

    public OrganizationsController(
        IOrganizationService organizationService,
        IMembershipService membershipService,
        IOrganizationInvitationService invitationService,
        IApprovalPermissionService approvalPermissionService,
        ICurrentOrganizationContext orgContext,
        ILogger<OrganizationsController> logger)
    {
        _organizationService = organizationService;
        _membershipService = membershipService;
        _invitationService = invitationService;
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
    // Current Organization Invitations (OrgAdmin)
    // =========================================================================

    /// <summary>
    /// Get all invitations for the current organization.
    /// </summary>
    [HttpGet("current/invitations")]
    [AuthorizeOrgAdmin]
    [ProducesResponseType(typeof(IEnumerable<InvitationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentOrganizationInvitations([FromQuery] string? status = null)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
        {
            return NotFound(new { message = "No organization context" });
        }

        var invitations = await _invitationService.GetInvitationsAsync(orgId.Value, status);
        return Ok(invitations);
    }

    /// <summary>
    /// Create and send an organization invitation.
    /// </summary>
    [HttpPost("current/invitations")]
    [AuthorizeOrgAdmin]
    [ProducesResponseType(typeof(InvitationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationRequest request)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
        {
            return NotFound(new { message = "No organization context" });
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var invitation = await _invitationService.CreateInvitationAsync(
                orgId.Value, request, currentUserId);

            _logger.LogInformation(
                "OrgAdmin {AdminId} invited {Email} to organization {OrgId}",
                currentUserId, request.Email, orgId);

            return CreatedAtAction(
                nameof(GetCurrentOrganizationInvitations),
                invitation);
        }
        catch (Core.Exceptions.ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Resend a pending or expired invitation.
    /// </summary>
    [HttpPost("current/invitations/{invitationId:guid}/resend")]
    [AuthorizeOrgAdmin]
    [ProducesResponseType(typeof(InvitationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResendInvitation(Guid invitationId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var invitation = await _invitationService.ResendInvitationAsync(
                invitationId, currentUserId);

            _logger.LogInformation(
                "OrgAdmin {AdminId} resent invitation {InviteId}",
                currentUserId, invitationId);

            return Ok(invitation);
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
    /// Cancel a pending invitation.
    /// </summary>
    [HttpDelete("current/invitations/{invitationId:guid}")]
    [AuthorizeOrgAdmin]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelInvitation(Guid invitationId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            await _invitationService.CancelInvitationAsync(invitationId, currentUserId);

            _logger.LogInformation(
                "OrgAdmin {AdminId} cancelled invitation {InviteId}",
                currentUserId, invitationId);

            return NoContent();
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
    /// Validate an invitation code (public endpoint for invitation acceptance flow).
    /// </summary>
    [HttpGet("/api/invitations/validate/{code}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InvitationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateInvitationCode(string code)
    {
        var invitation = await _invitationService.ValidateCodeAsync(code);
        if (invitation == null)
        {
            return NotFound(new { message = "Invitation not found, expired, or already used" });
        }

        return Ok(invitation);
    }

    /// <summary>
    /// Accept an invitation and join the organization.
    /// </summary>
    [HttpPost("/api/invitations/accept/{code}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptInvitation(string code)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            await _invitationService.AcceptInvitationAsync(code, currentUserId);

            return Ok(new { message = "Invitation accepted successfully" });
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

    /// <summary>
    /// Update approval policy for the current organization.
    /// Controls whether inject approval is Disabled, Optional, or Required.
    /// </summary>
    [HttpPut("current/settings/approval-policy")]
    [AuthorizeOrgAdmin]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCurrentOrganizationApprovalPolicy(
        [FromBody] UpdateApprovalPolicyRequest request)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
        {
            return NotFound(new { message = "No organization context" });
        }

        try
        {
            var organization = await _organizationService.UpdateApprovalPolicyAsync(
                orgId.Value,
                request.InjectApprovalPolicy);

            if (organization == null)
            {
                return NotFound(new { message = "Organization not found" });
            }

            _logger.LogInformation(
                "OrgAdmin {AdminId} updated organization {OrgId} approval policy to {Policy}",
                GetCurrentUserId(), orgId, request.InjectApprovalPolicy);

            return Ok(organization);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "validation_error", message = ex.Message });
        }
    }

    // =========================================================================
    // Helper Methods
    // =========================================================================

    /// <summary>
    /// Gets the current authenticated user's ID from JWT claims.
    /// </summary>
    private string GetCurrentUserId() => User.GetUserId();

    /// <summary>
    /// Gets the current organization ID from the organization context.
    /// </summary>
    private Guid? GetCurrentOrganizationId()
    {
        return _orgContext.CurrentOrganizationId;
    }
}
