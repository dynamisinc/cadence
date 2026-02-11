namespace Cadence.Core.Models.Entities;

/// <summary>
/// Photo captured during exercise conduct. Stores metadata referencing
/// blob storage URIs for full-size and thumbnail images.
/// Implements IOrganizationScoped for automatic organization-based data isolation.
/// </summary>
public class ExercisePhoto : BaseEntity, IOrganizationScoped
{
    // =========================================================================
    // Foreign Keys
    // =========================================================================

    /// <summary>
    /// The exercise this photo was captured during. Required.
    /// </summary>
    public Guid ExerciseId { get; set; }

    /// <summary>
    /// The organization this photo belongs to. Required for data isolation.
    /// Denormalized from Exercise for query filter efficiency.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// The ApplicationUser ID who captured this photo.
    /// References ApplicationUser (ASP.NET Core Identity) - string type to match IdentityUser.Id.
    /// </summary>
    public string CapturedById { get; set; } = string.Empty;

    /// <summary>
    /// The observation this photo is attached to. Optional - photos can exist
    /// unlinked (e.g., Quick Photo creates a draft observation separately).
    /// </summary>
    public Guid? ObservationId { get; set; }

    // =========================================================================
    // Photo Metadata
    // =========================================================================

    /// <summary>
    /// Original file name of the captured photo.
    /// Max 500 characters.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Azure Blob Storage URI for the full-size compressed photo.
    /// Max 2000 characters.
    /// </summary>
    public string BlobUri { get; set; } = string.Empty;

    /// <summary>
    /// Azure Blob Storage URI for the 300px thumbnail.
    /// Max 2000 characters.
    /// </summary>
    public string ThumbnailUri { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes of the compressed photo.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Wall clock UTC timestamp when the photo was captured.
    /// </summary>
    public DateTime CapturedAt { get; set; }

    /// <summary>
    /// Exercise scenario time when the photo was captured.
    /// Null if exercise clock was not running.
    /// </summary>
    public DateTime? ScenarioTime { get; set; }

    // =========================================================================
    // Location Data
    // =========================================================================

    /// <summary>
    /// GPS latitude where the photo was captured. Optional.
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// GPS longitude where the photo was captured. Optional.
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// GPS accuracy in meters. Optional.
    /// </summary>
    public double? LocationAccuracy { get; set; }

    // =========================================================================
    // Display Properties
    // =========================================================================

    /// <summary>
    /// Display order within an observation's photo collection.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Photo lifecycle status.
    /// </summary>
    public PhotoStatus Status { get; set; } = PhotoStatus.Draft;

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The exercise this photo belongs to.
    /// </summary>
    public Exercise Exercise { get; set; } = null!;

    /// <summary>
    /// The organization this photo belongs to. Required by IOrganizationScoped.
    /// </summary>
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// The observation this photo is attached to (if any).
    /// </summary>
    public Observation? Observation { get; set; }

    /// <summary>
    /// The user who captured this photo.
    /// May be null if the user has been deactivated.
    /// </summary>
    public ApplicationUser? CapturedByUser { get; set; }
}
