namespace Cadence.Core.Models.Entities;

// =============================================================================
// Exercise Enums
// =============================================================================

/// <summary>
/// Types of exercises per HSEEP classification.
/// </summary>
public enum ExerciseType
{
    /// <summary>Table Top Exercise - Discussion-based scenario walkthrough.</summary>
    TTX,

    /// <summary>Functional Exercise - Simulated operations in controlled environment.</summary>
    FE,

    /// <summary>Full-Scale Exercise - Actual deployment of resources.</summary>
    FSE,

    /// <summary>Computer-Aided Exercise - Technology-driven simulation.</summary>
    CAX,

    /// <summary>Hybrid Exercise - Combination of multiple types.</summary>
    Hybrid
}

/// <summary>
/// Exercise lifecycle status.
/// </summary>
public enum ExerciseStatus
{
    /// <summary>Initial creation state. Setup phase - can edit everything.</summary>
    Draft,

    /// <summary>Currently in conduct. Clock can run, injects can fire.</summary>
    Active,

    /// <summary>Temporarily stopped. Clock paused, can resume or revert to draft.</summary>
    Paused,

    /// <summary>Conduct finished. Read-only except observations.</summary>
    Completed,

    /// <summary>Read-only historical record. Fully read-only.</summary>
    Archived
}

// =============================================================================
// Inject Enums
// =============================================================================

/// <summary>
/// Types of injects based on their purpose.
/// </summary>
public enum InjectType
{
    /// <summary>Standard - Delivered at scheduled time.</summary>
    Standard,

    /// <summary>Contingency - Used if players deviate from expected path.</summary>
    Contingency,

    /// <summary>Adaptive - Branch based on player decision.</summary>
    Adaptive,

    /// <summary>Complexity - Increase difficulty for advanced players.</summary>
    Complexity
}

/// <summary>
/// HSEEP-compliant inject status values per FEMA PrepToolkit.
/// These statuses align with standard exercise management terminology
/// to ensure consistency with federal guidance and training materials.
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum InjectStatus
{
    /// <summary>
    /// Initial status during design and development phase.
    /// Inject is being authored and is not ready for review or use.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Event has been sent for review by Exercise Director.
    /// Awaiting approval before it can be scheduled for delivery.
    /// Only used when approval workflow is enabled.
    /// </summary>
    Submitted = 1,

    /// <summary>
    /// Event has been approved for use in the exercise.
    /// Director has reviewed and signed off on the content.
    /// Ready to be scheduled with a specific delivery time.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Approved event is ready and scheduled for a specific time.
    /// The inject has a scheduled delivery time and will appear
    /// in the Controller's queue when that time approaches.
    /// </summary>
    Synchronized = 3,

    /// <summary>
    /// Event has been delivered to players in real time.
    /// Controller has "fired" the inject - delivered the message
    /// via the specified delivery method (phone, email, radio, etc.).
    /// </summary>
    Released = 4,

    /// <summary>
    /// Event delivery confirmed, exercise has moved past this inject.
    /// The inject has been delivered and any expected player actions
    /// have occurred or the time window has passed.
    /// </summary>
    Complete = 5,

    /// <summary>
    /// A synchronized event that was cancelled before delivery.
    /// The inject was scheduled but was skipped during conduct,
    /// typically due to time constraints or scenario changes.
    /// Requires a reason to be recorded for after-action review.
    /// </summary>
    Deferred = 6,

    /// <summary>
    /// Event should be ignored but remains in MSEL for audit trail.
    /// Used for injects that were removed during planning but need
    /// to be retained for historical record. Soft-delete pattern.
    /// </summary>
    Obsolete = 7
}

/// <summary>
/// Methods for delivering injects to players.
/// </summary>
public enum DeliveryMethod
{
    /// <summary>Spoken directly to player.</summary>
    Verbal,

    /// <summary>Simulated phone call.</summary>
    Phone,

    /// <summary>Simulated email.</summary>
    Email,

    /// <summary>Radio communication.</summary>
    Radio,

    /// <summary>Paper document.</summary>
    Written,

    /// <summary>CAX/simulation input.</summary>
    Simulation,

    /// <summary>Custom method.</summary>
    Other
}

/// <summary>
/// Trigger type for inject delivery - determines how the inject is activated.
/// </summary>
public enum TriggerType
{
    /// <summary>Controller manually fires the inject (default).</summary>
    Manual,

    /// <summary>Auto-fire at scheduled time (future feature).</summary>
    Scheduled,

    /// <summary>Fire on meeting conditions (future feature).</summary>
    Conditional
}

// =============================================================================
// Role Enums
// =============================================================================

/// <summary>
/// HSEEP-aligned roles for exercise participation.
/// Values start at 1 to support EF Core seeding (0 is not allowed for seed data PKs).
/// </summary>
public enum ExerciseRole
{
    /// <summary>System-wide configuration and user management.</summary>
    Administrator = 1,

    /// <summary>Full exercise management authority.</summary>
    ExerciseDirector = 2,

    /// <summary>Inject delivery and conduct management.</summary>
    Controller = 3,

    /// <summary>Observation recording for AAR.</summary>
    Evaluator = 4,

    /// <summary>Read-only exercise monitoring.</summary>
    Observer = 5
}

// =============================================================================
// Exercise Clock Enums
// =============================================================================

/// <summary>
/// State of the exercise clock during conduct.
/// </summary>
public enum ExerciseClockState
{
    /// <summary>Clock not started - exercise not yet in conduct.</summary>
    Stopped,

    /// <summary>Clock actively running - exercise in progress.</summary>
    Running,

