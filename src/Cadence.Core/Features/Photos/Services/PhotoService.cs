using Cadence.Core.Data;
using Cadence.Core.Features.Observations.Models.DTOs;
using Cadence.Core.Features.Photos.Mappers;
using Cadence.Core.Features.Photos.Models.DTOs;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Photos.Services;

/// <summary>
/// Service for photo operations during exercise conduct.
/// Handles photo upload, storage, retrieval, and lifecycle management.
/// </summary>
public class PhotoService : IPhotoService
{
    private readonly AppDbContext _context;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IExerciseHubContext _hubContext;
    private readonly ILogger<PhotoService> _logger;
    private static readonly TimeSpan SasExpiry = TimeSpan.FromHours(1);

    public PhotoService(
        AppDbContext context,
        IBlobStorageService blobStorageService,
        IExerciseHubContext hubContext,
        ILogger<PhotoService> logger)
    {
        _context = context;
        _blobStorageService = blobStorageService;
        _hubContext = hubContext;
        _logger = logger;
    }

    private PhotoDto WithResolvedUrls(PhotoDto dto) => dto with
    {
        BlobUri = _blobStorageService.GetReadUri(dto.BlobUri, SasExpiry),
        ThumbnailUri = _blobStorageService.GetReadUri(dto.ThumbnailUri, SasExpiry)
    };

    private DeletedPhotoDto WithResolvedUrls(DeletedPhotoDto dto) => dto with
    {
        BlobUri = _blobStorageService.GetReadUri(dto.BlobUri, SasExpiry),
        ThumbnailUri = _blobStorageService.GetReadUri(dto.ThumbnailUri, SasExpiry)
    };

    /// <inheritdoc />
    public async Task<PhotoDto> UploadPhotoAsync(
        Guid exerciseId,
        Stream photoStream,
        Stream thumbnailStream,
        string fileName,
        long fileSizeBytes,
        UploadPhotoRequest request,
        string capturedById,
        CancellationToken ct = default)
    {
        // Validate exercise exists and is active or paused
        var exercise = await _context.Exercises.FindAsync([exerciseId], ct);
        if (exercise == null)
        {
            throw new InvalidOperationException($"Exercise {exerciseId} not found");
        }

        if (exercise.Status != ExerciseStatus.Active && exercise.Status != ExerciseStatus.Paused)
        {
            throw new InvalidOperationException(
                $"Cannot upload photos. Exercise is {exercise.Status}. Photos can only be uploaded during active or paused exercises.");
        }

        // Check for duplicate upload via idempotency key
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingPhoto = await _context.ExercisePhotos
                .Include(p => p.CapturedByUser)
                .FirstOrDefaultAsync(p =>
                    p.ExerciseId == exerciseId &&
                    p.CapturedById == capturedById &&
                    p.IdempotencyKey == request.IdempotencyKey, ct);

            if (existingPhoto != null)
            {
                _logger.LogInformation(
                    "Duplicate upload detected for idempotency key {Key}. Returning existing photo {PhotoId}.",
                    request.IdempotencyKey, existingPhoto.Id);
                return WithResolvedUrls(existingPhoto.ToDto());
            }
        }

        // Validate observation exists if specified
        if (request.ObservationId.HasValue)
        {
            var observationExists = await _context.Observations
                .AnyAsync(o => o.Id == request.ObservationId.Value && o.ExerciseId == exerciseId, ct);
            if (!observationExists)
            {
                throw new InvalidOperationException(
                    $"Observation {request.ObservationId.Value} not found in exercise {exerciseId}");
            }
        }

        // Create entity
        var photoId = Guid.NewGuid();
        var photo = new ExercisePhoto
        {
            Id = photoId,
            ExerciseId = exerciseId,
            OrganizationId = exercise.OrganizationId,
            CapturedById = capturedById,
            ObservationId = request.ObservationId,
            FileName = fileName,
            FileSizeBytes = fileSizeBytes,
            CapturedAt = request.CapturedAt,
            ScenarioTime = request.ScenarioTime,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LocationAccuracy = request.LocationAccuracy,
            DisplayOrder = 0,
            Status = PhotoStatus.Draft,
            IdempotencyKey = request.IdempotencyKey
        };

        // Build blob paths
        var photoBlobPath = $"{exercise.OrganizationId}/{exerciseId}/photos/{photoId}.jpg";
        var thumbnailBlobPath = $"{exercise.OrganizationId}/{exerciseId}/thumbnails/{photoId}.jpg";

        // Upload photo to blob storage
        var photoBlobUri = await _blobStorageService.UploadAsync(
            photoStream,
            photoBlobPath,
            "image/jpeg",
            ct);

