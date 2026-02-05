using System.Security.Claims;
using Cadence.Core.Features.Eeg.Models.DTOs;
using Cadence.Core.Features.Eeg.Services;
using Cadence.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for EEG entry management.
/// EEG entries are structured observations recorded against critical tasks during exercise conduct.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class EegEntriesController : ControllerBase
{
    private readonly IEegEntryService _eegEntryService;
    private readonly IEegExportService _eegExportService;
    private readonly ILogger<EegEntriesController> _logger;

    public EegEntriesController(
        IEegEntryService eegEntryService,
        IEegExportService eegExportService,
        ILogger<EegEntriesController> logger)
    {
        _eegEntryService = eegEntryService;
        _eegExportService = eegExportService;
        _logger = logger;
    }

    /// <summary>
    /// Get all EEG entries for an exercise.
    /// </summary>
    [HttpGet("exercises/{exerciseId:guid}/eeg-entries")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<EegEntryListResponse>> GetByExercise(Guid exerciseId)
    {
        var response = await _eegEntryService.GetByExerciseAsync(exerciseId);
        return Ok(response);
    }

    /// <summary>
    /// Get all EEG entries for a critical task.
    /// </summary>
    [HttpGet("critical-tasks/{taskId:guid}/eeg-entries")]
    public async Task<ActionResult<EegEntryListResponse>> GetByCriticalTask(Guid taskId)
    {
        var response = await _eegEntryService.GetByCriticalTaskAsync(taskId);
        return Ok(response);
    }

    /// <summary>
    /// Get a single EEG entry by ID.
    /// </summary>
    [HttpGet("eeg-entries/{id:guid}")]
    public async Task<ActionResult<EegEntryDto>> GetById(Guid id)
    {
        var entry = await _eegEntryService.GetByIdAsync(id);

        if (entry == null)
            return NotFound();

        return Ok(entry);
    }

    /// <summary>
    /// Create a new EEG entry.
    /// Requires Evaluator or higher role.
    /// </summary>
    [HttpPost("exercises/{exerciseId:guid}/critical-tasks/{taskId:guid}/eeg-entries")]
    [AuthorizeExerciseEvaluator]
    public async Task<ActionResult<EegEntryDto>> Create(Guid exerciseId, Guid taskId, CreateEegEntryRequest request)
    {
        // Ensure task ID matches
        if (request.CriticalTaskId != Guid.Empty && request.CriticalTaskId != taskId)
            return BadRequest(new { message = "Critical task ID in request does not match URL" });

        // Create modified request with correct task ID
        var modifiedRequest = new CreateEegEntryRequest
        {
            CriticalTaskId = taskId,
            ObservationText = request.ObservationText,
            Rating = request.Rating,
            ObservedAt = request.ObservedAt,
            TriggeringInjectId = request.TriggeringInjectId
        };

        // Validation
        if (string.IsNullOrWhiteSpace(modifiedRequest.ObservationText))
            return BadRequest(new { message = "Observation text is required" });

        if (modifiedRequest.ObservationText.Length > 4000)
            return BadRequest(new { message = "Observation text must be 4000 characters or less" });

        try
        {
            var evaluatorId = GetCurrentUserId();
            var entry = await _eegEntryService.CreateAsync(modifiedRequest, evaluatorId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = entry.Id },
                entry
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing EEG entry.
    /// Requires Evaluator or higher role.
    /// </summary>
    [HttpPut("exercises/{exerciseId:guid}/eeg-entries/{id:guid}")]
    [AuthorizeExerciseEvaluator]
    public async Task<ActionResult<EegEntryDto>> Update(Guid exerciseId, Guid id, UpdateEegEntryRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.ObservationText))
            return BadRequest(new { message = "Observation text is required" });

        if (request.ObservationText.Length > 4000)
            return BadRequest(new { message = "Observation text must be 4000 characters or less" });

        try
        {
            var modifiedBy = GetCurrentUserId();
            var entry = await _eegEntryService.UpdateAsync(id, request, modifiedBy);

            if (entry == null)
                return NotFound();

            return Ok(entry);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete an EEG entry (soft delete).
    /// Requires Exercise Director or Administrator role.
    /// </summary>
    [HttpDelete("exercises/{exerciseId:guid}/eeg-entries/{id:guid}")]
    [AuthorizeExerciseDirector]
    public async Task<IActionResult> Delete(Guid exerciseId, Guid id)
    {
        var deletedBy = GetCurrentUserId();
        var deleted = await _eegEntryService.DeleteAsync(id, deletedBy);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Get EEG coverage statistics for an exercise.
    /// Shows task coverage and rating distribution.
    /// </summary>
    [HttpGet("exercises/{exerciseId:guid}/eeg-coverage")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<EegCoverageDto>> GetCoverage(Guid exerciseId)
    {
        var coverage = await _eegEntryService.GetCoverageAsync(exerciseId);
        return Ok(coverage);
    }

    /// <summary>
    /// Export EEG data to Excel format for After-Action Review preparation.
    /// Generates a multi-sheet workbook with Summary, By Capability, All Entries, and Coverage Gaps.
    /// </summary>
    [HttpGet("exercises/{exerciseId:guid}/eeg-export")]
    [AuthorizeExerciseDirector]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportEeg(
        Guid exerciseId,
        [FromQuery] string format = "xlsx",
        [FromQuery] bool includeSummary = true,
        [FromQuery] bool includeByCapability = true,
        [FromQuery] bool includeAllEntries = true,
        [FromQuery] bool includeCoverageGaps = true,
        [FromQuery] bool includeEvaluatorNames = true,
        [FromQuery] bool includeFormatting = true,
        [FromQuery] string? filename = null)
    {
        try
        {
            if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                var jsonResult = await _eegExportService.ExportEegJsonAsync(exerciseId, includeEvaluatorNames);
                return Ok(jsonResult);
            }

            var request = new ExportEegRequest(
                ExerciseId: exerciseId,
                Format: format,
                IncludeSummary: includeSummary,
                IncludeByCapability: includeByCapability,
                IncludeAllEntries: includeAllEntries,
                IncludeCoverageGaps: includeCoverageGaps,
                IncludeEvaluatorNames: includeEvaluatorNames,
                IncludeFormatting: includeFormatting,
                Filename: filename
            );

            var result = await _eegExportService.ExportEegDataAsync(request);

            // Add metadata headers
            Response.Headers.Append("X-Entry-Count", result.ObjectiveCount.ToString());
            Response.Headers.Append("Access-Control-Expose-Headers", "X-Entry-Count, Content-Disposition");

            return File(result.Content, result.ContentType, result.Filename);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "EEG export failed: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting EEG data for exercise {ExerciseId}", exerciseId);
            return BadRequest(new { message = "Failed to export EEG data" });
        }
    }

    /// <summary>
    /// Export EEG data using POST with body options.
    /// </summary>
    [HttpPost("exercises/{exerciseId:guid}/eeg-export")]
    [AuthorizeExerciseDirector]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportEegPost(Guid exerciseId, [FromBody] ExportEegRequest request)
    {
        try
        {
            // Override exercise ID from route
            var modifiedRequest = request with { ExerciseId = exerciseId };

            if (modifiedRequest.Format.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                var jsonResult = await _eegExportService.ExportEegJsonAsync(exerciseId, modifiedRequest.IncludeEvaluatorNames);
                return Ok(jsonResult);
            }

            var result = await _eegExportService.ExportEegDataAsync(modifiedRequest);

            // Add metadata headers
            Response.Headers.Append("X-Entry-Count", result.ObjectiveCount.ToString());
            Response.Headers.Append("Access-Control-Expose-Headers", "X-Entry-Count, Content-Disposition");

            return File(result.Content, result.ContentType, result.Filename);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "EEG export failed: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting EEG data for exercise {ExerciseId}", exerciseId);
            return BadRequest(new { message = "Failed to export EEG data" });
        }
    }

    private string GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("User not authenticated");
        return userIdClaim;
    }
}
