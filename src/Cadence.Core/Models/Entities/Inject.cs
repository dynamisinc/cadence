namespace Cadence.Core.Models.Entities;

/// <summary>
/// Inject entity - a single event, message, or piece of information introduced
/// into an exercise to drive player actions.
/// Supports dual time tracking: ScheduledTime (wall clock) and ScenarioTime (in-story time).
/// </summary>
public class Inject : BaseEntity
{
    // =========================================================================
    // Core Properties
    // =========================================================================

    /// <summary>
    /// Sequential number within the MSEL. Unique within MSEL, auto-generated.
    /// </summary>
    public int InjectNumber { get; set; }

    /// <summary>
    /// Brief descriptive name. Required, 1-200 characters.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Full inject content. Required, 1-4000 characters.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    // =========================================================================
    // Time Properties (Dual Time Tracking)
    // =========================================================================

    /// <summary>
    /// Planned delivery time (wall clock). Required.
    /// </summary>
    public TimeOnly ScheduledTime { get; set; }

    /// <summary>
    /// Elapsed time from exercise start when inject should be delivered.
    /// Used in ClockDriven mode for auto-Ready functionality.
    /// Format: TimeSpan from 00:00:00 (e.g., 00:30:00 = 30 minutes into exercise).
    /// </summary>
    public TimeSpan? DeliveryTime { get; set; }

    /// <summary>
    /// In-story day number (1-99). Null if scenario time not used.
    /// </summary>
    public int? ScenarioDay { get; set; }

    /// <summary>
    /// In-story time. Null if scenario time not used.
    /// </summary>
    public TimeOnly? ScenarioTime { get; set; }

    // =========================================================================
    // Targeting Properties
    // =========================================================================

    /// <summary>
    /// Player/role receiving the inject. Required, max 200 characters.
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Simulated origin of the inject. Max 200 characters.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// How the inject is delivered to players (LEGACY - will be migrated to DeliveryMethodId).
    /// </summary>
    public DeliveryMethod? DeliveryMethod { get; set; }

    /// <summary>
    /// Foreign key to the delivery method lookup table.
    /// Replaces the DeliveryMethod enum.
    /// </summary>
    public Guid? DeliveryMethodId { get; set; }

    /// <summary>
    /// Free-text delivery method when "Other" is selected. Max 100 characters.
    /// Only used when the selected delivery method has IsOther=true.
    /// </summary>
    public string? DeliveryMethodOther { get; set; }

    // =========================================================================
    // Organization Properties
    // =========================================================================

    /// <summary>
    /// Type of inject (Standard, Contingency, Adaptive, Complexity).
    /// </summary>
    public InjectType InjectType { get; set; } = InjectType.Standard;

    /// <summary>
    /// Current status (Pending, Fired, Skipped).
    /// </summary>
    public InjectStatus Status { get; set; } = InjectStatus.Pending;

    /// <summary>
    /// Display order within the MSEL.
    /// </summary>
    public int Sequence { get; set; }

    // =========================================================================
    // Branching Properties
    // =========================================================================

    /// <summary>
    /// Parent inject for branching scenarios.
    /// </summary>
    public Guid? ParentInjectId { get; set; }

    /// <summary>
    /// Describes when to fire this branch inject. Max 500 characters.
    /// </summary>
    public string? FireCondition { get; set; }

    // =========================================================================
    // Import & Excel Properties
    // =========================================================================

    /// <summary>
    /// Original inject ID from imported source (e.g., Excel row ID). Max 50 characters.
    /// Used for traceability when importing MSELs from external systems.
    /// </summary>
    public string? SourceReference { get; set; }

    /// <summary>
    /// Priority level (1-5 scale): 1=Critical, 2=High, 3=Medium, 4=Low, 5=Info.
    /// Nullable to allow injects without assigned priority.
    /// </summary>
    public int? Priority { get; set; }

    /// <summary>
    /// How this inject should be triggered during the exercise.
    /// </summary>
    public TriggerType TriggerType { get; set; } = TriggerType.Manual;

