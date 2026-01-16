namespace Cadence.Core.Features.Msel.Models.DTOs;

/// <summary>
/// DTO for MSEL summary information.
/// Provides an overview of the MSEL including inject statistics.
/// </summary>
public record MselSummaryDto
{
    /// <summary>
    /// MSEL ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// MSEL name/version identifier.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// MSEL description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Version number.
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// Whether this is the active MSEL for the exercise.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Parent exercise ID.
    /// </summary>
    public Guid ExerciseId { get; init; }

    // =========================================================================
    // Inject Statistics
    // =========================================================================

    /// <summary>
    /// Total number of injects in this MSEL.
    /// </summary>
    public int TotalInjects { get; init; }

    /// <summary>
    /// Number of injects with Pending status.
    /// </summary>
    public int PendingCount { get; init; }

    /// <summary>
    /// Number of injects with Fired status.
    /// </summary>
    public int FiredCount { get; init; }

    /// <summary>
    /// Number of injects with Skipped status.
    /// </summary>
    public int SkippedCount { get; init; }

    /// <summary>
    /// Percentage of injects that have been fired or skipped (0-100).
    /// </summary>
    public int CompletionPercentage { get; init; }

    // =========================================================================
    // Metadata
    // =========================================================================

    /// <summary>
    /// Number of phases defined in the exercise.
    /// </summary>
    public int PhaseCount { get; init; }

    /// <summary>
    /// Number of objectives defined in the exercise.
    /// </summary>
    public int ObjectiveCount { get; init; }

    /// <summary>
    /// Last time any inject in this MSEL was modified.
    /// </summary>
    public DateTime? LastModifiedAt { get; init; }

    /// <summary>
    /// User who last modified an inject in this MSEL.
    /// </summary>
    public string? LastModifiedByName { get; init; }

    /// <summary>
    /// MSEL creation date.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// MSEL last updated date.
    /// </summary>
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// DTO for basic MSEL information (without statistics).
/// </summary>
public record MselDto
{
    /// <summary>
    /// MSEL ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// MSEL name/version identifier.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// MSEL description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Version number.
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// Whether this is the active MSEL for the exercise.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Parent exercise ID.
    /// </summary>
    public Guid ExerciseId { get; init; }

    /// <summary>
    /// Number of injects in this MSEL.
    /// </summary>
    public int InjectCount { get; init; }

    /// <summary>
    /// MSEL creation date.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// MSEL last updated date.
    /// </summary>
    public DateTime UpdatedAt { get; init; }
}
