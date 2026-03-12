using Cadence.Core.Data;
using Cadence.Core.Features.Eeg.Models.DTOs;
using Cadence.Core.Features.Eeg.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cadence.Core.Tests.Features.Eeg;

/// <summary>
/// Tests for CapabilityTargetService — CRUD operations, org isolation,
/// sort-order auto-increment, and reorder logic.
/// </summary>
public class CapabilityTargetServiceTests
{
    // =========================================================================
    // Test Fixture Helpers
    // =========================================================================

    private record TestContext(
        AppDbContext DbContext,
        Organization Org,
        Exercise Exercise,
        Capability Capability,
        string UserId);

    /// <summary>
    /// Builds an isolated in-memory database seeded with one organization,
    /// one exercise, and one capability. Each call uses a unique database name
    /// so tests do not share state.
    /// </summary>
    private static TestContext CreateTestContext()
    {
        var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            Slug = "test-org",
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Hurricane Response TTX",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            DeliveryMode = DeliveryMode.FacilitatorPaced,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.Exercises.Add(exercise);

        var capability = new Capability
        {
            Id = Guid.NewGuid(),
            Name = "Operational Communications",
            Category = "Response",
            OrganizationId = org.Id
        };
        context.Capabilities.Add(capability);

        context.SaveChanges();

        return new TestContext(context, org, exercise, capability, userId);
    }

    /// <summary>
    /// Wires up the service under test with the supplied context and an org-context
    /// mock that reports the given organization id.
    /// </summary>
    private static CapabilityTargetService CreateService(
        AppDbContext context,
        Guid organizationId)
    {
        var orgContextMock = new Mock<ICurrentOrganizationContext>();
        orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(organizationId);
        return new CapabilityTargetService(context, orgContextMock.Object);
    }

