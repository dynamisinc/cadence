using Cadence.Core.Features.ExcelImport.Models.DTOs;

namespace Cadence.Core.Features.ExcelImport.Services;

/// <summary>
/// Service for handling Excel file import operations.
/// </summary>
public interface IExcelImportService
{
    /// <summary>
    /// Analyzes an uploaded Excel file and returns information about its structure.
    /// </summary>
    /// <param name="fileName">Original file name</param>
    /// <param name="fileStream">The file content stream</param>
    /// <returns>Analysis result with worksheet information</returns>
    Task<FileAnalysisResultDto> AnalyzeFileAsync(string fileName, Stream fileStream);

    /// <summary>
    /// Selects a worksheet for import and returns detailed column information.
    /// </summary>
    /// <param name="request">Worksheet selection request</param>
    /// <returns>Detailed worksheet and column information</returns>
    Task<WorksheetSelectionResultDto> SelectWorksheetAsync(SelectWorksheetRequestDto request);

    /// <summary>
    /// Gets suggested column mappings based on column headers.
    /// </summary>
    /// <param name="sessionId">Import session ID</param>
    /// <returns>List of suggested column mappings</returns>
    Task<IReadOnlyList<ColumnMappingDto>> GetSuggestedMappingsAsync(Guid sessionId);

    /// <summary>
    /// Validates the import data with the configured mappings.
    /// </summary>
    /// <param name="request">Mapping configuration</param>
    /// <returns>Validation results</returns>
    Task<ValidationResultDto> ValidateImportAsync(ConfigureMappingsRequestDto request);

    /// <summary>
    /// Executes the import with the configured mappings.
    /// </summary>
    /// <param name="request">Import execution request</param>
    /// <returns>Import result</returns>
    Task<ImportResultDto> ExecuteImportAsync(ExecuteImportRequestDto request);

    /// <summary>
    /// Cancels an import session and cleans up temporary files.
    /// </summary>
    /// <param name="sessionId">Import session ID</param>
    Task CancelImportAsync(Guid sessionId);

    /// <summary>
    /// Gets the current state of an import session.
    /// </summary>
    /// <param name="sessionId">Import session ID</param>
    /// <returns>Session state or null if not found</returns>
    Task<ImportSessionStateDto?> GetSessionStateAsync(Guid sessionId);
}

/// <summary>
/// State of an import session.
/// </summary>
public record ImportSessionStateDto
{
    /// <summary>
    /// Session ID.
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// File name.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Current step in the wizard (Upload, SheetSelection, Mapping, Validation, Import, Complete).
    /// </summary>
    public required string CurrentStep { get; init; }

    /// <summary>
    /// Selected worksheet index (if any).
    /// </summary>
    public int? SelectedWorksheetIndex { get; init; }

    /// <summary>
    /// Configured mappings (if any).
    /// </summary>
    public IReadOnlyList<ColumnMappingDto>? Mappings { get; init; }

    /// <summary>
    /// Session creation time.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Session expiration time.
    /// </summary>
    public required DateTime ExpiresAt { get; init; }
}
