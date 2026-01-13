namespace Cadence.Core.Models.Entities;

/// <summary>
/// HseepRole entity - Reference data for HSEEP-defined exercise roles.
/// This is seeded data that should not be modified by users.
///
/// Per HSEEP 2020, these are the standard roles for exercise participation:
/// - Administrator: System-wide configuration and user management
/// - ExerciseDirector: Full exercise management authority, Go/No-Go decisions
/// - Controller: Delivers injects, manages scenario flow
/// - Evaluator: Observes and documents player performance
/// - Observer: Watches without interfering
/// </summary>
public class HseepRole
{
    /// <summary>
    /// Unique identifier (matches ExerciseRole enum int value for consistency).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Role code/key (matches ExerciseRole enum name).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the role.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the role's responsibilities per HSEEP.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Display order for UI presentation.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this is a system-wide role (Administrator) or exercise-scoped.
    /// </summary>
    public bool IsSystemWide { get; set; }

    /// <summary>
    /// Whether this role can fire injects during exercise conduct.
    /// </summary>
    public bool CanFireInjects { get; set; }

    /// <summary>
    /// Whether this role can record observations/evaluations.
    /// </summary>
    public bool CanRecordObservations { get; set; }

    /// <summary>
    /// Whether this role can modify exercise configuration.
    /// </summary>
    public bool CanManageExercise { get; set; }

    /// <summary>
    /// Whether this role is active/usable.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
