using Cadence.Core.Features.BulkParticipantImport.Models.DTOs;

namespace Cadence.Core.Features.BulkParticipantImport.Services;

/// <summary>
/// Orchestrates the bulk participant import flow: upload, preview, confirm.
/// Manages import sessions and coordinates parsing, classification, and processing.
/// </summary>
public interface IBulkParticipantImportService
{
    /// <summary>
    /// Uploads and parses a participant file, creating an import session.
    /// </summary>
    /// <param name="exerciseId">Target exercise.</param>
    /// <param name="fileStream">Uploaded file content.</param>
    /// <param name="fileName">Original file name.</param>
    /// <returns>Parse result with session ID for subsequent operations.</returns>
    Task<FileParseResult> UploadAndParseAsync(Guid exerciseId, Stream fileStream, string fileName);

    /// <summary>
    /// Gets the classification preview for an active import session.
    /// </summary>
    /// <param name="exerciseId">Target exercise.</param>
    /// <param name="sessionId">Import session ID from the upload step.</param>
    /// <returns>Preview with classifications for each row.</returns>
    Task<ImportPreviewResult> GetPreviewAsync(Guid exerciseId, Guid sessionId);

    /// <summary>
    /// Confirms and executes the import, processing all non-error rows.
    /// </summary>
    /// <param name="exerciseId">Target exercise.</param>
    /// <param name="sessionId">Import session ID.</param>
    /// <param name="importingUserId">The user performing the import.</param>
    /// <returns>Import result with processing outcomes.</returns>
    Task<BulkImportResult> ConfirmImportAsync(Guid exerciseId, Guid sessionId, string importingUserId);

    /// <summary>
    /// Gets import history for an exercise.
    /// </summary>
    /// <param name="exerciseId">Target exercise.</param>
    /// <returns>List of past import records.</returns>
    Task<IReadOnlyList<BulkImportRecordDto>> GetImportHistoryAsync(Guid exerciseId);

    /// <summary>
    /// Gets detailed results for a specific import record.
    /// </summary>
    /// <param name="exerciseId">Target exercise.</param>
    /// <param name="importRecordId">Import record ID.</param>
    /// <returns>Import record with row-level results.</returns>
    Task<BulkImportRecordDto?> GetImportRecordAsync(Guid exerciseId, Guid importRecordId);

    /// <summary>
    /// Gets the row results for a specific import record.
    /// </summary>
    /// <param name="importRecordId">Import record ID.</param>
    /// <returns>List of row results.</returns>
    Task<IReadOnlyList<BulkImportRowResultDto>> GetImportRowResultsAsync(Guid importRecordId);

    /// <summary>
    /// Gets pending exercise assignments for an exercise.
    /// </summary>
    /// <param name="exerciseId">Target exercise.</param>
    /// <returns>List of pending assignments with invitation status.</returns>
    Task<IReadOnlyList<PendingExerciseAssignmentDto>> GetPendingAssignmentsAsync(Guid exerciseId);

    /// <summary>
    /// Generates a template file for participant import.
    /// </summary>
    /// <param name="format">File format: "csv" or "xlsx".</param>
    /// <returns>File content and content type.</returns>
    Task<(byte[] Content, string ContentType, string FileName)> GenerateTemplateAsync(string format);
}
