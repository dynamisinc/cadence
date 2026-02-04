using System.Security.Claims;
using Cadence.Core.Constants;
using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Features.Msel.Models.DTOs;
using Cadence.Core.Features.Msel.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    private readonly AppDbContext _context;
    private readonly IExerciseDeleteService _deleteService;
    private readonly IMselService _mselService;
    private readonly ISetupProgressService _setupProgressService;
    private readonly IExerciseParticipantService _participantService;
    private readonly IExerciseApprovalSettingsService _approvalSettingsService;
    private readonly IExerciseApprovalQueueService _approvalQueueService;
    private readonly ICurrentOrganizationContext _orgContext;
    private readonly ILogger<ExercisesController> _logger;

    public ExercisesController(
        AppDbContext context,
        IExerciseDeleteService deleteService,
        IMselService mselService,
        ISetupProgressService setupProgressService,
        IExerciseParticipantService participantService,
        IExerciseApprovalSettingsService approvalSettingsService,
        IExerciseApprovalQueueService approvalQueueService,
        ICurrentOrganizationContext orgContext,
        ILogger<ExercisesController> logger)
    {
        _context = context;
        _deleteService = deleteService;
        _mselService = mselService;
        _setupProgressService = setupProgressService;
        _participantService = participantService;
        _approvalSettingsService = approvalSettingsService;
        _approvalQueueService = approvalQueueService;
        _orgContext = orgContext;
        _logger = logger;
    }

    // =========================================================================
    // Exercise CRUD Endpoints
    // =========================================================================

    /// <summary>
    /// Get all exercises with optional archive filtering.
    /// Includes inject count from active MSEL for each exercise.
    /// </summary>
    /// <param name="includeArchived">Include archived exercises (default: false)</param>
    /// <param name="archivedOnly">Return only archived exercises (default: false)</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExerciseDto>>> GetExercises(
        [FromQuery] bool includeArchived = false,
        [FromQuery] bool archivedOnly = false)
    {
        var query = _context.Exercises.AsQueryable();

        // Filter by organization context (SysAdmins see all, others see only their org)
        if (!_orgContext.IsSysAdmin && _orgContext.CurrentOrganizationId.HasValue)
        {
            query = query.Where(e => e.OrganizationId == _orgContext.CurrentOrganizationId.Value);
        }
        else if (!_orgContext.IsSysAdmin && !_orgContext.CurrentOrganizationId.HasValue)
        {
            // Non-SysAdmin with no org context sees nothing
            return Ok(Array.Empty<ExerciseDto>());
        }
        // SysAdmins with org context filter to that org for consistency
        else if (_orgContext.IsSysAdmin && _orgContext.CurrentOrganizationId.HasValue)
        {
            query = query.Where(e => e.OrganizationId == _orgContext.CurrentOrganizationId.Value);
        }

        // Apply archive filter
        if (archivedOnly)
        {
            query = query.Where(e => e.Status == ExerciseStatus.Archived);
        }
        else if (!includeArchived)
        {
            query = query.Where(e => e.Status != ExerciseStatus.Archived);
        }

        // Project to include inject counts from active MSEL in a single query
        var exercises = await query
            .OrderByDescending(e => e.ScheduledDate)
            .Select(e => new
            {
                Exercise = e,
                InjectCount = e.ActiveMselId != null
                    ? _context.Injects.Count(i => i.MselId == e.ActiveMselId)
                    : 0,
                FiredInjectCount = e.ActiveMselId != null
                    ? _context.Injects.Count(i => i.MselId == e.ActiveMselId && i.Status == InjectStatus.Released)
                    : 0
            })
            .ToListAsync();

        return Ok(exercises.Select(x => x.Exercise.ToDto(x.InjectCount, x.FiredInjectCount)));
    }

    /// <summary>
    /// Get a single exercise by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExerciseDto>> GetExercise(Guid id)
    {
        var exercise = await _context.Exercises.FindAsync(id);

        if (exercise == null)
        {
            return NotFound();
        }

        return Ok(exercise.ToDto());
    }

    /// <summary>
    /// Create a new exercise.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ExerciseDto>> CreateExercise(CreateExerciseRequest request)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Validation
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            _logger.LogWarning(
                "CreateExercise validation failed: Name is required. User: {UserId}, Request: {@Request}",
                currentUserId, new { request.Name, request.ExerciseType, request.DirectorId });
            return BadRequest(new { message = "Name is required", field = "name" });
        }

        if (request.Name.Length > 200)
        {
            _logger.LogWarning(
                "CreateExercise validation failed: Name too long ({Length} chars). User: {UserId}",
                request.Name.Length, currentUserId);
            return BadRequest(new { message = "Name must be 200 characters or less", field = "name" });
        }

        // Require organization context to create exercises
        if (!_orgContext.CurrentOrganizationId.HasValue)
        {
            _logger.LogWarning(
                "CreateExercise validation failed: No organization context. User: {UserId}, IsSysAdmin: {IsSysAdmin}",
                currentUserId, _orgContext.IsSysAdmin);
            return BadRequest(new { message = "Organization context required. Please select an organization.", field = "organization" });
        }

        var organizationId = _orgContext.CurrentOrganizationId.Value;

        // Validate user is authenticated (currentUserId was retrieved at method start for logging)
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        // For audit trail, use Guid.Empty until we update BaseEntity to use string
        var createdBy = SystemConstants.SystemUserIdString;
        var exercise = request.ToEntity(organizationId, createdBy);

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created exercise {ExerciseId}: {ExerciseName} by user {UserId}",
            exercise.Id, exercise.Name, currentUserId);

        // Assign Exercise Director
        string? directorId = request.DirectorId;

        // If no directorId provided, use creator if they are Admin or Manager
        if (string.IsNullOrEmpty(directorId))
        {
            var currentUser = await _context.ApplicationUsers.FindAsync(currentUserId);
            if (currentUser != null &&
                (currentUser.SystemRole == SystemRole.Admin || currentUser.SystemRole == SystemRole.Manager))
            {
                directorId = currentUserId;
            }
        }

        // Assign director if we have a valid ID
        if (!string.IsNullOrEmpty(directorId))
        {
            try
            {
                await _participantService.AddParticipantAsync(
                    exercise.Id,
                    new AddParticipantRequest
                    {
                        UserId = directorId,
                        Role = ExerciseRole.ExerciseDirector.ToString()
                    });

                _logger.LogInformation(
                    "Assigned user {UserId} as Exercise Director for exercise {ExerciseId}",
                    directorId, exercise.Id);
            }
            catch (KeyNotFoundException)
            {
                return BadRequest(new { message = "User not found" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to assign user {UserId} as Director for exercise {ExerciseId}. Exercise created successfully.",
                    directorId, exercise.Id);
                // Don't fail the exercise creation if auto-assignment fails
            }
        }

        return CreatedAtAction(
            nameof(GetExercise),
            new { id = exercise.Id },
            exercise.ToDto()
        );
    }

    /// <summary>
    /// Update an existing exercise.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExerciseDto>> UpdateExercise(Guid id, UpdateExerciseRequest request)
    {
        var exercise = await _context.Exercises.FindAsync(id);

        if (exercise == null)
        {
            return NotFound();
        }

        // Validation
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name is required" });
        }

        if (request.Name.Length > 200)
        {
            return BadRequest(new { message = "Name must be 200 characters or less" });
        }

        // Check status-based edit restrictions
        if (exercise.Status == ExerciseStatus.Completed || exercise.Status == ExerciseStatus.Archived)
        {
            return BadRequest(new { message = $"{exercise.Status} exercises cannot be modified" });
        }

        // Update fields (respecting status-based restrictions)
        exercise.Name = request.Name;
        exercise.Description = request.Description;

        // These fields can only be changed in Draft status
        if (exercise.Status == ExerciseStatus.Draft)
        {
            exercise.ExerciseType = request.ExerciseType;
            exercise.ScheduledDate = request.ScheduledDate;
            exercise.StartTime = request.StartTime;
            // Timing configuration fields (CLK-01)
            exercise.DeliveryMode = request.DeliveryMode;
            exercise.TimelineMode = request.TimelineMode;
            // ClockMultiplier is the source of truth; TimeScale is kept in sync for backwards compatibility
            exercise.ClockMultiplier = request.ClockMultiplier;
            exercise.TimeScale = request.ClockMultiplier;
        }

        // End time can always be updated (as long as not Completed/Archived)
        exercise.EndTime = request.EndTime;
        exercise.Location = request.Location;
        exercise.TimeZoneId = request.TimeZoneId;
        exercise.IsPracticeMode = request.IsPracticeMode;

        // System user until auth is implemented
        exercise.ModifiedBy = SystemConstants.SystemUserIdString;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated exercise {ExerciseId}: {ExerciseName}", exercise.Id, exercise.Name);

        // Handle director reassignment if provided
        if (!string.IsNullOrEmpty(request.DirectorId))
        {
            try
            {
                // Check if there's already a director
                var participants = await _participantService.GetParticipantsAsync(exercise.Id);
                var existingDirector = participants.FirstOrDefault(p => p.ExerciseRole == ExerciseRole.ExerciseDirector.ToString());

                // If there's a different director, we need to replace them
                if (existingDirector != null && existingDirector.UserId != request.DirectorId)
                {
                    // Remove old director
                    await _participantService.RemoveParticipantAsync(exercise.Id, existingDirector.UserId);

                    _logger.LogInformation(
                        "Removed previous director {OldDirectorId} from exercise {ExerciseId}",
                        existingDirector.UserId, exercise.Id);
                }

                // Add or update new director (only if not already director)
                if (existingDirector == null || existingDirector.UserId != request.DirectorId)
                {
                    // Check if user is already a participant with a different role
                    var existingParticipant = participants.FirstOrDefault(p => p.UserId == request.DirectorId);

                    if (existingParticipant != null)
                    {
                        // Update their role to Director
                        await _participantService.UpdateParticipantRoleAsync(
                            exercise.Id,
                            request.DirectorId,
                            new UpdateParticipantRoleRequest { Role = ExerciseRole.ExerciseDirector.ToString() });
                    }
                    else
                    {
                        // Add as new director
                        await _participantService.AddParticipantAsync(
                            exercise.Id,
                            new AddParticipantRequest
                            {
                                UserId = request.DirectorId,
                                Role = ExerciseRole.ExerciseDirector.ToString()
                            });
                    }

                    _logger.LogInformation(
                        "Assigned user {UserId} as Exercise Director for exercise {ExerciseId}",
                        request.DirectorId, exercise.Id);
                }
            }
            catch (KeyNotFoundException)
            {
                return BadRequest(new { message = "User not found" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to update director for exercise {ExerciseId}",
                    exercise.Id);
                // Don't fail the exercise update if director assignment fails
            }
        }

        return Ok(exercise.ToDto());
    }

    // =========================================================================
    // Exercise Duplication Endpoint
    // =========================================================================

    /// <summary>
    /// Duplicate an exercise with all its configuration.
    /// Creates a copy of the exercise, MSEL, injects, phases, and objectives.
    /// The new exercise starts in Draft status with a new date.
    /// </summary>
    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<ExerciseDto>> DuplicateExercise(Guid id, [FromBody] DuplicateExerciseRequest? request = null)
    {
        // Load the source exercise with all related data
        var source = await _context.Exercises
            .Include(e => e.Phases)
            .Include(e => e.Objectives)
            .Include(e => e.Msels)
                .ThenInclude(m => m.Injects)
                    .ThenInclude(i => i.InjectObjectives)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (source == null)
        {
            return NotFound(new { message = "Exercise not found" });
        }

        // Generate new name if not provided
        var newName = request?.Name ?? $"Copy of {source.Name}";
        if (newName.Length > 200)
        {
            newName = newName.Substring(0, 200);
        }

        // Create new exercise as copy
        var newExercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = newName,
            Description = source.Description,
            ExerciseType = source.ExerciseType,
            Status = ExerciseStatus.Draft, // Always start as Draft
            IsPracticeMode = source.IsPracticeMode,
            ScheduledDate = request?.ScheduledDate ?? source.ScheduledDate,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            TimeZoneId = source.TimeZoneId,
            Location = source.Location,
            OrganizationId = source.OrganizationId,
            // Clock state reset for new exercise
            ClockState = ExerciseClockState.Stopped,
            ClockStartedAt = null,
            ClockElapsedBeforePause = null,
            ClockStartedBy = null,
            CreatedBy = SystemConstants.SystemUserIdString,
            ModifiedBy = SystemConstants.SystemUserIdString,
        };

        _context.Exercises.Add(newExercise);

        // Map old IDs to new IDs for reference updates
        var phaseIdMap = new Dictionary<Guid, Guid>();
        var objectiveIdMap = new Dictionary<Guid, Guid>();

        // Copy phases
        foreach (var sourcePhase in source.Phases.OrderBy(p => p.Sequence))
        {
            var newPhaseId = Guid.NewGuid();
            phaseIdMap[sourcePhase.Id] = newPhaseId;

            var newPhase = new Phase
            {
                Id = newPhaseId,
                Name = sourcePhase.Name,
                Description = sourcePhase.Description,
                Sequence = sourcePhase.Sequence,
                StartTime = sourcePhase.StartTime,
                EndTime = sourcePhase.EndTime,
                ExerciseId = newExercise.Id,
                OrganizationId = source.OrganizationId, // Data isolation
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString,
            };
            _context.Phases.Add(newPhase);
        }

        // Copy objectives
        foreach (var sourceObjective in source.Objectives.OrderBy(o => o.ObjectiveNumber))
        {
            var newObjectiveId = Guid.NewGuid();
            objectiveIdMap[sourceObjective.Id] = newObjectiveId;

            var newObjective = new Objective
            {
                Id = newObjectiveId,
                ObjectiveNumber = sourceObjective.ObjectiveNumber,
                Name = sourceObjective.Name,
                Description = sourceObjective.Description,
                ExerciseId = newExercise.Id,
                OrganizationId = source.OrganizationId, // Data isolation
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString,
            };
            _context.Objectives.Add(newObjective);
        }

        // Copy the active MSEL (or first MSEL if none active)
        var sourceMsel = source.Msels.FirstOrDefault(m => m.IsActive) ?? source.Msels.FirstOrDefault();
        Guid? newMselId = null;
        if (sourceMsel != null)
        {
            newMselId = Guid.NewGuid();

            var newMsel = new Msel
            {
                Id = newMselId.Value,
                Name = "v1.0",
                Description = sourceMsel.Description,
                Version = 1,
                IsActive = true,
                ExerciseId = newExercise.Id,
                OrganizationId = source.OrganizationId, // Data isolation
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString,
            };
            _context.Msels.Add(newMsel);

            // Copy injects (reset status to Pending)
            foreach (var sourceInject in sourceMsel.Injects.OrderBy(i => i.Sequence))
            {
                var newInjectId = Guid.NewGuid();

                var newInject = new Inject
                {
                    Id = newInjectId,
                    InjectNumber = sourceInject.InjectNumber,
                    Title = sourceInject.Title,
                    Description = sourceInject.Description,
                    ScheduledTime = sourceInject.ScheduledTime,
                    ScenarioDay = sourceInject.ScenarioDay,
                    ScenarioTime = sourceInject.ScenarioTime,
                    Target = sourceInject.Target,
                    Source = sourceInject.Source,
                    DeliveryMethod = sourceInject.DeliveryMethod,
                    InjectType = sourceInject.InjectType,
                    Status = InjectStatus.Draft, // Always reset to Draft
                    Sequence = sourceInject.Sequence,
                    ParentInjectId = null,
                    FireCondition = sourceInject.FireCondition,
                    ExpectedAction = sourceInject.ExpectedAction,
                    ControllerNotes = sourceInject.ControllerNotes,
                    // Conduct data NOT copied
                    FiredAt = null,
                    FiredByUserId = null,
                    SkippedAt = null,
                    SkippedByUserId = null,
                    SkipReason = null,
                    MselId = newMselId.Value,
                    PhaseId = sourceInject.PhaseId.HasValue && phaseIdMap.ContainsKey(sourceInject.PhaseId.Value)
                        ? phaseIdMap[sourceInject.PhaseId.Value]
                        : null,
                    CreatedBy = SystemConstants.SystemUserIdString,
                    ModifiedBy = SystemConstants.SystemUserIdString,
                };
                _context.Injects.Add(newInject);

                // Copy inject-objective links
                foreach (var sourceLink in sourceInject.InjectObjectives)
                {
                    if (objectiveIdMap.ContainsKey(sourceLink.ObjectiveId))
                    {
                        _context.InjectObjectives.Add(new InjectObjective
                        {
                            InjectId = newInjectId,
                            ObjectiveId = objectiveIdMap[sourceLink.ObjectiveId],
                        });
                    }
                }
            }
        }

        // First save: Create all entities without the circular reference
        await _context.SaveChangesAsync();

        // Second save: Now set the ActiveMselId to complete the relationship
        if (newMselId.HasValue)
        {
            newExercise.ActiveMselId = newMselId.Value;
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Duplicated exercise {SourceId} to {NewId}: {NewName}",
            id, newExercise.Id, newExercise.Name);

        return CreatedAtAction(
            nameof(GetExercise),
            new { id = newExercise.Id },
            newExercise.ToDto()
        );
    }

    // =========================================================================
    // MSEL Endpoints
    // =========================================================================

    /// <summary>
    /// Get the active MSEL summary for an exercise.
    /// Returns progress metrics, counts, and last modified info.
    /// </summary>
    [HttpGet("{id:guid}/msel/summary")]
    public async Task<ActionResult<MselSummaryDto>> GetActiveMselSummary(Guid id)
    {
        var summary = await _mselService.GetActiveMselSummaryAsync(id);

        if (summary == null)
        {
            return NotFound(new { message = "Exercise or active MSEL not found" });
        }

        return Ok(summary);
    }

    /// <summary>
    /// Get all MSELs for an exercise.
    /// </summary>
    [HttpGet("{id:guid}/msels")]
    public async Task<ActionResult<IReadOnlyList<MselDto>>> GetMsels(Guid id)
    {
        var exercise = await _context.Exercises.FindAsync(id);

        if (exercise == null)
        {
            return NotFound();
        }

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
        {
            return NotFound();
        }

        return Ok(summary);
    }

    // =========================================================================
    // Setup Progress Endpoint
    // =========================================================================

    /// <summary>
    /// Get the setup progress for an exercise.
    /// Shows completion status for each configuration area (MSEL, Phases, Objectives, Scheduling).
    /// </summary>
    [HttpGet("{id:guid}/setup-progress")]
    public async Task<ActionResult<SetupProgressDto>> GetSetupProgress(Guid id)
    {
        var progress = await _setupProgressService.GetSetupProgressAsync(id);

        if (progress == null)
        {
            return NotFound();
        }

        return Ok(progress);
    }

    // =========================================================================
    // Delete Endpoints
    // =========================================================================

    /// <summary>
    /// Get a summary of what would be deleted if the exercise is permanently deleted.
    /// Also indicates whether the exercise can be deleted based on its status.
    /// </summary>
    [HttpGet("{id:guid}/delete-summary")]
    public async Task<ActionResult<DeleteSummaryResponse>> GetDeleteSummary(Guid id)
    {
        var userId = SystemConstants.SystemUserIdString;
        var isAdmin = true;

        var summary = await _deleteService.GetDeleteSummaryAsync(id, userId, isAdmin);

        if (summary == null)
        {
            return NotFound();
        }

        return Ok(summary);
    }

    /// <summary>
    /// Permanently delete an exercise and all related data.
    /// This action is irreversible.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteExercise(Guid id)
    {
        var userId = SystemConstants.SystemUserIdString;
        var isAdmin = true;

        var result = await _deleteService.DeleteExerciseAsync(id, userId, isAdmin);

        if (!result.Success)
        {
            if (result.CannotDeleteReason == CannotDeleteReason.NotFound)
            {
                return NotFound(new { message = result.ErrorMessage });
            }
            if (result.CannotDeleteReason == CannotDeleteReason.NotAuthorized)
            {
                return Forbid();
            }
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
        var exercise = await _context.Exercises.FindAsync(id);

        if (exercise == null)
        {
            return NotFound();
        }

        return Ok(new ExerciseSettingsDto(
            exercise.ClockMultiplier,
            exercise.AutoFireEnabled,
            exercise.ConfirmFireInject,
            exercise.ConfirmSkipInject,
            exercise.ConfirmClockControl
        ));
    }

    /// <summary>
    /// Update exercise settings.
    /// Only Directors+ can modify settings.
    /// Clock multiplier can only be changed when exercise is paused or in draft.
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
        var exercise = await _context.Exercises.FindAsync(id);

        if (exercise == null)
        {
            return NotFound();
        }

        // Validate clock multiplier change
        if (request.ClockMultiplier.HasValue)
        {
            // Validate range
            if (request.ClockMultiplier < 0.5m || request.ClockMultiplier > 20.0m)
            {
                return BadRequest(new { message = "Clock multiplier must be between 0.5 and 20" });
            }

            // Can only change when paused or draft
            if (exercise.ClockState == ExerciseClockState.Running)
            {
                return BadRequest(new { message = "Cannot change clock multiplier while clock is running. Pause the exercise first." });
            }

            exercise.ClockMultiplier = request.ClockMultiplier.Value;
            exercise.TimeScale = request.ClockMultiplier.Value;
        }

        // Update boolean settings (can be changed anytime)
        if (request.AutoFireEnabled.HasValue)
        {
            exercise.AutoFireEnabled = request.AutoFireEnabled.Value;
        }

        if (request.ConfirmFireInject.HasValue)
        {
            exercise.ConfirmFireInject = request.ConfirmFireInject.Value;
        }

        if (request.ConfirmSkipInject.HasValue)
        {
            exercise.ConfirmSkipInject = request.ConfirmSkipInject.Value;
        }

        if (request.ConfirmClockControl.HasValue)
        {
            exercise.ConfirmClockControl = request.ConfirmClockControl.Value;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated settings for exercise {ExerciseId}: ClockMultiplier={ClockMultiplier}, AutoFire={AutoFire}",
            id, exercise.ClockMultiplier, exercise.AutoFireEnabled);

        return Ok(new ExerciseSettingsDto(
            exercise.ClockMultiplier,
            exercise.AutoFireEnabled,
            exercise.ConfirmFireInject,
            exercise.ConfirmSkipInject,
            exercise.ConfirmClockControl
        ));
    }

    // =========================================================================
    // Approval Settings Endpoints (S02: Exercise Approval Configuration)
    // =========================================================================

    /// <summary>
    /// Get exercise approval settings.
    /// Returns approval configuration and organization policy context.
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
    /// Only Directors+ can modify settings.
    /// Admins can override Required organization policy.
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
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User not authenticated");

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
    /// Returns counts of injects by approval status (Draft, Submitted, Approved).
    /// Used for dashboard alerts and MSEL header summary.
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
