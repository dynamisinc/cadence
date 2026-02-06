using Cadence.Core.Features.Organizations.Models.DTOs;

namespace Cadence.Core.Features.Organizations.Services;

/// <summary>
/// Service for managing organization invitations.
/// Handles creating, resending, cancelling, and validating invitations.
/// </summary>
public interface IOrganizationInvitationService
{
    /// <summary>
    /// Create and send an organization invitation via email.
    /// </summary>
    Task<InvitationDto> CreateInvitationAsync(
        Guid organizationId,
        CreateInvitationRequest request,
        string invitedByUserId);

    /// <summary>
    /// Resend a pending or expired invitation with refreshed expiration.
    /// </summary>
    Task<InvitationDto> ResendInvitationAsync(Guid invitationId, string requestedByUserId);

    /// <summary>
    /// Cancel a pending invitation.
    /// </summary>
    Task CancelInvitationAsync(Guid invitationId, string requestedByUserId);

    /// <summary>
    /// Get all invitations for an organization.
    /// </summary>
    Task<IEnumerable<InvitationDto>> GetInvitationsAsync(
        Guid organizationId,
        string? statusFilter = null);

    /// <summary>
    /// Get a single invitation by ID.
    /// </summary>
    Task<InvitationDto?> GetInvitationAsync(Guid invitationId);

    /// <summary>
    /// Validate an invitation code and return the invitation details.
    /// Returns null if code is invalid, expired, or already used.
    /// </summary>
    Task<InvitationDto?> ValidateCodeAsync(string code);

    /// <summary>
    /// Accept an invitation - creates the org membership.
    /// </summary>
    Task AcceptInvitationAsync(string code, string userId);
}
