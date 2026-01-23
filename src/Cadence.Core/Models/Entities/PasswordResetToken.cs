using Cadence.Core.Data;

namespace Cadence.Core.Models.Entities;

/// <summary>
/// Password reset token entity for self-service password recovery.
/// Tokens are single-use, time-limited, and stored as SHA256 hashes.
/// </summary>
public class PasswordResetToken : IHasTimestamps
{
    /// <summary>
    /// Unique identifier for this reset token.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user this token belongs to.
    /// References IdentityUser.Id (string).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 hash of the actual reset token.
    /// The actual token is sent in the email link; only the hash is stored.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when this token expires.
    /// Typically 1 hour from creation per HSEEP security requirements.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// UTC timestamp when this token was used to complete password reset.
    /// Null if not yet used. Single-use tokens cannot be reused.
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// IP address where the reset was requested (optional).
    /// Used for security audit logging.
    /// </summary>
    public string? IpAddress { get; set; }

    // =========================================================================
    // IHasTimestamps - Set automatically by DbContext
    // =========================================================================

    /// <summary>
    /// UTC timestamp when this token was created.
    /// Set automatically on insert.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp when this token was last modified.
    /// Set automatically on update.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The user this token belongs to.
    /// </summary>
    public ApplicationUser User { get; set; } = null!;
}
