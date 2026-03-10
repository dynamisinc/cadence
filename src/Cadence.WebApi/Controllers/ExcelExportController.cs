using System.Globalization;
using Cadence.Core.Data;
using Cadence.Core.Features.ExcelExport.Models.DTOs;
using Cadence.Core.Features.ExcelExport.Services;
using Cadence.Core.Hubs;
using Cadence.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for Excel export operations.
/// Requires authentication for all endpoints.
/// </summary>
[ApiController]
[Route("api/export")]
[Authorize]
public class ExcelExportController : ControllerBase
{
    private readonly IExcelExportService _service;
    private readonly ILogger<ExcelExportController> _logger;
    private readonly ICurrentOrganizationContext _orgContext;
    private readonly AppDbContext _context;

    public ExcelExportController(
        IExcelExportService service,
        ILogger<ExcelExportController> logger,
        ICurrentOrganizationContext orgContext,
        AppDbContext context)
    {
        _service = service;
        _logger = logger;
        _orgContext = orgContext;
        _context = context;
    }

    /// <summary>
    /// Export the MSEL for an exercise to Excel or CSV format (POST).
    /// </summary>
    /// <param name="request">Export options</param>
    /// <returns>Downloadable file</returns>
    [HttpPost("msel")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportMselPost([FromBody] ExportMselRequest request)
    {
        var accessError = await ValidateExerciseOrgAccessAsync(request.ExerciseId);
        if (accessError != null) return accessError;

        try
        {
            var result = await _service.ExportMselAsync(request);

            // Add metadata headers for frontend consumption
            Response.Headers.Append("X-Inject-Count", result.InjectCount.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Append("X-Phase-Count", result.PhaseCount.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Append("X-Objective-Count", result.ObjectiveCount.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Append("Access-Control-Expose-Headers", "X-Inject-Count, X-Phase-Count, X-Objective-Count, Content-Disposition");

            return File(result.Content, result.ContentType, result.Filename);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Export failed: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting MSEL for exercise {ExerciseId}", request.ExerciseId);
            return BadRequest(new { message = "Failed to export MSEL" });
        }
    }

    /// <summary>
    /// Export the MSEL for an exercise to Excel or CSV format (GET).
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="format">Export format (xlsx or csv)</param>
    /// <param name="includeFormatting">Include header formatting</param>
    /// <param name="includeObjectives">Include objectives worksheet</param>
    /// <param name="includePhases">Include phases worksheet</param>
    /// <param name="includeConductData">Include conduct data (status, fired times)</param>
    /// <param name="filename">Custom filename (without extension)</param>
    /// <returns>Downloadable file</returns>
    [HttpGet("exercises/{exerciseId:guid}/msel")]
    [AuthorizeExerciseAccess]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportMselGet(
        Guid exerciseId,
        [FromQuery] string format = "xlsx",
        [FromQuery] bool includeFormatting = true,
        [FromQuery] bool includeObjectives = true,
        [FromQuery] bool includePhases = true,
        [FromQuery] bool includeConductData = false,
        [FromQuery] string? filename = null)
    {
        try
        {
            var request = new ExportMselRequest
            {
                ExerciseId = exerciseId,
                Format = format,
                IncludeFormatting = includeFormatting,
                IncludeObjectives = includeObjectives,
                IncludePhases = includePhases,
                IncludeConductData = includeConductData,
                Filename = filename
            };

            var result = await _service.ExportMselAsync(request);

            return File(result.Content, result.ContentType, result.Filename);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Export failed: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting MSEL for exercise {ExerciseId}", exerciseId);
            return BadRequest(new { message = "Failed to export MSEL" });
        }
    }

    /// <summary>
    /// Download a blank MSEL template for data entry.
    /// </summary>
    /// <param name="includeFormatting">Include header formatting</param>
    /// <returns>Downloadable template file</returns>
    [HttpGet("template")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadTemplate([FromQuery] bool includeFormatting = true)
    {
        try
        {
            var result = await _service.GenerateTemplateAsync(includeFormatting);
            return File(result.Content, result.ContentType, result.Filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating template");
            return BadRequest(new { message = "Failed to generate template" });
        }
    }

    /// <summary>
    /// Export observations for an exercise to Excel format.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="includeFormatting">Include header formatting</param>
    /// <param name="filename">Custom filename (without extension)</param>
    /// <returns>Downloadable Excel file</returns>
    [HttpGet("exercises/{exerciseId:guid}/observations")]
    [AuthorizeExerciseAccess]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportObservations(
        Guid exerciseId,
        [FromQuery] bool includeFormatting = true,
        [FromQuery] string? filename = null)
    {
        try
        {
            var request = new ExportObservationsRequest
            {
                ExerciseId = exerciseId,
                IncludeFormatting = includeFormatting,
                Filename = filename
            };

            var result = await _service.ExportObservationsAsync(request);

            // Add metadata headers
            Response.Headers.Append("X-Observation-Count", result.ObjectiveCount.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Append("Access-Control-Expose-Headers", "X-Observation-Count, Content-Disposition");

            return File(result.Content, result.ContentType, result.Filename);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Export failed: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting observations for exercise {ExerciseId}", exerciseId);
            return BadRequest(new { message = "Failed to export observations" });
        }
    }

    /// <summary>
    /// Export full exercise package as a ZIP file containing MSEL, Observations, and Summary.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="includeFormatting">Include formatting in Excel files</param>
    /// <param name="filename">Custom filename for the ZIP (without extension)</param>
    /// <returns>Downloadable ZIP file</returns>
    [HttpGet("exercises/{exerciseId:guid}/full")]
    [AuthorizeExerciseAccess]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportFullPackage(
        Guid exerciseId,
        [FromQuery] bool includeFormatting = true,
        [FromQuery] string? filename = null)
    {
        try
        {
            var request = new ExportFullPackageRequest
            {
                ExerciseId = exerciseId,
                IncludeFormatting = includeFormatting,
                Filename = filename
            };

            var result = await _service.ExportFullPackageAsync(request);

            // Add metadata headers
            Response.Headers.Append("X-Inject-Count", result.InjectCount.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Append("X-Phase-Count", result.PhaseCount.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Append("X-Objective-Count", result.ObjectiveCount.ToString(CultureInfo.InvariantCulture));
            Response.Headers.Append("Access-Control-Expose-Headers", "X-Inject-Count, X-Phase-Count, X-Objective-Count, Content-Disposition");

            return File(result.Content, result.ContentType, result.Filename);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Export failed: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting full package for exercise {ExerciseId}", exerciseId);
            return BadRequest(new { message = "Failed to export full package" });
        }
    }

    /// <summary>
    /// Validates that the exercise exists and belongs to the current user's organization.
    /// Used for endpoints where exerciseId comes from the request body (not the route),
    /// so [AuthorizeExerciseAccess] cannot extract it.
    /// </summary>
    private async Task<IActionResult?> ValidateExerciseOrgAccessAsync(Guid exerciseId)
    {
        var exercise = await _context.Exercises
            .AsNoTracking()
            .Where(e => e.Id == exerciseId)
            .Select(e => new { e.OrganizationId })
            .FirstOrDefaultAsync();

        if (exercise == null)
            return NotFound(new { message = "Exercise not found" });

        if (!_orgContext.IsSysAdmin &&
            (!_orgContext.CurrentOrganizationId.HasValue ||
             _orgContext.CurrentOrganizationId.Value != exercise.OrganizationId))
        {
            _logger.LogWarning(
                "User attempted export for exercise {ExerciseId} in org {ExerciseOrgId} but current org is {CurrentOrgId}",
                exerciseId, exercise.OrganizationId, _orgContext.CurrentOrganizationId);
            return StatusCode(403);
        }

        return null; // Access granted
    }
}
