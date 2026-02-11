namespace Cadence.Core.Models.Entities;

/// <summary>
/// Observation entity - an evaluator's documented assessment of player performance
/// during exercise conduct. Observations feed into the After-Action Report (AAR).
/// Implements IOrganizationScoped for automatic organization-based data isolation.
/// </summary>
public class Observation : BaseEntity, IOrganizationScoped
{
    // =========================================================================
    // Core Properties
    // =========================================================================

    /// <summary>
    /// Observation lifecycle status. Draft observations are auto-created by Quick Photo
    /// and completed when the user adds description and rating.
    /// Defaults to Complete for backwards compatibility with existing observations.
    /// </summary>
    public ObservationStatus Status { get; set; } = ObservationStatus.Complete;

    /// <summary>
    /// The observation content. Required for Complete observations, optional for Draft.
    /// 1-4000 characters when provided.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// HSEEP P/S/M/U performance rating. Optional - some observations
    /// may be general notes without a formal rating.
    /// </summary>
    public ObservationRating? Rating { get; set; }

    /// <summary>
    /// Evaluator's recommendation based on the observation.
    /// Max 2000 characters.
    /// </summary>
    public string? Recommendation { get; set; }

    /// <summary>
    /// UTC timestamp when the observation was made (in the exercise).
    /// Defaults to creation time but can be adjusted.
    /// </summary>
    public DateTime ObservedAt { get; set; }

    /// <summary>
    /// Physical or functional location where the observation was made.
    /// Max 200 characters.
    /// </summary>
    public string? Location { get; set; }

    // =========================================================================
    // Foreign Keys
    // =========================================================================

    /// <summary>
    /// The organization this observation belongs to. Required for data isolation.
    /// Denormalized from Exercise for query filter efficiency.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// The exercise this observation belongs to. Required.
    /// </summary>
    public Guid ExerciseId { get; set; }

    /// <summary>
    /// The inject this observation relates to. Optional - observations
    /// can be general or linked to a specific inject.
    /// </summary>
    public Guid? InjectId { get; set; }

    /// <summary>
    /// The objective this observation relates to. Optional - observations
    /// can be linked to specific exercise objectives.
    /// </summary>
    public Guid? ObjectiveId { get; set; }

    /// <summary>
    /// The ApplicationUser ID who created this observation.
    /// References ApplicationUser (ASP.NET Core Identity) - string type to match IdentityUser.Id.
    /// </summary>
    public string? CreatedByUserId { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The exercise this observation belongs to.
    /// </summary>
    public Exercise Exercise { get; set; } = null!;

    /// <summary>
    /// The inject this observation relates to (if any).
    /// </summary>
    public Inject? Inject { get; set; }

    /// <summary>
    /// The objective this observation relates to (if any).
    /// </summary>
    public Objective? Objective { get; set; }

    /// <summary>
    /// The user who created this observation.
    /// References ApplicationUser (ASP.NET Core Identity).
    /// May be null if the user has been deactivated.
    /// </summary>
    public ApplicationUser? CreatedByUser { get; set; }

    /// <summary>
    /// Core capabilities tagged on this observation (many-to-many).
    /// </summary>
    public ICollection<ObservationCapability> ObservationCapabilities { get; set; } = new List<ObservationCapability>();

    /// <summary>
    /// Photos attached to this observation as visual evidence.
    /// </summary>
    public ICollection<ExercisePhoto> Photos { get; set; } = new List<ExercisePhoto>();
}