    /// <summary>
    /// Name of the specific controller responsible for this inject. Max 200 characters.
    /// Different from FiredBy (which tracks who actually fired it).
    /// </summary>
    public string? ResponsibleController { get; set; }

    /// <summary>
    /// Name of the physical location for this inject. Max 200 characters.
    /// </summary>
    public string? LocationName { get; set; }

    /// <summary>
    /// Type or category of location (e.g., "EOC", "Field", "Hospital"). Max 100 characters.
    /// </summary>
    public string? LocationType { get; set; }

    /// <summary>
    /// Track or agency grouping for multi-agency exercises (e.g., "Fire", "EMS", "Police"). Max 100 characters.
    /// </summary>
    public string? Track { get; set; }

    // =========================================================================
    // Supplemental Properties
    // =========================================================================

    /// <summary>
    /// Anticipated player response. Max 2000 characters.
    /// </summary>
    public string? ExpectedAction { get; set; }

    /// <summary>
    /// Private guidance for the Controller. Max 2000 characters.
    /// </summary>
    public string? ControllerNotes { get; set; }

    // =========================================================================
    // Conduct Properties (Updated During Exercise)
    // =========================================================================

    /// <summary>
    /// UTC timestamp when the inject transitioned to Ready status.
    /// Null if not yet ready. Set automatically when clock reaches DeliveryTime.
    /// </summary>
    public DateTime? ReadyAt { get; set; }

    /// <summary>
    /// Actual UTC delivery timestamp. Set when fired.
    /// </summary>
    public DateTime? FiredAt { get; set; }

    /// <summary>
    /// Controller who fired the inject.
    /// </summary>
    public Guid? FiredBy { get; set; }

    /// <summary>
    /// UTC timestamp when skipped. Set when skipped.
    /// </summary>
    public DateTime? SkippedAt { get; set; }

    /// <summary>
    /// User who skipped the inject.
    /// </summary>
    public Guid? SkippedBy { get; set; }

    /// <summary>
    /// Reason for skipping. Max 500 characters.
    /// </summary>
    public string? SkipReason { get; set; }

    // =========================================================================
    // Foreign Keys
    // =========================================================================

    /// <summary>
    /// Parent MSEL.
    /// </summary>
    public Guid MselId { get; set; }

    /// <summary>
    /// Exercise phase (optional).
    /// </summary>
    public Guid? PhaseId { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The MSEL this inject belongs to.
    /// </summary>
    public Msel Msel { get; set; } = null!;

    /// <summary>
    /// The phase this inject is assigned to (if any).
    /// </summary>
    public Phase? Phase { get; set; }

    /// <summary>
    /// Parent inject for branching (if any).
    /// </summary>
    public Inject? ParentInject { get; set; }

    /// <summary>
    /// Child injects (branches).
    /// </summary>
    public ICollection<Inject> ChildInjects { get; set; } = new List<Inject>();

    /// <summary>
    /// User who fired this inject (if fired).
    /// May be null if inject not fired or if the user has been soft-deleted.
    /// For historical reports, use IgnoreQueryFilters() to include deleted users.
    /// </summary>
    public User? FiredByUser { get; set; }

    /// <summary>
    /// User who skipped this inject (if skipped).
    /// May be null if inject not skipped or if the user has been soft-deleted.
    /// For historical reports, use IgnoreQueryFilters() to include deleted users.
    /// </summary>
    public User? SkippedByUser { get; set; }

    /// <summary>
    /// Observations linked to this inject.
    /// </summary>
    public ICollection<Observation> Observations { get; set; } = new List<Observation>();

    /// <summary>
    /// Junction entities linking this inject to objectives.
    /// </summary>
    public ICollection<InjectObjective> InjectObjectives { get; set; } = new List<InjectObjective>();

    /// <summary>
    /// Delivery method lookup (replaces DeliveryMethod enum).
    /// </summary>
    public DeliveryMethodLookup? DeliveryMethodLookup { get; set; }

    /// <summary>
    /// Expected outcomes for this inject.
    /// </summary>
    public ICollection<ExpectedOutcome> ExpectedOutcomes { get; set; } = new List<ExpectedOutcome>();
}
