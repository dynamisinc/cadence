namespace Cadence.Core.Features.ExcelImport.Models.DTOs;

/// <summary>
/// Result of analyzing an uploaded Excel file.
/// </summary>
public record FileAnalysisResultDto
{
    /// <summary>
    /// Session ID for this import (used to track state across wizard steps).
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// Original file name.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public required long FileSize { get; init; }

    /// <summary>
    /// File format detected (xlsx, xls, csv).
    /// </summary>
    public required string FileFormat { get; init; }

    /// <summary>
    /// List of worksheets found in the file.
    /// </summary>
    public required IReadOnlyList<WorksheetInfoDto> Worksheets { get; init; }

    /// <summary>
    /// Whether the file appears to be password protected.
    /// </summary>
    public bool IsPasswordProtected { get; init; }

    /// <summary>
    /// Any warning messages about the file.
    /// </summary>
    public IReadOnlyList<string>? Warnings { get; init; }
}

/// <summary>
/// Information about a worksheet in an Excel file.
/// </summary>
public record WorksheetInfoDto
{
    /// <summary>
    /// Zero-based index of the worksheet.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Name of the worksheet.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Number of rows with data (excluding empty rows).
    /// </summary>
    public required int RowCount { get; init; }

    /// <summary>
    /// Number of columns with data.
    /// </summary>
    public required int ColumnCount { get; init; }

    /// <summary>
    /// Whether this worksheet appears to be the MSEL (based on column headers).
    /// </summary>
    public bool LooksLikeMsel { get; init; }

    /// <summary>
    /// Confidence score (0-100) that this is the MSEL worksheet.
    /// </summary>
    public int MselConfidence { get; init; }

    /// <summary>
    /// Suggested header row (1-based). Determined by scanning the first 10 rows for MSEL-like headers.
    /// </summary>
    public int SuggestedHeaderRow { get; init; } = 1;

    /// <summary>
    /// Suggested data start row (1-based). Typically SuggestedHeaderRow + 1.
    /// </summary>
    public int SuggestedDataStartRow { get; init; } = 2;
}

/// <summary>
/// Result of selecting a worksheet for import.
/// </summary>
public record WorksheetSelectionResultDto
{
    /// <summary>
    /// Session ID for this import.
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// The selected worksheet.
    /// </summary>
    public required WorksheetInfoDto Worksheet { get; init; }

    /// <summary>
    /// Column headers found in the worksheet.
    /// </summary>
    public required IReadOnlyList<ColumnInfoDto> Columns { get; init; }

    /// <summary>
    /// Preview of the first N rows of data.
    /// </summary>
    public required IReadOnlyList<Dictionary<string, object?>> PreviewRows { get; init; }

    /// <summary>
    /// Number of preview rows included.
    /// </summary>
    public required int PreviewRowCount { get; init; }
}

/// <summary>
/// Information about a column in the worksheet.
/// </summary>
public record ColumnInfoDto
{
    /// <summary>
    /// Zero-based column index.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Excel column letter (A, B, C, ..., AA, AB, etc.).
    /// </summary>
    public required string Letter { get; init; }

    /// <summary>
    /// Column header text (from first row).
    /// </summary>
    public required string Header { get; init; }

    /// <summary>
    /// Inferred data type of the column (text, number, date, boolean, mixed).
    /// </summary>
    public required string DataType { get; init; }

    /// <summary>
    /// Sample values from the column (first 3 non-empty values).
    /// </summary>
    public required IReadOnlyList<string?> SampleValues { get; init; }

    /// <summary>
    /// Percentage of cells with data (0-100).
    /// </summary>
    public required int FillRate { get; init; }
}

/// <summary>
/// Request to select a worksheet for import.
/// </summary>
public record SelectWorksheetRequestDto
{
    /// <summary>
    /// Session ID from the file analysis.
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// Index of the worksheet to select.
    /// </summary>
    public required int WorksheetIndex { get; init; }

