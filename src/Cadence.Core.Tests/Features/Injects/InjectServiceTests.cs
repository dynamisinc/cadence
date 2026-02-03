using Cadence.Core.Data;
using Cadence.Core.Features.Injects.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cadence.Core.Tests.Features.Injects;

/// <summary>
/// Tests for InjectService (inject conduct operations).
/// </summary>
public class InjectServiceTests
{
    private readonly Mock<IExerciseHubContext> _hubContextMock;

    public InjectServiceTests()
    {
        _hubContextMock = new Mock<IExerciseHubContext>();
    }

    private (AppDbContext context, Organization org, Exercise exercise, Msel msel, string userId) CreateTestContext(
        ExerciseStatus status = ExerciseStatus.Active,
        DeliveryMode deliveryMode = DeliveryMode.FacilitatorPaced)
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
            DeliveryMode = deliveryMode,
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

        context.Exercises.Add(exercise);
        context.Msels.Add(msel);
        context.SaveChanges();

        return (context, org, exercise, msel, userId);
    }

    private InjectService CreateService(AppDbContext context)
    {
        return new InjectService(context, _hubContextMock.Object);
    }

    private Inject CreateInject(
        Guid mselId,
        int injectNumber,
        InjectStatus status = InjectStatus.Draft,
        string? userId = null)
    {
        var actualUserId = userId ?? Guid.NewGuid().ToString();
        return new Inject
        {
            Id = Guid.NewGuid(),
            MselId = mselId,
            InjectNumber = injectNumber,
            Title = $"Test Inject {injectNumber}",
            Description = "Test Description",
            ScheduledTime = TimeOnly.FromDateTime(DateTime.Now),
            Target = "Test Target",
            Status = status,
            Sequence = injectNumber,
            CreatedBy = actualUserId,
            ModifiedBy = actualUserId
        };
    }

    #region FireInjectAsync Tests

    [Fact]
    public async Task FireInject_ValidInject_SetsModifiedBy()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(msel.Id, 1, InjectStatus.Draft, userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.FireInjectAsync(exercise.Id, inject.Id, userId.ToString());

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Released);
        updated.FiredByUserId.Should().Be(userId.ToString());
        updated.ModifiedBy.Should().Be(userId, "ModifiedBy should be set when firing an inject");
        updated.FiredAt.Should().NotBeNull();
        updated.FiredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _hubContextMock.Verify(
            h => h.NotifyInjectFired(exercise.Id, It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Once);
    }

    [Fact]
    public async Task FireInject_InactiveExercise_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext(status: ExerciseStatus.Draft);
        var inject = CreateInject(msel.Id, 1, InjectStatus.Draft, userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.FireInjectAsync(exercise.Id, inject.Id, userId.ToString()));
    }

    [Fact]
    public async Task FireInject_ClockDriven_RequiresSynchronizedStatus()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext(deliveryMode: DeliveryMode.ClockDriven);
        var inject = CreateInject(msel.Id, 1, InjectStatus.Draft, userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.FireInjectAsync(exercise.Id, inject.Id, userId.ToString()));
    }

    #endregion

    #region SkipInjectAsync Tests

    [Fact]
    public async Task SkipInject_ValidInject_SetsModifiedBy()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(msel.Id, 1, InjectStatus.Draft, userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.SkipInjectAsync(exercise.Id, inject.Id, userId);

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Deferred);
        updated.SkippedByUserId.Should().Be(userId.ToString());
        updated.ModifiedBy.Should().Be(userId, "ModifiedBy should be set when skipping an inject");
        updated.SkippedAt.Should().NotBeNull();
        updated.SkippedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _hubContextMock.Verify(
            h => h.NotifyInjectSkipped(exercise.Id, It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Once);
    }

    [Fact]
    public async Task SkipInject_InactiveExercise_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext(status: ExerciseStatus.Draft);
        var inject = CreateInject(msel.Id, 1, InjectStatus.Draft, userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SkipInjectAsync(exercise.Id, inject.Id, userId));
    }

    [Fact]
    public async Task SkipInject_AlreadyReleased_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(msel.Id, 1, InjectStatus.Released, userId);
        inject.FiredAt = DateTime.UtcNow;
        inject.FiredByUserId = userId.ToString();
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SkipInjectAsync(exercise.Id, inject.Id, userId));
    }

    #endregion

    #region ResetInjectAsync Tests

    [Fact]
    public async Task ResetInject_ReleasedInject_SetsModifiedBy()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(msel.Id, 1, InjectStatus.Released, userId);
        inject.FiredAt = DateTime.UtcNow.AddMinutes(-5);
        inject.FiredByUserId = userId.ToString();
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.ResetInjectAsync(exercise.Id, inject.Id, userId);

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Draft);
        updated.FiredAt.Should().BeNull();
        updated.FiredByUserId.Should().BeNull();
        updated.ModifiedBy.Should().Be(userId, "ModifiedBy should be set when resetting an inject");

        _hubContextMock.Verify(
            h => h.NotifyInjectReset(exercise.Id, It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetInject_DeferredInject_SetsModifiedBy()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(msel.Id, 1, InjectStatus.Deferred, userId);
        inject.SkippedAt = DateTime.UtcNow.AddMinutes(-5);
        inject.SkippedByUserId = userId.ToString();
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.ResetInjectAsync(exercise.Id, inject.Id, userId);

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Draft);
        updated.SkippedAt.Should().BeNull();
        updated.SkippedByUserId.Should().BeNull();
        updated.ModifiedBy.Should().Be(userId, "ModifiedBy should be set when resetting an inject");

        _hubContextMock.Verify(
            h => h.NotifyInjectReset(exercise.Id, It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetInject_SynchronizedInject_ClearsReadyAt()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(msel.Id, 1, InjectStatus.Synchronized, userId);
        inject.ReadyAt = DateTime.UtcNow.AddMinutes(-2);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.ResetInjectAsync(exercise.Id, inject.Id, userId);

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Draft);
        updated.ReadyAt.Should().BeNull();
        updated.ModifiedBy.Should().Be(userId);

        _hubContextMock.Verify(
            h => h.NotifyInjectReset(exercise.Id, It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetInject_InactiveExercise_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext(status: ExerciseStatus.Draft);
        var inject = CreateInject(msel.Id, 1, InjectStatus.Released, userId);
        inject.FiredAt = DateTime.UtcNow.AddMinutes(-5);
        inject.FiredByUserId = userId.ToString();
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResetInjectAsync(exercise.Id, inject.Id, userId));
    }

    #endregion

    #region Audit Trail Tests

    [Fact]
    public async Task InjectOperations_DifferentUsers_TrackCorrectModifier()
    {
        // Arrange
        var (context, _, exercise, msel, creatorUserId) = CreateTestContext();
        var controllerUserId = Guid.NewGuid();
        var supervisorUserId = Guid.NewGuid();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Draft, creatorUserId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert - Fire by controller
        await service.FireInjectAsync(exercise.Id, inject.Id, controllerUserId.ToString());
        var afterFire = await context.Injects.AsNoTracking().FirstAsync(i => i.Id == inject.Id);
        afterFire.FiredByUserId.Should().Be(controllerUserId.ToString());
        afterFire.ModifiedBy.Should().Be(controllerUserId.ToString(), "Controller should be tracked as modifier when firing");

        // Act & Assert - Reset by supervisor
        await service.ResetInjectAsync(exercise.Id, inject.Id, supervisorUserId.ToString());
        var afterReset = await context.Injects.AsNoTracking().FirstAsync(i => i.Id == inject.Id);
        afterReset.ModifiedBy.Should().Be(supervisorUserId.ToString(), "Supervisor should be tracked as modifier when resetting");
        afterReset.FiredByUserId.Should().BeNull("FiredBy should be cleared on reset");

        // Act & Assert - Skip by controller
        await service.SkipInjectAsync(exercise.Id, inject.Id, controllerUserId.ToString());
        var afterSkip = await context.Injects.AsNoTracking().FirstAsync(i => i.Id == inject.Id);
        afterSkip.SkippedByUserId.Should().Be(controllerUserId.ToString());
        afterSkip.ModifiedBy.Should().Be(controllerUserId.ToString(), "Controller should be tracked as modifier when skipping");
    }

    #endregion

    #region SubmitForApprovalAsync Tests

    [Fact]
    public async Task SubmitForApproval_DraftInjectWithApprovalEnabled_ChangesToSubmitted()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        var inject = CreateInject(msel.Id, 1, InjectStatus.Draft, userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.SubmitForApprovalAsync(exercise.Id, inject.Id, userId);

        // Assert
        result.Status.Should().Be(InjectStatus.Submitted);
        result.SubmittedByUserId.Should().Be(userId);
        result.SubmittedAt.Should().NotBeNull();
        result.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.ModifiedBy.Should().Be(userId);
    }

    [Fact]
    public async Task SubmitForApproval_ApprovalDisabled_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        exercise.RequireInjectApproval = false; // Approval disabled
        var inject = CreateInject(msel.Id, 1, InjectStatus.Draft, userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SubmitForApprovalAsync(exercise.Id, inject.Id, userId));

        exception.Message.Should().Contain("approval workflow is not enabled");
    }

    [Fact]
    public async Task SubmitForApproval_NotDraftStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        var inject = CreateInject(msel.Id, 1, InjectStatus.Submitted, userId); // Already submitted
        inject.SubmittedByUserId = userId;
        inject.SubmittedAt = DateTime.UtcNow.AddHours(-1);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SubmitForApprovalAsync(exercise.Id, inject.Id, userId));

        exception.Message.Should().Contain("Only Draft injects can be submitted");
    }

    [Fact]
    public async Task SubmitForApproval_ClearsPreviousRejection()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var approverUserId = Guid.NewGuid().ToString();
        exercise.RequireInjectApproval = true;
        var inject = CreateInject(msel.Id, 1, InjectStatus.Draft, userId);
        inject.RejectedByUserId = approverUserId;
        inject.RejectedAt = DateTime.UtcNow.AddDays(-1);
        inject.RejectionReason = "Missing details";
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.SubmitForApprovalAsync(exercise.Id, inject.Id, userId);

        // Assert
        result.Status.Should().Be(InjectStatus.Submitted);
        result.RejectedByUserId.Should().BeNull();
        result.RejectedAt.Should().BeNull();
        result.RejectionReason.Should().BeNull();
    }

    [Fact]
    public async Task SubmitForApproval_RecordsStatusHistory()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        var inject = CreateInject(msel.Id, 1, InjectStatus.Draft, userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.SubmitForApprovalAsync(exercise.Id, inject.Id, userId);

        // Assert
        var history = await context.InjectStatusHistories
            .Where(h => h.InjectId == inject.Id)
            .OrderByDescending(h => h.CreatedAt)
            .FirstOrDefaultAsync();

        history.Should().NotBeNull();
        history!.FromStatus.Should().Be(InjectStatus.Draft);
        history.ToStatus.Should().Be(InjectStatus.Submitted);
        history.ChangedByUserId.Should().Be(userId);
        history.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SubmitForApproval_InjectNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var nonExistentInjectId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.SubmitForApprovalAsync(exercise.Id, nonExistentInjectId, userId));
    }

    #endregion

    #region ApproveInjectAsync Tests

    [Fact]
    public async Task ApproveInject_SubmittedInject_ChangesToApproved()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true; // Enable approval workflow
        await context.SaveChangesAsync(); // Save the exercise changes
        var approverId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Submitted, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.ApproveInjectAsync(exercise.Id, inject.Id, approverId, "Looks good!");

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Approved);
        updated.ApprovedByUserId.Should().Be(approverId);
        updated.ApprovedAt.Should().NotBeNull();
        updated.ApprovedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        updated.ApproverNotes.Should().Be("Looks good!");
        updated.ModifiedBy.Should().Be(approverId);
    }

    [Fact]
    public async Task ApproveInject_WithoutNotes_Succeeds()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var approverId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Submitted, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.ApproveInjectAsync(exercise.Id, inject.Id, approverId, null);

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Approved);
        updated.ApproverNotes.Should().BeNull();
    }

    [Fact]
    public async Task ApproveInject_SelfSubmission_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Submitted, userId);
        inject.SubmittedByUserId = userId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveInjectAsync(exercise.Id, inject.Id, userId, null));

        ex.Message.Should().Contain("Cannot approve your own submission");
    }

    [Fact]
    public async Task ApproveInject_DraftInject_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var approverId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Draft, userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveInjectAsync(exercise.Id, inject.Id, approverId, null));

        ex.Message.Should().Contain("Only Submitted injects can be approved");
    }

    [Fact]
    public async Task ApproveInject_CreatesStatusHistory()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var approverId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Submitted, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.ApproveInjectAsync(exercise.Id, inject.Id, approverId, "Approved!");

        // Assert
        var history = await context.InjectStatusHistories
            .Where(h => h.InjectId == inject.Id)
            .OrderByDescending(h => h.ChangedAt)
            .FirstOrDefaultAsync();

        history.Should().NotBeNull();
        history!.FromStatus.Should().Be(InjectStatus.Submitted);
        history.ToStatus.Should().Be(InjectStatus.Approved);
        history.ChangedByUserId.Should().Be(approverId);
        history.Notes.Should().Be("Approved!");
    }

    [Fact]
    public async Task ApproveInject_ClearsPreviousRejection()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var approverId = Guid.NewGuid().ToString();
        var rejecterId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Submitted, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);
        // Previously rejected
        inject.RejectedByUserId = rejecterId;
        inject.RejectedAt = DateTime.UtcNow.AddMinutes(-20);
        inject.RejectionReason = "Needs more detail";
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.ApproveInjectAsync(exercise.Id, inject.Id, approverId, "Fixed now");

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Approved);
        updated.RejectedByUserId.Should().BeNull();
        updated.RejectedAt.Should().BeNull();
        updated.RejectionReason.Should().BeNull();
    }

    #endregion

    #region RejectInjectAsync Tests

    [Fact]
    public async Task RejectInject_SubmittedInject_ReturnsToDraft()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var rejecterId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Submitted, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.RejectInjectAsync(exercise.Id, inject.Id, rejecterId,
            "Needs more detail on expected actions");

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Draft);
        updated.RejectedByUserId.Should().Be(rejecterId);
        updated.RejectedAt.Should().NotBeNull();
        updated.RejectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        updated.RejectionReason.Should().Be("Needs more detail on expected actions");
        updated.ModifiedBy.Should().Be(rejecterId);
    }

    [Fact]
    public async Task RejectInject_ClearsSubmissionTracking()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var rejecterId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Submitted, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.RejectInjectAsync(exercise.Id, inject.Id, rejecterId, "Please revise");

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.SubmittedByUserId.Should().BeNull();
        updated.SubmittedAt.Should().BeNull();
    }

    [Fact]
    public async Task RejectInject_ShortReason_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var rejecterId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Submitted, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RejectInjectAsync(exercise.Id, inject.Id, rejecterId, "No"));

        ex.Message.Should().Contain("at least 10 characters");
    }

    [Fact]
    public async Task RejectInject_DraftInject_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var rejecterId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Draft, userId);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RejectInjectAsync(exercise.Id, inject.Id, rejecterId, "Valid rejection reason"));

        ex.Message.Should().Contain("Only Submitted injects can be rejected");
    }

    [Fact]
    public async Task RejectInject_CreatesStatusHistory()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var rejecterId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Submitted, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.RejectInjectAsync(exercise.Id, inject.Id, rejecterId,
            "Needs revision per S04 requirements");

        // Assert
        var history = await context.InjectStatusHistories
            .Where(h => h.InjectId == inject.Id)
            .OrderByDescending(h => h.ChangedAt)
            .FirstOrDefaultAsync();

        history.Should().NotBeNull();
        history!.FromStatus.Should().Be(InjectStatus.Submitted);
        history.ToStatus.Should().Be(InjectStatus.Draft);
        history.ChangedByUserId.Should().Be(rejecterId);
        history.Notes.Should().Be("Needs revision per S04 requirements");
    }

    #endregion

    #region BatchApproveAsync Tests

    [Fact]
    public async Task BatchApprove_MultipleSubmittedInjects_ApprovesAll()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var approverId = Guid.NewGuid().ToString();

        var inject1 = CreateInject(msel.Id, 1, InjectStatus.Submitted, submitterId);
        inject1.SubmittedByUserId = submitterId;
        inject1.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);

        var inject2 = CreateInject(msel.Id, 2, InjectStatus.Submitted, submitterId);
        inject2.SubmittedByUserId = submitterId;
        inject2.SubmittedAt = DateTime.UtcNow.AddMinutes(-9);

        var inject3 = CreateInject(msel.Id, 3, InjectStatus.Submitted, submitterId);
        inject3.SubmittedByUserId = submitterId;
        inject3.SubmittedAt = DateTime.UtcNow.AddMinutes(-8);

        context.Injects.AddRange(inject1, inject2, inject3);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var injectIds = new List<Guid> { inject1.Id, inject2.Id, inject3.Id };

        // Act
        var result = await service.BatchApproveAsync(exercise.Id, injectIds, "Batch approved for exercise", approverId);

        // Assert
        result.Should().NotBeNull();
        result.ApprovedCount.Should().Be(3);
        result.SkippedCount.Should().Be(0);
        result.ProcessedInjects.Should().HaveCount(3);

        var updated = await context.Injects.Where(i => injectIds.Contains(i.Id)).ToListAsync();
        updated.Should().AllSatisfy(i =>
        {
            i.Status.Should().Be(InjectStatus.Approved);
            i.ApprovedByUserId.Should().Be(approverId);
            i.ApprovedAt.Should().NotBeNull();
            i.ApproverNotes.Should().Be("Batch approved for exercise");
        });
    }

    [Fact]
    public async Task BatchApprove_WithSelfSubmissions_SkipsSelfAndApprovesOthers()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var approverId = Guid.NewGuid().ToString();
        var otherSubmitterId = Guid.NewGuid().ToString();

        // Inject by other user
        var inject1 = CreateInject(msel.Id, 1, InjectStatus.Submitted, otherSubmitterId);
        inject1.SubmittedByUserId = otherSubmitterId;
        inject1.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);

        // Self-submitted inject (approver is same as submitter)
        var inject2 = CreateInject(msel.Id, 2, InjectStatus.Submitted, approverId);
        inject2.SubmittedByUserId = approverId;
        inject2.SubmittedAt = DateTime.UtcNow.AddMinutes(-9);

        // Another inject by other user
        var inject3 = CreateInject(msel.Id, 3, InjectStatus.Submitted, otherSubmitterId);
        inject3.SubmittedByUserId = otherSubmitterId;
        inject3.SubmittedAt = DateTime.UtcNow.AddMinutes(-8);

        context.Injects.AddRange(inject1, inject2, inject3);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var injectIds = new List<Guid> { inject1.Id, inject2.Id, inject3.Id };

        // Act
        var result = await service.BatchApproveAsync(exercise.Id, injectIds, null, approverId);

        // Assert
        result.ApprovedCount.Should().Be(2, "two injects should be approved (excluding self-submission)");
        result.SkippedCount.Should().Be(1, "one inject should be skipped (self-submission)");
        result.SkippedReasons.Should().HaveCount(1);
        result.SkippedReasons[0].Should().Contain("Cannot approve your own submission");

        var inject1Updated = await context.Injects.FindAsync(inject1.Id);
        inject1Updated!.Status.Should().Be(InjectStatus.Approved);

        var inject2Updated = await context.Injects.FindAsync(inject2.Id);
        inject2Updated!.Status.Should().Be(InjectStatus.Submitted, "self-submission should remain Submitted");

        var inject3Updated = await context.Injects.FindAsync(inject3.Id);
        inject3Updated!.Status.Should().Be(InjectStatus.Approved);
    }

    [Fact]
    public async Task BatchApprove_MixedStatuses_OnlyApprovesSubmitted()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var approverId = Guid.NewGuid().ToString();

        var inject1 = CreateInject(msel.Id, 1, InjectStatus.Submitted, submitterId);
        inject1.SubmittedByUserId = submitterId;
        inject1.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);

        var inject2 = CreateInject(msel.Id, 2, InjectStatus.Draft, submitterId); // Draft

        var inject3 = CreateInject(msel.Id, 3, InjectStatus.Approved, submitterId); // Already approved
        inject3.ApprovedByUserId = approverId;
        inject3.ApprovedAt = DateTime.UtcNow.AddDays(-1);

        context.Injects.AddRange(inject1, inject2, inject3);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var injectIds = new List<Guid> { inject1.Id, inject2.Id, inject3.Id };

        // Act
        var result = await service.BatchApproveAsync(exercise.Id, injectIds, null, approverId);

        // Assert
        result.ApprovedCount.Should().Be(1);
        result.SkippedCount.Should().Be(2);
        result.SkippedReasons.Should().HaveCount(2);
        result.SkippedReasons.Should().Contain(r => r.Contains("Not in Submitted status"));
    }

    [Fact]
    public async Task BatchApprove_EmptyList_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var approverId = Guid.NewGuid().ToString();

        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.BatchApproveAsync(exercise.Id, new List<Guid>(), null, approverId));

        ex.Message.Should().Contain("at least one inject");
    }

    [Fact]
    public async Task BatchApprove_AllSelfSubmissions_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, approverId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();

        // All injects submitted by the same user who will approve
        var inject1 = CreateInject(msel.Id, 1, InjectStatus.Submitted, approverId);
        inject1.SubmittedByUserId = approverId;
        inject1.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);

        var inject2 = CreateInject(msel.Id, 2, InjectStatus.Submitted, approverId);
        inject2.SubmittedByUserId = approverId;
        inject2.SubmittedAt = DateTime.UtcNow.AddMinutes(-9);

        context.Injects.AddRange(inject1, inject2);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var injectIds = new List<Guid> { inject1.Id, inject2.Id };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.BatchApproveAsync(exercise.Id, injectIds, null, approverId));

        ex.Message.Should().Contain("all selected injects were submitted by you");
    }

    [Fact]
    public async Task BatchApprove_CreatesStatusHistoryForEach()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var approverId = Guid.NewGuid().ToString();

        var inject1 = CreateInject(msel.Id, 1, InjectStatus.Submitted, submitterId);
        inject1.SubmittedByUserId = submitterId;
        inject1.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);

        var inject2 = CreateInject(msel.Id, 2, InjectStatus.Submitted, submitterId);
        inject2.SubmittedByUserId = submitterId;
        inject2.SubmittedAt = DateTime.UtcNow.AddMinutes(-9);

        context.Injects.AddRange(inject1, inject2);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var injectIds = new List<Guid> { inject1.Id, inject2.Id };

        // Act
        await service.BatchApproveAsync(exercise.Id, injectIds, "Batch notes", approverId);

        // Assert
        var histories = await context.InjectStatusHistories
            .Where(h => injectIds.Contains(h.InjectId))
            .ToListAsync();

        histories.Should().HaveCount(2);
        histories.Should().AllSatisfy(h =>
        {
            h.FromStatus.Should().Be(InjectStatus.Submitted);
            h.ToStatus.Should().Be(InjectStatus.Approved);
            h.ChangedByUserId.Should().Be(approverId);
            h.Notes.Should().Be("Batch notes");
        });
    }

    #endregion

    #region BatchRejectAsync Tests

    [Fact]
    public async Task BatchReject_MultipleSubmittedInjects_RejectsAll()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var rejecterId = Guid.NewGuid().ToString();

        var inject1 = CreateInject(msel.Id, 1, InjectStatus.Submitted, submitterId);
        inject1.SubmittedByUserId = submitterId;
        inject1.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);

        var inject2 = CreateInject(msel.Id, 2, InjectStatus.Submitted, submitterId);
        inject2.SubmittedByUserId = submitterId;
        inject2.SubmittedAt = DateTime.UtcNow.AddMinutes(-9);

        var inject3 = CreateInject(msel.Id, 3, InjectStatus.Submitted, submitterId);
        inject3.SubmittedByUserId = submitterId;
        inject3.SubmittedAt = DateTime.UtcNow.AddMinutes(-8);

        context.Injects.AddRange(inject1, inject2, inject3);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var injectIds = new List<Guid> { inject1.Id, inject2.Id, inject3.Id };

        // Act
        var result = await service.BatchRejectAsync(exercise.Id, injectIds,
            "Timing needs adjustment per updated timeline", rejecterId);

        // Assert
        result.Should().NotBeNull();
        result.RejectedCount.Should().Be(3);
        result.SkippedCount.Should().Be(0);
        result.ProcessedInjects.Should().HaveCount(3);

        var updated = await context.Injects.Where(i => injectIds.Contains(i.Id)).ToListAsync();
        updated.Should().AllSatisfy(i =>
        {
            i.Status.Should().Be(InjectStatus.Draft);
            i.RejectedByUserId.Should().Be(rejecterId);
            i.RejectedAt.Should().NotBeNull();
            i.RejectionReason.Should().Be("Timing needs adjustment per updated timeline");
            i.SubmittedByUserId.Should().BeNull("submission tracking should be cleared");
            i.SubmittedAt.Should().BeNull("submission tracking should be cleared");
        });
    }

    [Fact]
    public async Task BatchReject_MixedStatuses_OnlyRejectsSubmitted()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var rejecterId = Guid.NewGuid().ToString();

        var inject1 = CreateInject(msel.Id, 1, InjectStatus.Submitted, submitterId);
        inject1.SubmittedByUserId = submitterId;
        inject1.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);

        var inject2 = CreateInject(msel.Id, 2, InjectStatus.Draft, submitterId);

        var inject3 = CreateInject(msel.Id, 3, InjectStatus.Approved, submitterId);

        context.Injects.AddRange(inject1, inject2, inject3);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var injectIds = new List<Guid> { inject1.Id, inject2.Id, inject3.Id };

        // Act
        var result = await service.BatchRejectAsync(exercise.Id, injectIds, "Please revise per HSEEP standards", rejecterId);

        // Assert
        result.RejectedCount.Should().Be(1);
        result.SkippedCount.Should().Be(2);
        result.SkippedReasons.Should().HaveCount(2);
        result.SkippedReasons.Should().Contain(r => r.Contains("Not in Submitted status"));
    }

    [Fact]
    public async Task BatchReject_EmptyList_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var rejecterId = Guid.NewGuid().ToString();

        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.BatchRejectAsync(exercise.Id, new List<Guid>(), "Reason", rejecterId));

        ex.Message.Should().Contain("at least one inject");
    }

    [Fact]
    public async Task BatchReject_ShortReason_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var rejecterId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Submitted, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.BatchRejectAsync(exercise.Id, new List<Guid> { inject.Id }, "Short", rejecterId));

        ex.Message.Should().Contain("at least 10 characters");
    }

    [Fact]
    public async Task BatchReject_CreatesStatusHistoryForEach()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var rejecterId = Guid.NewGuid().ToString();

        var inject1 = CreateInject(msel.Id, 1, InjectStatus.Submitted, submitterId);
        inject1.SubmittedByUserId = submitterId;
        inject1.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);

        var inject2 = CreateInject(msel.Id, 2, InjectStatus.Submitted, submitterId);
        inject2.SubmittedByUserId = submitterId;
        inject2.SubmittedAt = DateTime.UtcNow.AddMinutes(-9);

        context.Injects.AddRange(inject1, inject2);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var injectIds = new List<Guid> { inject1.Id, inject2.Id };

        // Act
        await service.BatchRejectAsync(exercise.Id, injectIds, "All need timing revision per director feedback", rejecterId);

        // Assert
        var histories = await context.InjectStatusHistories
            .Where(h => injectIds.Contains(h.InjectId))
            .ToListAsync();

        histories.Should().HaveCount(2);
        histories.Should().AllSatisfy(h =>
        {
            h.FromStatus.Should().Be(InjectStatus.Submitted);
            h.ToStatus.Should().Be(InjectStatus.Draft);
            h.ChangedByUserId.Should().Be(rejecterId);
            h.Notes.Should().Be("All need timing revision per director feedback");
        });
    }

    #endregion

    #region RevertApprovalAsync Tests

    [Fact]
    public async Task RevertApproval_ApprovedInject_ReturnsToSubmitted()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var approverId = Guid.NewGuid().ToString();
        var directorId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Approved, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-20);
        inject.ApprovedByUserId = approverId;
        inject.ApprovedAt = DateTime.UtcNow.AddMinutes(-10);
        inject.ApproverNotes = "Looks good";
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.RevertApprovalAsync(exercise.Id, inject.Id, directorId,
            "Need to add secondary contact phone number for media inquiries");

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Submitted);
        updated.RevertedByUserId.Should().Be(directorId);
        updated.RevertedAt.Should().NotBeNull();
        updated.RevertedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        updated.RevertReason.Should().Be("Need to add secondary contact phone number for media inquiries");
        updated.ModifiedBy.Should().Be(directorId);
    }

    [Fact]
    public async Task RevertApproval_ClearsApprovalInfo()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var approverId = Guid.NewGuid().ToString();
        var directorId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Approved, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-20);
        inject.ApprovedByUserId = approverId;
        inject.ApprovedAt = DateTime.UtcNow.AddMinutes(-10);
        inject.ApproverNotes = "Approved with minor edits";
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.RevertApprovalAsync(exercise.Id, inject.Id, directorId, "Needs revision after SME review");

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.ApprovedByUserId.Should().BeNull();
        updated.ApprovedAt.Should().BeNull();
        updated.ApproverNotes.Should().BeNull();
    }

    [Fact]
    public async Task RevertApproval_ResetSubmissionTracking()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var approverId = Guid.NewGuid().ToString();
        var directorId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Approved, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-20);
        inject.ApprovedByUserId = approverId;
        inject.ApprovedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.RevertApprovalAsync(exercise.Id, inject.Id, directorId, "Revert for changes");

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.SubmittedByUserId.Should().Be(submitterId, "submitter should be preserved");
        updated.SubmittedAt.Should().NotBeNull("submission timestamp should be reset");
        updated.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RevertApproval_NotApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var directorId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Submitted, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RevertApprovalAsync(exercise.Id, inject.Id, directorId, "Some reason"));

        ex.Message.Should().Contain("Only Approved injects can be reverted");
    }

    [Fact]
    public async Task RevertApproval_ShortReason_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var approverId = Guid.NewGuid().ToString();
        var directorId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Approved, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-20);
        inject.ApprovedByUserId = approverId;
        inject.ApprovedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RevertApprovalAsync(exercise.Id, inject.Id, directorId, "Short"));

        ex.Message.Should().Contain("at least 10 characters");
    }

    [Fact]
    public async Task RevertApproval_EmptyReason_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var approverId = Guid.NewGuid().ToString();
        var directorId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Approved, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-20);
        inject.ApprovedByUserId = approverId;
        inject.ApprovedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RevertApprovalAsync(exercise.Id, inject.Id, directorId, ""));

        ex.Message.Should().Contain("Revert reason is required");
    }

    [Fact]
    public async Task RevertApproval_CreatesStatusHistory()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var approverId = Guid.NewGuid().ToString();
        var directorId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Approved, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-20);
        inject.ApprovedByUserId = approverId;
        inject.ApprovedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.RevertApprovalAsync(exercise.Id, inject.Id, directorId,
            "After reviewing related injects, this needs to include the secondary contact phone number");

        // Assert
        var history = await context.InjectStatusHistories
            .Where(h => h.InjectId == inject.Id)
            .OrderByDescending(h => h.ChangedAt)
            .FirstOrDefaultAsync();

        history.Should().NotBeNull();
        history!.FromStatus.Should().Be(InjectStatus.Approved);
        history.ToStatus.Should().Be(InjectStatus.Submitted);
        history.ChangedByUserId.Should().Be(directorId);
        history.Notes.Should().Contain("Approval reverted");
        history.Notes.Should().Contain("After reviewing related injects, this needs to include the secondary contact phone number");
    }

    [Fact]
    public async Task RevertApproval_SynchronizedStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var directorId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Synchronized, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-30);
        inject.ApprovedByUserId = directorId;
        inject.ApprovedAt = DateTime.UtcNow.AddMinutes(-20);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RevertApprovalAsync(exercise.Id, inject.Id, directorId, "Cannot revert synchronized inject"));

        ex.Message.Should().Contain("Only Approved injects can be reverted");
    }

    [Fact]
    public async Task RevertApproval_ReleasedStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise, msel, submitterId) = CreateTestContext();
        exercise.RequireInjectApproval = true;
        await context.SaveChangesAsync();
        var directorId = Guid.NewGuid().ToString();

        var inject = CreateInject(msel.Id, 1, InjectStatus.Released, submitterId);
        inject.SubmittedByUserId = submitterId;
        inject.SubmittedAt = DateTime.UtcNow.AddMinutes(-40);
        inject.ApprovedByUserId = directorId;
        inject.ApprovedAt = DateTime.UtcNow.AddMinutes(-30);
        inject.FiredAt = DateTime.UtcNow.AddMinutes(-10);
        inject.FiredByUserId = directorId;
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RevertApprovalAsync(exercise.Id, inject.Id, directorId, "Cannot revert released inject"));

        ex.Message.Should().Contain("Only Approved injects can be reverted");
    }

    #endregion
}