    /// <summary>
    /// Adds a CapabilityTarget directly to the database and returns it.
    /// Useful for seeding without going through the service.
    /// </summary>
    private static CapabilityTarget SeedTarget(
        AppDbContext context,
        Guid exerciseId,
        Guid organizationId,
        Guid capabilityId,
        string userId,
        string description = "Activate EOC within 60 minutes",
        int sortOrder = 0)
    {
        var target = new CapabilityTarget
        {
            Id = Guid.NewGuid(),
            ExerciseId = exerciseId,
            OrganizationId = organizationId,
            CapabilityId = capabilityId,
            TargetDescription = description,
            SortOrder = sortOrder,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.CapabilityTargets.Add(target);
        context.SaveChanges();
        return target;
    }

    // =========================================================================
    // GetByExerciseAsync
    // =========================================================================

    [Fact]
    public async Task GetByExerciseAsync_ExerciseBelongsToOrg_ReturnsTargetsOrderedBySortOrder()
    {
        // Arrange
        var tc = CreateTestContext();
        SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Target C", sortOrder: 2);
        SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Target A", sortOrder: 0);
        SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Target B", sortOrder: 1);
        var service = CreateService(tc.DbContext, tc.Org.Id);

        // Act
        var result = await service.GetByExerciseAsync(tc.Exercise.Id);

        // Assert
        result.TotalCount.Should().Be(3);
        result.Items.Select(i => i.TargetDescription)
            .Should().ContainInOrder("Target A", "Target B", "Target C");
    }

    [Fact]
    public async Task GetByExerciseAsync_ExerciseNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var tc = CreateTestContext();
        var service = CreateService(tc.DbContext, tc.Org.Id);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetByExerciseAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByExerciseAsync_ExerciseBelongsToDifferentOrg_ThrowsInvalidOperationException()
    {
        // Arrange
        var tc = CreateTestContext();
        // Service is authenticated as a different org — the exercise is invisible
        var service = CreateService(tc.DbContext, Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetByExerciseAsync(tc.Exercise.Id));
    }

    [Fact]
    public async Task GetByExerciseAsync_NoTargets_ReturnsEmptyListWithZeroCount()
    {
        // Arrange
        var tc = CreateTestContext();
        var service = CreateService(tc.DbContext, tc.Org.Id);

        // Act
        var result = await service.GetByExerciseAsync(tc.Exercise.Id);

        // Assert
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByExerciseAsync_OnlyReturnsTargetsForSpecifiedExercise()
    {
        // Arrange
        var tc = CreateTestContext();

        // Create a second exercise in the same org
        var otherExercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Other Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            DeliveryMode = DeliveryMode.FacilitatorPaced,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = tc.Org.Id,
            CreatedBy = tc.UserId,
            ModifiedBy = tc.UserId
        };
        tc.DbContext.Exercises.Add(otherExercise);
        tc.DbContext.SaveChanges();

        SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Exercise 1 Target");
        SeedTarget(tc.DbContext, otherExercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Exercise 2 Target");

        var service = CreateService(tc.DbContext, tc.Org.Id);

        // Act
        var result = await service.GetByExerciseAsync(tc.Exercise.Id);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Single().TargetDescription.Should().Be("Exercise 1 Target");
    }

    [Fact]
    public async Task GetByExerciseAsync_ExcludesSoftDeletedTargets()
    {
        // Arrange
        var tc = CreateTestContext();
        var service = CreateService(tc.DbContext, tc.Org.Id);

        SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Active Target");
        var deletedTarget = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Deleted Target", sortOrder: 1);
        await service.DeleteAsync(deletedTarget.Id, tc.UserId);

        // Act
        var result = await service.GetByExerciseAsync(tc.Exercise.Id);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Single().TargetDescription.Should().Be("Active Target");
    }

    // =========================================================================
    // GetByIdAsync
    // =========================================================================

    [Fact]
    public async Task GetByIdAsync_ExistingTargetInSameOrg_ReturnsDto()
    {
        // Arrange
        var tc = CreateTestContext();
        var target = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "EOC Activation Target");
        var service = CreateService(tc.DbContext, tc.Org.Id);

        // Act
        var result = await service.GetByIdAsync(target.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(target.Id);
        result.TargetDescription.Should().Be("EOC Activation Target");
        result.ExerciseId.Should().Be(tc.Exercise.Id);
        result.CapabilityId.Should().Be(tc.Capability.Id);
        result.Capability.Id.Should().Be(tc.Capability.Id);
        result.Capability.Name.Should().Be(tc.Capability.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        var tc = CreateTestContext();
        var service = CreateService(tc.DbContext, tc.Org.Id);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_TargetBelongsToDifferentOrg_ReturnsNull()
    {
        // Arrange
        var tc = CreateTestContext();
        var target = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId);
        // Authenticate as a different org
        var service = CreateService(tc.DbContext, Guid.NewGuid());

        // Act
        var result = await service.GetByIdAsync(target.Id);

        // Assert
        result.Should().BeNull("org isolation must prevent cross-org reads");
    }

    [Fact]
    public async Task GetByIdAsync_SoftDeletedTarget_ReturnsNull()
    {
        // Arrange
        var tc = CreateTestContext();
        var target = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId);
        var service = CreateService(tc.DbContext, tc.Org.Id);
        await service.DeleteAsync(target.Id, tc.UserId);

        // Act
        var result = await service.GetByIdAsync(target.Id);

        // Assert
        result.Should().BeNull("soft-deleted targets should not be returned");
    }

    [Fact]
    public async Task GetByIdAsync_PopulatesCriticalTaskCount()
    {
        // Arrange
        var tc = CreateTestContext();
        var target = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId);

        var criticalTask1 = new CriticalTask
        {
            Id = Guid.NewGuid(),
            CapabilityTargetId = target.Id,
            OrganizationId = tc.Org.Id,
            TaskDescription = "Task One",
            SortOrder = 0,
            CreatedBy = tc.UserId,
            ModifiedBy = tc.UserId
        };
        var criticalTask2 = new CriticalTask
        {
            Id = Guid.NewGuid(),
            CapabilityTargetId = target.Id,
            OrganizationId = tc.Org.Id,
            TaskDescription = "Task Two",
            SortOrder = 1,
            CreatedBy = tc.UserId,
            ModifiedBy = tc.UserId
        };
        tc.DbContext.CriticalTasks.AddRange(criticalTask1, criticalTask2);
        tc.DbContext.SaveChanges();

        var service = CreateService(tc.DbContext, tc.Org.Id);

        // Act
        var result = await service.GetByIdAsync(target.Id);

        // Assert
        result.Should().NotBeNull();
        result!.CriticalTaskCount.Should().Be(2);
    }

    // =========================================================================
    // CreateAsync
    // =========================================================================

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedDto()
    {
        // Arrange
        var tc = CreateTestContext();
        var service = CreateService(tc.DbContext, tc.Org.Id);

        var request = new CreateCapabilityTargetRequest
        {
            CapabilityId = tc.Capability.Id,
            TargetDescription = "Establish interoperable communications within 30 minutes",
            Sources = "Metro County EOP, Annex F; SOP 5.2"
        };

        // Act
        var result = await service.CreateAsync(tc.Exercise.Id, request, tc.UserId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.ExerciseId.Should().Be(tc.Exercise.Id);
        result.CapabilityId.Should().Be(tc.Capability.Id);
        result.TargetDescription.Should().Be("Establish interoperable communications within 30 minutes");
        result.Sources.Should().Be("Metro County EOP, Annex F; SOP 5.2");
        result.Capability.Name.Should().Be(tc.Capability.Name);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsToDatabase()
    {
        // Arrange
        var tc = CreateTestContext();
        var service = CreateService(tc.DbContext, tc.Org.Id);

        var request = new CreateCapabilityTargetRequest
        {
            CapabilityId = tc.Capability.Id,
            TargetDescription = "Test persistence"
        };

        // Act
        var result = await service.CreateAsync(tc.Exercise.Id, request, tc.UserId);

        // Assert
        var persisted = await tc.DbContext.CapabilityTargets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ct => ct.Id == result.Id);
        persisted.Should().NotBeNull();
        persisted!.OrganizationId.Should().Be(tc.Org.Id, "OrganizationId must be copied from exercise");
        persisted.CreatedBy.Should().Be(tc.UserId);
        persisted.ModifiedBy.Should().Be(tc.UserId);
    }

    [Fact]
    public async Task CreateAsync_ExerciseNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var tc = CreateTestContext();
        var service = CreateService(tc.DbContext, tc.Org.Id);

        var request = new CreateCapabilityTargetRequest
        {
            CapabilityId = tc.Capability.Id,
            TargetDescription = "Should fail"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(Guid.NewGuid(), request, tc.UserId));
    }

    [Fact]
    public async Task CreateAsync_ExerciseBelongsToDifferentOrg_ThrowsInvalidOperationException()
    {
        // Arrange
        var tc = CreateTestContext();
        // Service sees a different org — exercise is not visible
        var service = CreateService(tc.DbContext, Guid.NewGuid());

        var request = new CreateCapabilityTargetRequest
        {
            CapabilityId = tc.Capability.Id,
            TargetDescription = "Should fail"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(tc.Exercise.Id, request, tc.UserId));
    }

    [Fact]
    public async Task CreateAsync_CapabilityNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var tc = CreateTestContext();
        var service = CreateService(tc.DbContext, tc.Org.Id);

        var request = new CreateCapabilityTargetRequest
        {
            CapabilityId = Guid.NewGuid(),
            TargetDescription = "Should fail"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(tc.Exercise.Id, request, tc.UserId));
    }

