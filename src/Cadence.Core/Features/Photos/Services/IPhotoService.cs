using Cadence.Core.Features.Photos.Models.DTOs;

namespace Cadence.Core.Features.Photos.Services;

/// <summary>
/// Service interface for photo operations during exercise conduct.
/// </summary>
public interface IPhotoService
{
    /// <summary>
    /// Upload a new photo to blob storage and create database record.
    /// </summary>
    Task<PhotoDto> UploadPhotoAsync(
        Guid exerciseId,
        Stream photoStream,
        Stream thumbnailStream,
        string fileName,
        long fileSizeBytes,
        UploadPhotoRequest request,
        string capturedById,
        CancellationToken ct = default);

    /// <summary>
    /// Get photos for an exercise with filtering and pagination.
    /// </summary>
    Task<PhotoListResponse> GetPhotosByExerciseAsync(
        Guid exerciseId,
        PhotoListQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Get a single photo by ID.
    /// </summary>
    Task<PhotoDto?> GetPhotoAsync(Guid photoId, CancellationToken ct = default);

    /// <summary>
    /// Update photo metadata (link to observation, display order).
    /// </summary>
    Task<PhotoDto?> UpdatePhotoAsync(
        Guid photoId,
        UpdatePhotoRequest request,
        string modifiedBy,
        CancellationToken ct = default);

    /// <summary>
    /// Soft delete a photo. Blobs are retained until permanent deletion.
    /// </summary>
    Task<bool> DeletePhotoAsync(Guid photoId, string deletedBy, CancellationToken ct = default);

    /// <summary>
    /// Quick photo capture - creates photo and auto-generates a draft observation.
    /// </summary>
    Task<QuickPhotoResponse> QuickPhotoAsync(
        Guid exerciseId,
        Stream photoStream,
        Stream thumbnailStream,
        string fileName,
        long fileSizeBytes,
        QuickPhotoRequest request,
        string capturedById,
        CancellationToken ct = default);

    /// <summary>
    /// Get soft-deleted photos for an exercise (trash view).
    /// </summary>
    Task<IEnumerable<DeletedPhotoDto>> GetDeletedPhotosAsync(
        Guid exerciseId, CancellationToken ct = default);

    /// <summary>
    /// Restore a soft-deleted photo back to the gallery.
    /// </summary>
    Task<PhotoDto?> RestorePhotoAsync(
        Guid photoId, string restoredBy, CancellationToken ct = default);

    /// <summary>
    /// Permanently delete a photo and remove blobs from storage.
    /// </summary>
    Task<bool> PermanentDeletePhotoAsync(
        Guid photoId, string deletedBy, CancellationToken ct = default);

    /// <summary>
    /// Soft delete all photos linked to an observation (cascade).
    /// </summary>
    Task<int> SoftDeletePhotosForObservationAsync(
        Guid observationId, string deletedBy, CancellationToken ct = default);
}
