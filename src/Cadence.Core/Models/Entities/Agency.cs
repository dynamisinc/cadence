namespace Cadence.Core.Models.Entities;

/// <summary>
/// Agency entity - represents a participating agency within an organization.
/// Used for exercise participant categorization and reporting.
/// Implements IOrganizationScoped for automatic organization-based data isolation.
/// </summary>
public class Agency : BaseEntity, IOrganizationScoped
{
    /// <summary>
    /// Organization ID this agency belongs to.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Agency name (e.g., "Fire Department", "Emergency Operations Center").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Agency abbreviation (e.g., "FD", "EOC").
    /// Optional for display convenience.
    /// </summary>
    public string? Abbreviation { get; set; }

    /// <summary>
    /// Agency description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this agency is active and available for assignment.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sort order for display in lists.
    /// </summary>
    public int SortOrder { get; set; } = 0;

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The organization this agency belongs to.
    /// </summary>
    public Organization Organization { get; set; } = null!;
}
