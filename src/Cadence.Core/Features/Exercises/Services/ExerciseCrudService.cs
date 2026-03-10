using Cadence.Core.Constants;
using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Organizations.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service for core exercise CRUD and settings operations.
/// </summary>
public class ExerciseCrudService : IExerciseCrudService
{
    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationContext _orgContext;
    private readonly IExerciseParticipantService _participantService;
    private readonly IMembershipService _membershipService;
    private readonly IValidator<CreateExerciseRequest> _createValidator;
    private readonly ILogger<ExerciseCrudService> _logger;

    public ExerciseCrudService(
        AppDbContext context,
        ICurrentOrganizationContext orgContext,
        IExerciseParticipantService participantService,
        IMembershipService membershipService,
        IValidator<CreateExerciseRequest> createValidator,
        ILogger<ExerciseCrudService> logger)
    {
        _context = context;
        _orgContext = orgContext;
        _participantService = participantService;
        _membershipService = membershipService;
        _createValidator = createValidator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ExerciseDto>> GetExercisesAsync(
        string? userId,
        bool includeArchived = false,
        bool archivedOnly = false,
        CancellationToken ct = default)
    {
        var query = _context.Exercises.AsQueryable();

        // Filter by organization context (SysAdmins see all, others see only their org)
        if (!_orgContext.IsSysAdmin && _orgContext.CurrentOrganizationId.HasValue)
        {
            query = query.Where(e => e.OrganizationId == _orgContext.CurrentOrganizationId.Value);
        }
        else if (!_orgContext.IsSysAdmin && !_orgContext.CurrentOrganizationId.HasValue)
        {
            // No org context: show exercises from all organizations the user belongs to.
            if (string.IsNullOrEmpty(userId))
                return [];

            var memberships = await _membershipService.GetUserMembershipsAsync(userId, ct);
            var orgIds = memberships.Select(m => m.OrganizationId).ToList();
            if (orgIds.Count == 0)
                return [];

            query = _context.Exercises
                .IgnoreQueryFilters()
                .Where(e => !e.IsDeleted && orgIds.Contains(e.OrganizationId));
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

        // Project to include inject counts and detail fields from active MSEL in a single query
        var exercises = await query
            .Include(e => e.Organization)
            .OrderByDescending(e => e.ScheduledDate)
            .Select(e => new
            {
                Exercise = e,
                OrganizationName = e.Organization.Name,
                InjectCount = e.ActiveMselId != null
                    ? _context.Injects.Count(i => i.MselId == e.ActiveMselId)
                    : 0,
                FiredInjectCount = e.ActiveMselId != null
                    ? _context.Injects.Count(i => i.MselId == e.ActiveMselId && i.Status == InjectStatus.Released)
                    : 0,
                ReadyInjectCount = e.Status == ExerciseStatus.Active && e.ActiveMselId != null
                    ? _context.Injects.Count(i => i.MselId == e.ActiveMselId && i.Status == InjectStatus.Synchronized)
                    : 0,
                ClockState = e.ClockState.ToString(),
                ElapsedSeconds = e.ClockState == ExerciseClockState.Running && e.ClockStartedAt.HasValue
                    ? (int)(DateTime.UtcNow - e.ClockStartedAt.Value).TotalSeconds + (e.ClockElapsedBeforePause.HasValue ? (int)e.ClockElapsedBeforePause.Value.TotalSeconds : 0)
                    : e.ClockElapsedBeforePause.HasValue
                        ? (int)e.ClockElapsedBeforePause.Value.TotalSeconds
                        : 0
            })
            .ToListAsync(ct);

        return exercises.Select(x => x.Exercise.ToDto(
            x.InjectCount,
            x.FiredInjectCount,
            x.OrganizationName,
            x.ClockState,
            x.ElapsedSeconds,
            x.ReadyInjectCount)).ToList();
    }

    /// <inheritdoc />
    public async Task<ExerciseDto?> GetExerciseAsync(Guid exerciseId, CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync([exerciseId], ct);
        return exercise?.ToDto();
    }

    /// <inheritdoc />
    public async Task<bool> ExerciseExistsAsync(Guid exerciseId, CancellationToken ct = default)
    {
        return await _context.Exercises
            .AsNoTracking()
            .AnyAsync(e => e.Id == exerciseId, ct);
    }

    /// <inheritdoc />
    public async Task<ExerciseDto> CreateExerciseAsync(
        CreateExerciseRequest request,
        string userId,
        CancellationToken ct = default)
    {
        // FluentValidation
        await _createValidator.ValidateAndThrowAsync(request, ct);

        // Require organization context
        if (!_orgContext.CurrentOrganizationId.HasValue)
        {
            throw new InvalidOperationException(
                "Organization context required. Please select an organization.");
        }

        var organizationId = _orgContext.CurrentOrganizationId.Value;

        var exercise = request.ToEntity(organizationId, userId);
        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created exercise {ExerciseId}: {ExerciseName} by user {UserId}",
            exercise.Id, exercise.Name, userId);

        // Assign Exercise Director
        string? directorId = request.DirectorId;

        // If no directorId provided, use creator if they are Admin or Manager
        if (string.IsNullOrEmpty(directorId))
        {
            var currentUser = await _context.ApplicationUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (currentUser != null &&
                (currentUser.SystemRole == SystemRole.Admin || currentUser.SystemRole == SystemRole.Manager))
            {
                directorId = userId;
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
                    },
                    ct);

                _logger.LogInformation(
                    "Assigned user {UserId} as Exercise Director for exercise {ExerciseId}",
                    directorId, exercise.Id);
            }
            catch (KeyNotFoundException)
            {
                throw; // Let controller handle as BadRequest
            }
            catch (InvalidOperationException)
            {
                throw; // Let controller handle as BadRequest
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to assign user {UserId} as Director for exercise {ExerciseId}. Exercise created successfully.",
                    directorId, exercise.Id);
                // Don't fail the exercise creation if auto-assignment fails
            }
        }

        return exercise.ToDto();
    }

    /// <inheritdoc />
    public async Task<ExerciseDto?> UpdateExerciseAsync(
        Guid exerciseId,
        UpdateExerciseRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync([exerciseId], ct);
        if (exercise == null)
            return null;

        // Validation
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required");

        if (request.Name.Length > 200)
            throw new ArgumentException("Name must be 200 characters or less");

        // Check status-based edit restrictions
        if (exercise.Status == ExerciseStatus.Completed || exercise.Status == ExerciseStatus.Archived)
            throw new InvalidOperationException($"{exercise.Status} exercises cannot be modified");

        // Update fields (respecting status-based restrictions)
        exercise.Name = request.Name;
        exercise.Description = request.Description;

        // These fields can only be changed in Draft status
        if (exercise.Status == ExerciseStatus.Draft)
        {
            exercise.ExerciseType = request.ExerciseType;
            exercise.ScheduledDate = request.ScheduledDate;
            exercise.StartTime = request.StartTime;
            exercise.DeliveryMode = request.DeliveryMode;
            exercise.TimelineMode = request.TimelineMode;
            exercise.ClockMultiplier = request.ClockMultiplier;
            exercise.TimeScale = request.ClockMultiplier;
        }

        // End time can always be updated (as long as not Completed/Archived)
        exercise.EndTime = request.EndTime;
        exercise.Location = request.Location;
        exercise.TimeZoneId = request.TimeZoneId;
        exercise.IsPracticeMode = request.IsPracticeMode;
        exercise.ModifiedBy = userId;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Updated exercise {ExerciseId}: {ExerciseName}", exercise.Id, exercise.Name);

        // Handle director reassignment if provided
        if (!string.IsNullOrEmpty(request.DirectorId))
        {
            await ReassignDirectorAsync(exercise.Id, request.DirectorId, ct);
        }

        return exercise.ToDto();
    }

    /// <inheritdoc />
    public async Task<ExerciseDto?> DuplicateExerciseAsync(
        Guid sourceExerciseId,
        DuplicateExerciseRequest? request,
        string userId,
        CancellationToken ct = default)
    {
        var source = await _context.Exercises
            .Include(e => e.Phases)
            .Include(e => e.Objectives)
            .Include(e => e.Msels)
                .ThenInclude(m => m.Injects)
                    .ThenInclude(i => i.InjectObjectives)
            .FirstOrDefaultAsync(e => e.Id == sourceExerciseId, ct);

        if (source == null)
            return null;

        var newName = request?.Name ?? $"Copy of {source.Name}";
        if (newName.Length > 200)
            newName = newName[..200];

        var newExercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = newName,
            Description = source.Description,
            ExerciseType = source.ExerciseType,
            Status = ExerciseStatus.Draft,
            IsPracticeMode = source.IsPracticeMode,
            ScheduledDate = request?.ScheduledDate ?? source.ScheduledDate,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            TimeZoneId = source.TimeZoneId,
            Location = source.Location,
            OrganizationId = source.OrganizationId,
            ClockState = ExerciseClockState.Stopped,
            ClockStartedAt = null,
            ClockElapsedBeforePause = null,
            ClockStartedBy = null,
            CreatedBy = userId,
            ModifiedBy = userId,
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

            _context.Phases.Add(new Phase
            {
                Id = newPhaseId,
                Name = sourcePhase.Name,
                Description = sourcePhase.Description,
                Sequence = sourcePhase.Sequence,
                StartTime = sourcePhase.StartTime,
                EndTime = sourcePhase.EndTime,
                ExerciseId = newExercise.Id,
                OrganizationId = source.OrganizationId,
                CreatedBy = userId,
                ModifiedBy = userId,
            });
        }

        // Copy objectives
        foreach (var sourceObjective in source.Objectives.OrderBy(o => o.ObjectiveNumber))
        {
            var newObjectiveId = Guid.NewGuid();
            objectiveIdMap[sourceObjective.Id] = newObjectiveId;

            _context.Objectives.Add(new Objective
            {
                Id = newObjectiveId,
                ObjectiveNumber = sourceObjective.ObjectiveNumber,
                Name = sourceObjective.Name,
                Description = sourceObjective.Description,
                ExerciseId = newExercise.Id,
                OrganizationId = source.OrganizationId,
                CreatedBy = userId,
                ModifiedBy = userId,
            });
        }

        // Copy the active MSEL (or first MSEL if none active)
        var sourceMsel = source.Msels.FirstOrDefault(m => m.IsActive) ?? source.Msels.FirstOrDefault();
        Guid? newMselId = null;
        if (sourceMsel != null)
        {
            newMselId = Guid.NewGuid();

            _context.Msels.Add(new Cadence.Core.Models.Entities.Msel
            {
                Id = newMselId.Value,
                Name = "v1.0",
                Description = sourceMsel.Description,
                Version = 1,
                IsActive = true,
                ExerciseId = newExercise.Id,
                OrganizationId = source.OrganizationId,
                CreatedBy = userId,
                ModifiedBy = userId,
            });

            // Copy injects (reset status to Draft)
            foreach (var sourceInject in sourceMsel.Injects.OrderBy(i => i.Sequence))
            {
                var newInjectId = Guid.NewGuid();

                _context.Injects.Add(new Inject
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
                    Status = InjectStatus.Draft,
                    Sequence = sourceInject.Sequence,
                    ParentInjectId = null,
                    FireCondition = sourceInject.FireCondition,
                    ExpectedAction = sourceInject.ExpectedAction,
                    ControllerNotes = sourceInject.ControllerNotes,
                    FiredAt = null,
                    FiredByUserId = null,
                    SkippedAt = null,
                    SkippedByUserId = null,
                    SkipReason = null,
                    MselId = newMselId.Value,
                    PhaseId = sourceInject.PhaseId.HasValue && phaseIdMap.TryGetValue(sourceInject.PhaseId.Value, out var mappedPhaseId)
                        ? mappedPhaseId
                        : null,
                    CreatedBy = userId,
                    ModifiedBy = userId,
                });

                // Copy inject-objective links
                foreach (var sourceLink in sourceInject.InjectObjectives)
                {
                    if (objectiveIdMap.TryGetValue(sourceLink.ObjectiveId, out var mappedObjectiveId))
                    {
                        _context.InjectObjectives.Add(new InjectObjective
                        {
                            InjectId = newInjectId,
                            ObjectiveId = mappedObjectiveId,
                        });
                    }
                }
            }
        }

        // First save: Create all entities without the circular reference
        await _context.SaveChangesAsync(ct);

        // Second save: Now set the ActiveMselId to complete the relationship
        if (newMselId.HasValue)
        {
            newExercise.ActiveMselId = newMselId.Value;
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Duplicated exercise {SourceId} to {NewId}: {NewName}",
            sourceExerciseId, newExercise.Id, newExercise.Name);

        return newExercise.ToDto();
    }

    /// <inheritdoc />
    public async Task<ExerciseSettingsDto?> GetExerciseSettingsAsync(Guid exerciseId, CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync([exerciseId], ct);
        if (exercise == null)
            return null;

        return new ExerciseSettingsDto(
            exercise.ClockMultiplier,
            exercise.AutoFireEnabled,
            exercise.ConfirmFireInject,
            exercise.ConfirmSkipInject,
            exercise.ConfirmClockControl,
            exercise.MaxDuration);
    }

    /// <inheritdoc />
    public async Task<ExerciseSettingsDto?> UpdateExerciseSettingsAsync(
        Guid exerciseId,
        UpdateExerciseSettingsRequest request,
        CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync([exerciseId], ct);
        if (exercise == null)
            return null;

        if (request.ClockMultiplier.HasValue)
        {
            if (request.ClockMultiplier < 0.5m || request.ClockMultiplier > 20.0m)
                throw new ArgumentException("Clock multiplier must be between 0.5 and 20.");

            if (exercise.ClockState == ExerciseClockState.Running)
                throw new InvalidOperationException(
                    "Cannot change clock multiplier while clock is running. Pause the exercise first.");

            exercise.ClockMultiplier = request.ClockMultiplier.Value;
            exercise.TimeScale = request.ClockMultiplier.Value;
        }

        if (request.AutoFireEnabled.HasValue)
            exercise.AutoFireEnabled = request.AutoFireEnabled.Value;

        if (request.ConfirmFireInject.HasValue)
            exercise.ConfirmFireInject = request.ConfirmFireInject.Value;

        if (request.ConfirmSkipInject.HasValue)
            exercise.ConfirmSkipInject = request.ConfirmSkipInject.Value;

        if (request.ConfirmClockControl.HasValue)
            exercise.ConfirmClockControl = request.ConfirmClockControl.Value;

        if (request.MaxDuration.HasValue)
        {
            if (request.MaxDuration.Value <= TimeSpan.Zero)
                throw new ArgumentException("Max duration must be greater than zero.");

            if (request.MaxDuration.Value > TimeSpan.FromDays(14))
                throw new ArgumentException("Max duration cannot exceed 336 hours (2 weeks).");

            exercise.MaxDuration = request.MaxDuration.Value;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Updated settings for exercise {ExerciseId}: ClockMultiplier={ClockMultiplier}, AutoFire={AutoFire}",
            exerciseId, exercise.ClockMultiplier, exercise.AutoFireEnabled);

        return new ExerciseSettingsDto(
            exercise.ClockMultiplier,
            exercise.AutoFireEnabled,
            exercise.ConfirmFireInject,
            exercise.ConfirmSkipInject,
            exercise.ConfirmClockControl,
            exercise.MaxDuration);
    }

    /// <summary>
    /// Handles director reassignment for an exercise update.
    /// </summary>
    private async Task ReassignDirectorAsync(Guid exerciseId, string newDirectorId, CancellationToken ct = default)
    {
        try
        {
            var participants = await _participantService.GetParticipantsAsync(exerciseId, ct);
            var existingDirector = participants.FirstOrDefault(
                p => p.ExerciseRole == ExerciseRole.ExerciseDirector.ToString());

            if (existingDirector != null && existingDirector.UserId != newDirectorId)
            {
                await _participantService.RemoveParticipantAsync(exerciseId, existingDirector.UserId, ct);
                _logger.LogInformation(
                    "Removed previous director {OldDirectorId} from exercise {ExerciseId}",
                    existingDirector.UserId, exerciseId);
            }

            if (existingDirector == null || existingDirector.UserId != newDirectorId)
            {
                var existingParticipant = participants.FirstOrDefault(p => p.UserId == newDirectorId);

                if (existingParticipant != null)
                {
                    await _participantService.UpdateParticipantRoleAsync(
                        exerciseId,
                        newDirectorId,
                        new UpdateParticipantRoleRequest { Role = ExerciseRole.ExerciseDirector.ToString() },
                        ct);
                }
                else
                {
                    await _participantService.AddParticipantAsync(
                        exerciseId,
                        new AddParticipantRequest
                        {
                            UserId = newDirectorId,
                            Role = ExerciseRole.ExerciseDirector.ToString()
                        },
                        ct);
                }

                _logger.LogInformation(
                    "Assigned user {UserId} as Exercise Director for exercise {ExerciseId}",
                    newDirectorId, exerciseId);
            }
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update director for exercise {ExerciseId}", exerciseId);
        }
    }
}
