using Cadence.Core.Features.ExpectedOutcomes.Models.DTOs;
using Cadence.Core.Features.ExpectedOutcomes.Services;
using Cadence.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for managing expected outcomes on injects.
/// Expected outcomes define what should happen when an inject is delivered
/// and can be evaluated during AAR.
/// Requires authentication for all endpoints.
/// </summary>
[ApiController]
[Route("api/injects/{injectId:guid}/outcomes")]
[Authorize]
public class ExpectedOutcomesController : ControllerBase
{
    private readonly IExpectedOutcomeService _service;
    private readonly ILogger<ExpectedOutcomesController> _logger;

    public ExpectedOutcomesController(
        IExpectedOutcomeService service,
        ILogger<ExpectedOutcomesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all expected outcomes for an inject.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpectedOutcomeDto>>> GetOutcomes(Guid injectId)
    {
        var validation = await _service.ValidateInjectAsync(injectId);
        if (!validation.InjectExists)
            return NotFound(new { message = "Inject not found" });

        var outcomes = await _service.GetByInjectIdAsync(injectId);
        return Ok(outcomes);
    }

    /// <summary>
    /// Get a single expected outcome by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExpectedOutcomeDto>> GetOutcome(Guid injectId, Guid id)
    {
        var validation = await _service.ValidateInjectAsync(injectId);
        if (!validation.InjectExists)
            return NotFound(new { message = "Inject not found" });

        var outcome = await _service.GetByIdAsync(id);
        if (outcome == null || outcome.InjectId != injectId)
            return NotFound(new { message = "Expected outcome not found" });

        return Ok(outcome);
    }

    /// <summary>
    /// Create a new expected outcome for an inject.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ExpectedOutcomeDto>> CreateOutcome(Guid injectId, CreateExpectedOutcomeRequest request)
    {
        var validation = await _service.ValidateInjectAsync(injectId);
        if (!validation.InjectExists)
            return NotFound(new { message = "Inject not found" });

        if (validation.ExerciseIsArchived)
            return BadRequest(new { message = "Cannot modify outcomes on archived exercises" });

        try
        {
            var outcome = await _service.CreateAsync(injectId, request, User.GetUserId());

            _logger.LogInformation("Created expected outcome {OutcomeId} for inject {InjectId}",
                outcome.Id, injectId);

            return CreatedAtAction(
                nameof(GetOutcome),
                new { injectId, id = outcome.Id },
                outcome
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an expected outcome's description.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExpectedOutcomeDto>> UpdateOutcome(Guid injectId, Guid id, UpdateExpectedOutcomeRequest request)
    {
        var validation = await _service.ValidateInjectAsync(injectId);
        if (!validation.InjectExists)
            return NotFound(new { message = "Inject not found" });

        if (validation.ExerciseIsArchived)
            return BadRequest(new { message = "Cannot modify outcomes on archived exercises" });

        // Validate outcome belongs to this inject
        var existingOutcome = await _service.GetByIdAsync(id);
        if (existingOutcome == null || existingOutcome.InjectId != injectId)
            return NotFound(new { message = "Expected outcome not found" });

        try
        {
            var outcome = await _service.UpdateAsync(id, request, User.GetUserId());

            _logger.LogInformation("Updated expected outcome {OutcomeId}", id);

            return Ok(outcome);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Evaluate an expected outcome (set WasAchieved and EvaluatorNotes).
    /// Used during AAR phase.
    /// </summary>
    [HttpPost("{id:guid}/evaluate")]
    public async Task<ActionResult<ExpectedOutcomeDto>> EvaluateOutcome(Guid injectId, Guid id, EvaluateExpectedOutcomeRequest request)
    {
        var validation = await _service.ValidateInjectAsync(injectId);
        if (!validation.InjectExists)
            return NotFound(new { message = "Inject not found" });

        // Validate outcome belongs to this inject
        var existingOutcome = await _service.GetByIdAsync(id);
        if (existingOutcome == null || existingOutcome.InjectId != injectId)
            return NotFound(new { message = "Expected outcome not found" });

        try
        {
            var outcome = await _service.EvaluateAsync(id, request, User.GetUserId());

            _logger.LogInformation("Evaluated expected outcome {OutcomeId}: WasAchieved={WasAchieved}",
                id, request.WasAchieved);

            return Ok(outcome);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reorder expected outcomes for an inject.
    /// </summary>
    [HttpPost("reorder")]
    public async Task<ActionResult> ReorderOutcomes(Guid injectId, ReorderExpectedOutcomesRequest request)
    {
        var validation = await _service.ValidateInjectAsync(injectId);
        if (!validation.InjectExists)
            return NotFound(new { message = "Inject not found" });

        if (validation.ExerciseIsArchived)
            return BadRequest(new { message = "Cannot modify outcomes on archived exercises" });

        try
        {
            var success = await _service.ReorderAsync(injectId, request, User.GetUserId());
            if (!success)
                return NotFound(new { message = "Inject not found" });

            _logger.LogInformation("Reordered expected outcomes for inject {InjectId}", injectId);

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete an expected outcome.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteOutcome(Guid injectId, Guid id)
    {
        var validation = await _service.ValidateInjectAsync(injectId);
        if (!validation.InjectExists)
            return NotFound(new { message = "Inject not found" });

        if (validation.ExerciseIsArchived)
            return BadRequest(new { message = "Cannot modify outcomes on archived exercises" });

        // Validate outcome belongs to this inject
        var existingOutcome = await _service.GetByIdAsync(id);
        if (existingOutcome == null || existingOutcome.InjectId != injectId)
            return NotFound(new { message = "Expected outcome not found" });

        var success = await _service.DeleteAsync(id, User.GetUserId());
        if (!success)
            return NotFound(new { message = "Expected outcome not found" });

        _logger.LogInformation("Deleted expected outcome {OutcomeId}", id);

        return NoContent();
    }

}
