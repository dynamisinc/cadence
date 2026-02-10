using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.BulkParticipantImport.Models.Entities;

/// <summary>
/// Individual row result from a bulk participant import.
/// Tracks the classification, processing outcome, and any error details for each row.
/// </summary>
public class BulkImportRowResult : BaseEntity
{
    /// <summary>
    /// The parent import record.
    /// </summary>
    public Guid BulkImportRecordId { get; set; }

    /// <summary>
    /// The 1-based row number from the uploaded file.
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// The email address from this row.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// The exercise role specified for this row.
    /// </summary>
    public string? ExerciseRole { get; set; }

    /// <summary>
    /// The display name from this row, if provided.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// How the system classified this row (Assign, Update, Invite, Error).
    /// </summary>
    public ParticipantClassification Classification { get; set; }

    /// <summary>
    /// Processing outcome for this row.
    /// </summary>
    public BulkImportRowStatus Status { get; set; }

    /// <summary>
    /// Error or detail message explaining the outcome.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// For Update classification: the previous exercise role before the update.
    /// </summary>
    public string? PreviousExerciseRole { get; set; }

    // Navigation properties
    public BulkImportRecord BulkImportRecord { get; set; } = null!;
}
