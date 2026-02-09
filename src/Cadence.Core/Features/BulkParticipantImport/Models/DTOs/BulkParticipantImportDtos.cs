using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.BulkParticipantImport.Models.DTOs;

/// <summary>
/// Result of parsing and analyzing an uploaded participant file.
/// </summary>
public record FileParseResult
{
    /// <summary>Session ID for the import flow.</summary>
    public Guid SessionId { get; init; }

    /// <summary>Original file name.</summary>
    public string FileName { get; init; } = null!;

    /// <summary>Total data rows parsed (excluding header).</summary>
    public int TotalRows { get; init; }

    /// <summary>Columns detected and mapped.</summary>
    public IReadOnlyList<ColumnMapping> ColumnMappings { get; init; } = [];

    /// <summary>Parsed rows with validation results.</summary>
    public IReadOnlyList<ParsedParticipantRow> Rows { get; init; } = [];

    /// <summary>File-level warnings (e.g., multiple columns matched same field).</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>File-level errors (e.g., missing required columns).</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Whether the file was parsed successfully (no file-level errors).</summary>
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// Mapping of a detected column header to a known field.
/// </summary>
public record ColumnMapping
{
    /// <summary>The original column header from the file.</summary>
    public string OriginalHeader { get; init; } = null!;

    /// <summary>The normalized field name it maps to.</summary>
    public string MappedField { get; init; } = null!;

    /// <summary>The column index in the file.</summary>
    public int ColumnIndex { get; init; }
}

/// <summary>
/// A single parsed row from the participant file with validation state.
/// </summary>
public record ParsedParticipantRow
{
    /// <summary>1-based row number from the file.</summary>
    public int RowNumber { get; init; }

    /// <summary>Email address.</summary>
    public string Email { get; init; } = null!;

    /// <summary>Exercise role as parsed from the file.</summary>
    public string ExerciseRole { get; init; } = null!;

    /// <summary>Normalized exercise role enum value, if valid.</summary>
    public ExerciseRole? NormalizedExerciseRole { get; init; }

    /// <summary>Display name, if provided.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Organization role, if provided.</summary>
    public string? OrganizationRole { get; init; }

    /// <summary>Normalized org role enum value, if valid.</summary>
    public OrgRole? NormalizedOrgRole { get; init; }

    /// <summary>Row-level validation errors.</summary>
    public IReadOnlyList<string> ValidationErrors { get; init; } = [];

    /// <summary>Whether this row passed validation.</summary>
    public bool IsValid => ValidationErrors.Count == 0;
}

/// <summary>
/// Classification result for a single parsed row, including context about the action.
/// </summary>
public record ClassifiedParticipantRow
{
    /// <summary>The original parsed row data.</summary>
    public ParsedParticipantRow ParsedRow { get; init; } = null!;

    /// <summary>System classification of what action to take.</summary>
    public ParticipantClassification Classification { get; init; }

    /// <summary>Human-readable classification label.</summary>
    public string ClassificationLabel { get; init; } = null!;

    /// <summary>The user ID if this is an existing user.</summary>
    public string? ExistingUserId { get; init; }

    /// <summary>The user's display name if known.</summary>
    public string? ExistingDisplayName { get; init; }

    /// <summary>For Update: the current exercise role.</summary>
    public ExerciseRole? CurrentExerciseRole { get; init; }

    /// <summary>Whether this is a role change (Update classification).</summary>
    public bool IsRoleChange { get; init; }

    /// <summary>Whether the email has a pending org invitation already.</summary>
    public bool HasPendingInvitation { get; init; }

    /// <summary>Whether this is a new Cadence account (Invite classification).</summary>
    public bool IsNewAccount { get; init; }

    /// <summary>Classification-specific notes or warnings.</summary>
    public IReadOnlyList<string> Notes { get; init; } = [];

    /// <summary>Error message if classification is Error.</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Preview of the import showing classifications for all rows.
/// </summary>
public record ImportPreviewResult
{
    /// <summary>Session ID for the import flow.</summary>
    public Guid SessionId { get; init; }