    [Fact]
    public async Task CreateAsync_CapabilityBelongsToDifferentOrg_ThrowsInvalidOperationException()
    {
        // Arrange
        var tc = CreateTestContext();

        // Capability owned by a different org
        var otherOrg = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Other Org",
            Slug = "other-org",
            CreatedBy = tc.UserId,
            ModifiedBy = tc.UserId
        };
        var foreignCapability = new Capability
        {
            Id = Guid.NewGuid(),
            Name = "Foreign Capability",
            OrganizationId = otherOrg.Id
        };
        tc.DbContext.Organizations.Add(otherOrg);
        tc.DbContext.Capabilities.Add(foreignCapability);
        tc.DbContext.SaveChanges();

        var service = CreateService(tc.DbContext, tc.Org.Id);
        var request = new CreateCapabilityTargetRequest
        {
            CapabilityId = foreignCapability.Id,
            TargetDescription = "Should fail — foreign capability"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(tc.Exercise.Id, request, tc.UserId));
    }

    [Fact]
    public async Task CreateAsync_NoSortOrderProvided_AutoAssignsSortOrderAsNextAvailable()
    {
        // Arrange
        var tc = CreateTestContext();
        SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Existing", sortOrder: 0);
        SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Existing 2", sortOrder: 1);
        var service = CreateService(tc.DbContext, tc.Org.Id);

        var request = new CreateCapabilityTargetRequest
        {
            CapabilityId = tc.Capability.Id,
            TargetDescription = "New target — should get SortOrder 2"
        };

        // Act
        var result = await service.CreateAsync(tc.Exercise.Id, request, tc.UserId);

        // Assert
        result.SortOrder.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_FirstTargetForExercise_AssignsSortOrderZero()
    {
        // Arrange
        var tc = CreateTestContext();
        var service = CreateService(tc.DbContext, tc.Org.Id);

        var request = new CreateCapabilityTargetRequest
        {
            CapabilityId = tc.Capability.Id,
            TargetDescription = "First target ever"
        };

        // Act
        var result = await service.CreateAsync(tc.Exercise.Id, request, tc.UserId);

        // Assert
        result.SortOrder.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_ExplicitSortOrderProvided_HonoursThatValue()
    {
        // Arrange
        var tc = CreateTestContext();
        var service = CreateService(tc.DbContext, tc.Org.Id);

        var request = new CreateCapabilityTargetRequest
        {
            CapabilityId = tc.Capability.Id,
            TargetDescription = "Target with explicit order",
            SortOrder = 42
        };

        // Act
        var result = await service.CreateAsync(tc.Exercise.Id, request, tc.UserId);

        // Assert
        result.SortOrder.Should().Be(42);
    }

    [Fact]
    public async Task CreateAsync_SourcesWithWhitespace_TrimsBeforePersisting()
    {
        // Arrange
        var tc = CreateTestContext();
        var service = CreateService(tc.DbContext, tc.Org.Id);

        var request = new CreateCapabilityTargetRequest
        {
            CapabilityId = tc.Capability.Id,
            TargetDescription = "Trim test",
            Sources = "  Metro EOP  "
        };

        // Act
        var result = await service.CreateAsync(tc.Exercise.Id, request, tc.UserId);

        // Assert
        result.Sources.Should().Be("Metro EOP");
    }

    // =========================================================================
    // UpdateAsync
    // =========================================================================

    [Fact]
    public async Task UpdateAsync_ExistingTarget_ReturnsUpdatedDto()
    {
        // Arrange
        var tc = CreateTestContext();
        var target = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Original description");
        var service = CreateService(tc.DbContext, tc.Org.Id);

        var request = new UpdateCapabilityTargetRequest
        {
            TargetDescription = "Updated description",
            Sources = "New SOP reference"
        };

        // Act
        var result = await service.UpdateAsync(target.Id, request, tc.UserId);

        // Assert
        result.Should().NotBeNull();
        result!.TargetDescription.Should().Be("Updated description");
        result.Sources.Should().Be("New SOP reference");
    }

    [Fact]
    public async Task UpdateAsync_ExistingTarget_PersistsChangesToDatabase()
    {
        // Arrange
        var tc = CreateTestContext();
        var target = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId);
        var service = CreateService(tc.DbContext, tc.Org.Id);

        var modifyingUser = Guid.NewGuid().ToString();
        var request = new UpdateCapabilityTargetRequest
        {
            TargetDescription = "Persisted change",
            SortOrder = 5
        };

        // Act
        await service.UpdateAsync(target.Id, request, modifyingUser);

        // Assert
        var persisted = await tc.DbContext.CapabilityTargets
            .IgnoreQueryFilters()
            .FirstAsync(ct => ct.Id == target.Id);
        persisted.TargetDescription.Should().Be("Persisted change");
        persisted.SortOrder.Should().Be(5);
        persisted.ModifiedBy.Should().Be(modifyingUser);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        var tc = CreateTestContext();
        var service = CreateService(tc.DbContext, tc.Org.Id);

        var request = new UpdateCapabilityTargetRequest
        {
            TargetDescription = "Does not matter"
        };

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), request, tc.UserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_TargetBelongsToDifferentOrg_ReturnsNull()
    {
        // Arrange
        var tc = CreateTestContext();
        var target = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId);
        // Different org in the service
        var service = CreateService(tc.DbContext, Guid.NewGuid());

        var request = new UpdateCapabilityTargetRequest
        {
            TargetDescription = "Should not update"
        };

        // Act
        var result = await service.UpdateAsync(target.Id, request, tc.UserId);

        // Assert
        result.Should().BeNull("org isolation must prevent cross-org writes");
    }

    [Fact]
    public async Task UpdateAsync_NoSortOrderInRequest_DoesNotChangeSortOrder()
    {
        // Arrange
        var tc = CreateTestContext();
        var target = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, sortOrder: 7);
        var service = CreateService(tc.DbContext, tc.Org.Id);

        var request = new UpdateCapabilityTargetRequest
        {
            TargetDescription = "Updated description only",
            SortOrder = null
        };

        // Act
        var result = await service.UpdateAsync(target.Id, request, tc.UserId);

        // Assert
        result.Should().NotBeNull();
        result!.SortOrder.Should().Be(7, "SortOrder must remain unchanged when not included in request");
    }

    [Fact]
    public async Task UpdateAsync_NullSources_ClearsSources()
    {
        // Arrange
        var tc = CreateTestContext();
        var target = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId);
        // Give it a sources value first
        var service = CreateService(tc.DbContext, tc.Org.Id);
        await service.UpdateAsync(target.Id, new UpdateCapabilityTargetRequest
        {
            TargetDescription = "With sources",
            Sources = "Some source"
        }, tc.UserId);

        // Act — clear sources
        var result = await service.UpdateAsync(target.Id, new UpdateCapabilityTargetRequest
        {
            TargetDescription = "Without sources",
            Sources = null
        }, tc.UserId);

        // Assert
        result.Should().NotBeNull();
        result!.Sources.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_SourcesWithWhitespace_TrimsBefore_Persisting()
    {
        // Arrange
        var tc = CreateTestContext();
        var target = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId);
        var service = CreateService(tc.DbContext, tc.Org.Id);

        var request = new UpdateCapabilityTargetRequest
        {
            TargetDescription = "Updated",
            Sources = "  Padded source  "
        };

        // Act
        var result = await service.UpdateAsync(target.Id, request, tc.UserId);

        // Assert
        result!.Sources.Should().Be("Padded source");
    }

    // =========================================================================
    // DeleteAsync
    // =========================================================================

    [Fact]
    public async Task DeleteAsync_ExistingTarget_ReturnsTrueAndSoftDeletes()
    {
        // Arrange
        var tc = CreateTestContext();
        var target = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId);
        var service = CreateService(tc.DbContext, tc.Org.Id);

        // Act
        var result = await service.DeleteAsync(target.Id, tc.UserId);

        // Assert
        result.Should().BeTrue();

        var persisted = await tc.DbContext.CapabilityTargets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ct => ct.Id == target.Id);
        persisted.Should().NotBeNull();
        persisted!.IsDeleted.Should().BeTrue();
        persisted.DeletedAt.Should().NotBeNull();
        persisted.DeletedBy.Should().Be(tc.UserId);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        // Arrange
        var tc = CreateTestContext();
        var service = CreateService(tc.DbContext, tc.Org.Id);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid(), tc.UserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_TargetBelongsToDifferentOrg_ReturnsFalse()
    {
        // Arrange
        var tc = CreateTestContext();
        var target = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId);
        var service = CreateService(tc.DbContext, Guid.NewGuid());

        // Act
        var result = await service.DeleteAsync(target.Id, tc.UserId);

        // Assert
        result.Should().BeFalse("org isolation must prevent cross-org deletes");

        // Verify row was NOT deleted
        var persisted = await tc.DbContext.CapabilityTargets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ct => ct.Id == target.Id);
        persisted!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_DeletedTargetBecomesInvisibleToGetById()
    {
        // Arrange
        var tc = CreateTestContext();
        var target = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId);
        var service = CreateService(tc.DbContext, tc.Org.Id);
        await service.DeleteAsync(target.Id, tc.UserId);

        // Act
        var result = await service.GetByIdAsync(target.Id);

        // Assert
        result.Should().BeNull("soft-deleted targets must not surface in reads");
    }

    // =========================================================================
    // ReorderAsync
    // =========================================================================

    [Fact]
    public async Task ReorderAsync_ValidOrderedIds_UpdatesSortOrdersBasedOnPosition()
    {
        // Arrange
        var tc = CreateTestContext();
        var t1 = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Target 1", sortOrder: 0);
        var t2 = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Target 2", sortOrder: 1);
        var t3 = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Target 3", sortOrder: 2);
        var service = CreateService(tc.DbContext, tc.Org.Id);

        // Act — new order: t3, t1, t2
        var result = await service.ReorderAsync(tc.Exercise.Id, new[] { t3.Id, t1.Id, t2.Id });

        // Assert
        result.Should().BeTrue();

        var reloaded = await service.GetByExerciseAsync(tc.Exercise.Id);
        reloaded.Items.Select(i => i.Id).Should().ContainInOrder(t3.Id, t1.Id, t2.Id);
    }

    [Fact]
    public async Task ReorderAsync_UnknownIdInList_IsIgnoredGracefully()
    {
        // Arrange
        var tc = CreateTestContext();
        var t1 = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Target 1", sortOrder: 0);
        var t2 = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Target 2", sortOrder: 1);
        var service = CreateService(tc.DbContext, tc.Org.Id);

        var phantomId = Guid.NewGuid();

        // Act — include an id that does not exist
        var result = await service.ReorderAsync(tc.Exercise.Id, new[] { phantomId, t2.Id, t1.Id });

        // Assert — should not throw; known targets get re-ordered at positions 1 and 2
        result.Should().BeTrue();

        var persisted1 = await tc.DbContext.CapabilityTargets.FindAsync(t1.Id);
        var persisted2 = await tc.DbContext.CapabilityTargets.FindAsync(t2.Id);
        persisted2!.SortOrder.Should().Be(1);
        persisted1!.SortOrder.Should().Be(2);
    }

    [Fact]
    public async Task ReorderAsync_OnlyAffectsTargetsInCurrentOrg()
    {
        // Arrange
        var tc = CreateTestContext();
        var t1 = SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Org 1 Target", sortOrder: 0);

        // Create a target in a different org/exercise — should not be touched
        var otherOrg = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Other Org",
            Slug = "other-org-2",
            CreatedBy = tc.UserId,
            ModifiedBy = tc.UserId
        };
        var otherExercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Other Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            DeliveryMode = DeliveryMode.FacilitatorPaced,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = otherOrg.Id,
            CreatedBy = tc.UserId,
            ModifiedBy = tc.UserId
        };
        var otherCapability = new Capability
        {
            Id = Guid.NewGuid(),
            Name = "Foreign Cap",
            OrganizationId = otherOrg.Id
        };
        tc.DbContext.Organizations.Add(otherOrg);
        tc.DbContext.Exercises.Add(otherExercise);
        tc.DbContext.Capabilities.Add(otherCapability);
        tc.DbContext.SaveChanges();

        var t2 = SeedTarget(tc.DbContext, otherExercise.Id, otherOrg.Id, otherCapability.Id, tc.UserId, "Org 2 Target", sortOrder: 99);

        var service = CreateService(tc.DbContext, tc.Org.Id);

        // Act — provide both ids but service is scoped to tc.Org
        await service.ReorderAsync(tc.Exercise.Id, new[] { t1.Id, t2.Id });

        // Assert — cross-org target's sort order must remain unchanged
        var foreignTarget = await tc.DbContext.CapabilityTargets
            .IgnoreQueryFilters()
            .FirstAsync(ct => ct.Id == t2.Id);
        foreignTarget.SortOrder.Should().Be(99, "ReorderAsync must not mutate targets in other orgs");
    }

    [Fact]
    public async Task ReorderAsync_EmptyOrderedIds_ReturnsTrueWithNoChanges()
    {
        // Arrange
        var tc = CreateTestContext();
        SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Stable Target", sortOrder: 3);
        var service = CreateService(tc.DbContext, tc.Org.Id);

        // Act
        var result = await service.ReorderAsync(tc.Exercise.Id, Enumerable.Empty<Guid>());

        // Assert
        result.Should().BeTrue();
    }

    // =========================================================================
    // Org Isolation — Cross-Cutting
    // =========================================================================

    [Fact]
    public async Task GetByExerciseAsync_TargetsFilteredByOrgEvenIfExerciseIdMatches()
    {
        // Arrange — same exercise, two different orgs' contexts
        var tc = CreateTestContext();
        SeedTarget(tc.DbContext, tc.Exercise.Id, tc.Org.Id, tc.Capability.Id, tc.UserId, "Org A Target");

        // Service for a different org sees the exercise as missing and throws
        var alienService = CreateService(tc.DbContext, Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => alienService.GetByExerciseAsync(tc.Exercise.Id));
    }

    [Fact]
    public async Task CreateAsync_SetsOrganizationIdFromExercise_NotFromRequest()
    {
        // Arrange
        var tc = CreateTestContext();
        var service = CreateService(tc.DbContext, tc.Org.Id);

        var request = new CreateCapabilityTargetRequest
        {
            CapabilityId = tc.Capability.Id,
            TargetDescription = "Org scoping validation"
        };

        // Act
        var result = await service.CreateAsync(tc.Exercise.Id, request, tc.UserId);

        // Assert — verify persisted OrganizationId matches the exercise's org
        var persisted = await tc.DbContext.CapabilityTargets
            .IgnoreQueryFilters()
            .FirstAsync(ct => ct.Id == result.Id);
        persisted.OrganizationId.Should().Be(tc.Org.Id);
    }
}
