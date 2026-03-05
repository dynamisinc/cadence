using System.Globalization;
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
    private readonly IEegDocumentService _eegDocumentService;
    private readonly ILogger<EegEntriesController> _logger;

    public EegEntriesController(
        IEegEntryService eegEntryService,
        IEegExportService eegExportService,
        IEegDocumentService eegDocumentService,
        ILogger<EegEntriesController> logger)
    {
        _eegEntryService = eegEntryService;
        _eegExportService = eegExportService;
        _eegDocumentService = eegDocumentService;
        _logger = logger;
    }

    /// <summary>
    /// Get EEG entries for an exercise with optional filtering and pagination.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="page">Page number (1-indexed). Default: 1</param>
    /// <param name="pageSize">Items per page (max: 100). Default: 20</param>
    /// <param name="rating">Filter by ratings (P, S, M, U). Comma-separated</param>
    /// <param name="evaluatorId">Filter by evaluator IDs. Comma-separated</param>
    /// <param name="capabilityTargetId">Filter by capability target ID</param>
    /// <param name="criticalTaskId">Filter by critical task ID</param>
    /// <param name="fromDate">Filter entries observed after this time</param>
    /// <param name="toDate">Filter entries observed before this time</param>
    /// <param name="sortBy">Sort field: observedAt, recordedAt, rating. Default: observedAt</param>
    /// <param name="sortOrder">Sort direction: asc, desc. Default: desc</param>
    /// <param name="search">Free-text search in observation text</param>
    [HttpGet("exercises/{exerciseId:guid}/eeg-entries")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<EegEntryListResponse>> GetByExercise(
        Guid exerciseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? rating = null,
        [FromQuery] string? evaluatorId = null,
        [FromQuery] Guid? capabilityTargetId = null,
        [FromQuery] Guid? criticalTaskId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string sortBy = "observedAt",
        [FromQuery] string sortOrder = "desc",
        [FromQuery] string? search = null)
    {
        var queryParams = new EegEntryQueryParams
        {
            Page = page,
            PageSize = pageSize,
            Rating = rating,
            EvaluatorId = evaluatorId,
            CapabilityTargetId = capabilityTargetId,
            CriticalTaskId = criticalTaskId,
            FromDate = fromDate,
            ToDate = toDate,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Search = search
        };

        var response = await _eegEntryService.GetByExerciseAsync(exerciseId, queryParams);
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
            Response.Headers.Append("X-Entry-Count", result.ObjectiveCount.ToString(CultureInfo.InvariantCulture));
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
            Response.Headers.Append("X-Entry-Count", result.ObjectiveCount.ToString(CultureInfo.InvariantCulture));
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
    /// Generate an EEG document (HSEEP-compliant Word document).
    /// Supports blank EEG for evaluators or completed EEG with recorded observations.
    /// </summary>
    [HttpPost("exercises/{exerciseId:guid}/eeg-document")]
    [AuthorizeExerciseAccess]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateEegDocument(
        Guid exerciseId,
        [FromBody] GenerateEegDocumentRequest? request = null)
    {
        try
        {
            var documentRequest = request ?? new GenerateEegDocumentRequest();
            var result = await _eegDocumentService.GenerateAsync(exerciseId, documentRequest);

            // Add metadata headers
            Response.Headers.Append("X-Capability-Target-Count", result.CapabilityTargetCount.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Append("X-Critical-Task-Count", result.CriticalTaskCount.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Append("Access-Control-Expose-Headers", "X-Capability-Target-Count, X-Critical-Task-Count, Content-Disposition");

            return File(result.Content, result.ContentType, result.Filename);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "EEG document generation failed: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating EEG document for exercise {ExerciseId}", exerciseId);
            return BadRequest(new { message = "Failed to generate EEG document" });
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
