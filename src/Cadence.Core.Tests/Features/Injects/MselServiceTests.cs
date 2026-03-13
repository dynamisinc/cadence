using Cadence.Core.Data;
using Cadence.Core.Features.Msel.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Injects;

/// <summary>
/// Tests for <see cref="MselService"/> — MSEL summary and listing operations.
/// </summary>
public class MselServiceTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private (AppDbContext context, Organization org, Exercise exercise) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();

        return (context, org, exercise);
    }

    private Msel CreateMsel(
        AppDbContext context,
        Organization org,
        Exercise exercise,
        bool isActive = true,
        int version = 1,
        string? name = null)
    {
        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"v{version}.0",
            Description = $"MSEL version {version}",
            Version = version,
            IsActive = isActive,
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Msels.Add(msel);
        context.SaveChanges();
        return msel;
    }

    private Inject CreateInject(
        AppDbContext context,
        Msel msel,
        int injectNumber = 1,
        InjectStatus status = InjectStatus.Draft,
        bool isDeleted = false)
    {
        var inject = new Inject
        {
            Id = Guid.NewGuid(),
            InjectNumber = injectNumber,
            Title = $"Inject {injectNumber}",
            Status = status,
            Sequence = injectNumber,
            MselId = msel.Id,
            TriggerType = TriggerType.Manual,
            InjectType = InjectType.Standard,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTime.UtcNow : null,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Injects.Add(inject);
        context.SaveChanges();
        return inject;
    }

    private Phase CreatePhase(AppDbContext context, Organization org, Exercise exercise)
    {
        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Phase 1",
            Sequence = 1,
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Phases.Add(phase);
        context.SaveChanges();
        return phase;
    }

    private Objective CreateObjective(AppDbContext context, Organization org, Exercise exercise)
    {
        var objective = new Objective
        {
            Id = Guid.NewGuid(),
            ObjectiveNumber = "1",
            Name = "Objective 1",
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Objectives.Add(objective);
        context.SaveChanges();
        return objective;
    }

    private MselService CreateService(AppDbContext context) =>
        new MselService(context);

    // =========================================================================
    // GetActiveMselSummaryAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetActiveMselSummary_ExerciseNotFound_ReturnsNull()
    {
        // Arrange
        var (context, _, _) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetActiveMselSummaryAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveMselSummary_NoActiveMsel_ReturnsNull()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        CreateMsel(context, org, exercise, isActive: false);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetActiveMselSummaryAsync(exercise.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveMselSummary_ActiveMselNoInjects_ReturnsSummaryWithZeroCounts()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var msel = CreateMsel(context, org, exercise, isActive: true);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetActiveMselSummaryAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(msel.Id);
        result.ExerciseId.Should().Be(exercise.Id);
        result.IsActive.Should().BeTrue();
        result.TotalInjects.Should().Be(0);
        result.DraftCount.Should().Be(0);
        result.ReleasedCount.Should().Be(0);
        result.DeferredCount.Should().Be(0);
        result.CompletionPercentage.Should().Be(0);
    }

    [Fact]
    public async Task GetActiveMselSummary_WithMixedStatusInjects_ReturnsCorrectCounts()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var msel = CreateMsel(context, org, exercise, isActive: true);

        CreateInject(context, msel, 1, InjectStatus.Draft);
        CreateInject(context, msel, 2, InjectStatus.Draft);
        CreateInject(context, msel, 3, InjectStatus.Released);
        CreateInject(context, msel, 4, InjectStatus.Deferred);

        var sut = CreateService(context);

        // Act
        var result = await sut.GetActiveMselSummaryAsync(exercise.Id);

        // Assert
        result!.TotalInjects.Should().Be(4);
        result.DraftCount.Should().Be(2);
        result.ReleasedCount.Should().Be(1);
        result.DeferredCount.Should().Be(1);
    }

    [Fact]
    public async Task GetActiveMselSummary_WithReleasedAndDeferred_CalculatesCompletionPercentage()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var msel = CreateMsel(context, org, exercise, isActive: true);

        CreateInject(context, msel, 1, InjectStatus.Draft);
        CreateInject(context, msel, 2, InjectStatus.Released);
        CreateInject(context, msel, 3, InjectStatus.Released);
        CreateInject(context, msel, 4, InjectStatus.Deferred);

        var sut = CreateService(context);

        // Act
        var result = await sut.GetActiveMselSummaryAsync(exercise.Id);

        // Assert
        result!.TotalInjects.Should().Be(4);
        // 3 out of 4 are Released or Deferred = 75%
        result.CompletionPercentage.Should().Be(75);
    }

    [Fact]
    public async Task GetActiveMselSummary_AllInjectsReleased_Returns100Percent()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var msel = CreateMsel(context, org, exercise, isActive: true);

        CreateInject(context, msel, 1, InjectStatus.Released);
        CreateInject(context, msel, 2, InjectStatus.Released);

        var sut = CreateService(context);

        // Act
        var result = await sut.GetActiveMselSummaryAsync(exercise.Id);

        // Assert
        result!.CompletionPercentage.Should().Be(100);
    }

    [Fact]
    public async Task GetActiveMselSummary_ExcludesDeletedInjects()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var msel = CreateMsel(context, org, exercise, isActive: true);

        CreateInject(context, msel, 1, InjectStatus.Draft);
        CreateInject(context, msel, 2, InjectStatus.Draft, isDeleted: true);

        var sut = CreateService(context);

        // Act
        var result = await sut.GetActiveMselSummaryAsync(exercise.Id);

        // Assert
        result!.TotalInjects.Should().Be(1, "deleted injects should be excluded");
        result.DraftCount.Should().Be(1);
    }

    [Fact]
    public async Task GetActiveMselSummary_ReturnsPhaseAndObjectiveCount()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        CreateMsel(context, org, exercise, isActive: true);
        CreatePhase(context, org, exercise);
        CreateObjective(context, org, exercise);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetActiveMselSummaryAsync(exercise.Id);

        // Assert
        result!.PhaseCount.Should().Be(1);
        result.ObjectiveCount.Should().Be(1);
    }

    [Fact]
    public async Task GetActiveMselSummary_DeletedPhasesAndObjectives_ExcludedFromCounts()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        CreateMsel(context, org, exercise, isActive: true);

        var deletedPhase = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Deleted Phase",
            Sequence = 1,
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Phases.Add(deletedPhase);

        var deletedObjective = new Objective
        {
            Id = Guid.NewGuid(),
            ObjectiveNumber = "1",
            Name = "Deleted Objective",
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Objectives.Add(deletedObjective);
        context.SaveChanges();

        var sut = CreateService(context);

        // Act
        var result = await sut.GetActiveMselSummaryAsync(exercise.Id);

        // Assert
        result!.PhaseCount.Should().Be(0, "deleted phases should be excluded");
        result.ObjectiveCount.Should().Be(0, "deleted objectives should be excluded");
    }

    [Fact]
    public async Task GetActiveMselSummary_ReturnsCorrectMselMetadata()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var msel = CreateMsel(context, org, exercise, isActive: true, version: 3, name: "Hurricane v3.0");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetActiveMselSummaryAsync(exercise.Id);

        // Assert
        result!.Name.Should().Be("Hurricane v3.0");
        result.Version.Should().Be(3);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveMselSummary_LastModifiedAt_ReflectsNewestInject()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var msel = CreateMsel(context, org, exercise, isActive: true);

        var inject1 = CreateInject(context, msel, 1, InjectStatus.Draft);
        var inject2 = CreateInject(context, msel, 2, InjectStatus.Draft);

        // Manually set UpdatedAt so we can predict the expected value
        inject1.UpdatedAt = DateTime.UtcNow.AddMinutes(-10);
        inject2.UpdatedAt = DateTime.UtcNow;
        context.SaveChanges();

        var sut = CreateService(context);

        // Act
        var result = await sut.GetActiveMselSummaryAsync(exercise.Id);

        // Assert
        result!.LastModifiedAt.Should().NotBeNull();
        result.LastModifiedAt.Should().BeCloseTo(inject2.UpdatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetActiveMselSummary_NoInjects_LastModifiedAtIsNull()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        CreateMsel(context, org, exercise, isActive: true);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetActiveMselSummaryAsync(exercise.Id);

        // Assert
        result!.LastModifiedAt.Should().BeNull();
    }

    // =========================================================================
    // GetMselsForExerciseAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetMselsForExercise_NoMsels_ReturnsEmptyList()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetMselsForExerciseAsync(exercise.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMselsForExercise_MultipleMsels_ReturnsAllOrderedByVersionDescending()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        CreateMsel(context, org, exercise, isActive: false, version: 1);
        CreateMsel(context, org, exercise, isActive: false, version: 2);
        CreateMsel(context, org, exercise, isActive: true, version: 3);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetMselsForExerciseAsync(exercise.Id);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInDescendingOrder(m => m.Version);
    }

    [Fact]
    public async Task GetMselsForExercise_OnlyReturnsNotDeletedMsels()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        CreateMsel(context, org, exercise, isActive: true, version: 1);

        var deletedMsel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Deleted MSEL",
            Version = 2,
            IsActive = false,
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Msels.Add(deletedMsel);
        context.SaveChanges();

        var sut = CreateService(context);

        // Act
        var result = await sut.GetMselsForExerciseAsync(exercise.Id);

        // Assert
        result.Should().HaveCount(1, "deleted MSELs should be excluded");
        result[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetMselsForExercise_ReturnsInjectCountPerMsel()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var msel1 = CreateMsel(context, org, exercise, isActive: false, version: 1);
        var msel2 = CreateMsel(context, org, exercise, isActive: true, version: 2);

        CreateInject(context, msel1, 1);
        CreateInject(context, msel1, 2);
        CreateInject(context, msel2, 1);

        var sut = CreateService(context);

        // Act
        var result = await sut.GetMselsForExerciseAsync(exercise.Id);

        // Assert
        var msel1Dto = result.Single(m => m.Version == 1);
        var msel2Dto = result.Single(m => m.Version == 2);
        msel1Dto.InjectCount.Should().Be(2);
        msel2Dto.InjectCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMselsForExercise_ExcludesDeletedInjectsFromCount()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var msel = CreateMsel(context, org, exercise, isActive: true, version: 1);
        CreateInject(context, msel, 1, isDeleted: false);
        CreateInject(context, msel, 2, isDeleted: true);

        var sut = CreateService(context);

        // Act
        var result = await sut.GetMselsForExerciseAsync(exercise.Id);

        // Assert
        result.Single().InjectCount.Should().Be(1, "deleted injects should not be counted");
    }

    [Fact]
    public async Task GetMselsForExercise_DoesNotReturnMselsFromOtherExercises()
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
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Exercises.Add(otherExercise);
        context.SaveChanges();

        CreateMsel(context, org, exercise, isActive: true, version: 1);
        CreateMsel(context, org, otherExercise, isActive: true, version: 1, name: "Other MSEL");

        var sut = CreateService(context);

        // Act
        var result = await sut.GetMselsForExerciseAsync(exercise.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].ExerciseId.Should().Be(exercise.Id);
    }

    // =========================================================================
    // GetMselSummaryAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetMselSummary_MselNotFound_ReturnsNull()
    {
        // Arrange
        var (context, _, _) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetMselSummaryAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMselSummary_DeletedMsel_ReturnsNull()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();

        var deletedMsel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Deleted MSEL",
            Version = 1,
            IsActive = false,
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Msels.Add(deletedMsel);
        context.SaveChanges();

        var sut = CreateService(context);

        // Act
        var result = await sut.GetMselSummaryAsync(deletedMsel.Id);

        // Assert
        result.Should().BeNull("a soft-deleted MSEL should not be returned");
    }

    [Fact]
    public async Task GetMselSummary_ValidMsel_ReturnsCorrectMetadata()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var msel = CreateMsel(context, org, exercise, isActive: true, version: 2, name: "Storm Response v2.0");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetMselSummaryAsync(msel.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(msel.Id);
        result.Name.Should().Be("Storm Response v2.0");
        result.Version.Should().Be(2);
        result.IsActive.Should().BeTrue();
        result.ExerciseId.Should().Be(exercise.Id);
    }

    [Fact]
    public async Task GetMselSummary_WithInjects_ReturnsCorrectStatusCounts()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var msel = CreateMsel(context, org, exercise, isActive: true);

        CreateInject(context, msel, 1, InjectStatus.Draft);
        CreateInject(context, msel, 2, InjectStatus.Draft);
        CreateInject(context, msel, 3, InjectStatus.Released);
        CreateInject(context, msel, 4, InjectStatus.Deferred);

        var sut = CreateService(context);

        // Act
        var result = await sut.GetMselSummaryAsync(msel.Id);

        // Assert
        result!.TotalInjects.Should().Be(4);
        result.DraftCount.Should().Be(2);
        result.ReleasedCount.Should().Be(1);
        result.DeferredCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMselSummary_WithReleasedInjects_CalculatesCompletionPercentage()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var msel = CreateMsel(context, org, exercise, isActive: true);

        CreateInject(context, msel, 1, InjectStatus.Released);
        CreateInject(context, msel, 2, InjectStatus.Released);
        CreateInject(context, msel, 3, InjectStatus.Draft);
        CreateInject(context, msel, 4, InjectStatus.Draft);

        var sut = CreateService(context);

        // Act
        var result = await sut.GetMselSummaryAsync(msel.Id);

        // Assert
        result!.CompletionPercentage.Should().Be(50);
    }

    [Fact]
    public async Task GetMselSummary_ExcludesDeletedInjectsFromCounts()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var msel = CreateMsel(context, org, exercise, isActive: true);

        CreateInject(context, msel, 1, InjectStatus.Released);
        CreateInject(context, msel, 2, InjectStatus.Released, isDeleted: true);

        var sut = CreateService(context);

        // Act
        var result = await sut.GetMselSummaryAsync(msel.Id);

        // Assert
        result!.TotalInjects.Should().Be(1, "deleted injects should not be counted");
        result.ReleasedCount.Should().Be(1);
        result.CompletionPercentage.Should().Be(100);
    }

    [Fact]
    public async Task GetMselSummary_IncludesPhaseAndObjectiveCountFromExercise()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var msel = CreateMsel(context, org, exercise, isActive: true);
        CreatePhase(context, org, exercise);
        CreatePhase(context, org, exercise);
        CreateObjective(context, org, exercise);

        var sut = CreateService(context);

        // Act
        var result = await sut.GetMselSummaryAsync(msel.Id);

        // Assert
        result!.PhaseCount.Should().Be(2);
        result.ObjectiveCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMselSummary_InactiveMsel_IsReturnedWithIsActiveFalse()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var msel = CreateMsel(context, org, exercise, isActive: false, version: 1);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetMselSummaryAsync(msel.Id);

        // Assert
        result.Should().NotBeNull("GetMselSummary should work for both active and inactive MSELs");
        result!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetMselSummary_LastModifiedByName_IsNull()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var msel = CreateMsel(context, org, exercise, isActive: true);
        CreateInject(context, msel, 1);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetMselSummaryAsync(msel.Id);

        // Assert
        result!.LastModifiedByName.Should().BeNull(
            "LastModifiedByName is intentionally not populated due to type mismatch between BaseEntity.ModifiedBy and ApplicationUser.Id");
    }

    [Fact]
    public async Task GetActiveMselSummaryAsync_NoMsel_ReturnsNull()
    {
        var (context, org, exercise) = CreateTestContext();
        // No MSEL created for this exercise
        var sut = CreateService(context);

        var result = await sut.GetActiveMselSummaryAsync(exercise.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveMselSummaryAsync_MultipleMsels_ReturnsActive()
    {
        var (context, org, exercise) = CreateTestContext();
        var activeMsel = CreateMsel(context, org, exercise, isActive: true, version: 2, name: "Active MSEL");
        CreateMsel(context, org, exercise, isActive: false, version: 1, name: "Archived MSEL");
        var sut = CreateService(context);

        var result = await sut.GetActiveMselSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(activeMsel.Id);
        result.Name.Should().Be("Active MSEL");
        result.IsActive.Should().BeTrue();
    }
}