        // Upload thumbnail if provided, otherwise fall back to full photo
        string thumbnailBlobUri;
        if (thumbnailStream != Stream.Null && thumbnailStream.Length > 0)
        {
            thumbnailBlobUri = await _blobStorageService.UploadAsync(
                thumbnailStream,
                thumbnailBlobPath,
                "image/jpeg",
                ct);
        }
        else
        {
            thumbnailBlobUri = photoBlobUri;
        }

        // Set blob URIs
        photo.BlobUri = photoBlobUri;
        photo.ThumbnailUri = thumbnailBlobUri;

        // Save to database
        _context.ExercisePhotos.Add(photo);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Photo {PhotoId} uploaded for exercise {ExerciseId} by user {UserId}",
            photoId, exerciseId, capturedById);

        // Reload with navigation properties
        await _context.Entry(photo)
            .Reference(p => p.CapturedByUser)
            .LoadAsync(ct);

        var dto = WithResolvedUrls(photo.ToDto());

        // Broadcast to all connected clients
        await _hubContext.NotifyPhotoAdded(exerciseId, dto);

        return dto;
    }

    /// <inheritdoc />
    public async Task<PhotoListResponse> GetPhotosByExerciseAsync(
        Guid exerciseId,
        PhotoListQuery query,
        CancellationToken ct = default)
    {
        // Build query with navigation properties
        var photosQuery = _context.ExercisePhotos
            .Include(p => p.CapturedByUser)
            .Where(p => p.ExerciseId == exerciseId);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.CapturedById))
        {
            photosQuery = photosQuery.Where(p => p.CapturedById == query.CapturedById);
        }

        if (query.From.HasValue)
        {
            photosQuery = photosQuery.Where(p => p.CapturedAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            photosQuery = photosQuery.Where(p => p.CapturedAt <= query.To.Value);
        }

        if (query.LinkedOnly.HasValue)
        {
            if (query.LinkedOnly.Value)
            {
                photosQuery = photosQuery.Where(p => p.ObservationId != null);
            }
            else
            {
                photosQuery = photosQuery.Where(p => p.ObservationId == null);
            }
        }

        // Get total count before pagination
        var totalCount = await photosQuery.CountAsync(ct);

        // Apply ordering and pagination
        var photos = await photosQuery
            .OrderByDescending(p => p.CapturedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var photoDtos = photos.Select(p => WithResolvedUrls(p.ToDto()));

        return new PhotoListResponse(
            photoDtos,
            totalCount,
            query.Page,
            query.PageSize);
    }

    /// <inheritdoc />
    public async Task<PhotoDto?> GetPhotoAsync(Guid photoId, CancellationToken ct = default)
    {
        var photo = await _context.ExercisePhotos
            .Include(p => p.CapturedByUser)
            .FirstOrDefaultAsync(p => p.Id == photoId, ct);

        return photo != null ? WithResolvedUrls(photo.ToDto()) : null;
    }

    /// <inheritdoc />
    public async Task<PhotoDto?> UpdatePhotoAsync(
        Guid photoId,
        UpdatePhotoRequest request,
        string modifiedBy,
        CancellationToken ct = default)
    {
        var photo = await _context.ExercisePhotos
            .Include(p => p.CapturedByUser)
            .Include(p => p.Exercise)
            .FirstOrDefaultAsync(p => p.Id == photoId, ct);

        if (photo == null)
        {
            return null;
        }

        // Validate observation exists if specified
        if (request.ObservationId.HasValue)
        {
            var observationExists = await _context.Observations
                .AnyAsync(o => o.Id == request.ObservationId.Value && o.ExerciseId == photo.ExerciseId, ct);
            if (!observationExists)
            {
                throw new InvalidOperationException(
                    $"Observation {request.ObservationId.Value} not found in exercise {photo.ExerciseId}");
            }
        }

        // Update fields
        photo.ObservationId = request.ObservationId;

        if (request.DisplayOrder.HasValue)
        {
            photo.DisplayOrder = request.DisplayOrder.Value;
        }

        if (request.AnnotationsJson != null)
        {
            photo.AnnotationsJson = request.AnnotationsJson;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Photo {PhotoId} updated by {UserId}",
            photoId, modifiedBy);

        var dto = WithResolvedUrls(photo.ToDto());

        // Broadcast to all connected clients
        await _hubContext.NotifyPhotoUpdated(photo.ExerciseId, dto);

        return dto;
    }

    /// <inheritdoc />
    public async Task<bool> DeletePhotoAsync(Guid photoId, string deletedBy, CancellationToken ct = default)
    {
        var photo = await _context.ExercisePhotos.FindAsync([photoId], ct);

        if (photo == null)
        {
            return false;
        }

        var exerciseId = photo.ExerciseId;

        // Soft delete
        photo.IsDeleted = true;
        photo.DeletedAt = DateTime.UtcNow;
        photo.DeletedBy = deletedBy;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Soft-deleted photo {PhotoId} by {UserId}. Blobs retained for restore.",
            photoId, deletedBy);

        // Broadcast to all connected clients
        await _hubContext.NotifyPhotoDeleted(exerciseId, photoId);

        return true;
    }

    /// <inheritdoc />
    public async Task<QuickPhotoResponse> QuickPhotoAsync(
        Guid exerciseId,
        Stream photoStream,
        Stream thumbnailStream,
        string fileName,
        long fileSizeBytes,
        QuickPhotoRequest request,
        string capturedById,
        CancellationToken ct = default)
    {
        var exercise = await ValidateExerciseForPhotos(exerciseId, ct);

        var duplicate = await CheckDuplicateQuickPhoto(exerciseId, capturedById, request.IdempotencyKey, ct);
        if (duplicate != null)
            return duplicate;

        var observation = await CreateDraftObservation(exercise, request, capturedById, ct);

        // Upload photo with observation link
        var uploadRequest = new UploadPhotoRequest
        {
            CapturedAt = request.CapturedAt,
            ScenarioTime = request.ScenarioTime,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LocationAccuracy = request.LocationAccuracy,
            ObservationId = observation.Id,
            IdempotencyKey = request.IdempotencyKey
        };

        var photoDto = await UploadPhotoAsync(
            exerciseId,
            photoStream,
            thumbnailStream,
            fileName,
            fileSizeBytes,
            uploadRequest,
            capturedById,
            ct);

        var observationDto = await BuildObservationDtoWithUrls(observation, ct);

        // Broadcast observation added (photo added was already broadcast in UploadPhotoAsync)
        await _hubContext.NotifyObservationAdded(exerciseId, observationDto);

        _logger.LogInformation(
            "Quick photo {PhotoId} created with draft observation {ObservationId} for exercise {ExerciseId}",
            photoDto.Id, observation.Id, exerciseId);

        return new QuickPhotoResponse(photoDto, observation.Id);
    }

    /// <summary>
    /// Finds the exercise, validates it is active or paused, and returns it.
    /// Throws <see cref="InvalidOperationException"/> if the exercise is not found or has an ineligible status.
    /// </summary>
    private async Task<Exercise> ValidateExerciseForPhotos(Guid exerciseId, CancellationToken ct)
    {
        var exercise = await _context.Exercises.FindAsync([exerciseId], ct);
        if (exercise == null)
            throw new InvalidOperationException($"Exercise {exerciseId} not found");

        if (exercise.Status != ExerciseStatus.Active && exercise.Status != ExerciseStatus.Paused)
        {
            throw new InvalidOperationException(
                $"Cannot capture photos. Exercise is {exercise.Status}. Photos can only be captured during active or paused exercises.");
        }

        return exercise;
    }

    /// <summary>
    /// Checks for a duplicate quick photo upload using the idempotency key.
    /// Returns a <see cref="QuickPhotoResponse"/> for the existing photo if a duplicate is found, or null otherwise.
    /// </summary>
    private async Task<QuickPhotoResponse?> CheckDuplicateQuickPhoto(
        Guid exerciseId,
        string capturedById,
        string? idempotencyKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return null;

        var existingPhoto = await _context.ExercisePhotos
            .Include(p => p.CapturedByUser)
            .FirstOrDefaultAsync(p =>
                p.ExerciseId == exerciseId &&
                p.CapturedById == capturedById &&
                p.IdempotencyKey == idempotencyKey, ct);

        if (existingPhoto == null)
            return null;

        _logger.LogInformation(
            "Duplicate quick photo detected for idempotency key {Key}. Returning existing photo {PhotoId}.",
            idempotencyKey, existingPhoto.Id);

        return new QuickPhotoResponse(WithResolvedUrls(existingPhoto.ToDto()), existingPhoto.ObservationId ?? Guid.Empty);
    }

    /// <summary>
    /// Creates and persists a draft observation linked to the quick photo capture.
    /// </summary>
    private async Task<Observation> CreateDraftObservation(
        Exercise exercise,
        QuickPhotoRequest request,
        string capturedById,
        CancellationToken ct)
    {
        var observation = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            OrganizationId = exercise.OrganizationId,
            Content = "Photo captured — add details",
            Status = ObservationStatus.Draft,
            ObservedAt = request.CapturedAt,
            Location = (request.Latitude.HasValue && request.Longitude.HasValue)
                ? $"{request.Latitude:F6}, {request.Longitude:F6}"
                : null,
            CreatedByUserId = capturedById,
            CreatedBy = capturedById,
            ModifiedBy = capturedById
        };

        _context.Observations.Add(observation);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created draft observation {ObservationId} for quick photo in exercise {ExerciseId}",
            observation.Id, exercise.Id);

        return observation;
    }

    /// <summary>
    /// Loads navigation properties on the observation and resolves blob URIs to SAS URLs.
    /// </summary>
    private async Task<ObservationDto> BuildObservationDtoWithUrls(Observation observation, CancellationToken ct)
    {
        await _context.Entry(observation)
            .Reference(o => o.CreatedByUser)
            .LoadAsync(ct);
        await _context.Entry(observation)
            .Collection(o => o.ObservationCapabilities)
            .LoadAsync(ct);
        await _context.Entry(observation)
            .Collection(o => o.Photos)
            .LoadAsync(ct);

        var observationDto = observation.ToDto();

        if (observationDto.Photos.Count > 0)
        {
            observationDto = observationDto with
            {
                Photos = observationDto.Photos.Select(p => p with
                {
                    ThumbnailUri = _blobStorageService.GetReadUri(p.ThumbnailUri, SasExpiry)
                }).ToList()
            };
        }

        return observationDto;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DeletedPhotoDto>> GetDeletedPhotosAsync(
        Guid exerciseId, CancellationToken ct = default)
    {
        var photos = await _context.ExercisePhotos
            .IgnoreQueryFilters()
            .Include(p => p.CapturedByUser)
            .Where(p => p.IsDeleted && p.ExerciseId == exerciseId)
            .OrderByDescending(p => p.DeletedAt)
            .ToListAsync(ct);

        return photos.Select(p => WithResolvedUrls(p.ToDeletedDto()));
    }

    /// <inheritdoc />
    public async Task<PhotoDto?> RestorePhotoAsync(
        Guid photoId, string restoredBy, CancellationToken ct = default)
    {
        var photo = await _context.ExercisePhotos
            .IgnoreQueryFilters()
            .Include(p => p.CapturedByUser)
            .FirstOrDefaultAsync(p => p.Id == photoId && p.IsDeleted, ct);

        if (photo == null)
        {
            return null;
        }

        photo.IsDeleted = false;
        photo.DeletedAt = null;
        photo.DeletedBy = null;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Restored photo {PhotoId} by {UserId}",
            photoId, restoredBy);

        var dto = WithResolvedUrls(photo.ToDto());
        await _hubContext.NotifyPhotoAdded(photo.ExerciseId, dto);

        return dto;
    }

    /// <inheritdoc />
    public async Task<bool> PermanentDeletePhotoAsync(
        Guid photoId, string deletedBy, CancellationToken ct = default)
    {
        var photo = await _context.ExercisePhotos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == photoId && p.IsDeleted, ct);

        if (photo == null)
        {
            return false;
        }

        // Delete blobs from storage
        try
        {
            await _blobStorageService.DeleteAsync(photo.BlobUri, ct);
            if (photo.ThumbnailUri != photo.BlobUri)
            {
                await _blobStorageService.DeleteAsync(photo.ThumbnailUri, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to delete blobs for photo {PhotoId}. Proceeding with DB removal.",
                photoId);
        }

        // Hard delete from database
        _context.ExercisePhotos.Remove(photo);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Permanently deleted photo {PhotoId} by {UserId}",
            photoId, deletedBy);

        return true;
    }

    /// <inheritdoc />
    public async Task<int> SoftDeletePhotosForObservationAsync(
        Guid observationId, string deletedBy, CancellationToken ct = default)
    {
        var photos = await _context.ExercisePhotos
            .Where(p => p.ObservationId == observationId)
            .ToListAsync(ct);

        if (photos.Count == 0)
        {
            return 0;
        }

        var exerciseId = photos[0].ExerciseId;

        foreach (var photo in photos)
        {
            photo.IsDeleted = true;
            photo.DeletedAt = DateTime.UtcNow;
            photo.DeletedBy = deletedBy;
        }

        await _context.SaveChangesAsync(ct);

        // Broadcast deletion for each photo
        foreach (var photo in photos)
        {
            await _hubContext.NotifyPhotoDeleted(exerciseId, photo.Id);
        }

        _logger.LogInformation(
            "Cascade soft-deleted {PhotoCount} photos for observation {ObservationId}",
            photos.Count, observationId);

        return photos.Count;
    }
}
