using Cadence.Core.Features.Photos.Mappers;
using Cadence.Core.Models.Entities;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Mappers;

public class PhotoMapperTests
{
    private static ExercisePhoto CreateTestPhoto() => new()
    {
        Id = Guid.NewGuid(),
        ExerciseId = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        ObservationId = Guid.NewGuid(),
        CapturedById = "user-123",
        CapturedByUser = new ApplicationUser { Id = "user-123", DisplayName = "John Doe" },
        FileName = "IMG_20260315.jpg",
        BlobUri = "https://storage.blob.core.windows.net/photos/full/abc.jpg",
        ThumbnailUri = "https://storage.blob.core.windows.net/photos/thumb/abc.jpg",
        FileSizeBytes = 1024000,
        CapturedAt = new DateTime(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc),
        ScenarioTime = new DateTime(2026, 3, 15, 9, 0, 0, DateTimeKind.Utc),
        Latitude = 40.7128,
        Longitude = -74.0060,
        LocationAccuracy = 5.0,
        DisplayOrder = 1,
        Status = PhotoStatus.Complete,
        AnnotationsJson = """{"circles":[]}""",
        CreatedAt = new DateTime(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 3, 15, 10, 31, 0, DateTimeKind.Utc)
    };

    // =========================================================================
    // ToDto Tests
    // =========================================================================

    [Fact]
    public void ToDto_MapsAllProperties()
    {
        var entity = CreateTestPhoto();

        var dto = entity.ToDto();

        dto.Id.Should().Be(entity.Id);
        dto.ExerciseId.Should().Be(entity.ExerciseId);
        dto.ObservationId.Should().Be(entity.ObservationId);
        dto.CapturedById.Should().Be("user-123");
        dto.CapturedByName.Should().Be("John Doe");
        dto.FileName.Should().Be("IMG_20260315.jpg");
        dto.BlobUri.Should().Be(entity.BlobUri);
        dto.ThumbnailUri.Should().Be(entity.ThumbnailUri);
        dto.FileSizeBytes.Should().Be(1024000);
        dto.CapturedAt.Should().Be(entity.CapturedAt);
        dto.ScenarioTime.Should().Be(entity.ScenarioTime);
        dto.Latitude.Should().Be(40.7128);
        dto.Longitude.Should().Be(-74.0060);
        dto.LocationAccuracy.Should().Be(5.0);
        dto.DisplayOrder.Should().Be(1);
        dto.Status.Should().Be("Complete");
        dto.AnnotationsJson.Should().Be("""{"circles":[]}""");
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.UpdatedAt.Should().Be(entity.UpdatedAt);
    }

    [Fact]
    public void ToDto_NullCapturedByUser_MapsNameAsNull()
    {
        var entity = CreateTestPhoto();
        entity.CapturedByUser = null;

        var dto = entity.ToDto();

        dto.CapturedByName.Should().BeNull();
    }

    [Fact]
    public void ToDto_NullOptionalFields_MapsAsNull()
    {
        var entity = CreateTestPhoto();
        entity.ObservationId = null;
        entity.ScenarioTime = null;
        entity.Latitude = null;
        entity.Longitude = null;
        entity.LocationAccuracy = null;
        entity.AnnotationsJson = null;

        var dto = entity.ToDto();

        dto.ObservationId.Should().BeNull();
        dto.ScenarioTime.Should().BeNull();
        dto.Latitude.Should().BeNull();
        dto.Longitude.Should().BeNull();
        dto.LocationAccuracy.Should().BeNull();
        dto.AnnotationsJson.Should().BeNull();
    }

    // =========================================================================
    // ToDeletedDto Tests
    // =========================================================================

    [Fact]
    public void ToDeletedDto_MapsAllProperties()
    {
        var entity = CreateTestPhoto();
        entity.IsDeleted = true;
        entity.DeletedAt = new DateTime(2026, 3, 16, 0, 0, 0, DateTimeKind.Utc);
        entity.DeletedBy = Guid.NewGuid().ToString();

        var dto = entity.ToDeletedDto();

        dto.Id.Should().Be(entity.Id);
        dto.ExerciseId.Should().Be(entity.ExerciseId);
        dto.ObservationId.Should().Be(entity.ObservationId);
        dto.CapturedById.Should().Be("user-123");
        dto.CapturedByName.Should().Be("John Doe");
        dto.FileName.Should().Be("IMG_20260315.jpg");
        dto.BlobUri.Should().Be(entity.BlobUri);
        dto.ThumbnailUri.Should().Be(entity.ThumbnailUri);
        dto.FileSizeBytes.Should().Be(1024000);
        dto.CapturedAt.Should().Be(entity.CapturedAt);
        dto.ScenarioTime.Should().Be(entity.ScenarioTime);
        dto.Status.Should().Be("Complete");
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.DeletedAt.Should().Be(entity.DeletedAt);
        dto.DeletedBy.Should().Be(entity.DeletedBy);
    }
}
