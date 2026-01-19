namespace Cadence.Core.Models.Entities;

/// <summary>
/// Delivery method lookup table - system-level reference data.
/// Replaces the DeliveryMethod enum to provide a consistent set of delivery methods.
/// For custom delivery methods, use the "Other" option with DeliveryMethodOther text field on Inject.
/// </summary>
public class DeliveryMethodLookup : BaseEntity
{
    /// <summary>
    /// Display name of the delivery method (e.g., "Radio", "Email", "Verbal").
    /// Required, max 100 characters.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of when/how to use this method. Max 500 characters.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this method is active and available for selection.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order in dropdowns and lists.
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Indicates if this is the "Other" option that allows free-text input.
    /// When selected, users can specify custom text in Inject.DeliveryMethodOther.
    /// </summary>
    public bool IsOther { get; set; } = false;

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// Injects using this delivery method.
    /// </summary>
    public ICollection<Inject> Injects { get; set; } = new List<Inject>();
}
