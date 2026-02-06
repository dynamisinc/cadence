using System.ComponentModel.DataAnnotations;

namespace Cadence.Core.Models.Entities;

/// <summary>
/// EEG Entry entity - a structured observation recorded against a Critical Task
/// during exercise conduct. Per HSEEP 2020 Doctrine, EEG entries are standardized
/// assessments that streamline data collection and support AAR development.
///
/// Unlike free-form observations, EEG entries are:
/// 1. Structured: Tied to a specific Critical Task
/// 2. Rated: Require a P/S/M/U performance rating
/// 3. Traceable: Can link to the triggering inject
/// </summary>
public class EegEntry : BaseEntity, IOrganizationScoped
{
    // =========================================================================
    // Core Properties
    // =========================================================================

    /// <summary>
    /// The observation/assessment text describing what the evaluator observed.
    /// Required, 1-4000 characters.
    /// </summary>
    [Required]
    [MaxLength(4000)]
    public string ObservationText { get; set; } = string.Empty;

    /// <summary>
    /// HSEEP P/S/M/U performance rating for this critical task assessment.
    /// Required for all EEG entries.
    /// </summary>
    public PerformanceRating Rating { get; set; }

    /// <summary>
    /// When the task performance was observed (exercise/scenario time).
    /// May differ from RecordedAt if using compressed timeline.
    /// </summary>
    public DateTime ObservedAt { get; set; }

    /// <summary>
    /// Wall clock time when this entry was recorded.
    /// Set automatically on creation.
    /// </summary>
    public DateTime RecordedAt { get; set; }

    // =========================================================================
    // Foreign Keys
    // =========================================================================

    /// <summary>
    /// The organization this EEG entry belongs to. Required for data isolation.
    /// Denormalized for query filter efficiency.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// The critical task this entry assesses. Required.
    /// </summary>
    public Guid CriticalTaskId { get; set; }

    /// <summary>
    /// The evaluator who made this observation.
    /// References ApplicationUser (ASP.NET Core Identity) - string type to match IdentityUser.Id.
    /// </summary>
    [MaxLength(450)]
    public string EvaluatorId { get; set; } = string.Empty;

    /// <summary>
    /// Optional: The inject that triggered this observation.
    /// Useful for traceability between MSEL injects and evaluations.
    /// </summary>
    public Guid? TriggeringInjectId { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The organization this EEG entry belongs to.
    /// </summary>
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// The critical task this entry assesses.
    /// </summary>
    public CriticalTask CriticalTask { get; set; } = null!;

    /// <summary>
    /// The evaluator who made this observation.
    /// May be null if the user has been deactivated.
    /// </summary>
    public ApplicationUser? Evaluator { get; set; }

    /// <summary>
    /// The inject that triggered this observation (if any).
    /// </summary>
    public Inject? TriggeringInject { get; set; }
}
