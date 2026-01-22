using Cadence.Core.Data;

namespace Cadence.Core.Models.Entities;

/// <summary>
/// Refresh token entity for JWT authentication.
/// Stores hashed tokens for secure session management.
/// </summary>
public class RefreshToken : IHasTimestamps
{
    /// <summary>
    /// Unique identifier for this refresh token.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user this token belongs to.
    /// References IdentityUser.Id (string).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 hash of the actual refresh token.
    /// The actual token is sent to the client; only the hash is stored.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when this token expires.
    /// Typically 4 hours (standard) or 30 days (RememberMe).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Indicates whether this token has been revoked before expiration.
    /// Used for logout or security events (password change, etc.).
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// UTC timestamp when this token was revoked.
    /// Null if not revoked.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Whether the user selected "Remember Me" during login.
    /// Affects token expiration duration.
    /// </summary>
    public bool RememberMe { get; set; }

    /// <summary>
    /// IP address where the token was created (optional).
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// Device/browser information for this session (optional).
    /// </summary>
    public string? DeviceInfo { get; set; }

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
