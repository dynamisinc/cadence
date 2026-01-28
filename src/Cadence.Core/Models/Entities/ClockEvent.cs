namespace Cadence.Core.Models.Entities;

/// <summary>
/// Types of clock events that can occur during exercise conduct.
/// </summary>
public enum ClockEventType
{
    /// <summary>Clock was started (Stopped → Running or Paused → Running).</summary>
    Started,

    /// <summary>Clock was paused (Running → Paused).</summary>
    Paused,

    /// <summary>Clock was stopped (any state → Stopped, typically when exercise ends).</summary>
    Stopped
}

/// <summary>
/// Records a clock state change event during exercise conduct.
/// Used for timeline analysis and pause history tracking.
/// This is an audit record and does not use soft-delete.
/// </summary>
public class ClockEvent
{
    /// <summary>
    /// Unique identifier for this event.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The exercise this event belongs to.
    /// </summary>
    public Guid ExerciseId { get; set; }

    /// <summary>
    /// Type of clock event (Started, Paused, Stopped).
    /// </summary>
    public ClockEventType EventType { get; set; }

    /// <summary>
    /// UTC timestamp when this event occurred.
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// ApplicationUser ID who triggered this event.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Total accumulated elapsed time when this event occurred.
    /// </summary>
    public TimeSpan ElapsedTimeAtEvent { get; set; }

    /// <summary>
    /// Optional notes or reason for the event (e.g., pause reason).
    /// </summary>
    public string? Notes { get; set; }

    // =========================================================================
    // Navigation Properties
    // =========================================================================

    /// <summary>
    /// The exercise this event belongs to.
    /// </summary>
    public Exercise Exercise { get; set; } = null!;

    /// <summary>
    /// The user who triggered this event (if any).
    /// </summary>
    public ApplicationUser? User { get; set; }
}
