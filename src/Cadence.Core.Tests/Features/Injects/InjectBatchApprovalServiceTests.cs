using Cadence.Core.Data;
using Cadence.Core.Features.Injects.Services;
using Cadence.Core.Features.Notifications.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Injects;

/// <summary>
/// Tests for <see cref="InjectBatchApprovalService"/> — batch approval and rejection of injects.
/// </summary>
public class InjectBatchApprovalServiceTests
{
    private readonly Mock<IApprovalNotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<InjectBatchApprovalService>> _loggerMock;

    public InjectBatchApprovalServiceTests()
    {
        _notificationServiceMock = new Mock<IApprovalNotificationService>();
        _loggerMock = new Mock<ILogger<InjectBatchApprovalService>>();

        // Default: notification service calls are no-ops
        _notificationServiceMock
            .Setup(x => x.NotifyBatchApprovedAsync(
                It.IsAny<string>(),
                It.IsAny<List<Inject>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _notificationServiceMock
            .Setup(x => x.NotifyBatchRejectedAsync(
                It.IsAny<string>(),
                It.IsAny<List<Inject>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private (AppDbContext context, Organization org, Exercise exercise, Msel msel, string userId)
        CreateTestContext(
            bool requireApproval = true,
            SelfApprovalPolicy selfApprovalPolicy = SelfApprovalPolicy.NeverAllowed)
    {
        var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            SelfApprovalPolicy = selfApprovalPolicy,
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
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            RequireInjectApproval = requireApproval,
            CreatedBy = userId,
            ModifiedBy = userId
        };

        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Version = 1,
            IsActive = true,
            ExerciseId = exercise.Id,
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
        AppDbContext context,
        Msel msel,
        int injectNumber = 1,
        InjectStatus status = InjectStatus.Submitted,
        string? submittedByUserId = null)
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
            SubmittedByUserId = submittedByUserId,
            SubmittedAt = submittedByUserId != null ? DateTime.UtcNow.AddMinutes(-5) : null,
            CreatedBy = submittedByUserId ?? "test-user",
            ModifiedBy = submittedByUserId ?? "test-user"
        };
        context.Injects.Add(inject);
        context.SaveChanges();
        return inject;
    }

    private InjectBatchApprovalService CreateService(AppDbContext context) =>
        new InjectBatchApprovalService(context, _notificationServiceMock.Object, _loggerMock.Object);

    // =========================================================================
    // BatchApproveAsync — Validation Tests
    // =========================================================================

    [Fact]
    public async Task BatchApprove_EmptyInjectIds_ThrowsInvalidOperation()
    {
        // Arrange
        var (context, _, exercise, _, userId) = CreateTestContext();
        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.BatchApproveAsync(exercise.Id, Enumerable.Empty<Guid>(), null, userId));
    }

    [Fact]
    public async Task BatchApprove_ExerciseNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (context, _, _, _, userId) = CreateTestContext();
        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.BatchApproveAsync(Guid.NewGuid(), new[] { Guid.NewGuid() }, null, userId));
    }

    [Fact]
    public async Task BatchApprove_ApprovalNotEnabled_ThrowsInvalidOperation()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext(requireApproval: false);
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted);
        var sut = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.BatchApproveAsync(exercise.Id, new[] { inject.Id }, null, userId));

        ex.Message.Should().Contain("approval workflow is not enabled");
    }

    [Fact]
    public async Task BatchApprove_NoActiveMsel_ThrowsInvalidOperation()
    {
        // Arrange
        var (context, _, exercise, _, userId) = CreateTestContext();
        exercise.ActiveMselId = null;
        context.SaveChanges();
        var sut = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.BatchApproveAsync(exercise.Id, new[] { Guid.NewGuid() }, null, userId));

        ex.Message.Should().Contain("no active MSEL");
    }

    // =========================================================================
    // BatchApproveAsync — Happy Path
    // =========================================================================

    [Fact]
    public async Task BatchApprove_SubmittedInjects_ApprovesAllAndReturnsCount()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var approverUserId = Guid.NewGuid().ToString();

        var inject1 = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);
        var inject2 = CreateInject(context, msel, 2, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // Act
        var result = await sut.BatchApproveAsync(
            exercise.Id,
            new[] { inject1.Id, inject2.Id },
            notes: "Approved",
            userId: approverUserId);

        // Assert
        result.ApprovedCount.Should().Be(2);
        result.SkippedCount.Should().Be(0);
    }

    [Fact]
    public async Task BatchApprove_SubmittedInject_SetsApprovedStatus()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var approverUserId = Guid.NewGuid().ToString();
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // Act
        await sut.BatchApproveAsync(exercise.Id, new[] { inject.Id }, notes: null, userId: approverUserId);

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated!.Status.Should().Be(InjectStatus.Approved);
        updated.ApprovedByUserId.Should().Be(approverUserId);
        updated.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task BatchApprove_WithNotes_SetsApproverNotesOnAllInjects()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var approverUserId = Guid.NewGuid().ToString();
        var inject1 = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);
        var inject2 = CreateInject(context, msel, 2, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // Act
        await sut.BatchApproveAsync(
            exercise.Id,
            new[] { inject1.Id, inject2.Id },
            notes: "LGTM",
            userId: approverUserId);

        // Assert
        var updated1 = await context.Injects.FindAsync(inject1.Id);
        var updated2 = await context.Injects.FindAsync(inject2.Id);
        updated1!.ApproverNotes.Should().Be("LGTM");
        updated2!.ApproverNotes.Should().Be("LGTM");
    }

    [Fact]
    public async Task BatchApprove_ClearsPreviousRejectionData()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var approverUserId = Guid.NewGuid().ToString();
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);

        // Simulate a previously-rejected then re-submitted inject
        inject.RejectedByUserId = Guid.NewGuid().ToString();
        inject.RejectedAt = DateTime.UtcNow.AddHours(-1);
        inject.RejectionReason = "Old rejection reason";
        context.SaveChanges();

        var sut = CreateService(context);

        // Act
        await sut.BatchApproveAsync(exercise.Id, new[] { inject.Id }, notes: null, userId: approverUserId);

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated!.RejectionReason.Should().BeNull("rejection data should be cleared on approval");
        updated.RejectedByUserId.Should().BeNull();
        updated.RejectedAt.Should().BeNull();
    }

    [Fact]
    public async Task BatchApprove_CreatesStatusHistoryForEachApprovedInject()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var approverUserId = Guid.NewGuid().ToString();
        var inject1 = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);
        var inject2 = CreateInject(context, msel, 2, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // Act
        await sut.BatchApproveAsync(
            exercise.Id,
            new[] { inject1.Id, inject2.Id },
            notes: "Batch approved",
            userId: approverUserId);

        // Assert
        var histories = await context.InjectStatusHistories
            .Where(h => h.InjectId == inject1.Id || h.InjectId == inject2.Id)
            .ToListAsync();

        histories.Should().HaveCount(2);
        histories.Should().OnlyContain(h =>
            h.FromStatus == InjectStatus.Submitted &&
            h.ToStatus == InjectStatus.Approved &&
            h.ChangedByUserId == approverUserId);
    }

    [Fact]
    public async Task BatchApprove_ReturnsDtosOfApprovedInjects()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var approverUserId = Guid.NewGuid().ToString();
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // Act
        var result = await sut.BatchApproveAsync(exercise.Id, new[] { inject.Id }, null, approverUserId);

        // Assert
        result.ProcessedInjects.Should().HaveCount(1);
        result.ProcessedInjects[0].Id.Should().Be(inject.Id);
        result.ProcessedInjects[0].Status.Should().Be(InjectStatus.Approved);
    }

    [Fact]
    public async Task BatchApprove_CallsNotificationService()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var approverUserId = Guid.NewGuid().ToString();
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // Act
        await sut.BatchApproveAsync(exercise.Id, new[] { inject.Id }, notes: "Done", userId: approverUserId);

        // Assert
        _notificationServiceMock.Verify(x => x.NotifyBatchApprovedAsync(
            approverUserId,
            It.Is<List<Inject>>(list => list.Count == 1),
            "Done",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // BatchApproveAsync — Non-Submitted Inject Skipping
    // =========================================================================

    [Fact]
    public async Task BatchApprove_NonSubmittedInject_IsSkipped()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var approverUserId = Guid.NewGuid().ToString();

        var submittedInject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);
        var draftInject = CreateInject(context, msel, 2, InjectStatus.Draft);

        var sut = CreateService(context);

        // Act
        var result = await sut.BatchApproveAsync(
            exercise.Id,
            new[] { submittedInject.Id, draftInject.Id },
            notes: null,
            userId: approverUserId);

        // Assert
        result.ApprovedCount.Should().Be(1);
        result.SkippedCount.Should().Be(1);
        result.SkippedReasons.Should().HaveCount(1);
        result.SkippedReasons[0].Should().Contain("Not in Submitted status");
    }

    [Fact]
    public async Task BatchApprove_AlreadyApprovedInject_IsSkipped()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var approverUserId = Guid.NewGuid().ToString();
        var inject = CreateInject(context, msel, 1, InjectStatus.Approved);

        // Need at least one valid inject to avoid the "all skipped" error
        var validInject = CreateInject(context, msel, 2, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // Act
        var result = await sut.BatchApproveAsync(
            exercise.Id,
            new[] { inject.Id, validInject.Id },
            notes: null,
            userId: approverUserId);

        // Assert
        result.SkippedCount.Should().Be(1);
        result.SkippedReasons.Should().HaveCount(1);
        result.SkippedReasons[0].Should().Contain("Not in Submitted status");
    }

    [Fact]
    public async Task BatchApprove_AllInjectsSkipped_ThrowsInvalidOperation()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var draftInject = CreateInject(context, msel, 1, InjectStatus.Draft);

        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.BatchApproveAsync(exercise.Id, new[] { draftInject.Id }, null, userId));
    }

    // =========================================================================
    // BatchApproveAsync — Self-Approval Policy
    // =========================================================================

    [Fact]
    public async Task BatchApprove_SelfSubmission_PolicyNeverAllowed_InjectIsSkipped()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext(
            selfApprovalPolicy: SelfApprovalPolicy.NeverAllowed);

        // Another inject submitted by a different user to prevent all-skipped error
        var otherUserId = Guid.NewGuid().ToString();
        var selfInject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);
        var otherInject = CreateInject(context, msel, 2, InjectStatus.Submitted, submittedByUserId: otherUserId);

        var sut = CreateService(context);

        // Act
        var result = await sut.BatchApproveAsync(
            exercise.Id,
            new[] { selfInject.Id, otherInject.Id },
            notes: null,
            userId: userId);

        // Assert
        result.SkippedCount.Should().Be(1);
        result.ApprovedCount.Should().Be(1);
        result.SkippedReasons[0].Should().Contain("self-approval not permitted");
    }

    [Fact]
    public async Task BatchApprove_SelfSubmission_PolicyAlwaysAllowed_InjectIsApproved()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext(
            selfApprovalPolicy: SelfApprovalPolicy.AlwaysAllowed);

        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);
        var sut = CreateService(context);

        // Act
        var result = await sut.BatchApproveAsync(
            exercise.Id,
            new[] { inject.Id },
            notes: null,
            userId: userId);

        // Assert
        result.ApprovedCount.Should().Be(1);
        result.SkippedCount.Should().Be(0);
    }

    [Fact]
    public async Task BatchApprove_SelfSubmission_PolicyAllowedWithWarning_InjectIsSkipped()
    {
        // Arrange - batch approval skips AllowedWithWarning self-submissions
        // (individual approval flow handles the confirmation dialog)
        var (context, _, exercise, msel, userId) = CreateTestContext(
            selfApprovalPolicy: SelfApprovalPolicy.AllowedWithWarning);

        var selfInject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);
        var otherUserId = Guid.NewGuid().ToString();
        var otherInject = CreateInject(context, msel, 2, InjectStatus.Submitted, submittedByUserId: otherUserId);

        var sut = CreateService(context);

        // Act
        var result = await sut.BatchApproveAsync(
            exercise.Id,
            new[] { selfInject.Id, otherInject.Id },
            notes: null,
            userId: userId);

        // Assert
        result.SkippedCount.Should().Be(1, "self-approval with warning requires individual confirmation");
        result.SkippedReasons[0].Should().Contain("individual confirmation");
        result.ApprovedCount.Should().Be(1);
    }

    [Fact]
    public async Task BatchApprove_AllSelfSubmissions_PolicyNeverAllowed_ThrowsInvalidOperation()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext(
            selfApprovalPolicy: SelfApprovalPolicy.NeverAllowed);

        var inject1 = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);
        var inject2 = CreateInject(context, msel, 2, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.BatchApproveAsync(exercise.Id, new[] { inject1.Id, inject2.Id }, null, userId));
    }

    [Fact]
    public async Task BatchApprove_InjectNotInActiveMsel_IsNotIncluded()
    {
        // Arrange
        var (context, org, exercise, msel, userId) = CreateTestContext();
        var approverUserId = Guid.NewGuid().ToString();

        // Create an inject in a DIFFERENT msel
        var otherMsel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Other MSEL",
            Version = 2,
            IsActive = false,
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Msels.Add(otherMsel);
        context.SaveChanges();

        var injectInOtherMsel = CreateInject(context, otherMsel, 1, InjectStatus.Submitted, submittedByUserId: userId);
        var injectInActiveMsel = CreateInject(context, msel, 2, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // Act
        var result = await sut.BatchApproveAsync(
            exercise.Id,
            new[] { injectInOtherMsel.Id, injectInActiveMsel.Id },
            notes: null,
            userId: approverUserId);

        // Assert
        result.ApprovedCount.Should().Be(1, "only injects in the active MSEL should be processed");
    }

    // =========================================================================
    // BatchRejectAsync — Validation Tests
    // =========================================================================

    [Fact]
    public async Task BatchReject_EmptyInjectIds_ThrowsInvalidOperation()
    {
        // Arrange
        var (context, _, exercise, _, userId) = CreateTestContext();
        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.BatchRejectAsync(exercise.Id, Enumerable.Empty<Guid>(), "Rejection reason here", userId));
    }

    [Fact]
    public async Task BatchReject_EmptyReason_ThrowsInvalidOperation()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted);
        var sut = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.BatchRejectAsync(exercise.Id, new[] { inject.Id }, "", userId));

        ex.Message.Should().Contain("Rejection reason is required");
    }

    [Fact]
    public async Task BatchReject_WhitespaceReason_ThrowsInvalidOperation()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted);
        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.BatchRejectAsync(exercise.Id, new[] { inject.Id }, "   ", userId));
    }

    [Fact]
    public async Task BatchReject_ReasonTooShort_ThrowsInvalidOperation()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted);
        var sut = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.BatchRejectAsync(exercise.Id, new[] { inject.Id }, "Too short", userId));

        ex.Message.Should().Contain("at least 10 characters");
    }

    [Fact]
    public async Task BatchReject_ReasonTooLong_ThrowsInvalidOperation()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted);
        var sut = CreateService(context);

        var tooLongReason = new string('x', 1001);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.BatchRejectAsync(exercise.Id, new[] { inject.Id }, tooLongReason, userId));

        ex.Message.Should().Contain("1000 characters or less");
    }

    [Fact]
    public async Task BatchReject_ExerciseNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (context, _, _, _, userId) = CreateTestContext();
        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.BatchRejectAsync(Guid.NewGuid(), new[] { Guid.NewGuid() }, "Rejection reason long enough", userId));
    }

    [Fact]
    public async Task BatchReject_ApprovalNotEnabled_ThrowsInvalidOperation()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext(requireApproval: false);
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted);
        var sut = CreateService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.BatchRejectAsync(exercise.Id, new[] { inject.Id }, "Rejection reason long enough", userId));

        ex.Message.Should().Contain("approval workflow is not enabled");
    }

    [Fact]
    public async Task BatchReject_NoActiveMsel_ThrowsInvalidOperation()
    {
        // Arrange
        var (context, _, exercise, _, userId) = CreateTestContext();
        exercise.ActiveMselId = null;
        context.SaveChanges();
        var sut = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.BatchRejectAsync(exercise.Id, new[] { Guid.NewGuid() }, "Rejection reason long enough", userId));
    }

    // =========================================================================
    // BatchRejectAsync — Happy Path
    // =========================================================================

    [Fact]
    public async Task BatchReject_SubmittedInjects_RejectsAllAndReturnsCount()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject1 = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);
        var inject2 = CreateInject(context, msel, 2, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // Act
        var result = await sut.BatchRejectAsync(
            exercise.Id,
            new[] { inject1.Id, inject2.Id },
            reason: "Insufficient detail provided",
            userId: userId);

        // Assert
        result.RejectedCount.Should().Be(2);
        result.SkippedCount.Should().Be(0);
    }

    [Fact]
    public async Task BatchReject_SubmittedInject_SetsStatusToDraft()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // Act
        await sut.BatchRejectAsync(
            exercise.Id,
            new[] { inject.Id },
            reason: "Needs more detail here",
            userId: userId);

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated!.Status.Should().Be(InjectStatus.Draft, "rejected injects return to Draft status");
        updated.RejectedByUserId.Should().Be(userId);
        updated.RejectedAt.Should().NotBeNull();
        updated.RejectionReason.Should().Be("Needs more detail here");
    }

    [Fact]
    public async Task BatchReject_ClearsSubmissionTracking()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // Act
        await sut.BatchRejectAsync(
            exercise.Id,
            new[] { inject.Id },
            reason: "Insufficient detail provided",
            userId: userId);

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated!.SubmittedByUserId.Should().BeNull("submission tracking is cleared on rejection so it can be re-set on resubmit");
        updated.SubmittedAt.Should().BeNull();
    }

    [Fact]
    public async Task BatchReject_CreatesStatusHistoryForEachRejectedInject()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject1 = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);
        var inject2 = CreateInject(context, msel, 2, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // Act
        await sut.BatchRejectAsync(
            exercise.Id,
            new[] { inject1.Id, inject2.Id },
            reason: "Missing required information",
            userId: userId);

        // Assert
        var histories = await context.InjectStatusHistories
            .Where(h => h.InjectId == inject1.Id || h.InjectId == inject2.Id)
            .ToListAsync();

        histories.Should().HaveCount(2);
        histories.Should().OnlyContain(h =>
            h.FromStatus == InjectStatus.Submitted &&
            h.ToStatus == InjectStatus.Draft &&
            h.ChangedByUserId == userId);
    }

    [Fact]
    public async Task BatchReject_StatusHistoryIncludesRejectionReason()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // Act
        await sut.BatchRejectAsync(
            exercise.Id,
            new[] { inject.Id },
            reason: "Content needs revision",
            userId: userId);

        // Assert
        var history = await context.InjectStatusHistories
            .Where(h => h.InjectId == inject.Id)
            .SingleAsync();

        history.Notes.Should().Be("Content needs revision");
    }

    [Fact]
    public async Task BatchReject_ReturnsDtosOfRejectedInjects()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // Act
        var result = await sut.BatchRejectAsync(
            exercise.Id,
            new[] { inject.Id },
            reason: "Content needs revision",
            userId: userId);

        // Assert
        result.ProcessedInjects.Should().HaveCount(1);
        result.ProcessedInjects[0].Id.Should().Be(inject.Id);
        result.ProcessedInjects[0].Status.Should().Be(InjectStatus.Draft);
    }

    [Fact]
    public async Task BatchReject_CallsNotificationService()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // Act
        await sut.BatchRejectAsync(
            exercise.Id,
            new[] { inject.Id },
            reason: "Content needs revision",
            userId: userId);

        // Assert
        _notificationServiceMock.Verify(x => x.NotifyBatchRejectedAsync(
            userId,
            It.Is<List<Inject>>(list => list.Count == 1),
            "Content needs revision",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // BatchRejectAsync — Non-Submitted Inject Skipping
    // =========================================================================

    [Fact]
    public async Task BatchReject_NonSubmittedInject_IsSkipped()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var submittedInject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);
        var approvedInject = CreateInject(context, msel, 2, InjectStatus.Approved);

        var sut = CreateService(context);

        // Act
        var result = await sut.BatchRejectAsync(
            exercise.Id,
            new[] { submittedInject.Id, approvedInject.Id },
            reason: "Content needs revision",
            userId: userId);

        // Assert
        result.RejectedCount.Should().Be(1);
        result.SkippedCount.Should().Be(1);
        result.SkippedReasons.Should().HaveCount(1);
        result.SkippedReasons[0].Should().Contain("Not in Submitted status");
    }

    [Fact]
    public async Task BatchReject_DraftInject_IsSkipped()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var submittedInject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);
        var draftInject = CreateInject(context, msel, 2, InjectStatus.Draft);

        var sut = CreateService(context);

        // Act
        var result = await sut.BatchRejectAsync(
            exercise.Id,
            new[] { submittedInject.Id, draftInject.Id },
            reason: "Needs more context here",
            userId: userId);

        // Assert
        result.RejectedCount.Should().Be(1);
        result.SkippedCount.Should().Be(1);
    }

    [Fact]
    public async Task BatchReject_SkippedInjectStatusMessageIncludesInjectNumber()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var submittedInject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);
        var draftInject = CreateInject(context, msel, 5, InjectStatus.Draft);

        var sut = CreateService(context);

        // Act
        var result = await sut.BatchRejectAsync(
            exercise.Id,
            new[] { submittedInject.Id, draftInject.Id },
            reason: "Needs more context here",
            userId: userId);

        // Assert
        result.SkippedReasons[0].Should().Contain("INJ-005", "skipped reasons should include the formatted inject number");
    }

    [Fact]
    public async Task BatchReject_ReasonAtExactMinimumLength_IsAccepted()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);

        // 10 chars exactly
        var minLengthReason = "1234567890";

        // Act
        var result = await sut.BatchRejectAsync(
            exercise.Id,
            new[] { inject.Id },
            reason: minLengthReason,
            userId: userId);

        // Assert
        result.RejectedCount.Should().Be(1);
    }

    [Fact]
    public async Task BatchReject_ReasonAtExactMaximumLength_IsAccepted()
    {
        // Arrange
        var (context, _, exercise, msel, userId) = CreateTestContext();
        var inject = CreateInject(context, msel, 1, InjectStatus.Submitted, submittedByUserId: userId);

        var sut = CreateService(context);
        var maxLengthReason = new string('x', 1000);

        // Act
        var result = await sut.BatchRejectAsync(
            exercise.Id,
            new[] { inject.Id },
            reason: maxLengthReason,
            userId: userId);

        // Assert
        result.RejectedCount.Should().Be(1);
    }
}
