using Cadence.Core.Data;

namespace Cadence.Core.Models.Entities;

/// <summary>
/// External login provider entity for SSO integration (e.g., Entra, Google).
/// Links external provider accounts to Cadence users.
/// </summary>
public class ExternalLogin : IHasTimestamps
{
    /// <summary>
    /// Unique identifier for this external login.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The Cadence user this external login is linked to.
    /// References IdentityUser.Id (string).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// External provider name (e.g., "Entra", "Google").
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// User's unique identifier in the external provider's system.
    /// </summary>
    public string ProviderUserId { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when this external login was linked.
    /// </summary>
    public DateTime LinkedAt { get; set; }

    // =========================================================================
    // IHasTimestamps - Set automatically by DbContext
    // =========================================================================

    /// <summary>
    /// UTC timestamp when this record was created.
    /// Set automatically on insert.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp when this record was last modified.
    /// Set automatically on update.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The Cadence user this external login belongs to.
    /// </summary>
    public ApplicationUser User { get; set; } = null!;
}
