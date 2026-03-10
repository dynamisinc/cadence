using Cadence.Core.Features.ExcelImport.Models.DTOs;

namespace Cadence.Core.Features.ExcelImport.Models;

/// <summary>
/// Represents the in-memory state of an active Excel import wizard session.
/// Sessions are stored by the <see cref="Cadence.Core.Features.ExcelImport.Services.IImportSessionStore"/> and
/// expire after a configurable timeout.
/// </summary>
public sealed class ImportSession
{
    // Lock object for thread-safe access to mutable session state.
    // Used for ExpiresAt and any read-modify-write on ValidationResults/Mappings.
    private readonly object _lock = new();
    private DateTime _expiresAt;

    /// <summary>
    /// Synchronization root for external callers that need to lock session state
    /// (e.g., UpdateRowsAsync mutating ValidationResults).
    /// </summary>
    public object SyncRoot => _lock;

    /// <summary>Session identifier.</summary>
    public Guid SessionId { get; init; }

    /// <summary>Original uploaded file name.</summary>
    public required string FileName { get; init; }

    /// <summary>File format: xlsx, xls, or csv.</summary>
    public required string FileFormat { get; init; }

    /// <summary>Path to the temporary file copy on disk.</summary>
    public required string TempFilePath { get; init; }

    /// <summary>UTC time when this session was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// UTC time when this session expires. Thread-safe via internal lock.
    /// </summary>
    public DateTime ExpiresAt
    {
        get { lock (_lock) { return _expiresAt; } }
        set { lock (_lock) { _expiresAt = value; } }
    }

    /// <summary>Current wizard step (Upload, SheetSelection, Mapping, Validation, Complete).</summary>
    public required string CurrentStep { get; set; }

    /// <summary>Worksheets discovered during file analysis.</summary>
    public required List<WorksheetInfoDto> Worksheets { get; init; }

    /// <summary>Zero-based index of the worksheet selected for import.</summary>
    public int? SelectedWorksheetIndex { get; set; }

    /// <summary>1-based row number of the header row.</summary>
    public int HeaderRow { get; set; } = 1;

    /// <summary>1-based row number where data begins.</summary>
    public int DataStartRow { get; set; } = 2;

    /// <summary>Column metadata discovered during worksheet selection.</summary>
    public List<ColumnInfoDto>? Columns { get; set; }

    /// <summary>Column-to-field mappings configured by the user.</summary>
    public List<ColumnMappingDto>? Mappings { get; set; }

    /// <summary>Optional time format hint from the user.</summary>
    public string? TimeFormat { get; set; }

    /// <summary>Optional date format hint from the user.</summary>
    public string? DateFormat { get; set; }

    /// <summary>Per-row validation results from the last validation run.</summary>
    public List<RowValidationResultDto>? ValidationResults { get; set; }
}
