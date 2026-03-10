using Cadence.Core.Data;
using Cadence.Core.Features.Observations.Models.DTOs;
using Cadence.Core.Features.Observations.Services;
using Cadence.Core.Hubs;
using Cadence.WebApi.Authorization;
using Cadence.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for observation management.
/// Observations are evaluator assessments of player performance during exercise conduct.
/// Requires authentication for all endpoints.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class ObservationsController : ControllerBase
{
    private readonly IObservationService _observationService;
    private readonly ILogger<ObservationsController> _logger;
    private readonly ICurrentOrganizationContext _orgContext;
    private readonly AppDbContext _context;

    public ObservationsController(
        IObservationService observationService,
        ILogger<ObservationsController> logger,
        ICurrentOrganizationContext orgContext,
        AppDbContext context)
    {
        _observationService = observationService;
        _logger = logger;
        _orgContext = orgContext;
        _context = context;
    }

    /// <summary>
    /// Get all observations for an exercise.
    /// </summary>
    [HttpGet("exercises/{exerciseId:guid}/observations")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<IEnumerable<ObservationDto>>> GetObservationsByExercise(Guid exerciseId)
    {
        var observations = await _observationService.GetObservationsByExerciseAsync(exerciseId);
        return Ok(observations);
    }

    /// <summary>
    /// Get all observations for a specific inject.
    /// </summary>
    [HttpGet("injects/{injectId:guid}/observations")]
    public async Task<IActionResult> GetObservationsByInject(Guid injectId)
    {
        var accessError = await ValidateInjectOrgAccessAsync(injectId);
        if (accessError != null) return accessError;

        var observations = await _observationService.GetObservationsByInjectAsync(injectId);
        return Ok(observations);
    }

    /// <summary>
    /// Get a single observation by ID.
    /// </summary>
    [HttpGet("observations/{id:guid}")]
    public async Task<IActionResult> GetObservation(Guid id)
    {
        var accessError = await ValidateObservationOrgAccessAsync(id);
        if (accessError != null) return accessError;

        var observation = await _observationService.GetObservationAsync(id);

        if (observation == null)
        {
            return NotFound();
        }

        return Ok(observation);
    }

    /// <summary>
    /// Create a new observation for an exercise.
    /// Requires Evaluator or higher role in the exercise.
    /// </summary>
    [HttpPost("exercises/{exerciseId:guid}/observations")]
    [AuthorizeExerciseEvaluator]
    public async Task<ActionResult<ObservationDto>> CreateObservation(Guid exerciseId, CreateObservationRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "Content is required" });
        }

        if (request.Content.Length > 4000)
        {
            return BadRequest(new { message = "Content must be 4000 characters or less" });
        }

        if (request.Recommendation?.Length > 2000)
        {
            return BadRequest(new { message = "Recommendation must be 2000 characters or less" });
        }

        if (request.Location?.Length > 200)
        {
            return BadRequest(new { message = "Location must be 200 characters or less" });
        }

        try
        {
            var createdBy = User.GetUserId();
            var observation = await _observationService.CreateObservationAsync(exerciseId, request, createdBy);

            return CreatedAtAction(
                nameof(GetObservation),
                new { id = observation.Id },
                observation
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing observation.
    /// Requires Evaluator or higher role. Evaluators can only edit their own observations.
    /// </summary>
    [HttpPut("observations/{id:guid}")]
    [AuthorizeExerciseEvaluator]
    public async Task<ActionResult<ObservationDto>> UpdateObservation(Guid id, UpdateObservationRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "Content is required" });
        }

        if (request.Content.Length > 4000)
        {
            return BadRequest(new { message = "Content must be 4000 characters or less" });
        }

        if (request.Recommendation?.Length > 2000)
        {
            return BadRequest(new { message = "Recommendation must be 2000 characters or less" });
        }

        if (request.Location?.Length > 200)
        {
            return BadRequest(new { message = "Location must be 200 characters or less" });
        }

        try
        {
            var modifiedBy = User.GetUserId();
            var observation = await _observationService.UpdateObservationAsync(id, request, modifiedBy);

            if (observation == null)
            {
                return NotFound();
            }

            return Ok(observation);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete an observation (soft delete).
    /// Requires Exercise Director or Administrator role.
    /// </summary>
    [HttpDelete("observations/{id:guid}")]
    [AuthorizeExerciseDirector]
    public async Task<IActionResult> DeleteObservation(Guid id)
    {
        var deletedBy = User.GetUserId();
        var deleted = await _observationService.DeleteObservationAsync(id, deletedBy);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Validates that the inject's exercise belongs to the current user's organization.
    /// </summary>
    private async Task<IActionResult?> ValidateInjectOrgAccessAsync(Guid injectId)
    {
        var orgId = await _context.Injects
            .AsNoTracking()
            .Where(i => i.Id == injectId)
            .Select(i => i.Msel.Exercise.OrganizationId)
            .FirstOrDefaultAsync();

        if (orgId == default)
            return NotFound(new { message = "Inject not found" });

        if (!_orgContext.IsSysAdmin &&
            (!_orgContext.CurrentOrganizationId.HasValue ||
             _orgContext.CurrentOrganizationId.Value != orgId))
        {
            return StatusCode(403);
        }

        return null;
    }

    /// <summary>
    /// Validates that the observation belongs to the current user's organization.
    /// </summary>
    private async Task<IActionResult?> ValidateObservationOrgAccessAsync(Guid observationId)
    {
        var orgId = await _context.Observations
            .AsNoTracking()
            .Where(o => o.Id == observationId)
            .Select(o => o.OrganizationId)
            .FirstOrDefaultAsync();

        if (orgId == default)
            return NotFound();

        if (!_orgContext.IsSysAdmin &&
            (!_orgContext.CurrentOrganizationId.HasValue ||
             _orgContext.CurrentOrganizationId.Value != orgId))
        {
            return StatusCode(403);
        }

        return null;
    }

}
