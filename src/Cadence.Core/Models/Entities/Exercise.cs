namespace Cadence.Core.Models.Entities;

/// <summary>
/// Exercise entity - top-level container for an emergency management exercise.
/// All other data (MSELs, participants, objectives, observations) belongs to an exercise.
/// Implements IOrganizationScoped for automatic organization-based data isolation.
/// </summary>
public class Exercise : BaseEntity, IOrganizationScoped
{
    // =========================================================================
    // Core Properties
    // =========================================================================

    /// <summary>
    /// Exercise name. Required, 1-200 characters.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description. Max 4000 characters.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of exercise (TTX, FE, FSE, CAX, Hybrid).
    /// </summary>
    public ExerciseType ExerciseType { get; set; }

    /// <summary>
    /// Current lifecycle status.
    /// </summary>
    public ExerciseStatus Status { get; set; } = ExerciseStatus.Draft;

    /// <summary>
    /// Training/test flag. Practice exercises are excluded from production reports.
    /// </summary>
    public bool IsPracticeMode { get; set; }

    // =========================================================================
    // Schedule Properties
    // =========================================================================

    /// <summary>
    /// Planned exercise date. Required.
    /// </summary>
    public DateOnly ScheduledDate { get; set; }

    /// <summary>
    /// Planned start time. Optional.
    /// </summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>
    /// Planned end time. Must be after StartTime if both are specified.
    /// </summary>
    public TimeOnly? EndTime { get; set; }

    /// <summary>
    /// IANA time zone identifier (e.g., "America/Chicago").
    /// </summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// Exercise location. Max 500 characters.
    /// </summary>
    public string? Location { get; set; }

    // =========================================================================
    // Foreign Keys
    // =========================================================================

    /// <summary>
    /// Owning organization.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Currently active MSEL version. Null if no MSEL is active.
    /// </summary>
    public Guid? ActiveMselId { get; set; }

    // =========================================================================
    // Status Transition Audit Properties
    // =========================================================================

    /// <summary>
    /// UTC timestamp when exercise was activated (Draft → Active).
    /// </summary>
    public DateTime? ActivatedAt { get; set; }

    /// <summary>
    /// ApplicationUser ID who activated the exercise.
    /// </summary>
    public string? ActivatedBy { get; set; }

    /// <summary>
    /// UTC timestamp when exercise was completed (Active/Paused → Completed).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// ApplicationUser ID who completed the exercise.
    /// </summary>
    public string? CompletedBy { get; set; }

    /// <summary>
    /// UTC timestamp when exercise was archived (Completed → Archived).
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>
    /// ApplicationUser ID who archived the exercise.
    /// </summary>
    public string? ArchivedBy { get; set; }

    /// <summary>
    /// True if the exercise has ever been published (left Draft status).
    /// Once true, never set back to false. Used to determine delete eligibility.
    /// Exercises that have never been published can be deleted by their creator.
    /// </summary>
    public bool HasBeenPublished { get; set; }

    /// <summary>
    /// Status before archiving. Used to restore exercise to correct state.
    /// Null if exercise has never been archived.
    /// </summary>
    public ExerciseStatus? PreviousStatus { get; set; }

    // =========================================================================
    // Exercise Clock Properties (Updated During Conduct)
    // =========================================================================

    /// <summary>
    /// Current state of the exercise clock.
    /// </summary>
    public ExerciseClockState ClockState { get; set; } = ExerciseClockState.Stopped;

    /// <summary>
    /// UTC timestamp when the clock was last started.
    /// Used to calculate elapsed time when clock is running.
    /// </summary>
    public DateTime? ClockStartedAt { get; set; }

    /// <summary>
    /// Accumulated elapsed time before the current running period.
    /// Updated when clock is paused to preserve total elapsed time.
    /// </summary>
    public TimeSpan? ClockElapsedBeforePause { get; set; }

    /// <summary>
    /// ApplicationUser ID of who last started the clock.
    /// </summary>
    public string? ClockStartedBy { get; set; }

    // =========================================================================
    // Timing Configuration Properties
    // =========================================================================

    /// <summary>
    /// How injects transition to Ready status during conduct.
    /// </summary>
    public DeliveryMode DeliveryMode { get; set; } = DeliveryMode.ClockDriven;

    /// <summary>
    /// How exercise time relates to story/scenario time.
    /// </summary>
    public TimelineMode TimelineMode { get; set; } = TimelineMode.RealTime;

    /// <summary>
    /// Time compression ratio. Only used when TimelineMode = Compressed.
    /// Example: 4.0 means 1 real minute = 4 story minutes.
    /// Valid range: 0.1 to 60.0
    /// </summary>
    public decimal? TimeScale { get; set; }

    // =========================================================================
    // Exercise Settings (S03-S05)
    // =========================================================================

