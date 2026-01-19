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
}
