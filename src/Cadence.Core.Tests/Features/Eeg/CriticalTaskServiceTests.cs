using Cadence.Core.Data;
using Cadence.Core.Features.Eeg.Models.DTOs;
using Cadence.Core.Features.Eeg.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Tests.Features.Eeg;

/// <summary>
/// Tests for CriticalTaskService (critical task CRUD and linking operations).
/// </summary>
public class CriticalTaskServiceTests
{
    private (AppDbContext context, Organization org, Exercise exercise, Capability capability, CapabilityTarget target, string userId) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            DeliveryMode = DeliveryMode.FacilitatorPaced,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = userId,
            ModifiedBy = userId
        };

        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Description = "Test MSEL Description",
            ExerciseId = exercise.Id,
            Version = 1,
            IsActive = true,
            CreatedBy = userId,
            ModifiedBy = userId
        };

        exercise.ActiveMselId = msel.Id;

        var capability = new Capability
        {
            Id = Guid.NewGuid(),
            Name = "Operational Communications",
            Description = "Test capability",
            OrganizationId = org.Id
        };

        var target = new CapabilityTarget
        {
            Id = Guid.NewGuid(),
            TargetDescription = "Establish interoperable communications within 30 minutes",
            SortOrder = 0,
            OrganizationId = org.Id,
            ExerciseId = exercise.Id,
            CapabilityId = capability.Id,
            CreatedBy = userId,
            ModifiedBy = userId
        };

        context.Exercises.Add(exercise);
        context.Msels.Add(msel);
        context.Capabilities.Add(capability);
        context.CapabilityTargets.Add(target);
        context.SaveChanges();

        return (context, org, exercise, capability, target, userId);
    }

    private CriticalTaskService CreateService(AppDbContext context)
    {
        return new CriticalTaskService(context);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesCriticalTask()
    {
        // Arrange
        var (context, org, exercise, _, target, userId) = CreateTestContext();
        var service = CreateService(context);

        var request = new CreateCriticalTaskRequest
        {
            TaskDescription = "Issue EOC activation notification",
            Standard = "Per SOP 5.2"
        };

        // Act
        var result = await service.CreateAsync(target.Id, request, userId);

        // Assert
        result.Should().NotBeNull();
        result.TaskDescription.Should().Be("Issue EOC activation notification");
        result.Standard.Should().Be("Per SOP 5.2");
        result.CapabilityTargetId.Should().Be(target.Id);
        result.SortOrder.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_SetsOrganizationIdFromCapabilityTarget()
    {
        // Arrange
        var (context, org, exercise, _, target, userId) = CreateTestContext();
        var service = CreateService(context);

        var request = new CreateCriticalTaskRequest
        {
            TaskDescription = "Test task for org scoping"
        };

        // Act
        var result = await service.CreateAsync(target.Id, request, userId);

        // Assert
        var task = await context.CriticalTasks.FindAsync(result.Id);
        task.Should().NotBeNull();
        task!.OrganizationId.Should().Be(org.Id, "OrganizationId should be set from CapabilityTarget");
    }

    [Fact]
    public async Task CreateAsync_AutoAssignsSortOrder()
    {
        // Arrange
        var (context, org, exercise, _, target, userId) = CreateTestContext();
        var service = CreateService(context);

        // Create first task
        await service.CreateAsync(target.Id, new CreateCriticalTaskRequest { TaskDescription = "Task 1" }, userId);

        // Act - Create second task
        var result = await service.CreateAsync(target.Id, new CreateCriticalTaskRequest { TaskDescription = "Task 2" }, userId);

        // Assert
        result.SortOrder.Should().Be(1, "second task should have SortOrder 1");
    }

    [Fact]
    public async Task CreateAsync_InvalidCapabilityTarget_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, _, _, _, userId) = CreateTestContext();
        var service = CreateService(context);

        var request = new CreateCriticalTaskRequest { TaskDescription = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(Guid.NewGuid(), request, userId));
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingTask_ReturnsTask()
    {
        // Arrange
        var (context, _, _, _, target, userId) = CreateTestContext();
        var service = CreateService(context);
        var created = await service.CreateAsync(target.Id, new CreateCriticalTaskRequest { TaskDescription = "Test Task" }, userId);

        // Act
        var result = await service.GetByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.TaskDescription.Should().Be("Test Task");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingTask_ReturnsNull()
    {
        // Arrange
        var (context, _, _, _, _, _) = CreateTestContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByCapabilityTargetAsync Tests

    [Fact]
    public async Task GetByCapabilityTargetAsync_ReturnsTasksOrderedBySortOrder()
    {
        // Arrange
        var (context, _, _, _, target, userId) = CreateTestContext();
        var service = CreateService(context);

        await service.CreateAsync(target.Id, new CreateCriticalTaskRequest { TaskDescription = "Task 1", SortOrder = 2 }, userId);
        await service.CreateAsync(target.Id, new CreateCriticalTaskRequest { TaskDescription = "Task 2", SortOrder = 0 }, userId);
        await service.CreateAsync(target.Id, new CreateCriticalTaskRequest { TaskDescription = "Task 3", SortOrder = 1 }, userId);

        // Act
        var result = await service.GetByCapabilityTargetAsync(target.Id);

        // Assert
        result.TotalCount.Should().Be(3);
        result.Items.Select(t => t.TaskDescription).Should().ContainInOrder("Task 2", "Task 3", "Task 1");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesTask()
    {
        // Arrange
        var (context, _, _, _, target, userId) = CreateTestContext();
        var service = CreateService(context);
        var created = await service.CreateAsync(target.Id, new CreateCriticalTaskRequest { TaskDescription = "Original" }, userId);

        var updateRequest = new UpdateCriticalTaskRequest
        {
            TaskDescription = "Updated Task Description",
            Standard = "New Standard"
        };

        // Act
        var result = await service.UpdateAsync(created.Id, updateRequest, userId);

        // Assert
        result.Should().NotBeNull();
        result!.TaskDescription.Should().Be("Updated Task Description");
        result.Standard.Should().Be("New Standard");
    }

    [Fact]
    public async Task UpdateAsync_NonExistingTask_ReturnsNull()
    {
        // Arrange
        var (context, _, _, _, _, userId) = CreateTestContext();
        var service = CreateService(context);

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), new UpdateCriticalTaskRequest { TaskDescription = "Test" }, userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingTask_SoftDeletesTask()
    {
        // Arrange
        var (context, _, _, _, target, userId) = CreateTestContext();
        var service = CreateService(context);
        var created = await service.CreateAsync(target.Id, new CreateCriticalTaskRequest { TaskDescription = "To Delete" }, userId);

        // Act
        var result = await service.DeleteAsync(created.Id, userId);

        // Assert
        result.Should().BeTrue();

        // Verify soft delete
        var task = await context.CriticalTasks.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == created.Id);
        task.Should().NotBeNull();
        task!.IsDeleted.Should().BeTrue();
        task.DeletedAt.Should().NotBeNull();
        task.DeletedBy.Should().Be(userId);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingTask_ReturnsFalse()
    {
        // Arrange
        var (context, _, _, _, _, userId) = CreateTestContext();
        var service = CreateService(context);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid(), userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SetLinkedInjectsAsync Tests

    [Fact]
    public async Task SetLinkedInjectsAsync_ValidInjects_CreatesLinks()
    {
        // Arrange
        var (context, org, exercise, _, target, userId) = CreateTestContext();
        var service = CreateService(context);

        var task = await service.CreateAsync(target.Id, new CreateCriticalTaskRequest { TaskDescription = "Task with injects" }, userId);

        // Create injects
        var msel = await context.Msels.FirstAsync(m => m.ExerciseId == exercise.Id);
        var inject1 = new Inject
        {
            Id = Guid.NewGuid(),
            MselId = msel.Id,
            InjectNumber = 1,
            Title = "Inject 1",
            Description = "Test",
            ScheduledTime = TimeOnly.FromDateTime(DateTime.Now),
            Target = "Test",
            Status = InjectStatus.Draft,
            Sequence = 1,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        var inject2 = new Inject
        {
            Id = Guid.NewGuid(),
            MselId = msel.Id,
            InjectNumber = 2,
            Title = "Inject 2",
            Description = "Test",
            ScheduledTime = TimeOnly.FromDateTime(DateTime.Now),
            Target = "Test",
            Status = InjectStatus.Draft,
            Sequence = 2,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.Injects.AddRange(inject1, inject2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.SetLinkedInjectsAsync(task.Id, new[] { inject1.Id, inject2.Id }, userId);

        // Assert
        result.Should().BeTrue();

        var links = await context.InjectCriticalTasks.Where(ict => ict.CriticalTaskId == task.Id).ToListAsync();
        links.Should().HaveCount(2);
        links.Should().AllSatisfy(l =>
        {
            l.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            l.CreatedBy.Should().Be(userId);
        });
    }

    [Fact]
    public async Task SetLinkedInjectsAsync_ReplacesExistingLinks()
    {
        // Arrange
        var (context, org, exercise, _, target, userId) = CreateTestContext();
        var service = CreateService(context);

        var task = await service.CreateAsync(target.Id, new CreateCriticalTaskRequest { TaskDescription = "Task" }, userId);

        // Create injects
        var msel = await context.Msels.FirstAsync(m => m.ExerciseId == exercise.Id);
        var inject1 = new Inject { Id = Guid.NewGuid(), MselId = msel.Id, InjectNumber = 1, Title = "Inject 1", Description = "Test", ScheduledTime = TimeOnly.FromDateTime(DateTime.Now), Target = "Test", Status = InjectStatus.Draft, Sequence = 1, CreatedBy = userId, ModifiedBy = userId };
        var inject2 = new Inject { Id = Guid.NewGuid(), MselId = msel.Id, InjectNumber = 2, Title = "Inject 2", Description = "Test", ScheduledTime = TimeOnly.FromDateTime(DateTime.Now), Target = "Test", Status = InjectStatus.Draft, Sequence = 2, CreatedBy = userId, ModifiedBy = userId };
        var inject3 = new Inject { Id = Guid.NewGuid(), MselId = msel.Id, InjectNumber = 3, Title = "Inject 3", Description = "Test", ScheduledTime = TimeOnly.FromDateTime(DateTime.Now), Target = "Test", Status = InjectStatus.Draft, Sequence = 3, CreatedBy = userId, ModifiedBy = userId };
        context.Injects.AddRange(inject1, inject2, inject3);
        await context.SaveChangesAsync();

        // Set initial links
        await service.SetLinkedInjectsAsync(task.Id, new[] { inject1.Id, inject2.Id }, userId);

        // Act - Replace with different injects
        var result = await service.SetLinkedInjectsAsync(task.Id, new[] { inject2.Id, inject3.Id }, userId);

        // Assert
        result.Should().BeTrue();

        var links = await context.InjectCriticalTasks.Where(ict => ict.CriticalTaskId == task.Id).ToListAsync();
        links.Should().HaveCount(2);
        links.Select(l => l.InjectId).Should().BeEquivalentTo(new[] { inject2.Id, inject3.Id });
    }

    [Fact]
    public async Task SetLinkedInjectsAsync_NonExistingTask_ReturnsFalse()
    {
        // Arrange
        var (context, _, _, _, _, userId) = CreateTestContext();
        var service = CreateService(context);

        // Act
        var result = await service.SetLinkedInjectsAsync(Guid.NewGuid(), new[] { Guid.NewGuid() }, userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetLinkedInjectIdsAsync Tests

    [Fact]
    public async Task GetLinkedInjectIdsAsync_ReturnsLinkedInjectIds()
    {
        // Arrange
        var (context, org, exercise, _, target, userId) = CreateTestContext();
        var service = CreateService(context);

        var task = await service.CreateAsync(target.Id, new CreateCriticalTaskRequest { TaskDescription = "Task" }, userId);

        var msel = await context.Msels.FirstAsync(m => m.ExerciseId == exercise.Id);
        var inject1 = new Inject { Id = Guid.NewGuid(), MselId = msel.Id, InjectNumber = 1, Title = "Inject 1", Description = "Test", ScheduledTime = TimeOnly.FromDateTime(DateTime.Now), Target = "Test", Status = InjectStatus.Draft, Sequence = 1, CreatedBy = userId, ModifiedBy = userId };
        var inject2 = new Inject { Id = Guid.NewGuid(), MselId = msel.Id, InjectNumber = 2, Title = "Inject 2", Description = "Test", ScheduledTime = TimeOnly.FromDateTime(DateTime.Now), Target = "Test", Status = InjectStatus.Draft, Sequence = 2, CreatedBy = userId, ModifiedBy = userId };
        context.Injects.AddRange(inject1, inject2);
        await context.SaveChangesAsync();

        await service.SetLinkedInjectsAsync(task.Id, new[] { inject1.Id, inject2.Id }, userId);

        // Act
        var result = await service.GetLinkedInjectIdsAsync(task.Id);

        // Assert
        result.Should().BeEquivalentTo(new[] { inject1.Id, inject2.Id });
    }

    #endregion

    #region ReorderAsync Tests

    [Fact]
    public async Task ReorderAsync_ValidOrder_UpdatesSortOrder()
    {
        // Arrange
        var (context, _, _, _, target, userId) = CreateTestContext();
        var service = CreateService(context);

        var task1 = await service.CreateAsync(target.Id, new CreateCriticalTaskRequest { TaskDescription = "Task 1" }, userId);
        var task2 = await service.CreateAsync(target.Id, new CreateCriticalTaskRequest { TaskDescription = "Task 2" }, userId);
        var task3 = await service.CreateAsync(target.Id, new CreateCriticalTaskRequest { TaskDescription = "Task 3" }, userId);

        // Act - Reorder: Task3, Task1, Task2
        var result = await service.ReorderAsync(target.Id, new[] { task3.Id, task1.Id, task2.Id });

        // Assert
        result.Should().BeTrue();

        var tasks = await service.GetByCapabilityTargetAsync(target.Id);
        tasks.Items.Select(t => t.Id).Should().ContainInOrder(task3.Id, task1.Id, task2.Id);
    }

    #endregion

    #region GetByExerciseAsync Tests

    [Fact]
    public async Task GetByExerciseAsync_ReturnsAllTasksForExercise()
    {
        // Arrange
        var (context, org, exercise, capability, target1, userId) = CreateTestContext();
        var service = CreateService(context);

        // Create second target
        var target2 = new CapabilityTarget
        {
            Id = Guid.NewGuid(),
            TargetDescription = "Second target",
            SortOrder = 1,
            OrganizationId = org.Id,
            ExerciseId = exercise.Id,
            CapabilityId = capability.Id,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.CapabilityTargets.Add(target2);
        await context.SaveChangesAsync();

        // Create tasks in both targets
        await service.CreateAsync(target1.Id, new CreateCriticalTaskRequest { TaskDescription = "Target1 Task1" }, userId);
        await service.CreateAsync(target1.Id, new CreateCriticalTaskRequest { TaskDescription = "Target1 Task2" }, userId);
        await service.CreateAsync(target2.Id, new CreateCriticalTaskRequest { TaskDescription = "Target2 Task1" }, userId);

        // Act
        var result = await service.GetByExerciseAsync(exercise.Id);

        // Assert
        result.TotalCount.Should().Be(3);
    }

    #endregion
}
