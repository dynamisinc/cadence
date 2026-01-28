using System.Security.Claims;
using Cadence.Core.Constants;
using Cadence.Core.Data;
using Cadence.Core.Features.ExerciseClock.Models.DTOs;
using Cadence.Core.Features.ExerciseClock.Services;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Features.Metrics.Models.DTOs;
using Cadence.Core.Features.Metrics.Services;
using Cadence.Core.Features.Msel.Models.DTOs;
using Cadence.Core.Features.Msel.Services;
using Cadence.Core.Models.Entities;
using Cadence.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for exercise management.
/// Requires authentication for all endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExercisesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IExerciseClockService _clockService;
    private readonly IExerciseStatusService _statusService;
    private readonly IExerciseDeleteService _deleteService;
    private readonly IMselService _mselService;
    private readonly ISetupProgressService _setupProgressService;
    private readonly IExerciseParticipantService _participantService;
    private readonly IExerciseMetricsService _metricsService;
    private readonly ILogger<ExercisesController> _logger;

    public ExercisesController(
        AppDbContext context,
        IExerciseClockService clockService,
        IExerciseStatusService statusService,
        IExerciseDeleteService deleteService,
        IMselService mselService,
        ISetupProgressService setupProgressService,
        IExerciseParticipantService participantService,
        IExerciseMetricsService metricsService,
        ILogger<ExercisesController> logger)
    {
        _context = context;
        _clockService = clockService;
        _statusService = statusService;
        _deleteService = deleteService;
        _mselService = mselService;
        _setupProgressService = setupProgressService;
        _participantService = participantService;
        _metricsService = metricsService;
        _logger = logger;
    }

    /// <summary>
    /// Get all exercises with optional archive filtering.
    /// </summary>
    /// <param name="includeArchived">Include archived exercises (default: false)</param>
    /// <param name="archivedOnly">Return only archived exercises (default: false)</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExerciseDto>>> GetExercises(
        [FromQuery] bool includeArchived = false,
        [FromQuery] bool archivedOnly = false)
    {
        var query = _context.Exercises.AsQueryable();

        // Apply archive filter
        if (archivedOnly)
        {
            query = query.Where(e => e.Status == ExerciseStatus.Archived);
        }
        else if (!includeArchived)
        {
            query = query.Where(e => e.Status != ExerciseStatus.Archived);
        }

        var exercises = await query
            .OrderByDescending(e => e.ScheduledDate)
            .ToListAsync();

        return Ok(exercises.Select(e => e.ToDto()));
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
        // Validation
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name is required" });
        }

        if (request.Name.Length > 200)
        {
            return BadRequest(new { message = "Name must be 200 characters or less" });
        }

        // Use default organization (seeded in database)
        var organization = await _context.Organizations.FirstOrDefaultAsync();
        if (organization == null)
        {
            return StatusCode(500, new { message = "Default organization not found. Please run database migrations." });
        }

        // Get current user ID from claims (ApplicationUser.Id is string)
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        // For audit trail, use Guid.Empty until we update BaseEntity to use string
        var createdBy = SystemConstants.SystemUserId;
        var exercise = request.ToEntity(organization.Id, createdBy);

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
        exercise.ModifiedBy = SystemConstants.SystemUserId;

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
    // Exercise Clock Endpoints
    // =========================================================================

    /// <summary>
    /// Get the current clock state for an exercise.
    /// </summary>
    [HttpGet("{id:guid}/clock")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<ClockStateDto>> GetClockState(Guid id)
    {
        var clockState = await _clockService.GetClockStateAsync(id);

        if (clockState == null)
        {
            return NotFound();
        }

        return Ok(clockState);
    }

    /// <summary>
    /// Start the exercise clock.
    /// This also transitions the exercise from Draft to Active status.
    /// </summary>
    [HttpPost("{id:guid}/clock/start")]
    [AuthorizeExerciseController]
    public async Task<ActionResult<ClockStateDto>> StartClock(Guid id)
    {
        try
        {
            var startedBy = GetCurrentUserIdString();

            var clockState = await _clockService.StartClockAsync(id, startedBy);

            _logger.LogInformation("Started clock for exercise {ExerciseId}", id);

            return Ok(clockState);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Pause the exercise clock.
    /// Preserves elapsed time for later resumption.
    /// </summary>
    [HttpPost("{id:guid}/clock/pause")]
    [AuthorizeExerciseController]
    public async Task<ActionResult<ClockStateDto>> PauseClock(Guid id)
    {
        try
        {
            var pausedBy = GetCurrentUserId();

            var clockState = await _clockService.PauseClockAsync(id, pausedBy);

            _logger.LogInformation("Paused clock for exercise {ExerciseId}", id);

            return Ok(clockState);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Stop the exercise clock and complete the exercise.
    /// This transitions the exercise to Completed status.
    /// </summary>
    [HttpPost("{id:guid}/clock/stop")]
    [AuthorizeExerciseController]
    public async Task<ActionResult<ClockStateDto>> StopClock(Guid id)
    {
        try
        {
            var stoppedBy = GetCurrentUserId();

            var clockState = await _clockService.StopClockAsync(id, stoppedBy);

            _logger.LogInformation("Stopped clock for exercise {ExerciseId}. Exercise completed.", id);

            return Ok(clockState);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reset the exercise clock to zero.
    /// Only allowed for Draft exercises or when clock is Stopped.
    /// </summary>
    [HttpPost("{id:guid}/clock/reset")]
    [AuthorizeExerciseController]
    public async Task<ActionResult<ClockStateDto>> ResetClock(Guid id)
    {
        try
        {
            var resetBy = GetCurrentUserId();

            var clockState = await _clockService.ResetClockAsync(id, resetBy);

            _logger.LogInformation("Reset clock for exercise {ExerciseId}", id);

            return Ok(clockState);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
            CreatedBy = SystemConstants.SystemUserId,
            ModifiedBy = SystemConstants.SystemUserId,
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
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
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
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
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
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
            };
            _context.Msels.Add(newMsel);

            // Note: ActiveMselId is set AFTER first SaveChanges to avoid circular dependency
            // (Exercise -> Msel via ActiveMselId, Msel -> Exercise via ExerciseId)

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
                    Status = InjectStatus.Pending, // Always reset to Pending
                    Sequence = sourceInject.Sequence,
                    // Skip ParentInjectId for simplicity (branching would need additional mapping)
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
                    // Map phase ID if inject was assigned to a phase
                    PhaseId = sourceInject.PhaseId.HasValue && phaseIdMap.ContainsKey(sourceInject.PhaseId.Value)
                        ? phaseIdMap[sourceInject.PhaseId.Value]
                        : null,
                    CreatedBy = SystemConstants.SystemUserId,
                    ModifiedBy = SystemConstants.SystemUserId,
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
        // This avoids the circular dependency (Exercise -> Msel -> Exercise)
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
    // Exercise Status Workflow Endpoints
    // =========================================================================

    /// <summary>
    /// Activate an exercise (Draft → Active).
    /// Requires at least one inject in the MSEL.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<ExerciseDto>> ActivateExercise(Guid id)
    {
        // System user until auth is implemented
        var userId = SystemConstants.SystemUserId;

        var result = await _statusService.ActivateAsync(id, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Activated exercise {ExerciseId}", id);

        return Ok(result.Exercise);
    }

    /// <summary>
    /// Pause an exercise (Active → Paused).
    /// Preserves clock elapsed time.
    /// </summary>
    [HttpPost("{id:guid}/pause")]
    public async Task<ActionResult<ExerciseDto>> PauseExercise(Guid id)
    {
        // System user until auth is implemented
        var userId = SystemConstants.SystemUserId;

        var result = await _statusService.PauseAsync(id, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Paused exercise {ExerciseId}", id);

        return Ok(result.Exercise);
    }

    /// <summary>
    /// Resume a paused exercise (Paused → Active).
    /// </summary>
    [HttpPost("{id:guid}/resume")]
    public async Task<ActionResult<ExerciseDto>> ResumeExercise(Guid id)
    {
        // System user until auth is implemented
        var userId = SystemConstants.SystemUserId;

        var result = await _statusService.ResumeAsync(id, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Resumed exercise {ExerciseId}", id);

        return Ok(result.Exercise);
    }

    /// <summary>
    /// Complete an exercise (Active/Paused → Completed).
    /// Permanently stops the clock.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<ExerciseDto>> CompleteExercise(Guid id)
    {
        // System user until auth is implemented
        var userId = SystemConstants.SystemUserId;

        var result = await _statusService.CompleteAsync(id, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Completed exercise {ExerciseId}", id);

        return Ok(result.Exercise);
    }

    /// <summary>
    /// Archive a completed exercise (Completed → Archived).
    /// Makes the exercise fully read-only.
    /// </summary>
    [HttpPost("{id:guid}/archive")]
    public async Task<ActionResult<ExerciseDto>> ArchiveExercise(Guid id)
    {
        // System user until auth is implemented
        var userId = SystemConstants.SystemUserId;

        var result = await _statusService.ArchiveAsync(id, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Archived exercise {ExerciseId}", id);

        return Ok(result.Exercise);
    }

    /// <summary>
    /// Unarchive an exercise (Archived → Completed).
    /// Restores the exercise to completed status.
    /// </summary>
    [HttpPost("{id:guid}/unarchive")]
    public async Task<ActionResult<ExerciseDto>> UnarchiveExercise(Guid id)
    {
        // System user until auth is implemented
        var userId = SystemConstants.SystemUserId;

        var result = await _statusService.UnarchiveAsync(id, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Unarchived exercise {ExerciseId}", id);

        return Ok(result.Exercise);
    }

    /// <summary>
    /// Revert a paused exercise to draft (Paused → Draft).
    /// WARNING: This clears all conduct data (fired times, observations).
    /// </summary>
    [HttpPost("{id:guid}/revert-to-draft")]
    public async Task<ActionResult<ExerciseDto>> RevertToDraft(Guid id)
    {
        // System user until auth is implemented
        var userId = SystemConstants.SystemUserId;

        var result = await _statusService.RevertToDraftAsync(id, userId);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogWarning(
            "Exercise {ExerciseId} reverted to Draft - conduct data cleared",
            id);

        return Ok(result.Exercise);
    }

    /// <summary>
    /// Get available status transitions for an exercise.
    /// </summary>
    [HttpGet("{id:guid}/available-transitions")]
    public async Task<ActionResult<IReadOnlyList<ExerciseStatus>>> GetAvailableTransitions(Guid id)
    {
        var exercise = await _context.Exercises.FindAsync(id);

        if (exercise == null)
        {
            return NotFound();
        }

        var transitions = _statusService.GetAvailableTransitions(exercise.Status);

        return Ok(transitions);
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
    // Setup Progress Endpoints
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
    ///
    /// Delete eligibility rules:
    /// - Draft exercises that have never been published: Creator OR Administrator
    /// - Archived exercises: Administrator only
    /// - Published/Active/Completed exercises (not archived): Cannot delete - must archive first
    /// </summary>
    [HttpGet("{id:guid}/delete-summary")]
    public async Task<ActionResult<DeleteSummaryResponse>> GetDeleteSummary(Guid id)
    {
        // TODO: Get actual user ID and admin status from auth context
        var userId = SystemConstants.SystemUserId;
        var isAdmin = true; // For now, treat all users as admin until auth is implemented

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
    ///
    /// Delete eligibility rules:
    /// - Draft exercises that have never been published: Creator OR Administrator
    /// - Archived exercises: Administrator only
    /// - Published/Active/Completed exercises (not archived): Cannot delete - must archive first
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteExercise(Guid id)
    {
        // TODO: Get actual user ID and admin status from auth context
        var userId = SystemConstants.SystemUserId;
        var isAdmin = true; // For now, treat all users as admin until auth is implemented

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
    // Participant Management Endpoints
    // =========================================================================

    /// <summary>
    /// Get all participants for an exercise.
    /// Shows exercise-specific roles and effective roles.
    /// </summary>
    [HttpGet("{id:guid}/participants")]
    public async Task<ActionResult<List<ExerciseParticipantDto>>> GetParticipants(Guid id)
    {
        var participants = await _participantService.GetParticipantsAsync(id);
        return Ok(participants);
    }

    /// <summary>
    /// Get a specific participant for an exercise.
    /// </summary>
    [HttpGet("{id:guid}/participants/{userId}")]
    public async Task<ActionResult<ExerciseParticipantDto>> GetParticipant(Guid id, string userId)
    {
        var participant = await _participantService.GetParticipantAsync(id, userId);

        if (participant == null)
        {
            return NotFound(new { message = "Participant not found" });
        }

        return Ok(participant);
    }

    /// <summary>
    /// Add a participant to an exercise with an optional exercise-specific role.
    /// If no role is specified, the user's global role is used.
    /// Only Administrators and Exercise Directors can add participants.
    /// </summary>
    [HttpPost("{id:guid}/participants")]
    public async Task<ActionResult<ExerciseParticipantDto>> AddParticipant(
        Guid id,
        [FromBody] AddParticipantRequest request)
    {
        try
        {
            var result = await _participantService.AddParticipantAsync(id, request);

            _logger.LogInformation(
                "Added participant {UserId} to exercise {ExerciseId}",
                request.UserId, id);

            return CreatedAtAction(
                nameof(GetParticipant),
                new { id, userId = request.UserId },
                result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update a participant's exercise-specific role.
    /// Setting role to null removes the override and uses the user's global role.
    /// Only Administrators and Exercise Directors can update participant roles.
    /// </summary>
    [HttpPut("{id:guid}/participants/{userId}/role")]
    public async Task<ActionResult<ExerciseParticipantDto>> UpdateParticipantRole(
        Guid id,
        string userId,
        [FromBody] UpdateParticipantRoleRequest request)
    {
        try
        {
            var result = await _participantService.UpdateParticipantRoleAsync(id, userId, request);

            _logger.LogInformation(
                "Updated participant {UserId} role in exercise {ExerciseId}",
                userId, id);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove a participant from an exercise.
    /// Only Administrators and Exercise Directors can remove participants.
    /// </summary>
    [HttpDelete("{id:guid}/participants/{userId}")]
    public async Task<IActionResult> RemoveParticipant(Guid id, string userId)
    {
        try
        {
            await _participantService.RemoveParticipantAsync(id, userId);

            _logger.LogInformation(
                "Removed participant {UserId} from exercise {ExerciseId}",
                userId, id);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Bulk update participants for an exercise.
    /// Adds or updates multiple participants in a single request.
    /// Only Administrators and Exercise Directors can bulk update participants.
    /// </summary>
    [HttpPut("{id:guid}/participants")]
    public async Task<ActionResult<List<ExerciseParticipantDto>>> BulkUpdateParticipants(
        Guid id,
        [FromBody] BulkUpdateParticipantsRequest request)
    {
        try
        {
            await _participantService.BulkUpdateParticipantsAsync(id, request);

            var participants = await _participantService.GetParticipantsAsync(id);

            _logger.LogInformation(
                "Bulk updated {Count} participants for exercise {ExerciseId}",
                request.Participants.Count, id);

            return Ok(participants);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================================================================
    // Metrics Endpoints (S01, S02, S03)
    // =========================================================================

    /// <summary>
    /// Get real-time exercise progress for conduct view.
    /// Provides situational awareness: inject counts, observation counts, clock status.
    /// Used by Controllers and Directors during active exercises.
    /// </summary>
    [HttpGet("{id:guid}/progress")]
    public async Task<ActionResult<ExerciseProgressDto>> GetExerciseProgress(Guid id)
    {
        var progress = await _metricsService.GetExerciseProgressAsync(id);

        if (progress == null)
        {
            return NotFound();
        }

        return Ok(progress);
    }

    /// <summary>
    /// Get comprehensive inject delivery statistics for after-action review.
    /// Shows timing performance, on-time rate, and breakdowns by phase/controller.
    /// </summary>
    /// <param name="id">The exercise ID.</param>
    /// <param name="onTimeToleranceMinutes">Minutes tolerance for on-time calculation (default: 5).</param>
    [HttpGet("{id:guid}/metrics/injects")]
    public async Task<ActionResult<InjectSummaryDto>> GetInjectMetrics(
        Guid id,
        [FromQuery] int onTimeToleranceMinutes = 5)
    {
        var summary = await _metricsService.GetInjectSummaryAsync(id, onTimeToleranceMinutes);

        if (summary == null)
        {
            return NotFound();
        }

        return Ok(summary);
    }

    /// <summary>
    /// Get comprehensive observation statistics for after-action review.
    /// Shows P/S/M/U distribution, coverage rates, and breakdowns by evaluator/phase.
    /// </summary>
    [HttpGet("{id:guid}/metrics/observations")]
    public async Task<ActionResult<ObservationSummaryDto>> GetObservationMetrics(Guid id)
    {
        var summary = await _metricsService.GetObservationSummaryAsync(id);

        if (summary == null)
        {
            return NotFound();
        }

        return Ok(summary);
    }

    /// <summary>
    /// Get comprehensive timeline and duration analysis for after-action review.
    /// Includes pause history, phase timing, and inject pacing analysis.
    /// </summary>
    /// <param name="id">Exercise ID.</param>
    /// <returns>Timeline summary data.</returns>
    [HttpGet("{id:guid}/metrics/timeline")]
    [ProducesResponseType(typeof(TimelineSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TimelineSummaryDto>> GetTimelineMetrics(Guid id)
    {
        var summary = await _metricsService.GetTimelineSummaryAsync(id);

        if (summary == null)
        {
            return NotFound();
        }

        return Ok(summary);
    }

    /// <summary>
    /// Get controller activity metrics for after-action review.
    /// Shows workload distribution, timing performance, and phase activity per controller.
    /// </summary>
    /// <param name="id">Exercise ID.</param>
    /// <param name="onTimeToleranceMinutes">Minutes tolerance for on-time calculation (default: 5).</param>
    /// <returns>Controller activity summary data.</returns>
    [HttpGet("{id:guid}/metrics/controllers")]
    [ProducesResponseType(typeof(ControllerActivitySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ControllerActivitySummaryDto>> GetControllerMetrics(
        Guid id,
        [FromQuery] int onTimeToleranceMinutes = 5)
    {
        var summary = await _metricsService.GetControllerActivityAsync(id, onTimeToleranceMinutes);

        if (summary == null)
        {
            return NotFound();
        }

        return Ok(summary);
    }

    /// <summary>
    /// Get evaluator coverage metrics for after-action review.
    /// Shows observation distribution, objective coverage, and rating consistency per evaluator.
    /// </summary>
    /// <param name="id">Exercise ID.</param>
    /// <returns>Evaluator coverage summary data.</returns>
    [HttpGet("{id:guid}/metrics/evaluators")]
    [ProducesResponseType(typeof(EvaluatorCoverageSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EvaluatorCoverageSummaryDto>> GetEvaluatorMetrics(Guid id)
    {
        var summary = await _metricsService.GetEvaluatorCoverageAsync(id);

        if (summary == null)
        {
            return NotFound();
        }

        return Ok(summary);
    }

    /// <summary>
    /// Get capability performance metrics for an exercise (S06).
    /// Shows P/S/M/U ratings broken down by FEMA Core Capability.
    /// </summary>
    /// <param name="id">The exercise ID.</param>
    /// <returns>Capability performance summary or 404 if not found.</returns>
    [HttpGet("{id:guid}/metrics/capabilities")]
    [ProducesResponseType(typeof(CapabilityPerformanceSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CapabilityPerformanceSummaryDto>> GetCapabilityMetrics(Guid id)
    {
        var summary = await _metricsService.GetCapabilityPerformanceAsync(id);

        if (summary == null)
        {
            return NotFound();
        }

        return Ok(summary);
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

            // ClockMultiplier is the source of truth; TimeScale is kept in sync for backwards compatibility
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
    // Private Helpers
    // =========================================================================

    /// <summary>
    /// Get current authenticated user's ID from JWT claims.
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }
        return Guid.Parse(userIdClaim);
    }

    /// <summary>
    /// Get current authenticated user's ID as string from JWT claims.
    /// Used for ApplicationUser FK references.
    /// </summary>
    private string GetCurrentUserIdString()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }
        return userIdClaim;
    }
}
