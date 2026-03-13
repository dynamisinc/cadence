using Cadence.Core.Data;
using Cadence.Core.Features.Objectives.Models.DTOs;
using Cadence.Core.Features.Objectives.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Objectives;

/// <summary>
/// Unit tests for <see cref="ObjectiveService"/>.
/// Covers CRUD operations, auto-numbering, duplicate detection, and archived-exercise guard.
/// </summary>
public class ObjectiveServiceTests
{
    private readonly Mock<ILogger<ObjectiveService>> _loggerMock = new();
    private const string TestUser = "test-user-id";

    // =========================================================================
    // Test Setup Helpers
    // =========================================================================

    private (AppDbContext context, Organization org, Exercise exercise) CreateTestContext(
        ExerciseStatus status = ExerciseStatus.Draft)
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = TestUser,
            ModifiedBy = TestUser
        };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Hurricane Response TTX",
            ExerciseType = ExerciseType.TTX,
            Status = status,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = TestUser,
            ModifiedBy = TestUser
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();

        return (context, org, exercise);
    }

    private Objective CreateObjective(
        AppDbContext context,
        Guid exerciseId,
        Guid organizationId,
        string objectiveNumber,
        string name = "Test Objective")
    {
        var objective = new Objective
        {
            Id = Guid.NewGuid(),
            ObjectiveNumber = objectiveNumber,
            Name = name,
            ExerciseId = exerciseId,
            OrganizationId = organizationId,
            CreatedBy = TestUser,
            ModifiedBy = TestUser
        };
        context.Objectives.Add(objective);
        context.SaveChanges();
        return objective;
    }

    private ObjectiveService CreateService(Cadence.Core.Data.AppDbContext context) =>
        new(context, _loggerMock.Object);

    // =========================================================================
    // GetObjectivesByExerciseAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetObjectivesByExerciseAsync_NoObjectives_ReturnsEmptyCollection()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetObjectivesByExerciseAsync(exercise.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetObjectivesByExerciseAsync_WithObjectives_ReturnsAllForExercise()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        CreateObjective(context, exercise.Id, org.Id, "1", "Objective One");
        CreateObjective(context, exercise.Id, org.Id, "2", "Objective Two");
        CreateObjective(context, exercise.Id, org.Id, "3", "Objective Three");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetObjectivesByExerciseAsync(exercise.Id);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetObjectivesByExerciseAsync_ReturnsOnlyObjectivesForSpecifiedExercise()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();

        // Second exercise in the same org
        var otherExercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Other Exercise",
            ExerciseType = ExerciseType.FE,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = TestUser,
            ModifiedBy = TestUser
        };
        context.Exercises.Add(otherExercise);
        context.SaveChanges();

        CreateObjective(context, exercise.Id, org.Id, "1", "Belongs to target exercise");
        CreateObjective(context, otherExercise.Id, org.Id, "1", "Belongs to other exercise");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetObjectivesByExerciseAsync(exercise.Id);

        // Assert
        result.Should().HaveCount(1);
        result.Single().Name.Should().Be("Belongs to target exercise");
    }

    [Fact]
    public async Task GetObjectivesByExerciseAsync_ReturnsObjectivesSortedByObjectiveNumber()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        CreateObjective(context, exercise.Id, org.Id, "3", "Third");
        CreateObjective(context, exercise.Id, org.Id, "1", "First");
        CreateObjective(context, exercise.Id, org.Id, "2", "Second");
        var sut = CreateService(context);

        // Act
        var result = (await sut.GetObjectivesByExerciseAsync(exercise.Id)).ToList();

        // Assert
        result[0].ObjectiveNumber.Should().Be("1");
        result[1].ObjectiveNumber.Should().Be("2");
        result[2].ObjectiveNumber.Should().Be("3");
    }

    // =========================================================================
    // GetObjectiveSummariesAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetObjectiveSummariesAsync_NoObjectives_ReturnsEmptyCollection()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetObjectiveSummariesAsync(exercise.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetObjectiveSummariesAsync_WithObjectives_ReturnsSummaryDtos()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        CreateObjective(context, exercise.Id, org.Id, "1", "Objective Alpha");
        CreateObjective(context, exercise.Id, org.Id, "2", "Objective Beta");
        var sut = CreateService(context);

        // Act
        var result = (await sut.GetObjectiveSummariesAsync(exercise.Id)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().BeOfType<ObjectiveSummaryDto>();
        result[0].Name.Should().Be("Objective Alpha");
    }

    // =========================================================================
    // GetObjectiveAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetObjectiveAsync_ObjectiveExists_ReturnsDto()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var objective = CreateObjective(context, exercise.Id, org.Id, "1", "EOC Activation");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetObjectiveAsync(exercise.Id, objective.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(objective.Id);
        result.Name.Should().Be("EOC Activation");
        result.ObjectiveNumber.Should().Be("1");
        result.ExerciseId.Should().Be(exercise.Id);
    }

    [Fact]
    public async Task GetObjectiveAsync_ObjectiveNotFound_ReturnsNull()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetObjectiveAsync(exercise.Id, Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetObjectiveAsync_ObjectiveBelongsToDifferentExercise_ReturnsNull()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var otherExercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Other Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = TestUser,
            ModifiedBy = TestUser
        };
        context.Exercises.Add(otherExercise);
        context.SaveChanges();

        var objective = CreateObjective(context, otherExercise.Id, org.Id, "1", "Other Objective");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetObjectiveAsync(exercise.Id, objective.Id);

        // Assert
        result.Should().BeNull();
    }

    // =========================================================================
    // CreateObjectiveAsync Tests
    // =========================================================================

    [Fact]
    public async Task CreateObjectiveAsync_ValidRequest_ReturnsCreatedObjective()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var request = new CreateObjectiveRequest
        {
            Name = "EOC Activation & Coordination",
            Description = "Evaluate EOC staffing and coordination protocols"
        };
        var sut = CreateService(context);

        // Act
        var result = await sut.CreateObjectiveAsync(exercise.Id, request, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("EOC Activation & Coordination");
        result.Description.Should().Be("Evaluate EOC staffing and coordination protocols");
        result.ExerciseId.Should().Be(exercise.Id);
    }

    [Fact]
    public async Task CreateObjectiveAsync_NoObjectiveNumber_AutoAssigns_One()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var request = new CreateObjectiveRequest { Name = "First Objective" };
        var sut = CreateService(context);

        // Act
        var result = await sut.CreateObjectiveAsync(exercise.Id, request, TestUser);

        // Assert
        result.ObjectiveNumber.Should().Be("1");
    }

    [Fact]
    public async Task CreateObjectiveAsync_TwoObjectivesWithoutNumbers_AutoAssignsSequentially()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var first = await sut.CreateObjectiveAsync(exercise.Id, new CreateObjectiveRequest { Name = "First" }, TestUser);
        var second = await sut.CreateObjectiveAsync(exercise.Id, new CreateObjectiveRequest { Name = "Second" }, TestUser);

        // Assert
        first.ObjectiveNumber.Should().Be("1");
        second.ObjectiveNumber.Should().Be("2");
    }

    [Fact]
    public async Task CreateObjectiveAsync_WithExistingNumericObjectives_AutoAssignsNextHighest()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        CreateObjective(context, exercise.Id, org.Id, "3", "Existing");
        CreateObjective(context, exercise.Id, org.Id, "1", "Another Existing");
        var sut = CreateService(context);

        // Act
        var result = await sut.CreateObjectiveAsync(exercise.Id, new CreateObjectiveRequest { Name = "New" }, TestUser);

        // Assert
        result.ObjectiveNumber.Should().Be("4");
    }

    [Fact]
    public async Task CreateObjectiveAsync_WithExplicitObjectiveNumber_UsesProvidedNumber()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var request = new CreateObjectiveRequest { Name = "Alpha Objective", ObjectiveNumber = "A1" };
        var sut = CreateService(context);

        // Act
        var result = await sut.CreateObjectiveAsync(exercise.Id, request, TestUser);

        // Assert
        result.ObjectiveNumber.Should().Be("A1");
    }

    [Fact]
    public async Task CreateObjectiveAsync_DuplicateObjectiveNumber_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        CreateObjective(context, exercise.Id, org.Id, "1", "Existing Objective");
        var request = new CreateObjectiveRequest { Name = "Duplicate Number", ObjectiveNumber = "1" };
        var sut = CreateService(context);

        // Act
        var act = () => sut.CreateObjectiveAsync(exercise.Id, request, TestUser);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateObjectiveAsync_ExerciseNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var request = new CreateObjectiveRequest { Name = "EOC Activation" };
        var sut = CreateService(context);

        // Act
        var act = () => sut.CreateObjectiveAsync(Guid.NewGuid(), request, TestUser);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task CreateObjectiveAsync_ArchivedExercise_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(ExerciseStatus.Archived);
        var request = new CreateObjectiveRequest { Name = "Cannot Add" };
        var sut = CreateService(context);

        // Act
        var act = () => sut.CreateObjectiveAsync(exercise.Id, request, TestUser);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Archived exercises cannot be modified*");
    }

    [Fact]
    public async Task CreateObjectiveAsync_SetsOrganizationIdFromExercise()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var request = new CreateObjectiveRequest { Name = "EOC Activation" };
        var sut = CreateService(context);

        // Act
        await sut.CreateObjectiveAsync(exercise.Id, request, TestUser);

        // Assert
        var persisted = context.Objectives.Single();
        persisted.OrganizationId.Should().Be(org.Id);
    }

    [Fact]
    public async Task CreateObjectiveAsync_PersistsToDatabase()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var request = new CreateObjectiveRequest { Name = "Mass Casualty Response" };
        var sut = CreateService(context);

        // Act
        var result = await sut.CreateObjectiveAsync(exercise.Id, request, TestUser);

        // Assert
        var persisted = await context.Objectives.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("Mass Casualty Response");
    }

    // =========================================================================
    // UpdateObjectiveAsync Tests
    // =========================================================================

    [Fact]
    public async Task UpdateObjectiveAsync_ValidRequest_ReturnsUpdatedDto()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var objective = CreateObjective(context, exercise.Id, org.Id, "1", "Original Name");
        var request = new UpdateObjectiveRequest
        {
            ObjectiveNumber = "1",
            Name = "Updated Name",
            Description = "Updated Description"
        };
        var sut = CreateService(context);

        // Act
        var result = await sut.UpdateObjectiveAsync(exercise.Id, objective.Id, request, TestUser);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task UpdateObjectiveAsync_ObjectiveNotFound_ReturnsNull()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var request = new UpdateObjectiveRequest { ObjectiveNumber = "1", Name = "Updated" };
        var sut = CreateService(context);

        // Act
        var result = await sut.UpdateObjectiveAsync(exercise.Id, Guid.NewGuid(), request, TestUser);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateObjectiveAsync_ArchivedExercise_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext(ExerciseStatus.Archived);
        var objective = CreateObjective(context, exercise.Id, org.Id, "1", "Objective");
        var request = new UpdateObjectiveRequest { ObjectiveNumber = "1", Name = "New Name" };
        var sut = CreateService(context);

        // Act
        var act = () => sut.UpdateObjectiveAsync(exercise.Id, objective.Id, request, TestUser);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Archived exercises cannot be modified*");
    }

    [Fact]
    public async Task UpdateObjectiveAsync_DuplicateObjectiveNumber_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        CreateObjective(context, exercise.Id, org.Id, "1", "First Objective");
        var second = CreateObjective(context, exercise.Id, org.Id, "2", "Second Objective");

        // Attempt to rename second to "1" which is taken
        var request = new UpdateObjectiveRequest { ObjectiveNumber = "1", Name = "Renamed" };
        var sut = CreateService(context);

        // Act
        var act = () => sut.UpdateObjectiveAsync(exercise.Id, second.Id, request, TestUser);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateObjectiveAsync_SameObjectiveNumber_DoesNotThrowDuplicateError()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var objective = CreateObjective(context, exercise.Id, org.Id, "1", "Original");
        var request = new UpdateObjectiveRequest { ObjectiveNumber = "1", Name = "Updated Name" };
        var sut = CreateService(context);

        // Act
        var result = await sut.UpdateObjectiveAsync(exercise.Id, objective.Id, request, TestUser);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.ObjectiveNumber.Should().Be("1");
    }

    [Fact]
    public async Task UpdateObjectiveAsync_PersistsChangesToDatabase()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var objective = CreateObjective(context, exercise.Id, org.Id, "1", "Original");
        var request = new UpdateObjectiveRequest { ObjectiveNumber = "2", Name = "Persisted Update" };
        var sut = CreateService(context);

        // Act
        await sut.UpdateObjectiveAsync(exercise.Id, objective.Id, request, TestUser);

        // Assert
        var persisted = await context.Objectives.FindAsync(objective.Id);
        persisted!.Name.Should().Be("Persisted Update");
        persisted.ObjectiveNumber.Should().Be("2");
    }

    // =========================================================================
    // DeleteObjectiveAsync Tests
    // =========================================================================

    [Fact]
    public async Task DeleteObjectiveAsync_ObjectiveExists_ReturnsTrue()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var objective = CreateObjective(context, exercise.Id, org.Id, "1", "Delete Me");
        var sut = CreateService(context);

        // Act
        var result = await sut.DeleteObjectiveAsync(exercise.Id, objective.Id, TestUser);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteObjectiveAsync_ObjectiveNotFound_ReturnsFalse()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var result = await sut.DeleteObjectiveAsync(exercise.Id, Guid.NewGuid(), TestUser);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteObjectiveAsync_ArchivedExercise_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext(ExerciseStatus.Archived);
        var objective = CreateObjective(context, exercise.Id, org.Id, "1", "Cannot Delete");
        var sut = CreateService(context);

        // Act
        var act = () => sut.DeleteObjectiveAsync(exercise.Id, objective.Id, TestUser);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Archived exercises cannot be modified*");
    }

    [Fact]
    public async Task DeleteObjectiveAsync_SoftDeletesRecord()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var objective = CreateObjective(context, exercise.Id, org.Id, "1", "Soft Delete Test");
        var sut = CreateService(context);

        // Act
        await sut.DeleteObjectiveAsync(exercise.Id, objective.Id, TestUser);

        // Assert — global soft-delete query filter hides it, so query with IgnoreQueryFilters
        var persisted = context.Objectives
            .IgnoreQueryFilters()
            .Single(o => o.Id == objective.Id);
        persisted.IsDeleted.Should().BeTrue();
        persisted.DeletedAt.Should().NotBeNull();
        persisted.DeletedBy.Should().Be(TestUser);
    }

    [Fact]
    public async Task DeleteObjectiveAsync_SoftDeletedObjectiveIsHiddenFromSubsequentQueries()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var objective = CreateObjective(context, exercise.Id, org.Id, "1", "Will Be Hidden");
        var sut = CreateService(context);

        // Act
        await sut.DeleteObjectiveAsync(exercise.Id, objective.Id, TestUser);
        var result = await sut.GetObjectiveAsync(exercise.Id, objective.Id);

        // Assert
        result.Should().BeNull();
    }

    // =========================================================================
    // IsObjectiveNumberUniqueAsync Tests
    // =========================================================================

    [Fact]
    public async Task IsObjectiveNumberUniqueAsync_NoDuplicateExists_ReturnsTrue()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var result = await sut.IsObjectiveNumberUniqueAsync(exercise.Id, "99");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsObjectiveNumberUniqueAsync_DuplicateExists_ReturnsFalse()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        CreateObjective(context, exercise.Id, org.Id, "1", "Existing");
        var sut = CreateService(context);

        // Act
        var result = await sut.IsObjectiveNumberUniqueAsync(exercise.Id, "1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsObjectiveNumberUniqueAsync_ExcludeIdMatchesDuplicate_ReturnsTrue()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var objective = CreateObjective(context, exercise.Id, org.Id, "1", "Self");
        var sut = CreateService(context);

        // Act — excluding the objective itself means the number is unique
        var result = await sut.IsObjectiveNumberUniqueAsync(exercise.Id, "1", objective.Id);

        // Assert
        result.Should().BeTrue();
    }
}
