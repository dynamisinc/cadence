using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.BulkParticipantImport.Models.Entities;

/// <summary>
/// Persisted record of a bulk participant import operation.
/// Tracks summary counts and links to individual row results.
/// </summary>
public class BulkImportRecord : BaseEntity
{
    /// <summary>
    /// The exercise that participants were imported into.
    /// </summary>
    public Guid ExerciseId { get; set; }

    /// <summary>
    /// The user who performed the import.
    /// </summary>
    public string ImportedById { get; set; } = null!;

    /// <summary>
    /// When the import was executed.
    /// </summary>
    public DateTime ImportedAt { get; set; }

    /// <summary>
    /// Original uploaded file name.
    /// </summary>
    public string FileName { get; set; } = null!;

    /// <summary>
    /// Total number of data rows in the uploaded file.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Number of participants immediately assigned to the exercise.
    /// </summary>
    public int AssignedCount { get; set; }

    /// <summary>
    /// Number of existing participants whose roles were updated.
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// Number of organization invitations sent with pending exercise assignments.
    /// </summary>
    public int InvitedCount { get; set; }

    /// <summary>
    /// Number of rows that failed processing.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Number of rows skipped (e.g., update with no change).
    /// </summary>
    public int SkippedCount { get; set; }

    // Navigation properties
    public Exercise Exercise { get; set; } = null!;
    public ApplicationUser ImportedBy { get; set; } = null!;
    public ICollection<BulkImportRowResult> RowResults { get; set; } = new List<BulkImportRowResult>();
    public ICollection<PendingExerciseAssignment> PendingAssignments { get; set; } = new List<PendingExerciseAssignment>();
}
