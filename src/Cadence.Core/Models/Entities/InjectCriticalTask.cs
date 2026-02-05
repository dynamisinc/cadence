namespace Cadence.Core.Models.Entities;

/// <summary>
/// Junction table for many-to-many relationship between Injects and Critical Tasks.
/// An inject can test multiple critical tasks, and a critical task can be tested
/// by multiple injects in the MSEL.
/// </summary>
public class InjectCriticalTask
{
    /// <summary>
    /// The inject that tests the critical task.
    /// </summary>
    public Guid InjectId { get; set; }

    /// <summary>
    /// The critical task being tested by the inject.
    /// </summary>
    public Guid CriticalTaskId { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The inject entity.
    /// </summary>
    public Inject Inject { get; set; } = null!;

    /// <summary>
    /// The critical task entity.
    /// </summary>
    public CriticalTask CriticalTask { get; set; } = null!;
}
