using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Assignments.Models.DTOs;

/// <summary>
/// Response containing user's assignments grouped by status.
/// </summary>
public record MyAssignmentsResponse
{
    /// <summary>
    /// Exercises currently in conduct (Active/Paused status with clock activity).
    /// </summary>
    public List<AssignmentDto> Active { get; init; } = new();

    /// <summary>
    /// Exercises scheduled for the future (Draft status or future scheduled date).
    /// </summary>
    public List<AssignmentDto> Upcoming { get; init; } = new();

    /// <summary>
    /// Exercises that have finished conduct (Completed/Archived status).
    /// </summary>
    public List<AssignmentDto> Completed { get; init; } = new();
}

/// <summary>
/// DTO representing a single assignment with exercise details.
/// </summary>
public record AssignmentDto
{
    /// <summary>
    /// Exercise unique identifier.
    /// </summary>
    public Guid ExerciseId { get; init; }

    /// <summary>
    /// Exercise name.
    /// </summary>
    public string ExerciseName { get; init; } = string.Empty;

    /// <summary>
    /// User's HSEEP role in this exercise.
    /// </summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// Current exercise status.
    /// </summary>
    public string ExerciseStatus { get; init; } = string.Empty;

    /// <summary>
    /// Exercise type (TTX, FE, FSE, etc.).
    /// </summary>
    public string ExerciseType { get; init; } = string.Empty;

    /// <summary>
    /// Scheduled date for the exercise.
    /// </summary>
    public DateOnly ScheduledDate { get; init; }

    /// <summary>
    /// Scheduled start time (if set).
    /// </summary>
    public TimeOnly? StartTime { get; init; }

    /// <summary>
    /// Current state of the exercise clock.
    /// </summary>
    public string? ClockState { get; init; }

    /// <summary>
    /// Total elapsed time in seconds (for active exercises).
    /// </summary>
    public double? ElapsedSeconds { get; init; }

    /// <summary>
    /// When the exercise was completed.
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// When the user was assigned to this exercise.
    /// </summary>
    public DateTime AssignedAt { get; init; }

    /// <summary>
    /// Total number of injects in the exercise.
    /// </summary>
    public int TotalInjects { get; init; }

    /// <summary>
    /// Number of injects that have been fired.
    /// </summary>
    public int FiredInjects { get; init; }

    /// <summary>
    /// Number of injects ready to fire (for Controllers).
    /// </summary>
    public int ReadyInjects { get; init; }

    /// <summary>
    /// Exercise location (if set).
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Exercise time zone.
    /// </summary>
    public string TimeZoneId { get; init; } = "UTC";
}