    /// <summary>
    /// Number of preview rows to return (default 5, max 20).
    /// </summary>
    public int PreviewRowCount { get; init; } = 5;

    /// <summary>
    /// Row number where data starts (1-based, default 2 assuming row 1 is headers).
    /// </summary>
    public int DataStartRow { get; init; } = 2;

    /// <summary>
    /// Row number containing headers (1-based, default 1).
    /// </summary>
    public int HeaderRow { get; init; } = 1;
}

/// <summary>
/// Column mapping configuration for import.
/// </summary>
public record ColumnMappingDto
{
    /// <summary>
    /// The Cadence field to map to.
    /// </summary>
    public required string CadenceField { get; init; }

    /// <summary>
    /// The Excel column index to map from (null if not mapped).
    /// </summary>
    public int? SourceColumnIndex { get; init; }

    /// <summary>
    /// Whether this field is required.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Display name for the field.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Description of what this field contains.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Suggested auto-mapping (column index) if any.
    /// </summary>
    public int? SuggestedColumnIndex { get; init; }

    /// <summary>
    /// Confidence score for the suggested mapping (0-100).
    /// </summary>
    public int SuggestedMappingConfidence { get; init; }
}

/// <summary>
/// Request to configure column mappings.
/// </summary>
public record ConfigureMappingsRequestDto
{
    /// <summary>
    /// Session ID from the file analysis.
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// Column mappings to apply.
    /// </summary>
    public required IReadOnlyList<ColumnMappingDto> Mappings { get; init; }

    /// <summary>
    /// Time format to use for parsing time values.
    /// </summary>
    public string? TimeFormat { get; init; }

    /// <summary>
    /// Date format to use for parsing date values.
    /// </summary>
    public string? DateFormat { get; init; }
}

/// <summary>
/// Result of validating import data.
/// </summary>
public record ValidationResultDto
{
    /// <summary>
    /// Session ID for this import.
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// Total number of data rows.
    /// </summary>
    public required int TotalRows { get; init; }

    /// <summary>
    /// Number of valid rows.
    /// </summary>
    public required int ValidRows { get; init; }

    /// <summary>
    /// Number of rows with errors.
    /// </summary>
    public required int ErrorRows { get; init; }

    /// <summary>
    /// Number of rows with warnings.
    /// </summary>
    public required int WarningRows { get; init; }

    /// <summary>
    /// Detailed row-level validation results.
    /// </summary>
    public required IReadOnlyList<RowValidationResultDto> Rows { get; init; }

    /// <summary>
    /// Whether all required mappings are configured.
    /// </summary>
    public required bool AllRequiredMappingsConfigured { get; init; }

    /// <summary>
    /// List of missing required mappings.
    /// </summary>
    public IReadOnlyList<string>? MissingRequiredMappings { get; init; }
}

/// <summary>
/// Validation result for a single row.
/// </summary>
public record RowValidationResultDto
{
    /// <summary>
    /// Row number in the source file (1-based).
    /// </summary>
    public required int RowNumber { get; init; }

    /// <summary>
    /// Validation status (Valid, Warning, Error).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Parsed values from the row.
    /// </summary>
    public required Dictionary<string, object?> Values { get; init; }

    /// <summary>
    /// Validation issues found.
    /// </summary>
    public IReadOnlyList<ValidationIssueDto>? Issues { get; init; }
}

/// <summary>
/// A validation issue for a specific field.
/// </summary>
public record ValidationIssueDto
{
    /// <summary>
    /// The field with the issue.
    /// </summary>
    public required string Field { get; init; }

    /// <summary>
    /// Issue severity (Error, Warning).
    /// </summary>
    public required string Severity { get; init; }

    /// <summary>
    /// Issue message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Original value that caused the issue.
    /// </summary>
    public string? OriginalValue { get; init; }
}

