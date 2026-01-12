namespace Cadence.Core.Models.Entities;

/// <summary>
/// User entity - represents a system user.
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// User's login email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name shown in the UI.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this user has system-wide administrator privileges.
    /// </summary>
    public bool IsSystemAdmin { get; set; }

    /// <summary>
    /// Primary organization this user belongs to.
    /// </summary>
    public Guid OrganizationId { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The organization this user belongs to.
    /// </summary>
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// Exercise participations for this user.
    /// </summary>
    public ICollection<ExerciseParticipant> ExerciseParticipations { get; set; } = new List<ExerciseParticipant>();
}
