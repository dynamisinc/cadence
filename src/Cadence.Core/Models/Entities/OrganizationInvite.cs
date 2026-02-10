namespace Cadence.Core.Models.Entities;

/// <summary>
/// OrganizationInvite entity - represents an invitation to join an organization.
/// Supports both email-based and code-based invitations.
/// Implements IOrganizationScoped for automatic organization-based data isolation.
/// </summary>
public class OrganizationInvite : BaseEntity, IOrganizationScoped
{
    /// <summary>
    /// Organization ID this invite belongs to.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Email address for the invite.
    /// Null for code-only invites (shareable links).
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Unique 8-character alphanumeric code for the invite.
    /// Used in shareable links and manual redemption.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Role that will be assigned when invite is redeemed.
    /// </summary>
    public OrgRole Role { get; set; } = OrgRole.OrgUser;

    /// <summary>
    /// UTC timestamp when this invite expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// UTC timestamp when this invite was first used.
    /// Null if not yet used.
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// User ID of the person who redeemed this invite.
    /// Null if not yet used.
    /// </summary>
    public string? UsedById { get; set; }

    /// <summary>
    /// User ID of the person who created this invite.
    /// Note: BaseEntity has CreatedBy (Guid) for system audit. This is a separate field for ApplicationUser FK.
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of times this invite can be used.
    /// Default is 1 for email invites, can be higher for shareable codes.
    /// </summary>
    public int MaxUses { get; set; } = 1;

    /// <summary>
    /// Current number of times this invite has been used.
    /// </summary>
    public int UseCount { get; set; } = 0;

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The organization this invite belongs to.
    /// </summary>
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// The user who redeemed this invite (if used).
    /// </summary>
    public ApplicationUser? UsedBy { get; set; }

    /// <summary>
    /// The user who created this invite.
    /// </summary>
    public ApplicationUser CreatedByUser { get; set; } = null!;

    /// <summary>
    /// Pending exercise assignments that will activate when this invite is accepted.
    /// </summary>
    public ICollection<Features.BulkParticipantImport.Models.Entities.PendingExerciseAssignment> PendingExerciseAssignments { get; set; }
        = new List<Features.BulkParticipantImport.Models.Entities.PendingExerciseAssignment>();
}
