using System.Security.Claims;
using Cadence.Core.Features.Photos.Models.DTOs;
using Cadence.Core.Features.Photos.Services;
using Cadence.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for photo management during exercise conduct.
/// Photos capture visual documentation of field activities and are time-stamped
/// with both wall clock and scenario time. They can be linked to observations.
/// Requires authentication for all endpoints.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class PhotosController : ControllerBase
{
    private readonly IPhotoService _photoService;
    private readonly ILogger<PhotosController> _logger;

    public PhotosController(IPhotoService photoService, ILogger<PhotosController> logger)
    {
        _photoService = photoService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a new photo for an exercise.
    /// Accepts multipart form data with photo file and optional thumbnail file.
    /// Requires any exercise participant role.
    /// Maximum file size: 10 MB.
    /// </summary>
    [HttpPost("exercises/{exerciseId:guid}/photos")]
    [AuthorizeExerciseAccess]
    [RequestSizeLimit(10_485_760)] // 10 MB max
    public async Task<ActionResult<PhotoDto>> UploadPhoto(
        Guid exerciseId,
        IFormFile photo,
        IFormFile? thumbnail,
        [FromForm] UploadPhotoRequest metadata)
    {
        // Validate file is present
        if (photo == null || photo.Length == 0)
        {
            return BadRequest(new { message = "Photo file is required" });
        }

        // Validate file is an image
        if (!photo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "File must be an image" });
        }

        // Validate thumbnail if provided
        if (thumbnail != null && thumbnail.Length > 0)
        {
            if (!thumbnail.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Thumbnail must be an image" });
            }
        }

        try
        {
            var capturedById = GetCurrentUserId();

            // Extract idempotency key from header if present
            var idempotencyKey = Request.Headers["X-Idempotency-Key"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                metadata.IdempotencyKey = idempotencyKey;
            }

            await using var photoStream = photo.OpenReadStream();
            await using var thumbnailStream = thumbnail != null && thumbnail.Length > 0
                ? thumbnail.OpenReadStream()
                : Stream.Null;

            var result = await _photoService.UploadPhotoAsync(
                exerciseId,
                photoStream,
                thumbnailStream,
                photo.FileName,
                photo.Length,
                metadata,
                capturedById);

            return CreatedAtAction(
                nameof(GetPhoto),
                new { exerciseId, photoId = result.Id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get photos for an exercise with optional filtering and pagination.
    /// Requires any exercise participant role.
    /// </summary>
    [HttpGet("exercises/{exerciseId:guid}/photos")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<PhotoListResponse>> GetPhotosByExercise(
        Guid exerciseId,
        [FromQuery] PhotoListQuery query)
    {
        var result = await _photoService.GetPhotosByExerciseAsync(exerciseId, query);
        return Ok(result);
    }

    /// <summary>
    /// Get a single photo by ID.
    /// Requires any exercise participant role.
    /// </summary>
    [HttpGet("exercises/{exerciseId:guid}/photos/{photoId:guid}")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<PhotoDto>> GetPhoto(Guid exerciseId, Guid photoId)
    {
        var photo = await _photoService.GetPhotoAsync(photoId);

        if (photo == null)
        {
            return NotFound();
        }

        // Verify photo belongs to the specified exercise
        if (photo.ExerciseId != exerciseId)
        {
            return NotFound();
        }

        return Ok(photo);
    }

    /// <summary>
    /// Update photo metadata (link to observation, display order).
    /// Requires any exercise participant role.
    /// </summary>
    [HttpPut("exercises/{exerciseId:guid}/photos/{photoId:guid}")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<PhotoDto>> UpdatePhoto(
        Guid exerciseId,
        Guid photoId,
        UpdatePhotoRequest request)
    {
        try
        {
            var modifiedBy = GetCurrentUserId();
            var photo = await _photoService.UpdatePhotoAsync(photoId, request, modifiedBy);

            if (photo == null)
            {
                return NotFound();
            }

            // Verify photo belongs to the specified exercise
            if (photo.ExerciseId != exerciseId)
            {
                return NotFound();
            }

            return Ok(photo);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a photo (soft delete) and remove from blob storage.
    /// Requires any exercise participant role.
    /// </summary>
    [HttpDelete("exercises/{exerciseId:guid}/photos/{photoId:guid}")]
    [AuthorizeExerciseAccess]
    public async Task<IActionResult> DeletePhoto(Guid exerciseId, Guid photoId)
    {
        var deletedBy = GetCurrentUserId();
        var deleted = await _photoService.DeletePhotoAsync(photoId, deletedBy);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Quick photo capture - uploads photo and auto-generates a draft observation.
    /// This is the simplified workflow for field evaluators to quickly document
    /// observations with a photo, creating both the photo and observation in one action.
    /// Requires Evaluator or higher role in the exercise.
    /// Maximum file size: 10 MB.
    /// </summary>
    [HttpPost("exercises/{exerciseId:guid}/photos/quick")]
    [AuthorizeExerciseAccess]
    [RequestSizeLimit(10_485_760)] // 10 MB max
    public async Task<ActionResult<QuickPhotoResponse>> QuickPhoto(
        Guid exerciseId,
        IFormFile photo,
        IFormFile? thumbnail,
        [FromForm] QuickPhotoRequest metadata)
    {
        // Validate file is present
        if (photo == null || photo.Length == 0)
        {
            return BadRequest(new { message = "Photo file is required" });
        }

        // Validate file is an image
        if (!photo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "File must be an image" });
        }

        // Validate thumbnail if provided
        if (thumbnail != null && thumbnail.Length > 0)
        {
            if (!thumbnail.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Thumbnail must be an image" });
            }
        }

        try
        {
            var capturedById = GetCurrentUserId();

            // Extract idempotency key from header if present
            var idempotencyKey = Request.Headers["X-Idempotency-Key"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                metadata.IdempotencyKey = idempotencyKey;
            }

            await using var photoStream = photo.OpenReadStream();
            await using var thumbnailStream = thumbnail != null && thumbnail.Length > 0
                ? thumbnail.OpenReadStream()
                : Stream.Null;

            var result = await _photoService.QuickPhotoAsync(
                exerciseId,
                photoStream,
                thumbnailStream,
                photo.FileName,
                photo.Length,
                metadata,
                capturedById);

            return CreatedAtAction(
                nameof(GetPhoto),
                new { exerciseId, photoId = result.Photo.Id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get soft-deleted photos for an exercise (trash view).
    /// Requires Exercise Director or Admin role.
    /// </summary>
    [HttpGet("exercises/{exerciseId:guid}/photos/deleted")]
    [AuthorizeExerciseDirector]
    public async Task<ActionResult<IEnumerable<DeletedPhotoDto>>> GetDeletedPhotos(Guid exerciseId)
    {
        var photos = await _photoService.GetDeletedPhotosAsync(exerciseId);
        return Ok(photos);
    }

    /// <summary>
    /// Restore a soft-deleted photo back to the gallery.
    /// Requires Exercise Director or Admin role.
    /// </summary>
    [HttpPost("exercises/{exerciseId:guid}/photos/{photoId:guid}/restore")]
    [AuthorizeExerciseDirector]
    public async Task<ActionResult<PhotoDto>> RestorePhoto(Guid exerciseId, Guid photoId)
    {
        var restoredBy = GetCurrentUserId();
        var photo = await _photoService.RestorePhotoAsync(photoId, restoredBy);

        if (photo == null)
        {
            return NotFound();
        }

        return Ok(photo);
    }

    /// <summary>
    /// Permanently delete a photo and remove blobs from storage.
    /// This action cannot be undone.
    /// Requires Exercise Director or Admin role.
    /// </summary>
    [HttpDelete("exercises/{exerciseId:guid}/photos/{photoId:guid}/permanent")]
    [AuthorizeExerciseDirector]
    public async Task<IActionResult> PermanentDeletePhoto(Guid exerciseId, Guid photoId)
    {
        var deletedBy = GetCurrentUserId();
        var deleted = await _photoService.PermanentDeletePhotoAsync(photoId, deletedBy);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Get current authenticated user's ID from JWT claims.
    /// </summary>
    private string GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }
        return userIdClaim;
    }
}
