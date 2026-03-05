namespace Cadence.Core.Features.SystemSettings.Models.Entities;

/// <summary>
/// Records a user's acceptance of a specific EULA version.
/// Not org-scoped — EULA acceptance is system-wide.
/// Not soft-deletable — acceptance records are permanent.
/// </summary>
public class EulaAcceptance
{
    public Guid Id { get; set; }

    /// <summary>FK to ApplicationUser.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>The EULA version that was accepted.</summary>
    public string EulaVersion { get; set; } = string.Empty;

    /// <summary>When the user accepted.</summary>
    public DateTime AcceptedAt { get; set; }
}
