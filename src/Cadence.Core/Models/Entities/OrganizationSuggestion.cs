namespace Cadence.Core.Models.Entities;

/// <summary>
/// Organization-managed autocomplete suggestion.
/// OrgAdmins can curate suggestion values per inject field.
/// Managed suggestions appear first in autocomplete dropdowns, before historical values.
/// </summary>
public class OrganizationSuggestion : BaseEntity, IOrganizationScoped
{
    /// <summary>
    /// Organization this suggestion belongs to.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// The inject field this suggestion applies to.
    /// Must be one of SuggestionFieldNames constants.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// The suggestion value (e.g., "Fire Department", "EOC").
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Display order within the field. Lower values appear first.
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Whether this suggestion is active and visible in autocomplete.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this entry blocks a historical value from appearing in autocomplete.
    /// When true, the Value is suppressed from historical suggestions.
    /// </summary>
    public bool IsBlocked { get; set; } = false;

    // Navigation
    public Organization Organization { get; set; } = null!;
}

/// <summary>
/// Valid field names for organization suggestions.
/// </summary>
public static class SuggestionFieldNames
{
    public const string Source = "Source";
    public const string Target = "Target";
    public const string Track = "Track";
    public const string LocationName = "LocationName";
    public const string LocationType = "LocationType";
    public const string ResponsibleController = "ResponsibleController";

    public static readonly string[] All =
    {
        Source, Target, Track, LocationName, LocationType, ResponsibleController
    };

    public static bool IsValid(string fieldName) => All.Contains(fieldName);
}
