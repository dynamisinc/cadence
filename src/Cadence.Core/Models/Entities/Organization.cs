namespace Cadence.Core.Models.Entities;

/// <summary>
/// Organization entity - parent container for users and exercises.
/// Provides multi-tenancy and data isolation.
/// </summary>
public class Organization : BaseEntity
{
    /// <summary>
    /// Organization name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-safe slug for organization identification.
    /// Used in URLs and API routes.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Organization description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Contact email for organization administration.
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Organization lifecycle status.
    /// </summary>
    public OrgStatus Status { get; set; } = OrgStatus.Active;

    /// <summary>
    /// Organization-level inject approval policy.
    /// Determines whether exercises require formal inject approval.
    /// </summary>
    public ApprovalPolicy InjectApprovalPolicy { get; set; } = ApprovalPolicy.Optional;

    /// <summary>
    /// Exercise roles that can approve injects (flags enum).
    /// Administrator is always included regardless of this setting.
    /// Default: Administrator | ExerciseDirector.
    /// </summary>
    public ApprovalRoles ApprovalAuthorizedRoles { get; set; } =
        ApprovalRoles.Administrator | ApprovalRoles.ExerciseDirector;

    /// <summary>
    /// Policy for self-approval of injects.
    /// Controls whether users can approve injects they submitted.
    /// Default: NeverAllowed (separation of duties).
    /// </summary>
    public SelfApprovalPolicy SelfApprovalPolicy { get; set; } = SelfApprovalPolicy.NeverAllowed;

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// Exercises owned by this organization.
    /// </summary>
    public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();

    /// <summary>
    /// Memberships - users assigned to this organization with specific roles.
    /// </summary>
    public ICollection<OrganizationMembership> Memberships { get; set; } = new List<OrganizationMembership>();

    /// <summary>
    /// Invites for users to join this organization.
    /// </summary>
    public ICollection<OrganizationInvite> Invites { get; set; } = new List<OrganizationInvite>();

    /// <summary>
    /// Agencies participating in exercises within this organization.
    /// </summary>
    public ICollection<Agency> Agencies { get; set; } = new List<Agency>();
}
