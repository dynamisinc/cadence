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
    /// <summary>Initial creation state.</summary>
    Draft,

    /// <summary>Currently in conduct.</summary>
    Active,

    /// <summary>Conduct finished.</summary>
    Completed,

    /// <summary>Read-only historical record.</summary>
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
/// Inject delivery status during exercise conduct.
/// </summary>
public enum InjectStatus
{
    /// <summary>Not yet delivered.</summary>
    Pending,

    /// <summary>Delivered to players.</summary>
    Fired,

    /// <summary>Intentionally not delivered.</summary>
    Skipped
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