    /// <summary>Total rows in the file.</summary>
    public int TotalRows { get; init; }

    /// <summary>Number of rows classified as Assign.</summary>
    public int AssignCount { get; init; }

    /// <summary>Number of rows classified as Update.</summary>
    public int UpdateCount { get; init; }

    /// <summary>Number of rows classified as Invite.</summary>
    public int InviteCount { get; init; }

    /// <summary>Number of rows classified as Error.</summary>
    public int ErrorCount { get; init; }

    /// <summary>Classified rows with full details.</summary>
    public IReadOnlyList<ClassifiedParticipantRow> Rows { get; init; } = [];

    /// <summary>Whether there are any non-error rows to process.</summary>
    public bool HasProcessableRows => AssignCount + UpdateCount + InviteCount > 0;
}

/// <summary>
/// Result of confirming and executing a bulk import.
/// </summary>
public record BulkImportResult
{
    /// <summary>The persisted import record ID.</summary>
    public Guid ImportRecordId { get; init; }

    /// <summary>Number of participants immediately assigned.</summary>
    public int AssignedCount { get; init; }

    /// <summary>Number of participant roles updated.</summary>
    public int UpdatedCount { get; init; }

    /// <summary>Number of organization invitations sent.</summary>
    public int InvitedCount { get; init; }

    /// <summary>Number of rows that failed.</summary>
    public int ErrorCount { get; init; }

    /// <summary>Number of rows skipped (no change needed).</summary>
    public int SkippedCount { get; init; }

    /// <summary>Details for each processed row.</summary>
    public IReadOnlyList<ImportRowOutcome> RowOutcomes { get; init; } = [];
}

/// <summary>
/// Outcome of processing a single import row.
/// </summary>
public record ImportRowOutcome
{
    /// <summary>1-based row number.</summary>
    public int RowNumber { get; init; }

    /// <summary>Email address.</summary>
    public string Email { get; init; } = null!;

    /// <summary>Exercise role.</summary>
    public string ExerciseRole { get; init; } = null!;

    /// <summary>Classification applied.</summary>
    public ParticipantClassification Classification { get; init; }

    /// <summary>Processing status.</summary>
    public BulkImportRowStatus Status { get; init; }

    /// <summary>Error or detail message.</summary>
    public string? Message { get; init; }
}

/// <summary>
/// DTO for a bulk import record in history views.
/// </summary>
public record BulkImportRecordDto
{
    public Guid Id { get; init; }
    public Guid ExerciseId { get; init; }
    public string ImportedById { get; init; } = null!;
    public string ImportedByName { get; init; } = null!;
    public DateTime ImportedAt { get; init; }
    public string FileName { get; init; } = null!;
    public int TotalRows { get; init; }
    public int AssignedCount { get; init; }
    public int UpdatedCount { get; init; }
    public int InvitedCount { get; init; }
    public int ErrorCount { get; init; }
    public int SkippedCount { get; init; }
}

/// <summary>
/// DTO for an individual row result in import details.
/// </summary>
public record BulkImportRowResultDto
{
    public Guid Id { get; init; }
    public int RowNumber { get; init; }
    public string Email { get; init; } = null!;
    public string? ExerciseRole { get; init; }
    public string? DisplayName { get; init; }
    public ParticipantClassification Classification { get; init; }
    public BulkImportRowStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public string? PreviousExerciseRole { get; init; }
}

/// <summary>
/// DTO for a pending exercise assignment with invitation status.
/// </summary>
public record PendingExerciseAssignmentDto
{
    public Guid Id { get; init; }
    public Guid OrganizationInviteId { get; init; }
    public string Email { get; init; } = null!;
    public string ExerciseRole { get; init; } = null!;
    public string? DisplayName { get; init; }
    public PendingAssignmentStatus Status { get; init; }
    public string InvitationStatus { get; init; } = null!;
    public DateTime? InvitationExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
