using Cadence.Core.Data;
using Cadence.Core.Features.Photos.Models.DTOs;
using Cadence.Core.Features.Photos.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Photos;

public class PhotoServiceTests
{
    private readonly Mock<IBlobStorageService> _blobStorageMock;
    private readonly Mock<IExerciseHubContext> _hubContextMock;
    private readonly Mock<ILogger<PhotoService>> _loggerMock;

    public PhotoServiceTests()
    {
        _blobStorageMock = new Mock<IBlobStorageService>();
        _hubContextMock = new Mock<IExerciseHubContext>();
        _loggerMock = new Mock<ILogger<PhotoService>>();
    }

    #region Helper Methods

    private (AppDbContext context, Organization org, Exercise exercise) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Organizations.Add(org);
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();
        return (context, org, exercise);
    }

    private PhotoService CreateService(AppDbContext context)
    {
        return new PhotoService(context, _blobStorageMock.Object, _hubContextMock.Object, _loggerMock.Object);
    }

    private void SetupBlobUpload()
    {
        _blobStorageMock
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream _, string path, string _, CancellationToken _) => $"https://blob.test/{path}");
    }

    private ExercisePhoto CreatePhoto(Exercise exercise, Organization org, string userId, Guid? observationId = null)
    {
        return new ExercisePhoto
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            ObservationId = observationId,
            CapturedById = userId,
            FileName = "test.jpg",
            BlobUri = "https://blob.test/photo.jpg",
            ThumbnailUri = "https://blob.test/thumb.jpg",
            FileSizeBytes = 1024,
            CapturedAt = DateTime.UtcNow,
            Status = PhotoStatus.Draft,
            CreatedBy = userId,
            ModifiedBy = userId
        };
    }

    #endregion

    #region UploadPhotoAsync Tests

    [Fact]
    public async Task UploadPhotoAsync_ValidRequest_ReturnsPhotoDto()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();
        SetupBlobUpload();

        var request = new UploadPhotoRequest
        {
            CapturedAt = DateTime.UtcNow,
            Latitude = 45.5,
            Longitude = -122.5
        };

        // Act
        var result = await service.UploadPhotoAsync(
            exercise.Id,
            new MemoryStream(new byte[] { 1, 2, 3 }),
            new MemoryStream(new byte[] { 4, 5, 6 }),
            "test-photo.jpg",
            1024,
            request,
            userId);

        // Assert
        result.Should().NotBeNull();
        result.ExerciseId.Should().Be(exercise.Id);
        result.FileName.Should().Be("test-photo.jpg");
        result.CapturedById.Should().Be(userId);
        result.FileSizeBytes.Should().Be(1024);
        result.Latitude.Should().Be(45.5);
        result.Longitude.Should().Be(-122.5);
        result.BlobUri.Should().StartWith("https://blob.test/");
        result.ThumbnailUri.Should().StartWith("https://blob.test/");
    }

    [Fact]
    public async Task UploadPhotoAsync_ExerciseNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, _) = CreateTestContext();
        var service = CreateService(context);
        var request = new UploadPhotoRequest { CapturedAt = DateTime.UtcNow };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UploadPhotoAsync(
                Guid.NewGuid(),
                new MemoryStream(new byte[] { 1 }),
                Stream.Null,
                "test.jpg", 100, request,
                Guid.NewGuid().ToString()));
    }

    [Fact]
    public async Task UploadPhotoAsync_InactiveExercise_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, org, _) = CreateTestContext();
        var draftExercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Draft Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Exercises.Add(draftExercise);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = new UploadPhotoRequest { CapturedAt = DateTime.UtcNow };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UploadPhotoAsync(
                draftExercise.Id,
                new MemoryStream(new byte[] { 1 }),
                Stream.Null,
                "test.jpg", 100, request,
                Guid.NewGuid().ToString()));
    }

    [Fact]
    public async Task UploadPhotoAsync_WithObservationId_LinksToObservation()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        var observation = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            Content = "Test observation",
            Status = ObservationStatus.Complete,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.Observations.Add(observation);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        SetupBlobUpload();

        var request = new UploadPhotoRequest
        {
            CapturedAt = DateTime.UtcNow,
            ObservationId = observation.Id
        };

        // Act
        var result = await service.UploadPhotoAsync(
            exercise.Id,
            new MemoryStream(new byte[] { 1, 2, 3 }),
            new MemoryStream(new byte[] { 4, 5, 6 }),
            "test.jpg", 1024, request, userId);

        // Assert
        result.ObservationId.Should().Be(observation.Id);
    }

    [Fact]
    public async Task UploadPhotoAsync_InvalidObservationId_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);
        var request = new UploadPhotoRequest
        {
            CapturedAt = DateTime.UtcNow,
            ObservationId = Guid.NewGuid()
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UploadPhotoAsync(
                exercise.Id,
                new MemoryStream(new byte[] { 1 }),
                Stream.Null,
                "test.jpg", 100, request,
                Guid.NewGuid().ToString()));
    }

    [Fact]
    public async Task UploadPhotoAsync_SetsOrganizationIdFromExercise()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();
        SetupBlobUpload();

        var request = new UploadPhotoRequest { CapturedAt = DateTime.UtcNow };

        // Act
        var result = await service.UploadPhotoAsync(
            exercise.Id,
            new MemoryStream(new byte[] { 1, 2, 3 }),
            new MemoryStream(new byte[] { 4, 5, 6 }),
            "test.jpg", 1024, request, userId);

        // Assert
        var savedPhoto = await context.ExercisePhotos.FindAsync(result.Id);
        savedPhoto.Should().NotBeNull();
        savedPhoto!.OrganizationId.Should().Be(org.Id);
    }

    [Fact]
    public async Task UploadPhotoAsync_BroadcastsPhotoAdded()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();
        SetupBlobUpload();

        var request = new UploadPhotoRequest { CapturedAt = DateTime.UtcNow };

        // Act
        await service.UploadPhotoAsync(
            exercise.Id,
            new MemoryStream(new byte[] { 1, 2, 3 }),
            new MemoryStream(new byte[] { 4, 5, 6 }),
            "test.jpg", 1024, request, userId);

        // Assert
        _hubContextMock.Verify(
            x => x.NotifyPhotoAdded(exercise.Id, It.IsAny<PhotoDto>()),
            Times.Once);
    }

    #endregion

    #region GetPhotosByExerciseAsync Tests

    [Fact]
    public async Task GetPhotosByExerciseAsync_ReturnsPhotosForExercise()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        context.ExercisePhotos.AddRange(
            CreatePhoto(exercise, org, userId),
            CreatePhoto(exercise, org, userId));
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var query = new PhotoListQuery { Page = 1, PageSize = 10 };

        // Act
        var result = await service.GetPhotosByExerciseAsync(exercise.Id, query);

        // Assert
        result.Should().NotBeNull();
        result.Photos.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPhotosByExerciseAsync_LinkedOnlyTrue_ReturnsOnlyLinkedPhotos()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        var observation = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            Content = "Test observation",
            Status = ObservationStatus.Complete,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.Observations.Add(observation);

        context.ExercisePhotos.AddRange(
            CreatePhoto(exercise, org, userId, observationId: observation.Id),
            CreatePhoto(exercise, org, userId, observationId: null));
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var query = new PhotoListQuery { LinkedOnly = true, Page = 1, PageSize = 10 };

        // Act
        var result = await service.GetPhotosByExerciseAsync(exercise.Id, query);

        // Assert
        result.Photos.Should().HaveCount(1);
        result.Photos.First().ObservationId.Should().Be(observation.Id);
    }

    [Fact]
    public async Task GetPhotosByExerciseAsync_LinkedOnlyFalse_ReturnsOnlyUnlinkedPhotos()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        var observation = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            Content = "Test observation",
            Status = ObservationStatus.Complete,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.Observations.Add(observation);

        context.ExercisePhotos.AddRange(
            CreatePhoto(exercise, org, userId, observationId: observation.Id),
            CreatePhoto(exercise, org, userId, observationId: null));
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var query = new PhotoListQuery { LinkedOnly = false, Page = 1, PageSize = 10 };

        // Act
        var result = await service.GetPhotosByExerciseAsync(exercise.Id, query);

        // Assert
        result.Photos.Should().HaveCount(1);
        result.Photos.First().ObservationId.Should().BeNull();
    }

    [Fact]
    public async Task GetPhotosByExerciseAsync_PaginationWorks()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        for (int i = 0; i < 5; i++)
        {
            var photo = CreatePhoto(exercise, org, userId);
            photo.FileName = $"photo{i}.jpg";
            photo.CapturedAt = DateTime.UtcNow.AddMinutes(-i);
            context.ExercisePhotos.Add(photo);
        }
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var page1 = await service.GetPhotosByExerciseAsync(exercise.Id, new PhotoListQuery { Page = 1, PageSize = 2 });
        var page2 = await service.GetPhotosByExerciseAsync(exercise.Id, new PhotoListQuery { Page = 2, PageSize = 2 });

        // Assert
        page1.Photos.Should().HaveCount(2);
        page1.TotalCount.Should().Be(5);
        page1.Page.Should().Be(1);

        page2.Photos.Should().HaveCount(2);
        page2.TotalCount.Should().Be(5);
        page2.Page.Should().Be(2);
    }

    #endregion

    #region GetPhotoAsync Tests

    [Fact]
    public async Task GetPhotoAsync_ExistingPhoto_ReturnsDto()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        var photo = CreatePhoto(exercise, org, userId);
        photo.FileName = "specific.jpg";
        context.ExercisePhotos.Add(photo);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetPhotoAsync(photo.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(photo.Id);
        result.FileName.Should().Be("specific.jpg");
    }

    [Fact]
    public async Task GetPhotoAsync_NonExistentPhoto_ReturnsNull()
    {
        // Arrange
        var (context, _, _) = CreateTestContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetPhotoAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeletePhotoAsync Tests

    [Fact]
    public async Task DeletePhotoAsync_ExistingPhoto_PerformsSoftDelete()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        var photo = CreatePhoto(exercise, org, userId);
        context.ExercisePhotos.Add(photo);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.DeletePhotoAsync(photo.Id, userId);

        // Assert
        result.Should().BeTrue();

        var deletedPhoto = await context.ExercisePhotos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == photo.Id);

        deletedPhoto.Should().NotBeNull();
        deletedPhoto!.IsDeleted.Should().BeTrue();
        deletedPhoto.DeletedAt.Should().NotBeNull();
        deletedPhoto.DeletedBy.Should().Be(userId);
    }

    [Fact]
    public async Task DeletePhotoAsync_NonExistentPhoto_ReturnsFalse()
    {
        // Arrange
        var (context, _, _) = CreateTestContext();
        var service = CreateService(context);

        // Act
        var result = await service.DeletePhotoAsync(Guid.NewGuid(), Guid.NewGuid().ToString());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeletePhotoAsync_BroadcastsPhotoDeleted()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        var photo = CreatePhoto(exercise, org, userId);
        context.ExercisePhotos.Add(photo);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.DeletePhotoAsync(photo.Id, userId);

        // Assert
        _hubContextMock.Verify(
            x => x.NotifyPhotoDeleted(exercise.Id, photo.Id),
            Times.Once);
    }

    [Fact]
    public async Task DeletePhotoAsync_SoftDeletedNotReturnedInQueries()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        var photo = CreatePhoto(exercise, org, userId);
        context.ExercisePhotos.Add(photo);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.DeletePhotoAsync(photo.Id, userId);
        var result = await service.GetPhotoAsync(photo.Id);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region RestorePhotoAsync Tests

    [Fact]
    public async Task RestorePhotoAsync_DeletedPhoto_RestoresSuccessfully()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        var photo = CreatePhoto(exercise, org, userId);
        photo.IsDeleted = true;
        photo.DeletedAt = DateTime.UtcNow;
        photo.DeletedBy = userId;
        context.ExercisePhotos.Add(photo);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.RestorePhotoAsync(photo.Id, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(photo.Id);

        var restoredPhoto = await context.ExercisePhotos.FindAsync(photo.Id);
        restoredPhoto.Should().NotBeNull();
        restoredPhoto!.IsDeleted.Should().BeFalse();
        restoredPhoto.DeletedAt.Should().BeNull();
        restoredPhoto.DeletedBy.Should().BeNull();
    }

    [Fact]
    public async Task RestorePhotoAsync_NonDeletedPhoto_ReturnsNull()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        var photo = CreatePhoto(exercise, org, userId);
        context.ExercisePhotos.Add(photo);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.RestorePhotoAsync(photo.Id, userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region PermanentDeletePhotoAsync Tests

    [Fact]
    public async Task PermanentDeletePhotoAsync_DeletedPhoto_RemovesFromDatabase()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        var photo = CreatePhoto(exercise, org, userId);
        photo.IsDeleted = true;
        photo.DeletedAt = DateTime.UtcNow;
        photo.DeletedBy = userId;
        context.ExercisePhotos.Add(photo);
        await context.SaveChangesAsync();

        _blobStorageMock
            .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService(context);

        // Act
        var result = await service.PermanentDeletePhotoAsync(photo.Id, userId);

        // Assert
        result.Should().BeTrue();

        var deletedPhoto = await context.ExercisePhotos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == photo.Id);

        deletedPhoto.Should().BeNull();
    }

    [Fact]
    public async Task PermanentDeletePhotoAsync_DeletesBlobsFromStorage()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        var photo = CreatePhoto(exercise, org, userId);
        photo.BlobUri = "https://blob.test/photo.jpg";
        photo.ThumbnailUri = "https://blob.test/thumb.jpg";
        photo.IsDeleted = true;
        photo.DeletedAt = DateTime.UtcNow;
        photo.DeletedBy = userId;
        context.ExercisePhotos.Add(photo);
        await context.SaveChangesAsync();

        _blobStorageMock
            .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService(context);

        // Act
        await service.PermanentDeletePhotoAsync(photo.Id, userId);

        // Assert
        _blobStorageMock.Verify(
            x => x.DeleteAsync("https://blob.test/photo.jpg", It.IsAny<CancellationToken>()),
            Times.Once);
        _blobStorageMock.Verify(
            x => x.DeleteAsync("https://blob.test/thumb.jpg", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PermanentDeletePhotoAsync_NonDeletedPhoto_ReturnsFalse()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        var photo = CreatePhoto(exercise, org, userId);
        context.ExercisePhotos.Add(photo);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.PermanentDeletePhotoAsync(photo.Id, userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SoftDeletePhotosForObservationAsync Tests

    [Fact]
    public async Task SoftDeletePhotosForObservationAsync_DeletesAllLinkedPhotos()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        var observation = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            Content = "Test observation",
            Status = ObservationStatus.Complete,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.Observations.Add(observation);

        var photo1 = CreatePhoto(exercise, org, userId, observation.Id);
        var photo2 = CreatePhoto(exercise, org, userId, observation.Id);
        context.ExercisePhotos.AddRange(photo1, photo2);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.SoftDeletePhotosForObservationAsync(observation.Id, userId);

        // Assert
        result.Should().Be(2);

        var deletedPhotos = await context.ExercisePhotos
            .IgnoreQueryFilters()
            .Where(p => p.ObservationId == observation.Id)
            .ToListAsync();

        deletedPhotos.Should().HaveCount(2);
        deletedPhotos.Should().AllSatisfy(p =>
        {
            p.IsDeleted.Should().BeTrue();
            p.DeletedAt.Should().NotBeNull();
            p.DeletedBy.Should().Be(userId);
        });
    }

    [Fact]
    public async Task SoftDeletePhotosForObservationAsync_NoPhotos_ReturnsZero()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        var observation = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            Content = "Test observation",
            Status = ObservationStatus.Complete,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.Observations.Add(observation);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.SoftDeletePhotosForObservationAsync(observation.Id, userId);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task SoftDeletePhotosForObservationAsync_BroadcastsDeletionForEachPhoto()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();
        var observation = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            Content = "Test observation",
            Status = ObservationStatus.Complete,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.Observations.Add(observation);

        var photo1 = CreatePhoto(exercise, org, userId, observation.Id);
        var photo2 = CreatePhoto(exercise, org, userId, observation.Id);
        context.ExercisePhotos.AddRange(photo1, photo2);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.SoftDeletePhotosForObservationAsync(observation.Id, userId);

        // Assert
        _hubContextMock.Verify(
            x => x.NotifyPhotoDeleted(exercise.Id, photo1.Id),
            Times.Once);
        _hubContextMock.Verify(
            x => x.NotifyPhotoDeleted(exercise.Id, photo2.Id),
            Times.Once);
    }

    #endregion
}
