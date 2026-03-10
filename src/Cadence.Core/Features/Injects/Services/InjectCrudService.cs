using Cadence.Core.Data;
using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Models.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Injects.Services;

/// <summary>
/// Implements CRUD operations for injects.
/// Extracts the business logic that previously lived inline in InjectsController,
/// making it independently testable and easier to maintain.
/// </summary>
public class InjectCrudService : IInjectCrudService
{
    private readonly AppDbContext _context;
    private readonly IValidator<CreateInjectRequest> _createValidator;
    private readonly IValidator<UpdateInjectRequest> _updateValidator;
    private readonly ILogger<InjectCrudService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="InjectCrudService"/>.
    /// </summary>
    public InjectCrudService(
        AppDbContext context,
        IValidator<CreateInjectRequest> createValidator,
        IValidator<UpdateInjectRequest> updateValidator,
        ILogger<InjectCrudService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<List<InjectDto>> GetInjectsAsync(
        Guid exerciseId,
        InjectStatus? status,
        string? currentUserId,
        bool mySubmissionsOnly,
        CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync(new object[] { exerciseId }, ct);
        if (exercise == null)
        {
            throw new KeyNotFoundException($"Exercise {exerciseId} not found.");
        }

        if (exercise.ActiveMselId == null)
        {
            return new List<InjectDto>();
        }

        // Build base query scoped to the active MSEL
        var injectsQuery = _context.Injects
            .Where(i => i.MselId == exercise.ActiveMselId);

        // Apply optional status filter
        if (status.HasValue)
        {
            injectsQuery = injectsQuery.Where(i => i.Status == status.Value);
        }

        // Apply my-submissions-only filter
        if (mySubmissionsOnly && !string.IsNullOrEmpty(currentUserId))
        {
            injectsQuery = injectsQuery.Where(i =>
                i.SubmittedByUserId == currentUserId ||
                i.CreatedBy == currentUserId);
        }

        injectsQuery = injectsQuery.OrderBy(i => i.Sequence);

        // Project to an anonymous type to avoid cartesian explosion with objectives
        var injectsData = await injectsQuery
            .Select(i => new
            {
                i.Id,
                i.InjectNumber,
                i.Title,
                i.Description,
                i.ScheduledTime,
                i.DeliveryTime,
                i.ScenarioDay,
                i.ScenarioTime,
                i.Target,
                i.Source,
                i.DeliveryMethod,
                i.DeliveryMethodId,
                DeliveryMethodName = i.DeliveryMethodLookup != null ? i.DeliveryMethodLookup.Name : null,
                i.DeliveryMethodOther,
                i.InjectType,
                i.Status,
                i.Sequence,
                i.ParentInjectId,
                i.FireCondition,
                i.ExpectedAction,
                i.ControllerNotes,
                i.ReadyAt,
                i.FiredAt,
                i.FiredByUserId,
                FiredByName = i.FiredByUser != null ? i.FiredByUser.DisplayName : null,
                i.SkippedAt,
                i.SkippedByUserId,
                SkippedByName = i.SkippedByUser != null ? i.SkippedByUser.DisplayName : null,
                i.SkipReason,
                i.MselId,
                i.PhaseId,
                PhaseName = i.Phase != null ? i.Phase.Name : null,
                i.CreatedAt,
                i.UpdatedAt,
                i.SourceReference,
                i.Priority,
                i.TriggerType,
                i.ResponsibleController,
                i.LocationName,
                i.LocationType,
                i.Track,
                i.SubmittedByUserId,
                SubmittedByName = i.SubmittedByUser != null ? i.SubmittedByUser.DisplayName : null,
                i.SubmittedAt,
                i.ApprovedByUserId,
                ApprovedByName = i.ApprovedByUser != null ? i.ApprovedByUser.DisplayName : null,
                i.ApprovedAt,
                i.ApproverNotes,
                i.RejectedByUserId,
                RejectedByName = i.RejectedByUser != null ? i.RejectedByUser.DisplayName : null,
                i.RejectedAt,
                i.RejectionReason,
                i.RevertedByUserId,
                RevertedByName = i.RevertedByUser != null ? i.RevertedByUser.DisplayName : null,
                i.RevertedAt,
                i.RevertReason,
                i.ModifiedBy
            })
            .ToListAsync(ct);

        // Fetch objective mappings in a single batch query
        var injectIds = injectsData.Select(i => i.Id).ToList();
        var objectiveMappings = await _context.InjectObjectives
            .Where(io => injectIds.Contains(io.InjectId))
            .Select(io => new { io.InjectId, io.ObjectiveId })
            .ToListAsync(ct);

        var objectivesByInject = objectiveMappings
            .GroupBy(io => io.InjectId)
            .ToDictionary(g => g.Key, g => g.Select(io => io.ObjectiveId).ToList());

        // Fetch critical task counts in a single batch query
        var criticalTaskCounts = await _context.Set<InjectCriticalTask>()
            .Where(ict => injectIds.Contains(ict.InjectId))
            .GroupBy(ict => ict.InjectId)
            .Select(g => new { InjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.InjectId, g => g.Count, ct);

        // Map to DTOs
        return injectsData.Select(i => new InjectDto(
            i.Id,
            i.InjectNumber,
            i.Title,
            i.Description,
            i.ScheduledTime,
            i.DeliveryTime,
            i.ScenarioDay,
            i.ScenarioTime,
            i.Target,
            i.Source,
            i.DeliveryMethod,
            i.DeliveryMethodId,
            i.DeliveryMethodName,
            i.DeliveryMethodOther,
            i.InjectType,
            i.Status,
            i.Sequence,
            i.ParentInjectId,
            i.FireCondition,
            i.ExpectedAction,
            i.ControllerNotes,
            i.ReadyAt,
            i.FiredAt,
            // Parse string ApplicationUser.Id to Guid for DTO backward compatibility
            string.IsNullOrEmpty(i.FiredByUserId) ? null : Guid.Parse(i.FiredByUserId),
            i.FiredByName,
            i.SkippedAt,
            string.IsNullOrEmpty(i.SkippedByUserId) ? null : Guid.Parse(i.SkippedByUserId),
            i.SkippedByName,
            i.SkipReason,
            i.MselId,
            i.PhaseId,
            i.PhaseName,
            objectivesByInject.GetValueOrDefault(i.Id) ?? new List<Guid>(),
            i.CreatedAt,
            i.UpdatedAt,
            i.SourceReference,
            i.Priority,
            i.TriggerType,
            i.ResponsibleController,
            i.LocationName,
            i.LocationType,
            i.Track,
            i.SubmittedByUserId,
            i.SubmittedByName,
            i.SubmittedAt,
            i.ApprovedByUserId,
            i.ApprovedByName,
            i.ApprovedAt,
            i.ApproverNotes,
            i.RejectedByUserId,
            i.RejectedByName,
            i.RejectedAt,
            i.RejectionReason,
            i.RevertedByUserId,
            i.RevertedByName,
            i.RevertedAt,
            i.RevertReason,
            i.ModifiedBy,
            criticalTaskCounts.GetValueOrDefault(i.Id, 0)
        )).ToList();
    }

    /// <inheritdoc />
    public async Task<InjectDto?> GetInjectAsync(
        Guid exerciseId,
        Guid injectId,
        CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync(new object[] { exerciseId }, ct);
        if (exercise == null)
        {
            throw new KeyNotFoundException($"Exercise {exerciseId} not found.");
        }

        var inject = await _context.Injects
            .Include(i => i.Phase)
            .Include(i => i.FiredByUser)
            .Include(i => i.SkippedByUser)
            .Include(i => i.InjectObjectives)
            .Include(i => i.LinkedCriticalTasks)
            .Include(i => i.DeliveryMethodLookup)
            .Include(i => i.SubmittedByUser)
            .Include(i => i.ApprovedByUser)
            .Include(i => i.RejectedByUser)
            .Include(i => i.RevertedByUser)
            .FirstOrDefaultAsync(i => i.Id == injectId && i.MselId == exercise.ActiveMselId, ct);

        return inject?.ToDto();
    }

    /// <inheritdoc />
    public async Task<List<InjectStatusHistoryDto>> GetInjectHistoryAsync(
        Guid exerciseId,
        Guid injectId,
        CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync(new object[] { exerciseId }, ct);
        if (exercise == null)
        {
            throw new KeyNotFoundException($"Exercise {exerciseId} not found.");
        }

        // Verify inject belongs to this exercise via the active MSEL
        var injectExists = await _context.Injects
            .AnyAsync(i => i.Id == injectId && i.MselId == exercise.ActiveMselId, ct);
        if (!injectExists)
        {
            throw new KeyNotFoundException($"Inject {injectId} not found in exercise {exerciseId}.");
        }

        return await _context.InjectStatusHistories
            .Include(h => h.ChangedByUser)
            .Where(h => h.InjectId == injectId)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new InjectStatusHistoryDto(
                h.Id,
                h.InjectId,
                h.FromStatus,
                h.ToStatus,
                h.ChangedByUserId,
                h.ChangedByUser != null ? h.ChangedByUser.DisplayName : null,
                h.ChangedAt,
                h.Notes
            ))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<InjectDto> CreateInjectAsync(
        Guid exerciseId,
        CreateInjectRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync(new object[] { exerciseId }, ct);
        if (exercise == null)
        {
            throw new KeyNotFoundException($"Exercise {exerciseId} not found.");
        }

        // Validate via FluentValidation (throws ValidationException on failure)
        await _createValidator.ValidateAndThrowAsync(request, ct);

        // Get or auto-create the MSEL for this exercise
        Guid mselId;
        if (exercise.ActiveMselId == null)
        {
            var msel = new Cadence.Core.Models.Entities.Msel
            {
                Id = Guid.NewGuid(),
                Name = $"{exercise.Name} MSEL",
                Description = $"Master Scenario Events List for {exercise.Name}",
                Version = 1,
                IsActive = true,
                ExerciseId = exerciseId,
                OrganizationId = exercise.OrganizationId,
                CreatedBy = userId,
                ModifiedBy = userId
            };
            _context.Msels.Add(msel);
            exercise.ActiveMselId = msel.Id;
            mselId = msel.Id;
        }
        else
        {
            mselId = exercise.ActiveMselId.Value;
        }

        // Assign the next sequential InjectNumber and Sequence
        var maxInjectNumber = await _context.Injects
            .Where(i => i.MselId == mselId)
            .MaxAsync(i => (int?)i.InjectNumber, ct) ?? 0;

        var maxSequence = await _context.Injects
            .Where(i => i.MselId == mselId)
            .MaxAsync(i => (int?)i.Sequence, ct) ?? 0;

        var inject = request.ToEntity(mselId, maxInjectNumber + 1, maxSequence + 1, userId);
        _context.Injects.Add(inject);

        // Link objectives if provided
        if (request.ObjectiveIds != null && request.ObjectiveIds.Count > 0)
        {
            foreach (var objectiveId in request.ObjectiveIds.Distinct())
            {
                var objectiveExists = await _context.Objectives
                    .AnyAsync(o => o.Id == objectiveId && o.ExerciseId == exerciseId, ct);
                if (objectiveExists)
                {
                    inject.InjectObjectives.Add(new InjectObjective
                    {
                        InjectId = inject.Id,
                        ObjectiveId = objectiveId
                    });
                }
            }
        }

        await _context.SaveChangesAsync(ct);

        // Reload navigation properties needed for the DTO
        await _context.Entry(inject).Reference(i => i.Phase).LoadAsync(ct);
        await _context.Entry(inject).Collection(i => i.InjectObjectives).LoadAsync(ct);

        _logger.LogInformation("Created inject {InjectId}: {InjectTitle} for exercise {ExerciseId}",
            inject.Id, inject.Title, exerciseId);

        return inject.ToDto();
    }

    /// <inheritdoc />
    public async Task<(InjectDto dto, bool statusReverted)> UpdateInjectAsync(
        Guid exerciseId,
        Guid injectId,
        UpdateInjectRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync(new object[] { exerciseId }, ct);
        if (exercise == null)
        {
            throw new KeyNotFoundException($"Exercise {exerciseId} not found.");
        }

        if (exercise.Status == ExerciseStatus.Archived)
        {
            throw new InvalidOperationException("Archived exercises cannot be modified.");
        }

        var inject = await _context.Injects
            .Include(i => i.Phase)
            .Include(i => i.FiredByUser)
            .Include(i => i.SkippedByUser)
            .Include(i => i.InjectObjectives)
            .Include(i => i.SubmittedByUser)
            .Include(i => i.ApprovedByUser)
            .Include(i => i.RejectedByUser)
            .Include(i => i.RevertedByUser)
            .FirstOrDefaultAsync(i => i.Id == injectId && i.MselId == exercise.ActiveMselId, ct);

        if (inject == null)
        {
            throw new KeyNotFoundException($"Inject {injectId} not found in exercise {exerciseId}.");
        }

        // Validate via FluentValidation (throws ValidationException on failure)
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var previousStatus = inject.Status;
        var statusReverted = false;

        if (inject.Status == InjectStatus.Released)
        {
            // Released injects: only ControllerNotes can be updated
            inject.ControllerNotes = request.ControllerNotes;
            inject.ModifiedBy = userId;
        }
        else
        {
            var shouldRevertToDraft = exercise.RequireInjectApproval &&
                (inject.Status == InjectStatus.Approved || inject.Status == InjectStatus.Submitted);

            // Full-field update for Draft / Deferred / Submitted / Approved injects
            inject.UpdateFromRequest(request, userId);

            if (shouldRevertToDraft)
            {
                inject.Status = InjectStatus.Draft;

                // Clear approval tracking fields so re-approval starts fresh
                inject.ApprovedByUserId = null;
                inject.ApprovedAt = null;
                inject.ApproverNotes = null;

                // Clear submission tracking so the user must re-submit
                inject.SubmittedByUserId = null;
                inject.SubmittedAt = null;

                // Clear any prior rejection
                inject.RejectedByUserId = null;
                inject.RejectedAt = null;
                inject.RejectionReason = null;

                // Record the status-history entry
                var history = new InjectStatusHistory
                {
                    Id = Guid.NewGuid(),
                    InjectId = inject.Id,
                    FromStatus = previousStatus,
                    ToStatus = InjectStatus.Draft,
                    ChangedByUserId = userId,
                    ChangedAt = DateTime.UtcNow,
                    Notes = "Content edited - reverted to Draft for re-approval",
                    CreatedBy = userId,
                    ModifiedBy = userId
                };
                _context.InjectStatusHistories.Add(history);

                _logger.LogInformation(
                    "Inject {InjectId} reverted from {PreviousStatus} to Draft due to content edit (approval workflow enabled)",
                    inject.Id, previousStatus);

                statusReverted = true;
            }

            // Update objective links (only for injects that are not Released)
            if (request.ObjectiveIds != null)
            {
                _context.InjectObjectives.RemoveRange(inject.InjectObjectives);
                inject.InjectObjectives.Clear();

                foreach (var objectiveId in request.ObjectiveIds.Distinct())
                {
                    var objectiveExists = await _context.Objectives
                        .AnyAsync(o => o.Id == objectiveId && o.ExerciseId == exerciseId, ct);
                    if (objectiveExists)
                    {
                        inject.InjectObjectives.Add(new InjectObjective
                        {
                            InjectId = inject.Id,
                            ObjectiveId = objectiveId
                        });
                    }
                }
            }
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Updated inject {InjectId}: {InjectTitle}", inject.Id, inject.Title);

        return (inject.ToDto(), statusReverted);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteInjectAsync(
        Guid exerciseId,
        Guid injectId,
        string userId,
        CancellationToken ct = default)
    {
        var exercise = await _context.Exercises.FindAsync(new object[] { exerciseId }, ct);
        if (exercise == null)
        {
            throw new KeyNotFoundException($"Exercise {exerciseId} not found.");
        }

        if (exercise.Status == ExerciseStatus.Archived)
        {
            throw new InvalidOperationException("Archived exercises cannot be modified.");
        }

        var inject = await _context.Injects
            .FirstOrDefaultAsync(i => i.Id == injectId && i.MselId == exercise.ActiveMselId, ct);

        if (inject == null)
        {
            throw new KeyNotFoundException($"Inject {injectId} not found in exercise {exerciseId}.");
        }

        // Soft-delete preserves audit history
        inject.IsDeleted = true;
        inject.DeletedAt = DateTime.UtcNow;
        inject.DeletedBy = userId;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted inject {InjectId}: {InjectTitle}", inject.Id, inject.Title);

        return true;
    }
}
