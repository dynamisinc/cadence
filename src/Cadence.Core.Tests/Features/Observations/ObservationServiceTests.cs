using Cadence.Core.Data;
using Cadence.Core.Features.Notifications.Models.DTOs;
using Cadence.Core.Features.Notifications.Services;
using Cadence.Core.Features.Observations.Models.DTOs;
using Cadence.Core.Features.Observations.Services;
using Cadence.Core.Features.Photos.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Observations;

public class ObservationServiceTests
{
    private readonly Mock<IExerciseHubContext> _hubContextMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IPhotoService> _photoServiceMock;
    private readonly Mock<IBlobStorageService> _blobStorageMock;
    private readonly Mock<ILogger<ObservationService>> _loggerMock;

    public ObservationServiceTests()
    {
        _hubContextMock = new Mock<IExerciseHubContext>();
        _notificationServiceMock = new Mock<INotificationService>();
        _photoServiceMock = new Mock<IPhotoService>();
        _blobStorageMock = new Mock<IBlobStorageService>();
        _loggerMock = new Mock<ILogger<ObservationService>>();

        // Default: GetReadUri returns input URI unchanged (like local storage)
        _blobStorageMock
            .Setup(x => x.GetReadUri(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns((string uri, TimeSpan _) => uri);
    }

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

    private (Msel msel, Inject inject) CreateInject(AppDbContext context, Exercise exercise)
    {
        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Version = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Msels.Add(msel);

        var inject = new Inject
        {
            Id = Guid.NewGuid(),
            InjectNumber = 1,
            Title = "Test Inject",
            Description = "Test description",
            ScheduledTime = new TimeOnly(9, 0),
            Target = "Target",
            InjectType = InjectType.Standard,
            Status = InjectStatus.Draft,
            Sequence = 1,
            MselId = msel.Id,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Injects.Add(inject);
        context.SaveChanges();

        return (msel, inject);
    }

    private Objective CreateObjective(AppDbContext context, Exercise exercise)
    {
        var objective = new Objective
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            ObjectiveNumber = "1",
            Name = "Test Objective",
            Description = "Test objective description",
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Objectives.Add(objective);
        context.SaveChanges();

        return objective;
    }

    private Observation CreateObservation(AppDbContext context, Exercise exercise, Guid? injectId = null)
    {
        var observation = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            InjectId = injectId,
            Content = "Test observation content",
            Rating = ObservationRating.Performed,
            Recommendation = "Test recommendation",
            ObservedAt = DateTime.UtcNow,
            Location = "Test location",
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Observations.Add(observation);
        context.SaveChanges();

        return observation;
    }

    private ObservationService CreateService(AppDbContext context)
    {
        return new ObservationService(context, _hubContextMock.Object, _notificationServiceMock.Object, _photoServiceMock.Object, _blobStorageMock.Object, _loggerMock.Object);
    }

    #region GetObservationsByExerciseAsync Tests

    [Fact]
    public async Task GetObservationsByExerciseAsync_NoObservations_ReturnsEmptyList()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetObservationsByExerciseAsync(exercise.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetObservationsByExerciseAsync_WithObservations_ReturnsAll()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        CreateObservation(context, exercise);
        CreateObservation(context, exercise);
        CreateObservation(context, exercise);
        var service = CreateService(context);

        // Act
        var result = await service.GetObservationsByExerciseAsync(exercise.Id);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetObservationsByExerciseAsync_ReturnsOrderedByObservedAtDesc()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();

        var obs1 = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "First",
            ObservedAt = DateTime.UtcNow.AddHours(-2),
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        var obs2 = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Second",
            ObservedAt = DateTime.UtcNow.AddHours(-1),
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        var obs3 = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Third",
            ObservedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Observations.AddRange(obs1, obs2, obs3);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = (await service.GetObservationsByExerciseAsync(exercise.Id)).ToList();

        // Assert
        result[0].Content.Should().Be("Third"); // Most recent first
        result[1].Content.Should().Be("Second");
        result[2].Content.Should().Be("First");
    }

    [Fact]
    public async Task GetObservationsByExerciseAsync_OnlyReturnsObservationsForExercise()
    {
        // Arrange
        var (context, org, exercise1) = CreateTestContext();

        var exercise2 = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Exercise 2",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Exercises.Add(exercise2);
        await context.SaveChangesAsync();

        CreateObservation(context, exercise1);
        CreateObservation(context, exercise1);
        CreateObservation(context, exercise2);

        var service = CreateService(context);

        // Act
        var result = await service.GetObservationsByExerciseAsync(exercise1.Id);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetObservationsByInjectAsync Tests

    [Fact]
    public async Task GetObservationsByInjectAsync_NoObservations_ReturnsEmptyList()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var (_, inject) = CreateInject(context, exercise);
        var service = CreateService(context);

        // Act
        var result = await service.GetObservationsByInjectAsync(inject.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetObservationsByInjectAsync_WithObservations_ReturnsAll()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var (_, inject) = CreateInject(context, exercise);
        CreateObservation(context, exercise, inject.Id);
        CreateObservation(context, exercise, inject.Id);
        var service = CreateService(context);

        // Act
        var result = await service.GetObservationsByInjectAsync(inject.Id);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetObservationsByInjectAsync_ReturnsOrderedByObservedAtDesc()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var (_, inject) = CreateInject(context, exercise);

        var obs1 = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            InjectId = inject.Id,
            Content = "First",
            ObservedAt = DateTime.UtcNow.AddHours(-1),
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        var obs2 = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            InjectId = inject.Id,
            Content = "Second",
            ObservedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Observations.AddRange(obs1, obs2);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = (await service.GetObservationsByInjectAsync(inject.Id)).ToList();

        // Assert
        result[0].Content.Should().Be("Second"); // Most recent first
        result[1].Content.Should().Be("First");
    }

    [Fact]
    public async Task GetObservationsByInjectAsync_OnlyReturnsObservationsForInject()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var (msel, inject1) = CreateInject(context, exercise);

        var inject2 = new Inject
        {
            Id = Guid.NewGuid(),
            InjectNumber = 2,
            Title = "Inject 2",
            Description = "Description",
            ScheduledTime = new TimeOnly(10, 0),
            Target = "Target",
            InjectType = InjectType.Standard,
            Status = InjectStatus.Draft,
            Sequence = 2,
            MselId = msel.Id,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Injects.Add(inject2);
        await context.SaveChangesAsync();

        CreateObservation(context, exercise, inject1.Id);
        CreateObservation(context, exercise, inject1.Id);
        CreateObservation(context, exercise, inject2.Id);

        var service = CreateService(context);

        // Act
        var result = await service.GetObservationsByInjectAsync(inject1.Id);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetObservationAsync Tests

    [Fact]
    public async Task GetObservationAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var (context, _, _) = CreateTestContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetObservationAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetObservationAsync_Found_ReturnsDto()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var observation = CreateObservation(context, exercise);
        var service = CreateService(context);

        // Act
        var result = await service.GetObservationAsync(observation.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(observation.Id);
        result.Content.Should().Be(observation.Content);
    }

    [Fact]
    public async Task GetObservationAsync_IncludesInjectNavigation()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var (_, inject) = CreateInject(context, exercise);
        var observation = CreateObservation(context, exercise, inject.Id);
        var service = CreateService(context);

        // Act
        var result = await service.GetObservationAsync(observation.Id);

        // Assert
        result.Should().NotBeNull();
        result!.InjectId.Should().Be(inject.Id);
        result.InjectTitle.Should().Be(inject.Title);
        result.InjectNumber.Should().Be(inject.InjectNumber);
    }

    #endregion

    #region CreateObservationAsync Tests

    [Fact]
    public async Task CreateObservationAsync_ExerciseNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);
        var request = new CreateObservationRequest { Content = "Test content" };
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.CreateObservationAsync(Guid.NewGuid(), request, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task CreateObservationAsync_InjectNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);
        var request = new CreateObservationRequest
        {
            Content = "Test content",
            InjectId = Guid.NewGuid()
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Inject*not found*");
    }

    [Fact]
    public async Task CreateObservationAsync_ObjectiveNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);
        var request = new CreateObservationRequest
        {
            Content = "Test content",
            ObjectiveId = Guid.NewGuid()
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Objective*not found*");
    }

    [Fact]
    public async Task CreateObservationAsync_ValidRequest_CreatesObservation()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);
        var request = new CreateObservationRequest
        {
            Content = "Test observation content",
            Rating = ObservationRating.Satisfactory,
            Recommendation = "Test recommendation",
            Location = "EOC"
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be(request.Content);
        result.Rating.Should().Be(ObservationRating.Satisfactory);
        result.Recommendation.Should().Be(request.Recommendation);
        result.Location.Should().Be(request.Location);
        result.ExerciseId.Should().Be(exercise.Id);
    }

    [Fact]
    public async Task CreateObservationAsync_SetsCreatedBy()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);
        var request = new CreateObservationRequest { Content = "Test content" };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert
        result.CreatedBy.Should().Be(userId);
    }

    [Fact]
    public async Task CreateObservationAsync_WithInject_LinksToInject()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var (_, inject) = CreateInject(context, exercise);
        var service = CreateService(context);
        var request = new CreateObservationRequest
        {
            Content = "Test content",
            InjectId = inject.Id
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert
        result.InjectId.Should().Be(inject.Id);
        result.InjectTitle.Should().Be(inject.Title);
    }

    [Fact]
    public async Task CreateObservationAsync_WithObjective_LinksToObjective()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var objective = CreateObjective(context, exercise);
        var service = CreateService(context);
        var request = new CreateObservationRequest
        {
            Content = "Test content",
            ObjectiveId = objective.Id
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert
        result.ObjectiveId.Should().Be(objective.Id);
    }

    [Fact]
    public async Task CreateObservationAsync_WithoutObservedAt_DefaultsToUtcNow()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);
        var beforeCreate = DateTime.UtcNow;
        var request = new CreateObservationRequest { Content = "Test content" };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert
        result.ObservedAt.Should().BeOnOrAfter(beforeCreate);
        result.ObservedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateObservationAsync_WithObservedAt_UsesProvidedTime()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);
        var specifiedTime = DateTime.UtcNow.AddHours(-2);
        var request = new CreateObservationRequest
        {
            Content = "Test content",
            ObservedAt = specifiedTime
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert
        result.ObservedAt.Should().BeCloseTo(specifiedTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateObservationAsync_BroadcastsObservationAddedEvent()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);
        var request = new CreateObservationRequest { Content = "Test content" };
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert
        _hubContextMock.Verify(
            h => h.NotifyObservationAdded(exercise.Id, It.Is<ObservationDto>(dto =>
                dto.ExerciseId == exercise.Id &&
                dto.Content == request.Content)),
            Times.Once);
    }

    [Fact]
    public async Task CreateObservationAsync_PersistsToDatabase()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);
        var request = new CreateObservationRequest { Content = "Persisted observation" };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert
        var saved = await context.Observations.FindAsync(result.Id);
        saved.Should().NotBeNull();
        saved!.Content.Should().Be("Persisted observation");
    }

    #endregion

    #region UpdateObservationAsync Tests

    [Fact]
    public async Task UpdateObservationAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);
        var request = new UpdateObservationRequest { Content = "Updated content" };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.UpdateObservationAsync(Guid.NewGuid(), request, userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateObservationAsync_InjectNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var observation = CreateObservation(context, exercise);
        var service = CreateService(context);
        var request = new UpdateObservationRequest
        {
            Content = "Updated content",
            InjectId = Guid.NewGuid()
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.UpdateObservationAsync(observation.Id, request, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Inject*not found*");
    }

    [Fact]
    public async Task UpdateObservationAsync_ObjectiveNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var observation = CreateObservation(context, exercise);
        var service = CreateService(context);
        var request = new UpdateObservationRequest
        {
            Content = "Updated content",
            ObjectiveId = Guid.NewGuid()
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.UpdateObservationAsync(observation.Id, request, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Objective*not found*");
    }

    [Fact]
    public async Task UpdateObservationAsync_ValidRequest_UpdatesAllFields()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var observation = CreateObservation(context, exercise);
        var service = CreateService(context);
        var request = new UpdateObservationRequest
        {
            Content = "Updated content",
            Rating = ObservationRating.Marginal,
            Recommendation = "Updated recommendation",
            Location = "Updated location"
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.UpdateObservationAsync(observation.Id, request, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Be("Updated content");
        result.Rating.Should().Be(ObservationRating.Marginal);
        result.Recommendation.Should().Be("Updated recommendation");
        result.Location.Should().Be("Updated location");
    }

    [Fact]
    public async Task UpdateObservationAsync_SetsModifiedBy()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var observation = CreateObservation(context, exercise);
        var service = CreateService(context);
        var request = new UpdateObservationRequest { Content = "Updated content" };
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.UpdateObservationAsync(observation.Id, request, userId);

        // Assert
        var updated = await context.Observations.FindAsync(observation.Id);
        updated!.ModifiedBy.Should().Be(userId);
    }

    [Fact]
    public async Task UpdateObservationAsync_WithObservedAt_UpdatesObservedAt()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var observation = CreateObservation(context, exercise);
        var service = CreateService(context);
        var newTime = DateTime.UtcNow.AddHours(-5);
        var request = new UpdateObservationRequest
        {
            Content = "Updated content",
            ObservedAt = newTime
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.UpdateObservationAsync(observation.Id, request, userId);

        // Assert
        result!.ObservedAt.Should().BeCloseTo(newTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateObservationAsync_CanLinkToInject()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var observation = CreateObservation(context, exercise);
        var (_, inject) = CreateInject(context, exercise);
        var service = CreateService(context);
        var request = new UpdateObservationRequest
        {
            Content = "Updated content",
            InjectId = inject.Id
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.UpdateObservationAsync(observation.Id, request, userId);

        // Assert
        result!.InjectId.Should().Be(inject.Id);
    }

    [Fact]
    public async Task UpdateObservationAsync_BroadcastsObservationUpdatedEvent()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var observation = CreateObservation(context, exercise);
        var service = CreateService(context);
        var request = new UpdateObservationRequest { Content = "Updated content" };
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.UpdateObservationAsync(observation.Id, request, userId);

        // Assert
        _hubContextMock.Verify(
            h => h.NotifyObservationUpdated(exercise.Id, It.Is<ObservationDto>(dto =>
                dto.Id == observation.Id &&
                dto.Content == "Updated content")),
            Times.Once);
    }

    [Fact]
    public async Task UpdateObservationAsync_DoesNotChangeCreatedBy()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var observation = CreateObservation(context, exercise);
        var originalCreatedBy = observation.CreatedBy;
        var service = CreateService(context);
        var request = new UpdateObservationRequest { Content = "Updated content" };
        var differentUserId = Guid.NewGuid().ToString();

        // Act
        await service.UpdateObservationAsync(observation.Id, request, differentUserId);

        // Assert
        var updated = await context.Observations.FindAsync(observation.Id);
        updated!.CreatedBy.Should().Be(originalCreatedBy);
    }

    #endregion

    #region DeleteObservationAsync Tests

    [Fact]
    public async Task DeleteObservationAsync_NotFound_ReturnsFalse()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.DeleteObservationAsync(Guid.NewGuid(), userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteObservationAsync_ValidObservation_ReturnsTrue()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var observation = CreateObservation(context, exercise);
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.DeleteObservationAsync(observation.Id, userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteObservationAsync_PerformsSoftDelete()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var observation = CreateObservation(context, exercise);
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.DeleteObservationAsync(observation.Id, userId);

        // Assert - Use IgnoreQueryFilters to see soft-deleted record
        var deleted = await context.Observations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == observation.Id);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteObservationAsync_SetsDeletedAtAndDeletedBy()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var observation = CreateObservation(context, exercise);
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();
        var beforeDelete = DateTime.UtcNow;

        // Act
        await service.DeleteObservationAsync(observation.Id, userId);

        // Assert
        var deleted = await context.Observations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == observation.Id);
        deleted!.DeletedAt.Should().NotBeNull();
        deleted.DeletedAt!.Value.Should().BeOnOrAfter(beforeDelete);
        deleted.DeletedBy.Should().Be(userId);
    }

    [Fact]
    public async Task DeleteObservationAsync_BroadcastsObservationDeletedEvent()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var observation = CreateObservation(context, exercise);
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.DeleteObservationAsync(observation.Id, userId);

        // Assert
        _hubContextMock.Verify(
            h => h.NotifyObservationDeleted(exercise.Id, observation.Id),
            Times.Once);
    }

    [Fact]
    public async Task DeleteObservationAsync_SoftDeletedNotReturnedInQueries()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var observation = CreateObservation(context, exercise);
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.DeleteObservationAsync(observation.Id, userId);

        // Assert - Normal query should not find soft-deleted observation
        var result = await service.GetObservationAsync(observation.Id);
        result.Should().BeNull();

        var exerciseObservations = await service.GetObservationsByExerciseAsync(exercise.Id);
        exerciseObservations.Should().BeEmpty();
    }

    #endregion

    #region Capability Tagging Tests (S05)

    private Capability CreateCapability(AppDbContext context, Organization org, string name, string? category = null)
    {
        var capability = new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            Name = name,
            Category = category,
            IsActive = true,
            SortOrder = 1
        };
        context.Capabilities.Add(capability);
        context.SaveChanges();

        return capability;
    }

    [Fact]
    public async Task CreateObservationAsync_WithCapabilities_LinksCapabilities()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var capability1 = CreateCapability(context, org, "Mass Care Services");
        var capability2 = CreateCapability(context, org, "Operational Communications");
        var service = CreateService(context);
        var request = new CreateObservationRequest
        {
            Content = "Test observation",
            CapabilityIds = new List<Guid> { capability1.Id, capability2.Id }
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert
        result.Should().NotBeNull();
        result.Capabilities.Should().HaveCount(2);
        result.Capabilities.Should().ContainSingle(c => c.Id == capability1.Id && c.Name == "Mass Care Services");
        result.Capabilities.Should().ContainSingle(c => c.Id == capability2.Id && c.Name == "Operational Communications");
    }

    [Fact]
    public async Task CreateObservationAsync_NullCapabilities_NoLinks()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);
        var request = new CreateObservationRequest
        {
            Content = "Test observation",
            CapabilityIds = null
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert
        result.Should().NotBeNull();
        result.Capabilities.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateObservationAsync_EmptyCapabilities_NoLinks()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);
        var request = new CreateObservationRequest
        {
            Content = "Test observation",
            CapabilityIds = new List<Guid>()
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert
        result.Should().NotBeNull();
        result.Capabilities.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateObservationAsync_ChangesCapabilities()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var capability1 = CreateCapability(context, org, "Planning");
        var capability2 = CreateCapability(context, org, "Public Information");
        var capability3 = CreateCapability(context, org, "Intelligence");

        var observation = CreateObservation(context, exercise);

        // Initially link capability1
        context.ObservationCapabilities.Add(new ObservationCapability
        {
            ObservationId = observation.Id,
            CapabilityId = capability1.Id
        });
        context.SaveChanges();

        var service = CreateService(context);
        var request = new UpdateObservationRequest
        {
            Content = "Updated observation",
            CapabilityIds = new List<Guid> { capability2.Id, capability3.Id }
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.UpdateObservationAsync(observation.Id, request, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Capabilities.Should().HaveCount(2);
        result.Capabilities.Should().ContainSingle(c => c.Id == capability2.Id && c.Name == "Public Information");
        result.Capabilities.Should().ContainSingle(c => c.Id == capability3.Id && c.Name == "Intelligence");
        result.Capabilities.Should().NotContain(c => c.Id == capability1.Id);
    }

    [Fact]
    public async Task UpdateObservationAsync_ClearsCapabilities_WhenEmptyList()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var capability1 = CreateCapability(context, org, "Planning");

        var observation = CreateObservation(context, exercise);

        // Initially link capability
        context.ObservationCapabilities.Add(new ObservationCapability
        {
            ObservationId = observation.Id,
            CapabilityId = capability1.Id
        });
        context.SaveChanges();

        var service = CreateService(context);
        var request = new UpdateObservationRequest
        {
            Content = "Updated observation",
            CapabilityIds = new List<Guid>() // Empty list should clear all capabilities
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.UpdateObservationAsync(observation.Id, request, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Capabilities.Should().BeEmpty();

        // Verify database state
        var dbCapabilities = await context.ObservationCapabilities
            .Where(oc => oc.ObservationId == observation.Id)
            .ToListAsync();
        dbCapabilities.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateObservationAsync_NullCapabilities_DoesNotChangeExisting()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var capability1 = CreateCapability(context, org, "Planning");

        var observation = CreateObservation(context, exercise);

        // Initially link capability
        context.ObservationCapabilities.Add(new ObservationCapability
        {
            ObservationId = observation.Id,
            CapabilityId = capability1.Id
        });
        context.SaveChanges();

        var service = CreateService(context);
        var request = new UpdateObservationRequest
        {
            Content = "Updated observation",
            CapabilityIds = null // Null should not change capabilities
        };
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.UpdateObservationAsync(observation.Id, request, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Capabilities.Should().HaveCount(1);
        result.Capabilities.Should().ContainSingle(c => c.Id == capability1.Id);
    }

    [Fact]
    public async Task GetObservationAsync_IncludesCapabilities()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var capability1 = CreateCapability(context, org, "Mass Care Services", "Response");
        var capability2 = CreateCapability(context, org, "Operational Communications", "Response");

        var observation = CreateObservation(context, exercise);

        // Link capabilities
        context.ObservationCapabilities.AddRange(new[]
        {
            new ObservationCapability { ObservationId = observation.Id, CapabilityId = capability1.Id },
            new ObservationCapability { ObservationId = observation.Id, CapabilityId = capability2.Id }
        });
        context.SaveChanges();

        var service = CreateService(context);

        // Act
        var result = await service.GetObservationAsync(observation.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Capabilities.Should().HaveCount(2);
        result.Capabilities.Should().ContainSingle(c => c.Id == capability1.Id && c.Name == "Mass Care Services" && c.Category == "Response");
        result.Capabilities.Should().ContainSingle(c => c.Id == capability2.Id && c.Name == "Operational Communications" && c.Category == "Response");
    }

    [Fact]
    public async Task GetObservationsByExerciseAsync_IncludesCapabilities()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var capability = CreateCapability(context, org, "Planning");

        var observation1 = CreateObservation(context, exercise);
        var observation2 = CreateObservation(context, exercise);

        // Link capability to observation1 only
        context.ObservationCapabilities.Add(new ObservationCapability
        {
            ObservationId = observation1.Id,
            CapabilityId = capability.Id
        });
        context.SaveChanges();

        var service = CreateService(context);

        // Act
        var result = (await service.GetObservationsByExerciseAsync(exercise.Id)).ToList();

        // Assert
        result.Should().HaveCount(2);

        var resultObs1 = result.First(o => o.Id == observation1.Id);
        resultObs1.Capabilities.Should().HaveCount(1);
        resultObs1.Capabilities.Should().ContainSingle(c => c.Id == capability.Id && c.Name == "Planning");

        var resultObs2 = result.First(o => o.Id == observation2.Id);
        resultObs2.Capabilities.Should().BeEmpty();
    }

    [Fact]
    public async Task GetObservationsByInjectAsync_IncludesCapabilities()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var (_, inject) = CreateInject(context, exercise);
        var capability = CreateCapability(context, org, "Public Health");

        var observation = CreateObservation(context, exercise, inject.Id);

        // Link capability
        context.ObservationCapabilities.Add(new ObservationCapability
        {
            ObservationId = observation.Id,
            CapabilityId = capability.Id
        });
        context.SaveChanges();

        var service = CreateService(context);

        // Act
        var result = (await service.GetObservationsByInjectAsync(inject.Id)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Capabilities.Should().HaveCount(1);
        result[0].Capabilities.Should().ContainSingle(c => c.Id == capability.Id && c.Name == "Public Health");
    }

    #endregion

    #region Observation Notification Tests

    private ExerciseParticipant CreateExerciseParticipant(AppDbContext context, Exercise exercise, ExerciseRole role, string? userId = null)
    {
        var participant = new ExerciseParticipant
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            UserId = userId ?? Guid.NewGuid().ToString(),
            Role = role,
            AssignedAt = DateTime.UtcNow
        };
        context.ExerciseParticipants.Add(participant);
        context.SaveChanges();

        return participant;
    }

    [Fact]
    public async Task CreateObservationAsync_WithExerciseDirectors_SendsNotificationToDirectors()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var director1 = CreateExerciseParticipant(context, exercise, ExerciseRole.ExerciseDirector);
        var director2 = CreateExerciseParticipant(context, exercise, ExerciseRole.ExerciseDirector);
        var controller = CreateExerciseParticipant(context, exercise, ExerciseRole.Controller);

        var service = CreateService(context);
        var request = new CreateObservationRequest { Content = "Test observation" };
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert - Notification should be sent to Exercise Directors only
        _notificationServiceMock.Verify(
            n => n.CreateNotificationsForUsersAsync(
                It.Is<IEnumerable<string>>(users =>
                    users.Count() == 2 &&
                    users.Contains(director1.UserId) &&
                    users.Contains(director2.UserId) &&
                    !users.Contains(controller.UserId)),
                It.Is<CreateNotificationRequest>(req =>
                    req.Type == NotificationType.ObservationCreated &&
                    req.Priority == NotificationPriority.Low &&
                    req.RelatedEntityType == "Observation"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateObservationAsync_NoExerciseDirectors_DoesNotSendNotification()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        // Only add a Controller, no Exercise Directors
        CreateExerciseParticipant(context, exercise, ExerciseRole.Controller);

        var service = CreateService(context);
        var request = new CreateObservationRequest { Content = "Test observation" };
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert - No notification should be sent
        _notificationServiceMock.Verify(
            n => n.CreateNotificationsForUsersAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CreateNotificationRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateObservationAsync_NotificationIncludesCorrectDetails()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var director = CreateExerciseParticipant(context, exercise, ExerciseRole.ExerciseDirector);

        var service = CreateService(context);
        var request = new CreateObservationRequest { Content = "Test observation content" };
        var userId = Guid.NewGuid().ToString();

        CreateNotificationRequest? capturedRequest = null;
        _notificationServiceMock
            .Setup(n => n.CreateNotificationsForUsersAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CreateNotificationRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<string>, CreateNotificationRequest, CancellationToken>((_, req, _) => capturedRequest = req)
            .ReturnsAsync(new List<NotificationDto>());

        // Act
        var result = await service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Type.Should().Be(NotificationType.ObservationCreated);
        capturedRequest.Priority.Should().Be(NotificationPriority.Low);
        capturedRequest.Title.Should().Be("Observation Recorded");
        capturedRequest.Message.Should().Contain("observation");
        capturedRequest.ActionUrl.Should().Contain(exercise.Id.ToString());
        capturedRequest.ActionUrl.Should().Contain("observations");
        capturedRequest.RelatedEntityType.Should().Be("Observation");
        capturedRequest.RelatedEntityId.Should().Be(result.Id);
    }

    [Fact]
    public async Task CreateObservationAsync_ExcludesDeletedExerciseDirectors()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var activeDirector = CreateExerciseParticipant(context, exercise, ExerciseRole.ExerciseDirector);
        var deletedDirector = CreateExerciseParticipant(context, exercise, ExerciseRole.ExerciseDirector);

        // Soft delete one director
        deletedDirector.IsDeleted = true;
        deletedDirector.DeletedAt = DateTime.UtcNow;
        context.SaveChanges();

        var service = CreateService(context);
        var request = new CreateObservationRequest { Content = "Test observation" };
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.CreateObservationAsync(exercise.Id, request, userId);

        // Assert - Only active director should receive notification
        _notificationServiceMock.Verify(
            n => n.CreateNotificationsForUsersAsync(
                It.Is<IEnumerable<string>>(users =>
                    users.Count() == 1 &&
                    users.Contains(activeDirector.UserId)),
                It.IsAny<CreateNotificationRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
