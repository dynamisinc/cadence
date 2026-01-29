namespace Cadence.Core.Models.Entities;

/// <summary>
/// Join entity for the many-to-many relationship between Observations and Capabilities.
/// Allows an observation to be tagged with multiple capabilities.
/// </summary>
public class ObservationCapability
{
    /// <summary>
    /// The observation being tagged.
    /// </summary>
    public Guid ObservationId { get; set; }

    /// <summary>
    /// The capability tag.
    /// </summary>
    public Guid CapabilityId { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The observation.
    /// </summary>
    public Observation Observation { get; set; } = null!;

    /// <summary>
    /// The capability.
    /// </summary>
    public Capability Capability { get; set; } = null!;
}
