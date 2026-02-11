namespace Cadence.Core.Features.Photos.Models.DTOs;

/// <summary>
/// DTO for photo response.
/// </summary>
public record PhotoDto(
    Guid Id,
    Guid ExerciseId,
    Guid? ObservationId,
    string CapturedById,
    string? CapturedByName,
    string FileName,
    string BlobUri,
    string ThumbnailUri,
    long FileSizeBytes,
    DateTime CapturedAt,
    DateTime? ScenarioTime,
    double? Latitude,
    double? Longitude,
    double? LocationAccuracy,
    int DisplayOrder,
    string Status,
    string? AnnotationsJson,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// DTO for soft-deleted photos shown in the trash/admin view.
/// </summary>
public record DeletedPhotoDto(
    Guid Id,
    Guid ExerciseId,
    Guid? ObservationId,
    string CapturedById,
    string? CapturedByName,
    string FileName,
    string BlobUri,
    string ThumbnailUri,
    long FileSizeBytes,
    DateTime CapturedAt,
    DateTime? ScenarioTime,
    string Status,
    DateTime CreatedAt,
    DateTime? DeletedAt,
    string? DeletedBy
);

/// <summary>
/// Lightweight photo tag for embedding in observation DTOs.
/// </summary>
public record PhotoTagDto(
    Guid Id,
    string ThumbnailUri,
    DateTime CapturedAt,
    int DisplayOrder
);

/// <summary>
/// DTO for uploading a new photo. Metadata sent alongside multipart file.
/// </summary>
public class UploadPhotoRequest
{
    /// <summary>
    /// Wall clock UTC timestamp when the photo was captured. Required.
    /// </summary>
    public DateTime CapturedAt { get; init; }

    /// <summary>
    /// Exercise scenario time when the photo was captured.
    /// Null if exercise clock was not running.
    /// </summary>
    public DateTime? ScenarioTime { get; init; }

    /// <summary>
    /// GPS latitude where the photo was captured. Optional.
    /// </summary>
    public double? Latitude { get; init; }

    /// <summary>
    /// GPS longitude where the photo was captured. Optional.
    /// </summary>
    public double? Longitude { get; init; }

    /// <summary>
    /// GPS accuracy in meters. Optional.
    /// </summary>
    public double? LocationAccuracy { get; init; }

    /// <summary>
    /// The observation this photo is attached to. Optional.
    /// Photos can exist unlinked for later association.
    /// </summary>
    public Guid? ObservationId { get; init; }

    /// <summary>
    /// Client-generated idempotency key to prevent duplicate uploads on retry.
    /// Optional - only set for offline-originating uploads.
    /// </summary>
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// DTO for updating an existing photo (e.g., linking to observation, changing display order).
/// </summary>
public class UpdatePhotoRequest
{
    /// <summary>
    /// The observation this photo is attached to. Optional.
    /// Set to null to unlink from an observation.
    /// </summary>
    public Guid? ObservationId { get; init; }

    /// <summary>
    /// Display order within an observation's photo collection.
    /// </summary>
    public int? DisplayOrder { get; init; }

    /// <summary>
    /// JSON-serialized annotation data (circles, arrows, text overlays).
    /// Stored as relative coordinates (0-1 range) for cross-device rendering.
    /// </summary>
    public string? AnnotationsJson { get; init; }
}

/// <summary>
/// Query parameters for listing photos with filtering and pagination.
/// </summary>
public class PhotoListQuery
{
    /// <summary>
    /// Filter by user who captured the photo. Optional.
    /// </summary>
    public string? CapturedById { get; init; }

    /// <summary>
    /// Filter photos captured on or after this date. Optional.
    /// </summary>
    public DateTime? From { get; init; }

    /// <summary>
    /// Filter photos captured on or before this date. Optional.
    /// </summary>
    public DateTime? To { get; init; }

    /// <summary>
    /// Filter by observation linkage status. Optional.
    /// true = has ObservationId, false = no ObservationId, null = all.
    /// </summary>
    public bool? LinkedOnly { get; init; }

    /// <summary>
    /// Page number (1-indexed). Defaults to 1.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of items per page. Defaults to 50, max 100.
    /// </summary>
    public int PageSize { get; init; } = 50;
}

/// <summary>
/// DTO for Quick Photo feature. Metadata sent alongside multipart file.
/// Creates a photo and auto-generates a draft observation.
/// </summary>
public class QuickPhotoRequest
{
    /// <summary>
    /// Wall clock UTC timestamp when the photo was captured. Required.
    /// </summary>
    public DateTime CapturedAt { get; init; }

    /// <summary>
    /// Exercise scenario time when the photo was captured.
    /// Null if exercise clock was not running.
    /// </summary>
    public DateTime? ScenarioTime { get; init; }

    /// <summary>
    /// GPS latitude where the photo was captured. Optional.
    /// </summary>
    public double? Latitude { get; init; }

    /// <summary>
    /// GPS longitude where the photo was captured. Optional.
    /// </summary>
    public double? Longitude { get; init; }

    /// <summary>
    /// GPS accuracy in meters. Optional.
    /// </summary>
    public double? LocationAccuracy { get; init; }

    /// <summary>
    /// Client-generated idempotency key to prevent duplicate uploads on retry.
    /// Optional - only set for offline-originating uploads.
    /// </summary>
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// Response for Quick Photo feature. Includes both the photo and the auto-created draft observation.
/// </summary>
public record QuickPhotoResponse(
    PhotoDto Photo,
    Guid ObservationId
);

/// <summary>
/// Paginated response wrapper for photo lists.
/// </summary>
public record PhotoListResponse(
    IEnumerable<PhotoDto> Photos,
    int TotalCount,
    int Page,
    int PageSize
);
