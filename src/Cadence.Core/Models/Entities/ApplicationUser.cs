using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Cadence.Core.Models.Entities;

/// <summary>
/// Application user entity extending ASP.NET Core Identity.
/// Represents an authenticated user with global role and organization membership.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Display name shown in the UI.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// System-level access role determining application permissions.
    /// This is distinct from HSEEP exercise roles which are assigned per-exercise via ExerciseParticipant.
    /// Default: User (standard access).
    /// </summary>
    public SystemRole SystemRole { get; set; } = SystemRole.User;

    /// <summary>
    /// Account status (Active or Deactivated).
    /// </summary>
    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>
    /// UTC timestamp of the user's most recent login.
    /// Null if user has never logged in.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// UTC timestamp when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID of the user who created this account.
    /// Null for self-registered users.
    /// </summary>
    public string? CreatedById { get; set; }

    /// <summary>
    /// Optional phone number for EEG document generation.
    /// Stored as entered (no format normalization).
    /// Hides the base IdentityUser.PhoneNumber to add length validation.
    /// </summary>
    [MaxLength(25)]
    public new string? PhoneNumber { get; set; }

    /// <summary>
    /// Primary organization this user belongs to.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Current active organization context for the user.
    /// Used for multi-organization membership - determines which org's data is visible.
    /// Null if user has no organization assignments.
    /// </summary>
    public Guid? CurrentOrganizationId { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The organization this user belongs to.
    /// </summary>
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// The current active organization context.
    /// </summary>
    public Organization? CurrentOrganization { get; set; }

    /// <summary>
    /// All organization memberships for this user (supports multi-org access).
    /// </summary>
    public ICollection<OrganizationMembership> Memberships { get; set; } = new List<OrganizationMembership>();

    /// <summary>
    /// The user who created this account.
    /// Null for self-registered users.
    /// </summary>
    public ApplicationUser? CreatedByUser { get; set; }

    /// <summary>
    /// Refresh tokens issued to this user.
    /// </summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>
    /// Password reset tokens for this user.
    /// </summary>
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    /// <summary>
    /// External login providers linked to this user (e.g., Entra, Google).
    /// </summary>
    public ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();

    /// <summary>
    /// Exercise participations for this user (includes HSEEP role per exercise).
    /// </summary>
    public ICollection<ExerciseParticipant> ExerciseParticipations { get; set; } = new List<ExerciseParticipant>();

    /// <summary>
    /// Exercises created by this user (for ownership tracking).
    /// </summary>
    public ICollection<Exercise> CreatedExercises { get; set; } = new List<Exercise>();

    /// <summary>
    /// User preferences for display and behavior settings.
    /// </summary>
    public UserPreferences? Preferences { get; set; }
}