/// <summary>
/// Request to execute the import.
/// </summary>
public record ExecuteImportRequestDto
{
    /// <summary>
    /// Session ID from the file analysis.
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// Exercise ID to import into.
    /// </summary>
    public required Guid ExerciseId { get; init; }

    /// <summary>
    /// Import strategy (Append, Replace, Merge).
    /// </summary>
    public required string Strategy { get; init; }

    /// <summary>
    /// Whether to skip rows with errors.
    /// </summary>
    public bool SkipErrorRows { get; init; } = true;

    /// <summary>
    /// Whether to create missing phases automatically.
    /// </summary>
    public bool CreateMissingPhases { get; init; } = true;

    /// <summary>
    /// Whether to create missing objectives automatically.
    /// </summary>
    public bool CreateMissingObjectives { get; init; }
}

/// <summary>
/// Result of executing an import.
/// </summary>
public record ImportResultDto
{
    /// <summary>
    /// Whether the import was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Number of injects created.
    /// </summary>
    public required int InjectsCreated { get; init; }

    /// <summary>
    /// Number of injects updated (for merge strategy).
    /// </summary>
    public int InjectsUpdated { get; init; }

    /// <summary>
    /// Number of rows skipped.
    /// </summary>
    public int RowsSkipped { get; init; }

    /// <summary>
    /// Number of phases created.
    /// </summary>
    public int PhasesCreated { get; init; }

    /// <summary>
    /// Number of objectives created.
    /// </summary>
    public int ObjectivesCreated { get; init; }

    /// <summary>
    /// Errors that occurred during import.
    /// </summary>
    public IReadOnlyList<string>? Errors { get; init; }

    /// <summary>
    /// Warnings from the import.
    /// </summary>
    public IReadOnlyList<string>? Warnings { get; init; }

    /// <summary>
    /// The MSEL ID that was imported into.
    /// </summary>
    public Guid? MselId { get; init; }
}

/// <summary>
/// Request to update one or more row values in a validation session and re-validate.
/// Used for both bulk auto-fix and individual inline edits.
/// </summary>
public record UpdateRowsRequestDto
{
    /// <summary>
    /// Session ID.
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// List of row updates to apply.
    /// </summary>
    public required IReadOnlyList<RowUpdateDto> Updates { get; init; }
}

/// <summary>
/// A single row value update.
/// </summary>
public record RowUpdateDto
{
    /// <summary>
    /// Row number (1-based, matching RowValidationResultDto.RowNumber).
    /// </summary>
    public required int RowNumber { get; init; }

    /// <summary>
    /// Cadence field name to update (e.g., "Title", "ScheduledTime").
    /// </summary>
    public required string Field { get; init; }

    /// <summary>
    /// New value for the field.
    /// </summary>
    public string? Value { get; init; }
}

/// <summary>
/// Response after updating rows. Returns only the changed rows plus updated counts.
/// </summary>
public record UpdateRowsResultDto
{
    /// <summary>
    /// Session ID.
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// Updated total row count.
    /// </summary>
    public required int TotalRows { get; init; }

    /// <summary>
    /// Updated valid row count.
    /// </summary>
    public required int ValidRows { get; init; }

    /// <summary>
    /// Updated error row count.
    /// </summary>
    public required int ErrorRows { get; init; }

    /// <summary>
    /// Updated warning row count.
    /// </summary>
    public required int WarningRows { get; init; }

    /// <summary>
    /// Only the rows that were re-validated (changed).
    /// </summary>
    public required IReadOnlyList<RowValidationResultDto> UpdatedRows { get; init; }
}

/// <summary>
/// Import strategy options.
/// </summary>
public static class ImportStrategy
{
    /// <summary>
    /// Append new injects to the existing MSEL.
    /// </summary>
    public const string Append = "Append";

    /// <summary>
    /// Replace all injects in the MSEL with imported data.
    /// </summary>
    public const string Replace = "Replace";

    /// <summary>
    /// Merge with existing injects by inject number.
    /// </summary>
    public const string Merge = "Merge";
}
