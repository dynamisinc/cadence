using Cadence.Core.Data;

namespace Cadence.Core.Models.Entities;

/// <summary>
/// Capability entity - represents an organizational capability that can be evaluated during exercises.
/// Capabilities are scoped to organizations and support multiple frameworks
/// (FEMA Core Capabilities, NATO Baseline Requirements, NIST CSF, ISO 22301, custom).
/// Implements IOrganizationScoped for automatic organization-based data isolation.
/// </summary>
public class Capability : IHasTimestamps, IOrganizationScoped
{
    /// <summary>
    /// Unique identifier for this capability.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The organization that owns this capability definition.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Display name of the capability (e.g., "Mass Care Services", "Cybersecurity").
    /// Required, max 200 characters.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of what this capability encompasses.
    /// Optional, max 1000 characters.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Grouping category (e.g., "Response", "Protection" for FEMA Mission Areas,
    /// or custom categories for other frameworks).
    /// Optional, max 100 characters.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Display order within category. Lower numbers appear first.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this capability is available for selection.
    /// Inactive capabilities are hidden from UIs but preserved for historical data.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Identifies the predefined library this was imported from (FEMA, NATO, NIST, ISO).
    /// Null for custom capabilities. Used for display and potential updates.
    /// </summary>
    public string? SourceLibrary { get; set; }

    /// <summary>
    /// UTC timestamp when this capability was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp when this capability was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The organization that owns this capability.
    /// </summary>
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// Observations tagged with this capability.
    /// </summary>
    public ICollection<ObservationCapability> ObservationCapabilities { get; set; } = new List<ObservationCapability>();

    /// <summary>
    /// Exercises that target this capability.
    /// </summary>
    public ICollection<ExerciseTargetCapability> ExerciseTargetCapabilities { get; set; } = new List<ExerciseTargetCapability>();

    /// <summary>
    /// EEG Capability Targets that reference this capability.
    /// Each CapabilityTarget is an exercise-specific performance threshold for this capability.
    /// </summary>
    public ICollection<CapabilityTarget> CapabilityTargets { get; set; } = new List<CapabilityTarget>();
}
