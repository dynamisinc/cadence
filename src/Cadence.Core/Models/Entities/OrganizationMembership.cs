namespace Cadence.Core.Models.Entities;

/// <summary>
/// OrganizationMembership entity - represents a user's membership in an organization with a specific role.
/// Enables multi-organization membership for users.
/// Implements IOrganizationScoped for automatic organization-based data isolation.
/// </summary>
public class OrganizationMembership : BaseEntity, IOrganizationScoped
{
    /// <summary>
    /// User ID - references ApplicationUser.Id (string from IdentityUser).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Organization ID.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Role within this organization.
    /// </summary>
    public OrgRole Role { get; set; } = OrgRole.OrgUser;

    /// <summary>
    /// Membership status.
    /// </summary>
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;

    /// <summary>
    /// UTC timestamp when the user joined this organization.
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID of the person who invited this user to the organization.
    /// Null if user was assigned by system or joined via code.
    /// </summary>
    public string? InvitedById { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The user who is a member of the organization.
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// The organization this membership belongs to.
    /// </summary>
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// The user who invited this member (if applicable).
    /// </summary>
    public ApplicationUser? InvitedBy { get; set; }
}
