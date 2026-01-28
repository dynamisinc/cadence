namespace Cadence.Core.Models.Entities;

/// <summary>
/// Core Capability entity - FEMA-defined capability areas from the National Preparedness Goal.
/// Used to categorize observations and track performance by capability area.
/// This is reference data - not inheriting from BaseEntity.
/// </summary>
public class CoreCapability
{
    /// <summary>
    /// Unique identifier for this capability.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the core capability (e.g., "Planning", "Public Information and Warning").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The FEMA mission area this capability belongs to.
    /// </summary>
    public MissionArea MissionArea { get; set; }

    /// <summary>
    /// Display order within the mission area.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this capability is active in the system.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// Observations tagged with this capability.
    /// </summary>
    public ICollection<ObservationCapability> ObservationCapabilities { get; set; } = new List<ObservationCapability>();

    /// <summary>
    /// Exercises that target this capability.
    /// </summary>
    public ICollection<ExerciseTargetCapability> ExerciseTargetCapabilities { get; set; } = new List<ExerciseTargetCapability>();
}

/// <summary>
/// FEMA Mission Areas grouping core capabilities.
/// </summary>
public enum MissionArea
{
    /// <summary>
    /// Capabilities that relate to avoiding, preventing, or stopping a threatened or actual act of terrorism.
    /// </summary>
    Prevention = 1,

    /// <summary>
    /// Capabilities that secure the homeland against acts of terrorism and manmade or natural disasters.
    /// </summary>
    Protection = 2,

    /// <summary>
    /// Capabilities that reduce loss of life and property by lessening the impact of disasters.
    /// </summary>
    Mitigation = 3,

    /// <summary>
    /// Capabilities that save lives, protect property and the environment, and meet basic human needs.
    /// </summary>
    Response = 4,

    /// <summary>
    /// Capabilities that assist communities affected by an incident to recover effectively.
    /// </summary>
    Recovery = 5,
}