    /// <summary>
    /// Clock speed multiplier for exercise conduct.
    /// 1.0 = real-time, 2.0 = 2x speed, etc.
    /// Valid range: 0.5 to 20.0. Default: 1.0 (real-time)
    /// </summary>
    public decimal ClockMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Whether injects should automatically fire at their scheduled time.
    /// When false, injects remain pending until manually fired.
    /// Default: false (manual fire)
    /// </summary>
    public bool AutoFireEnabled { get; set; } = false;

    /// <summary>
    /// Whether to show confirmation dialog before firing an inject.
    /// Default: true (show confirmation)
    /// </summary>
    public bool ConfirmFireInject { get; set; } = true;

    /// <summary>
    /// Whether to show confirmation dialog before skipping an inject.
    /// Default: true (show confirmation)
    /// </summary>
    public bool ConfirmSkipInject { get; set; } = true;

    /// <summary>
    /// Whether to show confirmation dialog for clock control actions (start/pause/stop).
    /// Default: true (show confirmation)
    /// </summary>
    public bool ConfirmClockControl { get; set; } = true;

    // =========================================================================
    // Governance Properties (Inject Approval Workflow)
    // =========================================================================

    /// <summary>
    /// Whether inject approval workflow is enabled for this exercise.
    /// When true, injects must go through Draft → Submitted → Approved workflow.
    /// Default value depends on organization's ApprovalPolicy setting.
    /// </summary>
    public bool RequireInjectApproval { get; set; } = false;

    /// <summary>
    /// If true, an Administrator has overridden the organization's "Required" policy
    /// to disable approval for this specific exercise.
    /// </summary>
    public bool ApprovalPolicyOverridden { get; set; } = false;

    /// <summary>
    /// Optional reason provided when admin overrode the approval policy.
    /// </summary>
    public string? ApprovalOverrideReason { get; set; }

    /// <summary>
    /// User who overrode the approval policy. Null if not overridden.
    /// </summary>
    public string? ApprovalOverriddenById { get; set; }

    /// <summary>
    /// When the approval policy was overridden. Null if not overridden.
    /// </summary>
    public DateTime? ApprovalOverriddenAt { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The organization that owns this exercise.
    /// </summary>
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// The currently active MSEL (if any).
    /// </summary>
    public Msel? ActiveMsel { get; set; }

    /// <summary>
    /// All MSEL versions for this exercise.
    /// </summary>
    public ICollection<Msel> Msels { get; set; } = new List<Msel>();

    /// <summary>
    /// Exercise phases/segments.
    /// </summary>
    public ICollection<Phase> Phases { get; set; } = new List<Phase>();

    /// <summary>
    /// Exercise participants and their roles.
    /// </summary>
    public ICollection<ExerciseParticipant> Participants { get; set; } = new List<ExerciseParticipant>();

    /// <summary>
    /// Exercise objectives for evaluation.
    /// </summary>
    public ICollection<Objective> Objectives { get; set; } = new List<Objective>();

    /// <summary>
    /// Evaluator observations recorded during exercise conduct.
    /// </summary>
    public ICollection<Observation> Observations { get; set; } = new List<Observation>();

    /// <summary>
    /// ApplicationUser who last started the clock (if any).
    /// </summary>
    public ApplicationUser? ClockStartedByUser { get; set; }

    /// <summary>
    /// ApplicationUser who activated the exercise (if any).
    /// </summary>
    public ApplicationUser? ActivatedByUser { get; set; }

    /// <summary>
    /// ApplicationUser who completed the exercise (if any).
    /// </summary>
    public ApplicationUser? CompletedByUser { get; set; }

    /// <summary>
    /// ApplicationUser who archived the exercise (if any).
    /// </summary>
    public ApplicationUser? ArchivedByUser { get; set; }

    /// <summary>
    /// Clock events recorded during exercise conduct (start, pause, stop).
    /// Used for timeline analysis and pause history.
    /// </summary>
    public ICollection<ClockEvent> ClockEvents { get; set; } = new List<ClockEvent>();

    /// <summary>
    /// Core capabilities specifically targeted for evaluation in this exercise.
    /// </summary>
    public ICollection<ExerciseTargetCapability> TargetCapabilities { get; set; } = new List<ExerciseTargetCapability>();

    /// <summary>
    /// ApplicationUser who overrode the approval policy (if any).
    /// </summary>
    public ApplicationUser? ApprovalOverriddenByUser { get; set; }

    /// <summary>
    /// Capability targets defined for EEG evaluation in this exercise.
    /// Each target represents a measurable performance threshold for a capability.
    /// </summary>
    public ICollection<CapabilityTarget> CapabilityTargets { get; set; } = new List<CapabilityTarget>();

    /// <summary>
    /// Photos captured during exercise conduct.
    /// </summary>
    public ICollection<ExercisePhoto> Photos { get; set; } = new List<ExercisePhoto>();
}
