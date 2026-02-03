namespace Cadence.Core.Models.Entities;

/// <summary>
/// Audit trail for inject status changes.
/// Records who changed what status and when, with optional notes.
/// </summary>
public class InjectStatusHistory : BaseEntity
{
    /// <summary>
    /// The inject this history entry belongs to.
    /// </summary>
    public Guid InjectId { get; set; }

    /// <summary>
    /// Status before the change.
    /// </summary>
    public InjectStatus FromStatus { get; set; }

    /// <summary>
    /// Status after the change.
    /// </summary>
    public InjectStatus ToStatus { get; set; }

    /// <summary>
    /// ApplicationUser ID who made the change.
    /// References ApplicationUser (ASP.NET Core Identity) - string type to match IdentityUser.Id.
    /// </summary>
    public string ChangedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the change occurred.
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// Optional notes (approval notes, rejection reason, etc.). Max 1000 characters.
    /// </summary>
    public string? Notes { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The inject this history entry belongs to.
    /// </summary>
    public Inject Inject { get; set; } = null!;

    /// <summary>
    /// The user who made the change.
    /// References ApplicationUser (ASP.NET Core Identity).
    /// May be null if the user has been deactivated.
    /// </summary>
    public ApplicationUser? ChangedByUser { get; set; }
}
