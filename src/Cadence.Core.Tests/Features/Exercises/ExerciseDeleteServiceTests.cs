using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Exercises;

public class ExerciseDeleteServiceTests
{
    private readonly Mock<ILogger<ExerciseDeleteService>> _loggerMock;

    public ExerciseDeleteServiceTests()
    {
        _loggerMock = new Mock<ILogger<ExerciseDeleteService>>();
    }

    private (AppDbContext context, Organization org) CreateTestContext()
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
        context.SaveChanges();

        return (context, org);
    }

    private Exercise CreateExercise(
        AppDbContext context,
        Organization org,
        ExerciseStatus status = ExerciseStatus.Draft,
        bool hasBeenPublished = false,
        string? createdBy = null)
    {
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = status,
            HasBeenPublished = hasBeenPublished,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = createdBy ?? Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();

        return exercise;
    }

    private ExerciseDeleteService CreateService(AppDbContext context)
    {
        return new ExerciseDeleteService(context, _loggerMock.Object);
    }

    #region GetDeleteSummaryAsync Tests

    [Fact]
    public async Task GetDeleteSummaryAsync_ExerciseNotFound_ReturnsNull()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.GetDeleteSummaryAsync(Guid.NewGuid(), userId, isAdmin: false);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDeleteSummaryAsync_DraftExerciseNeverPublished_CreatorCanDelete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var creatorId = Guid.NewGuid().ToString();
        var exercise = CreateExercise(context, org, ExerciseStatus.Draft, hasBeenPublished: false, createdBy: creatorId);
        var service = CreateService(context);

        // Act
        var result = await service.GetDeleteSummaryAsync(exercise.Id, creatorId, isAdmin: false);

        // Assert
        result.Should().NotBeNull();
        result!.CanDelete.Should().BeTrue();
        result.DeleteReason.Should().Be(DeleteEligibilityReason.NeverPublished);
        result.CannotDeleteReason.Should().BeNull();
    }

    [Fact]
    public async Task GetDeleteSummaryAsync_DraftExerciseNeverPublished_AdminCanDelete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Draft, hasBeenPublished: false);
        var service = CreateService(context);
        var adminId = Guid.NewGuid().ToString();

        // Act
        var result = await service.GetDeleteSummaryAsync(exercise.Id, adminId, isAdmin: true);

        // Assert
        result.Should().NotBeNull();
        result!.CanDelete.Should().BeTrue();
        result.DeleteReason.Should().Be(DeleteEligibilityReason.NeverPublished);
    }

    [Fact]
    public async Task GetDeleteSummaryAsync_DraftExerciseNeverPublished_NonCreatorCannotDelete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Draft, hasBeenPublished: false);
        var service = CreateService(context);
        var otherUserId = Guid.NewGuid().ToString();

        // Act
        var result = await service.GetDeleteSummaryAsync(exercise.Id, otherUserId.ToString(), isAdmin: false);

        // Assert
        result.Should().NotBeNull();
        result!.CanDelete.Should().BeFalse();
        result.CannotDeleteReason.Should().Be(CannotDeleteReason.NotAuthorized);
    }

    [Fact]
    public async Task GetDeleteSummaryAsync_ArchivedExercise_AdminCanDelete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Archived, hasBeenPublished: true);
        var service = CreateService(context);
        var adminId = Guid.NewGuid().ToString();

        // Act
        var result = await service.GetDeleteSummaryAsync(exercise.Id, adminId, isAdmin: true);

        // Assert
        result.Should().NotBeNull();
        result!.CanDelete.Should().BeTrue();
        result.DeleteReason.Should().Be(DeleteEligibilityReason.Archived);
    }

    [Fact]
    public async Task GetDeleteSummaryAsync_ArchivedExercise_NonAdminCannotDelete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Archived, hasBeenPublished: true);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        // Act
        var result = await service.GetDeleteSummaryAsync(exercise.Id, userId.ToString(), isAdmin: false);

        // Assert
        result.Should().NotBeNull();
        result!.CanDelete.Should().BeFalse();
        result.CannotDeleteReason.Should().Be(CannotDeleteReason.NotAuthorized);
    }

    [Fact]
    public async Task GetDeleteSummaryAsync_ActiveExercise_CannotDelete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Active, hasBeenPublished: true);
        var service = CreateService(context);
        var adminId = Guid.NewGuid().ToString();

        // Act
        var result = await service.GetDeleteSummaryAsync(exercise.Id, adminId, isAdmin: true);

        // Assert
        result.Should().NotBeNull();
        result!.CanDelete.Should().BeFalse();
        result.CannotDeleteReason.Should().Be(CannotDeleteReason.MustArchiveFirst);
    }

    [Fact]
    public async Task GetDeleteSummaryAsync_CompletedExercise_CannotDelete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Completed, hasBeenPublished: true);
        var service = CreateService(context);
        var adminId = Guid.NewGuid().ToString();

        // Act
        var result = await service.GetDeleteSummaryAsync(exercise.Id, adminId, isAdmin: true);

        // Assert
        result.Should().NotBeNull();
        result!.CanDelete.Should().BeFalse();
        result.CannotDeleteReason.Should().Be(CannotDeleteReason.MustArchiveFirst);
    }

    [Fact]
    public async Task GetDeleteSummaryAsync_ReturnsCorrectDataCounts()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var creatorId = Guid.NewGuid().ToString();
        var exercise = CreateExercise(context, org, ExerciseStatus.Draft, hasBeenPublished: false, createdBy: creatorId);

        // Add related data
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
            Description = "Description",
            ScheduledTime = new TimeOnly(9, 0),
            Target = "Target",
            InjectType = InjectType.Standard,
            Status = InjectStatus.Pending,
            Sequence = 1,
            MselId = msel.Id,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Injects.Add(inject);

        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Name = "Phase 1",
            Sequence = 1,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Phases.Add(phase);

        var observation = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Test observation",
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Observations.Add(observation);

        context.SaveChanges();

        var service = CreateService(context);

        // Act
        var result = await service.GetDeleteSummaryAsync(exercise.Id, creatorId, isAdmin: false);

        // Assert
        result.Should().NotBeNull();
        result!.Summary.InjectCount.Should().Be(1);
        result.Summary.PhaseCount.Should().Be(1);
        result.Summary.ObservationCount.Should().Be(1);
        result.Summary.MselCount.Should().Be(1);
    }

    #endregion

    #region DeleteExerciseAsync Tests

    [Fact]
    public async Task DeleteExerciseAsync_ExerciseNotFound_ReturnsFailed()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        // Act
        var result = await service.DeleteExerciseAsync(Guid.NewGuid(), userId.ToString(), isAdmin: false);

        // Assert
        result.Success.Should().BeFalse();
        result.CannotDeleteReason.Should().Be(CannotDeleteReason.NotFound);
    }

    // NOTE: Tests that perform actual deletion are skipped because ExecuteUpdateAsync/ExecuteDeleteAsync
    // (bulk operations) are not supported by the EF Core in-memory provider.
    // The GetDeleteSummaryAsync tests verify eligibility logic without needing the actual delete.
    // DeleteExerciseAsync is tested via integration tests with a real database.

    [Fact(Skip = "ExecuteUpdateAsync/ExecuteDeleteAsync not supported by in-memory provider - requires integration test")]
    public async Task DeleteExerciseAsync_DraftExerciseNeverPublished_CreatorCanDelete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var creatorId = Guid.NewGuid().ToString();
        var exercise = CreateExercise(context, org, ExerciseStatus.Draft, hasBeenPublished: false, createdBy: creatorId);
        var service = CreateService(context);

        // Act
        var result = await service.DeleteExerciseAsync(exercise.Id, creatorId, isAdmin: false);

        // Assert
        result.Success.Should().BeTrue();

        // Verify exercise is deleted
        var deleted = await context.Exercises.FirstOrDefaultAsync(e => e.Id == exercise.Id);
        deleted.Should().BeNull();
    }

    [Fact(Skip = "ExecuteUpdateAsync/ExecuteDeleteAsync not supported by in-memory provider - requires integration test")]
    public async Task DeleteExerciseAsync_ArchivedExercise_AdminCanDelete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Archived, hasBeenPublished: true);
        var service = CreateService(context);
        var adminId = Guid.NewGuid().ToString();

        // Act
        var result = await service.DeleteExerciseAsync(exercise.Id, adminId, isAdmin: true);

        // Assert
        result.Success.Should().BeTrue();

        // Verify exercise is deleted
        var deleted = await context.Exercises.FirstOrDefaultAsync(e => e.Id == exercise.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteExerciseAsync_ArchivedExercise_NonAdminCannotDelete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Archived, hasBeenPublished: true);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        // Act
        var result = await service.DeleteExerciseAsync(exercise.Id, userId.ToString(), isAdmin: false);

        // Assert
        result.Success.Should().BeFalse();
        result.CannotDeleteReason.Should().Be(CannotDeleteReason.NotAuthorized);

        // Verify exercise still exists
        var exists = await context.Exercises.AnyAsync(e => e.Id == exercise.Id);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteExerciseAsync_ActiveExercise_CannotDelete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Active, hasBeenPublished: true);
        var service = CreateService(context);
        var adminId = Guid.NewGuid().ToString();

        // Act
        var result = await service.DeleteExerciseAsync(exercise.Id, adminId, isAdmin: true);

        // Assert
        result.Success.Should().BeFalse();
        result.CannotDeleteReason.Should().Be(CannotDeleteReason.MustArchiveFirst);

        // Verify exercise still exists
        var exists = await context.Exercises.AnyAsync(e => e.Id == exercise.Id);
        exists.Should().BeTrue();
    }

    [Fact(Skip = "ExecuteUpdateAsync/ExecuteDeleteAsync not supported by in-memory provider - requires integration test")]
    public async Task DeleteExerciseAsync_CascadeDeletesRelatedData()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var creatorId = Guid.NewGuid().ToString();
        var exercise = CreateExercise(context, org, ExerciseStatus.Draft, hasBeenPublished: false, createdBy: creatorId);

        // Add related data
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

        // Set as active MSEL
        exercise.ActiveMselId = msel.Id;

        var inject = new Inject
        {
            Id = Guid.NewGuid(),
            InjectNumber = 1,
            Title = "Test Inject",
            Description = "Description",
            ScheduledTime = new TimeOnly(9, 0),
            Target = "Target",
            InjectType = InjectType.Standard,
            Status = InjectStatus.Pending,
            Sequence = 1,
            MselId = msel.Id,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Injects.Add(inject);

        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Name = "Phase 1",
            Sequence = 1,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Phases.Add(phase);

        var observation = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Test observation",
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Observations.Add(observation);

        var objective = new Objective
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            ObjectiveNumber = "1",
            Name = "Test Objective",
            Description = "Description",
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Objectives.Add(objective);

        context.SaveChanges();

        var service = CreateService(context);

        // Act
        var result = await service.DeleteExerciseAsync(exercise.Id, creatorId, isAdmin: false);

        // Assert
        result.Success.Should().BeTrue();

        // Verify all related data is deleted
        (await context.Exercises.IgnoreQueryFilters().AnyAsync(e => e.Id == exercise.Id)).Should().BeFalse();
        (await context.Msels.IgnoreQueryFilters().AnyAsync(m => m.Id == msel.Id)).Should().BeFalse();
        (await context.Injects.IgnoreQueryFilters().AnyAsync(i => i.Id == inject.Id)).Should().BeFalse();
        (await context.Phases.IgnoreQueryFilters().AnyAsync(p => p.Id == phase.Id)).Should().BeFalse();
        (await context.Observations.IgnoreQueryFilters().AnyAsync(o => o.Id == observation.Id)).Should().BeFalse();
        (await context.Objectives.IgnoreQueryFilters().AnyAsync(o => o.Id == objective.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteExerciseAsync_PublishedDraftExercise_CannotDelete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var creatorId = Guid.NewGuid().ToString();
        // Draft but was previously published (HasBeenPublished = true)
        var exercise = CreateExercise(context, org, ExerciseStatus.Draft, hasBeenPublished: true, createdBy: creatorId);
        var service = CreateService(context);

        // Act
        var result = await service.DeleteExerciseAsync(exercise.Id, creatorId, isAdmin: false);

        // Assert
        result.Success.Should().BeFalse();
        result.CannotDeleteReason.Should().Be(CannotDeleteReason.MustArchiveFirst);
    }

    #endregion
}
