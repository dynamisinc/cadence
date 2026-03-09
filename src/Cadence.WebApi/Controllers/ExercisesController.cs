using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Features.Msel.Models.DTOs;
using Cadence.Core.Features.Msel.Services;
using Cadence.Core.Models.Entities;
using Cadence.WebApi.Authorization;
using Cadence.WebApi.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for exercise management - CRUD, duplication, deletion, MSEL, setup, and settings.
/// Additional endpoints are in ExerciseClockController, ExerciseStatusController,
/// ExerciseParticipantsController, and ExerciseMetricsController.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExercisesController : ControllerBase
{
    private readonly IExerciseCrudService _exerciseCrudService;
    private readonly IExerciseDeleteService _deleteService;
    private readonly IMselService _mselService;
    private readonly ISetupProgressService _setupProgressService;
    private readonly IExerciseApprovalSettingsService _approvalSettingsService;
    private readonly IExerciseApprovalQueueService _approvalQueueService;
    private readonly ILogger<ExercisesController> _logger;

    public ExercisesController(
        IExerciseCrudService exerciseCrudService,
        IExerciseDeleteService deleteService,
        IMselService mselService,
        ISetupProgressService setupProgressService,
        IExerciseApprovalSettingsService approvalSettingsService,
        IExerciseApprovalQueueService approvalQueueService,
        ILogger<ExercisesController> logger)
    {
        _exerciseCrudService = exerciseCrudService;
        _deleteService = deleteService;
        _mselService = mselService;
        _setupProgressService = setupProgressService;
        _approvalSettingsService = approvalSettingsService;
        _approvalQueueService = approvalQueueService;
        _logger = logger;
    }

    // =========================================================================
    // Exercise CRUD Endpoints
    // =========================================================================

    /// <summary>
    /// Get all exercises with optional archive filtering.
    /// Includes inject count from active MSEL for each exercise.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExerciseDto>>> GetExercises(
        [FromQuery] bool includeArchived = false,
        [FromQuery] bool archivedOnly = false)
    {
        var userId = User.TryGetUserId();
        var exercises = await _exerciseCrudService.GetExercisesAsync(userId, includeArchived, archivedOnly);
        return Ok(exercises);
    }

    /// <summary>
    /// Get a single exercise by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExerciseDto>> GetExercise(Guid id)
    {
        var exercise = await _exerciseCrudService.GetExerciseAsync(id);

        if (exercise == null)
            return NotFound();

        return Ok(exercise);
    }

    /// <summary>
    /// Create a new exercise.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ExerciseDto>> CreateExercise(CreateExerciseRequest request)
    {
        var currentUserId = User.TryGetUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized(new { message = "User not authenticated" });

        try
        {
            var exercise = await _exerciseCrudService.CreateExerciseAsync(request, currentUserId);

            return CreatedAtAction(
                nameof(GetExercise),
                new { id = exercise.Id },
                exercise);
        }
        catch (ValidationException ex)
        {
            var firstError = ex.Errors.FirstOrDefault();
            return BadRequest(new
            {
                message = firstError?.ErrorMessage ?? ex.Message,
                field = firstError?.PropertyName?.ToLowerInvariant()
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message, field = "organization" });
        }
        catch (KeyNotFoundException)
        {
            return BadRequest(new { message = "User not found" });
        }
    }

    /// <summary>
    /// Update an existing exercise.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExerciseDto>> UpdateExercise(Guid id, UpdateExerciseRequest request)
    {
        try
        {
            var exercise = await _exerciseCrudService.UpdateExerciseAsync(id, request, User.GetUserId());

            if (exercise == null)
                return NotFound();

            return Ok(exercise);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return BadRequest(new { message = "User not found" });
        }
    }

    // =========================================================================
    // Exercise Duplication Endpoint
    // =========================================================================

    /// <summary>
    /// Duplicate an exercise with all its configuration.
    /// </summary>
    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<ExerciseDto>> DuplicateExercise(Guid id, [FromBody] DuplicateExerciseRequest? request = null)
    {
        var exercise = await _exerciseCrudService.DuplicateExerciseAsync(id, request);

        if (exercise == null)
            return NotFound(new { message = "Exercise not found" });

        _logger.LogInformation("Duplicated exercise {SourceId} to {NewId}: {NewName}",
            id, exercise.Id, exercise.Name);

        return CreatedAtAction(
            nameof(GetExercise),
            new { id = exercise.Id },
            exercise);
    }

    // =========================================================================
    // MSEL Endpoints
    // =========================================================================

    /// <summary>
    /// Get the active MSEL summary for an exercise.
    /// </summary>
    [HttpGet("{id:guid}/msel/summary")]
    public async Task<ActionResult<MselSummaryDto>> GetActiveMselSummary(Guid id)
    {
        var summary = await _mselService.GetActiveMselSummaryAsync(id);

        if (summary == null)
            return NotFound(new { message = "Exercise or active MSEL not found" });

        return Ok(summary);
    }

    /// <summary>
    /// Get all MSELs for an exercise.
    /// </summary>
    [HttpGet("{id:guid}/msels")]
    public async Task<ActionResult<IReadOnlyList<MselDto>>> GetMsels(Guid id)
    {
        if (!await _exerciseCrudService.ExerciseExistsAsync(id))
            return NotFound();

        var msels = await _mselService.GetMselsForExerciseAsync(id);

        return Ok(msels);
    }

    /// <summary>
    /// Get a specific MSEL summary by ID.
    /// </summary>
    [HttpGet("msels/{mselId:guid}/summary")]
    public async Task<ActionResult<MselSummaryDto>> GetMselSummary(Guid mselId)
    {
        var summary = await _mselService.GetMselSummaryAsync(mselId);

        if (summary == null)
            return NotFound();

        return Ok(summary);
    }

    // =========================================================================
    // Setup Progress Endpoint
    // =========================================================================

    /// <summary>
    /// Get the setup progress for an exercise.
    /// </summary>
    [HttpGet("{id:guid}/setup-progress")]
    public async Task<ActionResult<SetupProgressDto>> GetSetupProgress(Guid id)
    {
        var progress = await _setupProgressService.GetSetupProgressAsync(id);

        if (progress == null)
            return NotFound();

        return Ok(progress);
    }

    // =========================================================================
    // Delete Endpoints
    // =========================================================================

    /// <summary>
    /// Get a summary of what would be deleted if the exercise is permanently deleted.
    /// </summary>
    [HttpGet("{id:guid}/delete-summary")]
    public async Task<ActionResult<DeleteSummaryResponse>> GetDeleteSummary(Guid id)
    {
        var userId = User.TryGetUserId() ?? string.Empty;
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("OrgAdmin");

        var summary = await _deleteService.GetDeleteSummaryAsync(id, userId, isAdmin);

        if (summary == null)
            return NotFound();

        return Ok(summary);
    }

    /// <summary>
    /// Permanently delete an exercise and all related data.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteExercise(Guid id)
    {
        var userId = User.TryGetUserId() ?? string.Empty;
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("OrgAdmin");

        var result = await _deleteService.DeleteExerciseAsync(id, userId, isAdmin);

        if (!result.Success)
        {
            if (result.CannotDeleteReason == CannotDeleteReason.NotFound)
                return NotFound(new { message = result.ErrorMessage });
            if (result.CannotDeleteReason == CannotDeleteReason.NotAuthorized)
                return Forbid();
            return BadRequest(new { message = result.ErrorMessage, reason = result.CannotDeleteReason?.ToString() });
        }

        _logger.LogWarning("Exercise {ExerciseId} permanently deleted", id);

        return NoContent();
    }

    // =========================================================================
    // Exercise Settings Endpoints (S03-S05)
    // =========================================================================

    /// <summary>
    /// Get exercise settings (clock mode, auto-fire, confirmations).
    /// </summary>
    [HttpGet("{id:guid}/settings")]
    [AuthorizeExerciseAccess]
    [ProducesResponseType(typeof(ExerciseSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseSettingsDto>> GetExerciseSettings(Guid id)
    {
        var settings = await _exerciseCrudService.GetExerciseSettingsAsync(id);

        if (settings == null)
            return NotFound();

        return Ok(settings);
    }

    /// <summary>
    /// Update exercise settings.
    /// </summary>
    [HttpPut("{id:guid}/settings")]
    [AuthorizeExerciseDirector]
    [ProducesResponseType(typeof(ExerciseSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseSettingsDto>> UpdateExerciseSettings(
        Guid id,
        [FromBody] UpdateExerciseSettingsRequest request)
    {
        try
        {
            var settings = await _exerciseCrudService.UpdateExerciseSettingsAsync(id, request);

            if (settings == null)
                return NotFound();

            _logger.LogInformation(
                "Updated settings for exercise {ExerciseId}: ClockMultiplier={ClockMultiplier}, AutoFire={AutoFire}",
                id, settings.ClockMultiplier, settings.AutoFireEnabled);

            return Ok(settings);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================================================================
    // Approval Settings Endpoints (S02: Exercise Approval Configuration)
    // =========================================================================

    /// <summary>
    /// Get exercise approval settings.
    /// </summary>
    [HttpGet("{id:guid}/approval-settings")]
    [AuthorizeExerciseAccess]
    [ProducesResponseType(typeof(ApprovalSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApprovalSettingsDto>> GetApprovalSettings(Guid id)
    {
        try
        {
            var settings = await _approvalSettingsService.GetApprovalSettingsAsync(id);
            return Ok(settings);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Update exercise approval settings.
    /// </summary>
    [HttpPut("{id:guid}/approval-settings")]
    [AuthorizeExerciseDirector]
    [ProducesResponseType(typeof(ApprovalSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApprovalSettingsDto>> UpdateApprovalSettings(
        Guid id,
        [FromBody] UpdateApprovalSettingsRequest request)
    {
        try
        {
            var userId = User.GetUserId();

            var settings = await _approvalSettingsService.UpdateApprovalSettingsAsync(
                id,
                request,
                userId);

            _logger.LogInformation(
                "Updated approval settings for exercise {ExerciseId}: RequireApproval={RequireApproval}",
                id, request.RequireInjectApproval);

            return Ok(settings);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex,
                "Invalid approval settings update for exercise {ExerciseId}",
                id);
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================================================================
    // Approval Queue Endpoints (S06: Approval Queue View)
    // =========================================================================

    /// <summary>
    /// Get approval status summary for an exercise.
    /// </summary>
    [HttpGet("{id:guid}/approval-status")]
    [AuthorizeExerciseAccess]
    [ProducesResponseType(typeof(ApprovalStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApprovalStatusDto>> GetApprovalStatus(Guid id)
    {
        try
        {
            var status = await _approvalQueueService.GetApprovalStatusAsync(id);
            return Ok(status);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Exercise {ExerciseId} not found for approval status", id);
            return NotFound(new { message = ex.Message });
        }
    }
}
