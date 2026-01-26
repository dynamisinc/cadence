namespace Cadence.Core.Features.ExcelExport.Models.DTOs;

/// <summary>
/// Request to export MSEL data to Excel.
/// </summary>
public record ExportMselRequest
{
    /// <summary>
    /// Exercise ID to export.
    /// </summary>
    public required Guid ExerciseId { get; init; }

    /// <summary>
    /// Export format (xlsx, csv).
    /// </summary>
    public string Format { get; init; } = "xlsx";

    /// <summary>
    /// Whether to include header formatting (Excel only).
    /// </summary>
    public bool IncludeFormatting { get; init; } = true;

    /// <summary>
    /// Whether to include objectives worksheet (Excel only).
    /// </summary>
    public bool IncludeObjectives { get; init; } = true;

    /// <summary>
    /// Whether to include phases worksheet (Excel only).
    /// </summary>
    public bool IncludePhases { get; init; } = true;

    /// <summary>
    /// Whether to include conduct data (status, fired times).
    /// </summary>
    public bool IncludeConductData { get; init; }

    /// <summary>
    /// Custom filename (without extension).
    /// </summary>
    public string? Filename { get; init; }
}

/// <summary>
/// Result of an export operation.
/// </summary>
public record ExportResult
{
    /// <summary>
    /// The exported file bytes.
    /// </summary>
    public required byte[] Content { get; init; }

    /// <summary>
    /// The filename with extension.
    /// </summary>
    public required string Filename { get; init; }

    /// <summary>
    /// MIME type for the file.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Number of injects exported.
    /// </summary>
    public int InjectCount { get; init; }

    /// <summary>
    /// Number of phases exported.
    /// </summary>
    public int PhaseCount { get; init; }

    /// <summary>
    /// Number of objectives exported.
    /// </summary>
    public int ObjectiveCount { get; init; }
}

/// <summary>
/// Request to export observations to Excel.
/// </summary>
public record ExportObservationsRequest
{
    /// <summary>
    /// Exercise ID to export observations from.
    /// </summary>
    public required Guid ExerciseId { get; init; }

    /// <summary>
    /// Whether to include header formatting.
    /// </summary>
    public bool IncludeFormatting { get; init; } = true;

    /// <summary>
    /// Custom filename (without extension).
    /// </summary>
    public string? Filename { get; init; }
}

/// <summary>
/// Request to export full exercise package.
/// </summary>
public record ExportFullPackageRequest
{
    /// <summary>
    /// Exercise ID to export.
    /// </summary>
    public required Guid ExerciseId { get; init; }

    /// <summary>
    /// Whether to include formatting in Excel files.
    /// </summary>
    public bool IncludeFormatting { get; init; } = true;

    /// <summary>
    /// Custom filename for the ZIP (without extension).
    /// </summary>
    public string? Filename { get; init; }
}

/// <summary>
/// Exercise summary metadata for JSON export.
/// </summary>
public record ExerciseSummaryDto
{
    /// <summary>
    /// Exercise name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Exercise type (TTX, FE, FSE, etc.).
    /// </summary>
    public string ExerciseType { get; init; } = string.Empty;

    /// <summary>
    /// Exercise description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Scheduled date for the exercise.
    /// </summary>
    public string ScheduledDate { get; init; } = string.Empty;

    /// <summary>
    /// Planned start time.
    /// </summary>
    public string? StartTime { get; init; }

    /// <summary>
    /// Planned end time.
    /// </summary>
    public string? EndTime { get; init; }

    /// <summary>
    /// Exercise status.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Total number of injects.
    /// </summary>
    public int InjectCount { get; init; }

    /// <summary>
    /// Number of injects fired.
    /// </summary>
    public int InjectsFired { get; init; }

    /// <summary>
    /// Number of injects skipped.
    /// </summary>
    public int InjectsSkipped { get; init; }

    /// <summary>
    /// Number of injects pending.
    /// </summary>
    public int InjectsPending { get; init; }

    /// <summary>
    /// Total number of observations.
    /// </summary>
    public int ObservationCount { get; init; }

    /// <summary>
    /// Number of phases.
    /// </summary>
    public int PhaseCount { get; init; }

    /// <summary>
    /// Number of objectives.
    /// </summary>
    public int ObjectiveCount { get; init; }

    /// <summary>
    /// Export timestamp.
    /// </summary>
    public DateTime ExportedAt { get; init; }
}

/// <summary>
/// Export format options.
/// </summary>
public static class ExportFormat
{
    /// <summary>
    /// Excel workbook format (.xlsx).
    /// </summary>
    public const string Excel = "xlsx";

    /// <summary>
    /// CSV format (.csv).
    /// </summary>
    public const string Csv = "csv";

    /// <summary>
    /// ZIP archive format (.zip).
    /// </summary>
    public const string Zip = "zip";
}
