using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.SystemSettings.Models.Entities;

/// <summary>
/// Records a user's acceptance of a specific EULA version.
/// Not org-scoped — EULA acceptance is system-wide.
/// Not soft-deletable — acceptance records are permanent (immutable audit record).
/// Does not inherit BaseEntity because it requires neither soft-delete nor auto-timestamps.
/// </summary>
public class EulaAcceptance
{
    public Guid Id { get; set; }

    /// <summary>FK to ApplicationUser.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Navigation property to the user.</summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>The EULA version that was accepted.</summary>
    public string EulaVersion { get; set; } = string.Empty;

    /// <summary>When the user accepted.</summary>
    public DateTime AcceptedAt { get; set; }
}
