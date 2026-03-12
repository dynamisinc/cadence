using Cadence.Core.Features.Photos.Mappers;
using Cadence.Core.Features.Photos.Models.DTOs;
using Cadence.Core.Models.Entities;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Photos.Mappers;

/// <summary>
/// Unit tests for PhotoMapper extension methods.
/// Verifies correct projection of ExercisePhoto entities to PhotoDto and DeletedPhotoDto.
/// </summary>
public class PhotoMapperTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static ExercisePhoto BuildFullPhoto(ApplicationUser? capturedByUser = null) => new()
    {
        Id = Guid.NewGuid(),
        ExerciseId = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        ObservationId = Guid.NewGuid(),
        CapturedById = "user-id-string",
        CapturedByUser = capturedByUser,
        FileName = "photo_001.jpg",
        BlobUri = "https://blob.example.com/photos/photo_001.jpg",
        ThumbnailUri = "https://blob.example.com/thumbs/photo_001_thumb.jpg",
        FileSizeBytes = 2_048_000L,
        CapturedAt = new DateTime(2025, 9, 1, 14, 30, 0, DateTimeKind.Utc),
        ScenarioTime = new DateTime(2025, 9, 1, 10, 0, 0, DateTimeKind.Utc),
        Latitude = 37.7749,
        Longitude = -122.4194,
        LocationAccuracy = 5.5,
        DisplayOrder = 3,
        Status = PhotoStatus.Complete,
        AnnotationsJson = """{"circles":[{"x":0.5,"y":0.5,"r":0.1}]}""",
        CreatedAt = new DateTime(2025, 9, 1, 14, 31, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 9, 2, 8, 0, 0, DateTimeKind.Utc)
    };

    // =========================================================================
    // ToDto
    // =========================================================================

    [Fact]
    public void ToDto_AllPropertiesPopulated_MapsEveryField()
    {
        // Arrange
        var user = new ApplicationUser { DisplayName = "Jane Controller" };
        var entity = BuildFullPhoto(user);

        // Act
        PhotoDto dto = entity.ToDto();

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.ExerciseId.Should().Be(entity.ExerciseId);
        dto.ObservationId.Should().Be(entity.ObservationId);
        dto.CapturedById.Should().Be(entity.CapturedById);
        dto.CapturedByName.Should().Be("Jane Controller");
        dto.FileName.Should().Be(entity.FileName);
        dto.BlobUri.Should().Be(entity.BlobUri);
        dto.ThumbnailUri.Should().Be(entity.ThumbnailUri);
        dto.FileSizeBytes.Should().Be(entity.FileSizeBytes);
        dto.CapturedAt.Should().Be(entity.CapturedAt);
        dto.ScenarioTime.Should().Be(entity.ScenarioTime);
        dto.Latitude.Should().Be(entity.Latitude);
        dto.Longitude.Should().Be(entity.Longitude);
        dto.LocationAccuracy.Should().Be(entity.LocationAccuracy);
        dto.DisplayOrder.Should().Be(entity.DisplayOrder);
        dto.Status.Should().Be(PhotoStatus.Complete.ToString());
        dto.AnnotationsJson.Should().Be(entity.AnnotationsJson);
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.UpdatedAt.Should().Be(entity.UpdatedAt);
    }

    [Fact]
    public void ToDto_CapturedByUserIsNull_MapsCapturedByNameAsNull()
    {
        // Arrange
        var entity = BuildFullPhoto(capturedByUser: null);

        // Act
        PhotoDto dto = entity.ToDto();

        // Assert
        dto.CapturedByName.Should().BeNull(
            "CapturedByUser?.DisplayName is null when navigation property is not loaded");
    }

    [Fact]
    public void ToDto_NullableLocationFieldsAreNull_MapsNullsThrough()
    {
        // Arrange
        var entity = new ExercisePhoto
        {
            Id = Guid.NewGuid(),
            ExerciseId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            CapturedById = "user-id",
            ObservationId = null,
            FileName = "img.jpg",
            BlobUri = "https://blob.example.com/img.jpg",
            ThumbnailUri = "https://blob.example.com/thumb.jpg",
            FileSizeBytes = 500_000L,
            CapturedAt = DateTime.UtcNow,
            ScenarioTime = null,
            Latitude = null,
            Longitude = null,
            LocationAccuracy = null,
            DisplayOrder = 0,
            Status = PhotoStatus.Draft,
            AnnotationsJson = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        PhotoDto dto = entity.ToDto();

        // Assert
        dto.ObservationId.Should().BeNull();
        dto.ScenarioTime.Should().BeNull();
        dto.Latitude.Should().BeNull();
        dto.Longitude.Should().BeNull();
        dto.LocationAccuracy.Should().BeNull();
        dto.AnnotationsJson.Should().BeNull();
    }

    [Fact]
    public void ToDto_DraftStatus_MapsStatusAsString()
    {
        // Arrange
        var entity = BuildFullPhoto();
        entity.Status = PhotoStatus.Draft;

        // Act
        PhotoDto dto = entity.ToDto();

        // Assert
        dto.Status.Should().Be("Draft");
    }

    [Fact]
    public void ToDto_NegativeCoordinates_MapsNegativeValues()
    {
        // Arrange
        var entity = BuildFullPhoto();
        entity.Latitude = -33.8688;
        entity.Longitude = -70.6693;

        // Act
        PhotoDto dto = entity.ToDto();

        // Assert
        dto.Latitude.Should().Be(-33.8688);
        dto.Longitude.Should().Be(-70.6693);
    }

    // =========================================================================
    // ToDeletedDto
    // =========================================================================

    [Fact]
    public void ToDeletedDto_AllPropertiesPopulated_MapsEveryField()
    {
        // Arrange
        var user = new ApplicationUser { DisplayName = "Tom Evaluator" };
        var entity = BuildFullPhoto(user);
        entity.DeletedAt = new DateTime(2025, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        entity.DeletedBy = "admin-user-id";

        // Act
        DeletedPhotoDto dto = entity.ToDeletedDto();

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.ExerciseId.Should().Be(entity.ExerciseId);
        dto.ObservationId.Should().Be(entity.ObservationId);
        dto.CapturedById.Should().Be(entity.CapturedById);
        dto.CapturedByName.Should().Be("Tom Evaluator");
        dto.FileName.Should().Be(entity.FileName);
        dto.BlobUri.Should().Be(entity.BlobUri);
        dto.ThumbnailUri.Should().Be(entity.ThumbnailUri);
        dto.FileSizeBytes.Should().Be(entity.FileSizeBytes);
        dto.CapturedAt.Should().Be(entity.CapturedAt);
        dto.ScenarioTime.Should().Be(entity.ScenarioTime);
        dto.Status.Should().Be(PhotoStatus.Complete.ToString());
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.DeletedAt.Should().Be(entity.DeletedAt);
        dto.DeletedBy.Should().Be("admin-user-id");
    }

    [Fact]
    public void ToDeletedDto_CapturedByUserIsNull_MapsCapturedByNameAsNull()
    {
        // Arrange
        var entity = BuildFullPhoto(capturedByUser: null);
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = "some-user";

        // Act
        DeletedPhotoDto dto = entity.ToDeletedDto();

        // Assert
        dto.CapturedByName.Should().BeNull();
    }

    [Fact]
    public void ToDeletedDto_NotYetDeleted_DeletedAtAndDeletedByAreNull()
    {
        // Arrange
        var entity = BuildFullPhoto();
        entity.DeletedAt = null;
        entity.DeletedBy = null;

        // Act
        DeletedPhotoDto dto = entity.ToDeletedDto();

        // Assert
        dto.DeletedAt.Should().BeNull();
        dto.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void ToDeletedDto_DoesNotIncludeLocationOrAnnotations()
    {
        // Arrange
        var entity = BuildFullPhoto();
        entity.DeletedAt = DateTime.UtcNow;

        // Act
        DeletedPhotoDto dto = entity.ToDeletedDto();

        // Assert — DeletedPhotoDto intentionally omits location and annotation fields
        var dtoType = dto.GetType();
        dtoType.GetProperty("Latitude").Should().BeNull(
            "DeletedPhotoDto must not expose Latitude");
        dtoType.GetProperty("Longitude").Should().BeNull(
            "DeletedPhotoDto must not expose Longitude");
        dtoType.GetProperty("AnnotationsJson").Should().BeNull(
            "DeletedPhotoDto must not expose AnnotationsJson");
    }
}
