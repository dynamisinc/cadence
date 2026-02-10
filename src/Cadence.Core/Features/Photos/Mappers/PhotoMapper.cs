using Cadence.Core.Features.Photos.Models.DTOs;
using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Photos.Mappers;

/// <summary>
/// Extension methods for mapping between ExercisePhoto entity and DTOs.
/// </summary>
public static class PhotoMapper
{
    /// <summary>
    /// Converts an ExercisePhoto entity to a PhotoDto.
    /// </summary>
    /// <param name="entity">The ExercisePhoto entity to convert.</param>
    /// <returns>A PhotoDto with all mapped properties.</returns>
    public static PhotoDto ToDto(this ExercisePhoto entity) => new(
        entity.Id,
        entity.ExerciseId,
        entity.ObservationId,
        entity.CapturedById,
        entity.CapturedByUser?.DisplayName,
        entity.FileName,
        entity.BlobUri,
        entity.ThumbnailUri,
        entity.FileSizeBytes,
        entity.CapturedAt,
        entity.ScenarioTime,
        entity.Latitude,
        entity.Longitude,
        entity.LocationAccuracy,
        entity.DisplayOrder,
        entity.Status.ToString(),
        entity.CreatedAt,
        entity.UpdatedAt
    );

    public static DeletedPhotoDto ToDeletedDto(this ExercisePhoto entity) => new(
        entity.Id,
        entity.ExerciseId,
        entity.ObservationId,
        entity.CapturedById,
        entity.CapturedByUser?.DisplayName,
        entity.FileName,
        entity.BlobUri,
        entity.ThumbnailUri,
        entity.FileSizeBytes,
        entity.CapturedAt,
        entity.ScenarioTime,
        entity.Status.ToString(),
        entity.CreatedAt,
        entity.DeletedAt,
        entity.DeletedBy
    );
}
