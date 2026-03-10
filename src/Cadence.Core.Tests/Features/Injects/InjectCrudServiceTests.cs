using Cadence.Core.Data;
using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Features.Injects.Services;
using Cadence.Core.Features.Injects.Validators;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Injects;

/// <summary>
/// Tests for <see cref="InjectCrudService"/> — CRUD operations extracted from InjectsController.
/// </summary>
public class InjectCrudServiceTests
{
    private readonly Mock<ILogger<InjectCrudService>> _loggerMock = new();
    private readonly IValidator<CreateInjectRequest> _createValidator = new CreateInjectRequestValidator();
    private readonly IValidator<UpdateInjectRequest> _updateValidator = new UpdateInjectRequestValidator();

    // =========================================================================
    // Helpers
    // =========================================================================

    private (AppDbContext context, Organization org, Exercise exercise, Msel msel, string userId)
        CreateTestContext(
            ExerciseStatus status = ExerciseStatus.Draft,
            bool requireApproval = false)
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
            Status = status,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            RequireInjectApproval = requireApproval,
            CreatedBy = userId,
            ModifiedBy = userId
        };

        var msel = new Cadence.Core.Models.Entities.Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Description = "Test MSEL Description",
            ExerciseId = exercise.Id,
            Version = 1,
            IsActive = true,
            OrganizationId = org.Id,
            CreatedBy = userId,
            ModifiedBy = userId
        };

        exercise.ActiveMselId = msel.Id;

        context.Exercises.Add(exercise);
        context.Msels.Add(msel);
        context.SaveChanges();

        return (context, org, exercise, msel, userId);
    }

    private Inject CreateInject(
        Guid mselId,
        int injectNumber = 1,
        InjectStatus status = InjectStatus.Draft,
        string? userId = null)
    {
        var uid = userId ?? Guid.NewGuid().ToString();
        return new Inject
        {
            Id = Guid.NewGuid(),
            MselId = mselId,
            InjectNumber = injectNumber,
            Title = $"Inject {injectNumber}",
            Description = "Test Description",
            ScheduledTime = new TimeOnly(9, 0),
            Target = "Test Target",
            Status = status,
            Sequence = injectNumber,
            TriggerType = TriggerType.Manual,
            InjectType = InjectType.Standard,
            CreatedBy = uid,
            ModifiedBy = uid
        };
    }

    private InjectCrudService CreateService(AppDbContext context) =>
        new InjectCrudService(context, _createValidator, _updateValidator, _loggerMock.Object);

    private CreateInjectRequest BuildCreateRequest(
        string title = "My Inject Title",
        string description = "My inject description content",
        string target = "EOC Commander") =>
        new CreateInjectRequest
        {
            Title = title,
            Description = description,
            Target = target,
            ScheduledTime = new TimeOnly(9, 0),
            InjectType = InjectType.Standard,
            TriggerType = TriggerType.Manual
        };

    private UpdateInjectRequest BuildUpdateRequest(
        string title = "Updated Title",
        string description = "Updated description content",
        string target = "Updated Target") =>
        new UpdateInjectRequest
        {
            Title = title,
            Description = description,
            Target = target,
            ScheduledTime = new TimeOnly(10, 0),
            InjectType = InjectType.Standard,
            TriggerType = TriggerType.Manual
        };

    // =========================================================================
    // GetInjectsAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetInjects_ReturnsAllForActiveMsel()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        context.Injects.Add(CreateInject(msel.Id, 1, userId: userId));
        context.Injects.Add(CreateInject(msel.Id, 2, userId: userId));
        await context.SaveChangesAsync();

        var sut = CreateService(context);

        // Act
        var result = await sut.GetInjectsAsync(exercise.Id, null, userId, false);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(i => i.Sequence);
    }

    [Fact]
    public async Task GetInjects_NoActiveMsel_ReturnsEmpty()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org", CreatedBy = userId, ModifiedBy = userId };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "No-MSEL Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            ActiveMselId = null,  // no active MSEL
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.Exercises.Add(exercise);
        await context.SaveChangesAsync();

        var sut = CreateService(context);

        // Act
        var result = await sut.GetInjectsAsync(exercise.Id, null, userId, false);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInjects_FilterByStatus_ReturnsMatchingOnly()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        context.Injects.Add(CreateInject(msel.Id, 1, InjectStatus.Draft, userId));
        context.Injects.Add(CreateInject(msel.Id, 2, InjectStatus.Submitted, userId));
        context.Injects.Add(CreateInject(msel.Id, 3, InjectStatus.Submitted, userId));
        await context.SaveChangesAsync();

        var sut = CreateService(context);

        // Act
        var result = await sut.GetInjectsAsync(exercise.Id, InjectStatus.Submitted, userId, false);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.Status == InjectStatus.Submitted);
    }

    [Fact]
    public async Task GetInjects_IncludesCriticalTaskCounts()
    {
        // Arrange
        var (context, org, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(msel.Id, 1, userId: userId);
        context.Injects.Add(inject);

        // Build the full EEG hierarchy required by the InjectCriticalTask query filter:
        // CapabilityTarget -> CriticalTask -> InjectCriticalTask
        var capabilityTarget = new CapabilityTarget
        {
            Id = Guid.NewGuid(),
            TargetDescription = "Test Target",
            ExerciseId = exercise.Id,
            CapabilityId = Guid.NewGuid(), // not FK-constrained in in-memory DB
            OrganizationId = org.Id,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.CapabilityTargets.Add(capabilityTarget);

        var criticalTask = new CriticalTask
        {
            Id = Guid.NewGuid(),
            TaskDescription = "Test Task",
            CapabilityTargetId = capabilityTarget.Id,
            OrganizationId = org.Id,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.CriticalTasks.Add(criticalTask);
        await context.SaveChangesAsync();

        var criticalTaskLink = new InjectCriticalTask
        {
            InjectId = inject.Id,
            CriticalTaskId = criticalTask.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };
        context.InjectCriticalTasks.Add(criticalTaskLink);
        await context.SaveChangesAsync();

        var sut = CreateService(context);

        // Act
        var result = await sut.GetInjectsAsync(exercise.Id, null, userId, false);

        // Assert
        result.Should().HaveCount(1);
        result[0].LinkedCriticalTaskCount.Should().Be(1,
            "the service should batch-count InjectCriticalTask records per inject");
    }

    [Fact]
    public async Task GetInjects_ExerciseNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.GetInjectsAsync(Guid.NewGuid(), null, null, false));
    }

    // =========================================================================
    // GetInjectAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetInject_ReturnsWithNavProperties()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(msel.Id, 1, userId: userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var sut = CreateService(context);

        // Act
        var result = await sut.GetInjectAsync(exercise.Id, inject.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(inject.Id);
        result.Title.Should().Be(inject.Title);
        result.Status.Should().Be(InjectStatus.Draft);
    }

    [Fact]
    public async Task GetInject_NotFound_ReturnsNull()
    {
        // Arrange
        var (context, _, exercise, _, _) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetInjectAsync(exercise.Id, Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetInject_ExerciseNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.GetInjectAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    // =========================================================================
    // GetInjectHistoryAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetInjectHistory_ReturnsOrderedByDate()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(msel.Id, 1, userId: userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        context.InjectStatusHistories.AddRange(
            new InjectStatusHistory
            {
                Id = Guid.NewGuid(),
                InjectId = inject.Id,
                FromStatus = InjectStatus.Draft,
                ToStatus = InjectStatus.Submitted,
                ChangedByUserId = userId,
                ChangedAt = now.AddMinutes(-10),
                CreatedBy = userId,
                ModifiedBy = userId
            },
            new InjectStatusHistory
            {
                Id = Guid.NewGuid(),
                InjectId = inject.Id,
                FromStatus = InjectStatus.Submitted,
                ToStatus = InjectStatus.Approved,
                ChangedByUserId = userId,
                ChangedAt = now,
                CreatedBy = userId,
                ModifiedBy = userId
            }
        );
        await context.SaveChangesAsync();

        var sut = CreateService(context);

        // Act
        var result = await sut.GetInjectHistoryAsync(exercise.Id, inject.Id);

        // Assert
        result.Should().HaveCount(2);
        // Most recent first (OrderByDescending)
        result[0].ChangedAt.Should().BeAfter(result[1].ChangedAt);
        result[0].ToStatus.Should().Be(InjectStatus.Approved);
    }

    [Fact]
    public async Task GetInjectHistory_InjectNotInExercise_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (context, _, exercise, _, _) = CreateTestContext();
        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.GetInjectHistoryAsync(exercise.Id, Guid.NewGuid()));
    }

    // =========================================================================
    // CreateInjectAsync Tests
    // =========================================================================

    [Fact]
    public async Task CreateInject_CreatesWithNextSequenceNumber()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        context.Injects.Add(CreateInject(msel.Id, 1, userId: userId));
        context.Injects.Add(CreateInject(msel.Id, 2, userId: userId));
        await context.SaveChangesAsync();

        var sut = CreateService(context);
        var request = BuildCreateRequest();

        // Act
        var result = await sut.CreateInjectAsync(exercise.Id, request, userId);

        // Assert
        result.Should().NotBeNull();
        result.InjectNumber.Should().Be(3, "next in sequence after inject numbers 1 and 2");
        result.Sequence.Should().Be(3);
    }

    [Fact]
    public async Task CreateInject_NoActiveMsel_AutoCreatesMsel()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Org", CreatedBy = userId, ModifiedBy = userId };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "No-MSEL Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            ActiveMselId = null,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.Exercises.Add(exercise);
        await context.SaveChangesAsync();

        var sut = CreateService(context);
        var request = BuildCreateRequest();

        // Act
        var result = await sut.CreateInjectAsync(exercise.Id, request, userId);

        // Assert
        result.Should().NotBeNull();
        result.InjectNumber.Should().Be(1);

        // Verify MSEL was created
        var updatedExercise = await context.Exercises.FindAsync(exercise.Id);
        updatedExercise!.ActiveMselId.Should().NotBeNull("MSEL should be auto-created");

        var msel = await context.Msels.FindAsync(updatedExercise.ActiveMselId);
        msel.Should().NotBeNull();
        msel!.Name.Should().Contain("MSEL");
    }

    [Fact]
    public async Task CreateInject_LinksObjectives()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();

        var objective = new Objective
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            ObjectiveNumber = "1",
            Name = "Test Objective",
            Description = "Test Objective Description",
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.Objectives.Add(objective);
        await context.SaveChangesAsync();

        var sut = CreateService(context);
        var request = new CreateInjectRequest
        {
            Title = "Linked Inject",
            Description = "Links an objective",
            Target = "Test Target",
            ScheduledTime = new TimeOnly(9, 0),
            InjectType = InjectType.Standard,
            TriggerType = TriggerType.Manual,
            ObjectiveIds = new List<Guid> { objective.Id }
        };

        // Act
        var result = await sut.CreateInjectAsync(exercise.Id, request, userId);

        // Assert
        result.Should().NotBeNull();
        result.ObjectiveIds.Should().Contain(objective.Id);
    }

    [Fact]
    public async Task CreateInject_InvalidTitle_ThrowsValidationException()
    {
        // Arrange
        var (context, _, exercise, _, userId) = CreateTestContext();
        var sut = CreateService(context);

        var request = BuildCreateRequest(title: "ab"); // too short (< 3 chars)

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => sut.CreateInjectAsync(exercise.Id, request, userId));
    }

    [Fact]
    public async Task CreateInject_ExerciseNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.CreateInjectAsync(Guid.NewGuid(), BuildCreateRequest(), "user-1"));
    }

    // =========================================================================
    // UpdateInjectAsync Tests
    // =========================================================================

    [Fact]
    public async Task UpdateInject_FullEdit_UpdatesAllFields()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(msel.Id, 1, InjectStatus.Draft, userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var sut = CreateService(context);
        var request = BuildUpdateRequest("New Title", "New Description", "New Target");

        // Act
        var (dto, reverted) = await sut.UpdateInjectAsync(exercise.Id, inject.Id, request, userId);

        // Assert
        dto.Should().NotBeNull();
        dto.Title.Should().Be("New Title");
        dto.Description.Should().Be("New Description");
        dto.Target.Should().Be("New Target");
        reverted.Should().BeFalse("Draft inject does not trigger approval revert");
    }

    [Fact]
    public async Task UpdateInject_ReleasedInject_OnlyUpdatesNotes()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(msel.Id, 1, InjectStatus.Released, userId);
        inject.Title = "Original Title";
        inject.ControllerNotes = "Original notes";
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var sut = CreateService(context);
        var request = new UpdateInjectRequest
        {
            Title = "Attempted New Title",
            Description = "Attempted new description",
            Target = "Attempted new target",
            ScheduledTime = new TimeOnly(11, 0),
            InjectType = InjectType.Standard,
            TriggerType = TriggerType.Manual,
            ControllerNotes = "Updated notes only"
        };

        // Act
        var (dto, reverted) = await sut.UpdateInjectAsync(exercise.Id, inject.Id, request, userId);

        // Assert
        dto.Title.Should().Be("Original Title",
            "Released injects do not allow title changes");
        dto.ControllerNotes.Should().Be("Updated notes only",
            "ControllerNotes is the only editable field on Released injects");
        reverted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateInject_ApprovalEnabled_RevertsApprovedToDraft()
    {
        // Arrange: exercise with approval workflow enabled
        var (context, _, exercise, msel, userId) = CreateTestContext(requireApproval: true);
        var inject = CreateInject(msel.Id, 1, InjectStatus.Approved, userId);
        inject.ApprovedByUserId = Guid.NewGuid().ToString();
        inject.ApprovedAt = DateTime.UtcNow.AddMinutes(-5);
        inject.SubmittedByUserId = userId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var sut = CreateService(context);
        var request = BuildUpdateRequest();

        // Act
        var (dto, reverted) = await sut.UpdateInjectAsync(exercise.Id, inject.Id, request, userId);

        // Assert
        dto.Status.Should().Be(InjectStatus.Draft,
            "Editing an Approved inject reverts it to Draft when approval workflow is enabled");
        reverted.Should().BeTrue("statusReverted flag must be true so controller can send SignalR notification");

        // Approval tracking fields should be cleared
        dto.ApprovedByUserId.Should().BeNull();
        dto.ApprovedAt.Should().BeNull();
        dto.SubmittedByUserId.Should().BeNull();

        // Verify history record was created
        var history = await context.InjectStatusHistories
            .Where(h => h.InjectId == inject.Id)
            .ToListAsync();
        history.Should().HaveCount(1);
        history[0].FromStatus.Should().Be(InjectStatus.Approved);
        history[0].ToStatus.Should().Be(InjectStatus.Draft);
    }

    [Fact]
    public async Task UpdateInject_ArchivedExercise_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext(status: ExerciseStatus.Archived);
        var inject = CreateInject(msel.Id, 1, InjectStatus.Draft, userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.UpdateInjectAsync(exercise.Id, inject.Id, BuildUpdateRequest(), userId));
    }

    [Fact]
    public async Task UpdateInject_InjectNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (context, _, exercise, _, userId) = CreateTestContext();
        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.UpdateInjectAsync(exercise.Id, Guid.NewGuid(), BuildUpdateRequest(), userId));
    }

    [Fact]
    public async Task UpdateInject_InvalidTitle_ThrowsValidationException()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(msel.Id, 1, userId: userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var sut = CreateService(context);
        var request = BuildUpdateRequest(title: "ab"); // too short

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => sut.UpdateInjectAsync(exercise.Id, inject.Id, request, userId));
    }

    // =========================================================================
    // DeleteInjectAsync Tests
    // =========================================================================

    [Fact]
    public async Task DeleteInject_SoftDeletes()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(msel.Id, 1, userId: userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var sut = CreateService(context);

        // Act
        var result = await sut.DeleteInjectAsync(exercise.Id, inject.Id, userId);

        // Assert
        result.Should().BeTrue();

        // Because of global soft-delete query filter, use IgnoreQueryFilters to verify
        var deleted = await context.Injects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == inject.Id);

        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue("soft delete sets IsDeleted flag, not removes the row");
        deleted.DeletedAt.Should().NotBeNull();
        deleted.DeletedBy.Should().Be(userId);
    }

    [Fact]
    public async Task DeleteInject_ArchivedExercise_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext(status: ExerciseStatus.Archived);
        var inject = CreateInject(msel.Id, 1, userId: userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.DeleteInjectAsync(exercise.Id, inject.Id, userId));
    }

    [Fact]
    public async Task DeleteInject_InjectNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (context, _, exercise, _, userId) = CreateTestContext();
        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.DeleteInjectAsync(exercise.Id, Guid.NewGuid(), userId));
    }

    [Fact]
    public async Task DeleteInject_ExerciseNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.DeleteInjectAsync(Guid.NewGuid(), Guid.NewGuid(), "user-1"));
    }
}
