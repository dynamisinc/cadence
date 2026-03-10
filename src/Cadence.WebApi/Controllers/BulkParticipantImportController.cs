using Cadence.Core.Features.BulkParticipantImport.Models.DTOs;
using Cadence.Core.Features.BulkParticipantImport.Services;
using Cadence.WebApi.Authorization;
using Cadence.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for bulk participant import functionality.
/// Handles file upload, preview, confirmation, and import history.
/// </summary>
[ApiController]
[Authorize]
[AuthorizeExerciseAccess]
[Route("api/exercises/{exerciseId:guid}/participants/bulk-import")]
public class BulkParticipantImportController : ControllerBase
{
    private readonly IBulkParticipantImportService _importService;
    private readonly ILogger<BulkParticipantImportController> _logger;

    public BulkParticipantImportController(
        IBulkParticipantImportService importService,
        ILogger<BulkParticipantImportController> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    /// <summary>
    /// Upload and parse a participant file (CSV or XLSX).
    /// Returns parse results with session ID for subsequent operations.
    /// </summary>
    /// <param name="exerciseId">Target exercise ID</param>
    /// <param name="file">Uploaded file (CSV or XLSX)</param>
    /// <returns>Parse result with session ID</returns>
    [HttpPost("upload")]
    public async Task<ActionResult<FileParseResult>> UploadFile(
        Guid exerciseId,
        IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file provided" });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _importService.UploadAndParseAsync(exerciseId, stream, file.FileName);

            _logger.LogInformation(
                "Parsed participant file {FileName} for exercise {ExerciseId}: {RowCount} rows, session {SessionId}",
                file.FileName, exerciseId, result.TotalRows, result.SessionId);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing participant file for exercise {ExerciseId}", exerciseId);
            return Problem(detail: "An error occurred while parsing the file", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get classification preview for an active import session.
    /// Shows how each row will be processed (Assign, Update, Invite, Error).
    /// </summary>
    /// <param name="exerciseId">Target exercise ID</param>
    /// <param name="sessionId">Import session ID from upload step</param>
    /// <returns>Preview with row classifications</returns>
    [HttpGet("{sessionId:guid}/preview")]
    public async Task<ActionResult<ImportPreviewResult>> GetPreview(
        Guid exerciseId,
        Guid sessionId)
    {
        try
        {
            var result = await _importService.GetPreviewAsync(exerciseId, sessionId);

            _logger.LogInformation(
                "Generated preview for session {SessionId}: {AssignCount} assign, {UpdateCount} update, {InviteCount} invite, {ErrorCount} error",
                sessionId, result.AssignCount, result.UpdateCount, result.InviteCount, result.ErrorCount);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview for session {SessionId}", sessionId);
            return Problem(detail: "An error occurred while generating the preview", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Confirm and execute the bulk import.
    /// Processes all non-error rows and creates participants, updates, and invitations.
    /// </summary>
    /// <param name="exerciseId">Target exercise ID</param>
    /// <param name="sessionId">Import session ID</param>
    /// <returns>Import result with processing outcomes</returns>
    [HttpPost("{sessionId:guid}/confirm")]
    public async Task<ActionResult<BulkImportResult>> ConfirmImport(
        Guid exerciseId,
        Guid sessionId)
    {
        try
        {
            var userId = User.GetUserId();
            var result = await _importService.ConfirmImportAsync(exerciseId, sessionId, userId);

            _logger.LogInformation(
                "Completed bulk import {ImportRecordId} for exercise {ExerciseId} by user {UserId}: {AssignedCount} assigned, {UpdatedCount} updated, {InvitedCount} invited",
                result.ImportRecordId, exerciseId, userId, result.AssignedCount, result.UpdatedCount, result.InvitedCount);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming import for session {SessionId}", sessionId);
            return Problem(detail: "An error occurred while processing the import", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get import history for an exercise.
    /// Returns all past bulk imports with summary counts.
    /// </summary>
    /// <param name="exerciseId">Target exercise ID</param>
    /// <returns>List of import records</returns>
    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<BulkImportRecordDto>>> GetImportHistory(Guid exerciseId)
    {
        try
        {
            var records = await _importService.GetImportHistoryAsync(exerciseId);
            return Ok(records);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving import history for exercise {ExerciseId}", exerciseId);
            return Problem(detail: "An error occurred while retrieving import history", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get detailed results for a specific import record.
    /// </summary>
    /// <param name="exerciseId">Target exercise ID</param>
    /// <param name="importRecordId">Import record ID</param>
    /// <returns>Import record details</returns>
    [HttpGet("records/{importRecordId:guid}")]
    public async Task<ActionResult<BulkImportRecordDto>> GetImportRecord(
        Guid exerciseId,
        Guid importRecordId)
    {
        try
        {
            var record = await _importService.GetImportRecordAsync(exerciseId, importRecordId);

            if (record == null)
            {
                return NotFound(new { message = "Import record not found" });
            }

            return Ok(record);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving import record {ImportRecordId}", importRecordId);
            return Problem(detail: "An error occurred while retrieving the import record", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get row-level results for a specific import record.
    /// Shows the outcome for each processed row.
    /// </summary>
    /// <param name="importRecordId">Import record ID</param>
    /// <returns>List of row results</returns>
    [HttpGet("records/{importRecordId:guid}/rows")]
    public async Task<ActionResult<IReadOnlyList<BulkImportRowResultDto>>> GetImportRowResults(Guid importRecordId)
    {
        try
        {
            var results = await _importService.GetImportRowResultsAsync(importRecordId);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving row results for import record {ImportRecordId}", importRecordId);
            return Problem(detail: "An error occurred while retrieving row results", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get pending exercise assignments for an exercise.
    /// Shows participants awaiting organization invitation acceptance.
    /// </summary>
    /// <param name="exerciseId">Target exercise ID</param>
    /// <returns>List of pending assignments</returns>
    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<PendingExerciseAssignmentDto>>> GetPendingAssignments(Guid exerciseId)
    {
        try
        {
            var assignments = await _importService.GetPendingAssignmentsAsync(exerciseId);
            return Ok(assignments);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending assignments for exercise {ExerciseId}", exerciseId);
            return Problem(detail: "An error occurred while retrieving pending assignments", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Download a participant import template file.
    /// </summary>
    /// <param name="format">Template format: "csv" or "xlsx" (default: csv)</param>
    /// <returns>Template file</returns>
    [HttpGet("template")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTemplate([FromQuery] string format = "csv")
    {
        try
        {
            var (content, contentType, fileName) = await _importService.GenerateTemplateAsync(format);
            return File(content, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating template with format {Format}", format);
            return Problem(detail: "An error occurred while generating the template", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

}
