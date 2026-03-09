using Cadence.Core.Data;
using Cadence.Core.Features.Notifications.Models.DTOs;
using Cadence.Core.Features.Notifications.Services;
using Cadence.Core.Features.Observations.Models.DTOs;
using Cadence.Core.Features.Photos.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Observations.Services;

/// <summary>
/// Service for observation operations.
/// </summary>
public class ObservationService : IObservationService
{
    private readonly AppDbContext _context;
    private readonly IExerciseHubContext _hubContext;
    private readonly INotificationService _notificationService;
    private readonly IPhotoService _photoService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<ObservationService> _logger;
    private static readonly TimeSpan SasExpiry = TimeSpan.FromHours(1);

    public ObservationService(
        AppDbContext context,
        IExerciseHubContext hubContext,
        INotificationService notificationService,
        IPhotoService photoService,
        IBlobStorageService blobStorageService,
        ILogger<ObservationService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _notificationService = notificationService;
        _photoService = photoService;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    private ObservationDto WithResolvedPhotoUrls(ObservationDto dto)
    {
        if (dto.Photos.Count == 0)
            return dto;

        return dto with
        {
            Photos = dto.Photos.Select(p => p with
            {
                ThumbnailUri = _blobStorageService.GetReadUri(p.ThumbnailUri, SasExpiry)
            }).ToList()
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ObservationDto>> GetObservationsByExerciseAsync(Guid exerciseId)
    {
        var observations = await _context.Observations
            .AsNoTracking()
            .Where(o => o.ExerciseId == exerciseId)
            .OrderByDescending(o => o.ObservedAt)
            .Select(ObservationMapper.ToObservationDtoExpression)
            .ToListAsync();

        return observations.Select(WithResolvedPhotoUrls);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ObservationDto>> GetObservationsByInjectAsync(Guid injectId)
    {
        var observations = await _context.Observations
            .AsNoTracking()
            .Where(o => o.InjectId == injectId)
            .OrderByDescending(o => o.ObservedAt)
            .Select(ObservationMapper.ToObservationDtoExpression)
            .ToListAsync();

        return observations.Select(WithResolvedPhotoUrls);
    }

    /// <inheritdoc />
    public async Task<ObservationDto?> GetObservationAsync(Guid id)
    {
        var dto = await _context.Observations
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(ObservationMapper.ToObservationDtoExpression)
            .FirstOrDefaultAsync();

        return dto != null ? WithResolvedPhotoUrls(dto) : null;
    }

    /// <inheritdoc />
    public async Task<ObservationDto> CreateObservationAsync(Guid exerciseId, CreateObservationRequest request, string createdBy)
    {
        // Validate exercise exists and is active
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            throw new InvalidOperationException($"Exercise {exerciseId} not found");
        }

        if (exercise.Status != ExerciseStatus.Active && exercise.Status != ExerciseStatus.Paused)
        {
            throw new InvalidOperationException($"Cannot add observations. Exercise is {exercise.Status}. Observations can only be added during an active or paused exercise.");
        }

        // Validate inject exists if specified
        if (request.InjectId.HasValue)
        {
            var injectExists = await _context.Injects.AnyAsync(i => i.Id == request.InjectId.Value);
            if (!injectExists)
            {
                throw new InvalidOperationException($"Inject {request.InjectId.Value} not found");
            }
        }

        // Validate objective exists if specified
        if (request.ObjectiveId.HasValue)
        {
            var objectiveExists = await _context.Objectives.AnyAsync(o => o.Id == request.ObjectiveId.Value);
            if (!objectiveExists)
            {
                throw new InvalidOperationException($"Objective {request.ObjectiveId.Value} not found");
            }
        }

        var observation = request.ToEntity(exerciseId, createdBy);

        // Set OrganizationId from the parent exercise for data isolation
        observation.OrganizationId = exercise.OrganizationId;

        _context.Observations.Add(observation);
        await _context.SaveChangesAsync();

        // Link capabilities if provided
        if (request.CapabilityIds?.Count > 0)
        {
            var capabilityLinks = request.CapabilityIds.Select(capId => new ObservationCapability
            {
                ObservationId = observation.Id,
                CapabilityId = capId
            });
            _context.ObservationCapabilities.AddRange(capabilityLinks);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Created observation {ObservationId} for exercise {ExerciseId}",
            observation.Id, exerciseId);

        // Reload as a single projected query instead of multiple round-trips
        var dto = await _context.Observations
            .AsNoTracking()
            .Where(o => o.Id == observation.Id)
            .Select(ObservationMapper.ToObservationDtoExpression)
            .FirstAsync();

        dto = WithResolvedPhotoUrls(dto);

        // Broadcast to all connected clients
        await _hubContext.NotifyObservationAdded(exerciseId, dto);

        // Send notification to Exercise Directors
        await SendObservationNotificationToDirectorsAsync(exerciseId, dto);

        return dto;
    }

    private async Task SendObservationNotificationToDirectorsAsync(Guid exerciseId, ObservationDto observation)
    {
        // Get Exercise Director user IDs for this exercise
        var exerciseDirectorIds = await _context.ExerciseParticipants
            .Where(ep => ep.ExerciseId == exerciseId &&
                         ep.Role == ExerciseRole.ExerciseDirector &&
                         !ep.IsDeleted)
            .Select(ep => ep.UserId)
            .ToListAsync();

        if (exerciseDirectorIds.Count == 0)
        {
            return;
        }

        var notificationRequest = new CreateNotificationRequest
        {
            Type = NotificationType.ObservationCreated,
            Priority = NotificationPriority.Low,
            Title = "Observation Recorded",
            Message = $"A new observation has been recorded in the exercise.",
            ActionUrl = $"/exercises/{exerciseId}/observations",
            RelatedEntityType = "Observation",
            RelatedEntityId = observation.Id
        };

        await _notificationService.CreateNotificationsForUsersAsync(
            exerciseDirectorIds,
            notificationRequest);

        _logger.LogInformation(
            "Sent observation notification to {DirectorCount} Exercise Directors for exercise {ExerciseId}",
            exerciseDirectorIds.Count, exerciseId);
    }

    /// <inheritdoc />
    public async Task<ObservationDto?> UpdateObservationAsync(Guid id, UpdateObservationRequest request, string modifiedBy)
    {
        var observation = await _context.Observations
            .Include(o => o.Exercise)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (observation == null)
        {
            return null;
        }

        // Validate exercise is active or paused
        if (observation.Exercise.Status != ExerciseStatus.Active && observation.Exercise.Status != ExerciseStatus.Paused)
        {
            throw new InvalidOperationException($"Cannot update observations. Exercise is {observation.Exercise.Status}. Observations can only be modified during an active or paused exercise.");
        }

        // Validate inject exists if specified
        if (request.InjectId.HasValue)
        {
            var injectExists = await _context.Injects.AnyAsync(i => i.Id == request.InjectId.Value);
            if (!injectExists)
            {
                throw new InvalidOperationException($"Inject {request.InjectId.Value} not found");
            }
        }

        // Validate objective exists if specified
        if (request.ObjectiveId.HasValue)
        {
            var objectiveExists = await _context.Objectives.AnyAsync(o => o.Id == request.ObjectiveId.Value);
            if (!objectiveExists)
            {
                throw new InvalidOperationException($"Objective {request.ObjectiveId.Value} not found");
            }
        }

        // Update fields
        observation.Content = request.Content;
        observation.Rating = request.Rating;
        observation.Recommendation = request.Recommendation;
        observation.Location = request.Location;
        observation.InjectId = request.InjectId;
        observation.ObjectiveId = request.ObjectiveId;
        observation.ModifiedBy = modifiedBy;

        // Promote Draft observation to Complete when content is substantive
        if (observation.Status == ObservationStatus.Draft &&
            !string.IsNullOrWhiteSpace(request.Content) &&
            request.Content != "Photo captured — add details")
        {
            observation.Status = ObservationStatus.Complete;
        }

        if (request.ObservedAt.HasValue)
        {
            observation.ObservedAt = request.ObservedAt.Value;
        }

        // Update capability links if provided
        // null = keep existing, empty list = clear all, populated list = replace
        if (request.CapabilityIds != null)
        {
            // Remove existing links
            var existingLinks = await _context.ObservationCapabilities
                .Where(oc => oc.ObservationId == id)
                .ToListAsync();
            _context.ObservationCapabilities.RemoveRange(existingLinks);

            // Add new links
            if (request.CapabilityIds.Count > 0)
            {
                var newLinks = request.CapabilityIds.Select(capId => new ObservationCapability
                {
                    ObservationId = id,
                    CapabilityId = capId
                });
                _context.ObservationCapabilities.AddRange(newLinks);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated observation {ObservationId}", id);

        // Reload as a single projected query instead of multiple round-trips
        var dto = await _context.Observations
            .AsNoTracking()
            .Where(o => o.Id == observation.Id)
            .Select(ObservationMapper.ToObservationDtoExpression)
            .FirstAsync();

        dto = WithResolvedPhotoUrls(dto);

        // Broadcast to all connected clients
        await _hubContext.NotifyObservationUpdated(observation.ExerciseId, dto);

        return dto;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteObservationAsync(Guid id, string deletedBy)
    {
        var observation = await _context.Observations.FindAsync(id);

        if (observation == null)
        {
            return false;
        }

        var exerciseId = observation.ExerciseId;

        // Cascade soft-delete linked photos first (while they're still queryable)
        var photosDeleted = await _photoService.SoftDeletePhotosForObservationAsync(id, deletedBy);
        if (photosDeleted > 0)
        {
            _logger.LogInformation(
                "Cascade soft-deleted {PhotoCount} photos for observation {ObservationId}",
                photosDeleted, id);
        }

        // Soft delete the observation
        observation.IsDeleted = true;
        observation.DeletedAt = DateTime.UtcNow;
        observation.DeletedBy = deletedBy;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted observation {ObservationId}", id);

        // Broadcast to all connected clients
        await _hubContext.NotifyObservationDeleted(exerciseId, id);

        return true;
    }
}