    /// <summary>Clock temporarily paused - exercise on hold.</summary>
    Paused
}

/// <summary>
/// Delivery mode determines how injects transition to Ready status.
/// </summary>
public enum DeliveryMode
{
    /// <summary>
    /// Injects become Ready when exercise clock reaches DeliveryTime.
    /// </summary>
    ClockDriven = 0,

    /// <summary>
    /// Injects are fired manually by Controller in Sequence order.
    /// </summary>
    FacilitatorPaced = 1
}

/// <summary>
/// Timeline mode determines how exercise time relates to story time.
/// </summary>
public enum TimelineMode
{
    /// <summary>
    /// 1:1 ratio - exercise time matches wall clock.
    /// </summary>
    RealTime = 0,

    /// <summary>
    /// Story time advances faster than real time per TimeScale.
    /// </summary>
    Compressed = 1,

    /// <summary>
    /// No real-time clock; only Story Time is used.
    /// </summary>
    StoryOnly = 2
}

// =============================================================================
// Observation Enums
// =============================================================================

/// <summary>
/// HSEEP performance rating scale (P/S/M/U) for evaluator observations.
/// </summary>
public enum ObservationRating
{
    /// <summary>P - Performed without challenges. The targets/objectives were completed as expected.</summary>
    Performed,

    /// <summary>S - Performed with some difficulty. Minor issues noted but did not significantly impact achievement.</summary>
    Satisfactory,

    /// <summary>M - Performed with major difficulty. Significant issues impacted achievement of objectives.</summary>
    Marginal,

    /// <summary>U - Unable to be performed. The targets/objectives were not achieved.</summary>
    Unsatisfactory
}

// =============================================================================
// User/Authentication Enums
// =============================================================================

/// <summary>
/// System-level access roles determining application permissions.
/// These are distinct from HSEEP exercise roles (ExerciseRole enum).
/// </summary>
public enum SystemRole
{
    /// <summary>
    /// Standard user - can only access exercises they are assigned to.
    /// Cannot create exercises or manage users.
    /// </summary>
    User = 0,

    /// <summary>
    /// Can create exercises and manage exercises they create/own.
    /// Automatically becomes Director when creating an exercise.
    /// </summary>
    Manager = 1,

    /// <summary>
    /// Full system access - user management, all exercises, system settings.
    /// Can see and access ALL exercises for support/oversight purposes.
    /// </summary>
    Admin = 2
}

/// <summary>
/// User account status.
/// </summary>
public enum UserStatus
{
    /// <summary>Account is pending organization assignment.</summary>
    Pending = 0,

    /// <summary>Account is active and can authenticate.</summary>
    Active = 1,

    /// <summary>Account has been disabled and cannot authenticate.</summary>
    Disabled = 2
}

// =============================================================================
// Organization Enums
// =============================================================================

/// <summary>
/// Organization-level roles determining permissions within a specific organization.
/// These are distinct from system-level roles (SystemRole) and exercise-level roles (ExerciseRole).
/// </summary>
public enum OrgRole
{
    /// <summary>Full organization access - can manage users, settings, and all exercises.</summary>
    OrgAdmin = 1,

    /// <summary>Can create and manage exercises within the organization.</summary>
    OrgManager = 2,

    /// <summary>Can participate in assigned exercises within the organization.</summary>
    OrgUser = 3
}

/// <summary>
/// Organization lifecycle status.
/// </summary>
public enum OrgStatus
{
    /// <summary>Organization is active and operational.</summary>
    Active = 1,

    /// <summary>Organization is archived - read-only, hidden from non-admins.</summary>
    Archived = 2,

    /// <summary>Organization is inactive - completely hidden, data preserved.</summary>
    Inactive = 3
}

/// <summary>
/// Organization membership status.
/// </summary>
public enum MembershipStatus
{
    /// <summary>Membership is active.</summary>
    Active = 1,

    /// <summary>Membership is inactive.</summary>
    Inactive = 2
}

/// <summary>
/// Organization-level policy for inject approval workflow.
/// Determines default behavior and constraints for exercises.
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum ApprovalPolicy
{
    /// <summary>
    /// Approval workflow is not available.
    /// All injects move directly from Draft to Approved.
    /// Exercise-level toggle is hidden.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Exercise Directors can choose to enable approval per exercise.
    /// Approval is disabled by default for new exercises.
    /// Recommended for most organizations.
    /// </summary>
    Optional = 1,

    /// <summary>
    /// All exercises require inject approval workflow.
    /// Directors cannot disable approval.
    /// Administrators can override for specific exercises.
    /// </summary>
    Required = 2
}

// =============================================================================
// User Preferences Enums
// =============================================================================

/// <summary>
/// Theme preference for UI appearance.
/// </summary>
public enum ThemePreference
{
    /// <summary>Light theme - bright background, dark text.</summary>
    Light = 0,

    /// <summary>Dark theme - dark background, light text.</summary>
    Dark = 1,

    /// <summary>Follow operating system preference.</summary>
    System = 2
}

/// <summary>
/// Display density preference for UI spacing.
/// </summary>
public enum DisplayDensity
{
    /// <summary>Standard spacing - more whitespace, easier to read.</summary>
    Comfortable = 0,

    /// <summary>Reduced spacing - more information density.</summary>
    Compact = 1
}

/// <summary>
/// Time format preference for displaying times.
/// </summary>
public enum TimeFormat
{
    /// <summary>24-hour format (e.g., 14:30) - military time, EM standard.</summary>
    TwentyFourHour = 0,

    /// <summary>12-hour format (e.g., 2:30 PM).</summary>
    TwelveHour = 1
}
