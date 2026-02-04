using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Exercises;

/// <summary>
/// Tests for ExerciseApprovalSettingsService (S02: Exercise Approval Configuration).
/// </summary>
public class ExerciseApprovalSettingsServiceTests
{
    private readonly Mock<ICurrentOrganizationContext> _orgContextMock;
    private readonly Mock<ILogger<ExerciseApprovalSettingsService>> _loggerMock;

    public ExerciseApprovalSettingsServiceTests()
    {
        _orgContextMock = new Mock<ICurrentOrganizationContext>();
        _loggerMock = new Mock<ILogger<ExerciseApprovalSettingsService>>();
    }

    private (AppDbContext context, Organization org) CreateTestContext(
        ApprovalPolicy policy = ApprovalPolicy.Optional)
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            Slug = "test-org",
            InjectApprovalPolicy = policy,
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
        bool requireApproval = false)
    {
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            RequireInjectApproval = requireApproval,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();

        return exercise;
    }

    private (Msel msel, Inject inject) CreateInjectWithStatus(
        AppDbContext context,
        Exercise exercise,
        InjectStatus status)
    {
        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Version = 1,
            ExerciseId = exercise.Id,
            IsActive = true,
            OrganizationId = exercise.OrganizationId,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Msels.Add(msel);

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
            Status = status,
            Sequence = 1,
            MselId = msel.Id,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Injects.Add(inject);
        context.SaveChanges();

        return (msel, inject);
    }

    private ExerciseApprovalSettingsService CreateService(AppDbContext context)
    {
        return new ExerciseApprovalSettingsService(
            context,
            _orgContextMock.Object,
            _loggerMock.Object);
    }

    #region Policy: Optional (Director Can Toggle)

    [Fact]
    public async Task UpdateApprovalSettingsAsync_OptionalPolicy_CanEnableApproval()
    {
        // Arrange
        var (context, org) = CreateTestContext(ApprovalPolicy.Optional);
        var exercise = CreateExercise(context, org, requireApproval: false);
        var service = CreateService(context);

        _orgContextMock.Setup(x => x.IsSysAdmin).Returns(false);

        var request = new UpdateApprovalSettingsRequest(
            RequireInjectApproval: true);

        // Act
        var result = await service.UpdateApprovalSettingsAsync(
            exercise.Id,
            request,
            userId: "director-123");

        // Assert
        result.Should().NotBeNull();
        result.RequireInjectApproval.Should().BeTrue();
        result.ApprovalPolicyOverridden.Should().BeFalse();

        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.RequireInjectApproval.Should().BeTrue();
        updated.ApprovalPolicyOverridden.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateApprovalSettingsAsync_OptionalPolicy_CanDisableApproval()
    {
        // Arrange
        var (context, org) = CreateTestContext(ApprovalPolicy.Optional);
        var exercise = CreateExercise(context, org, requireApproval: true);
        var service = CreateService(context);

        _orgContextMock.Setup(x => x.IsSysAdmin).Returns(false);

        var request = new UpdateApprovalSettingsRequest(
            RequireInjectApproval: false);

        // Act
        var result = await service.UpdateApprovalSettingsAsync(
            exercise.Id,
            request,
            userId: "director-123");

        // Assert
        result.Should().NotBeNull();
        result.RequireInjectApproval.Should().BeFalse();

        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.RequireInjectApproval.Should().BeFalse();
    }

    #endregion

    #region Policy: Required (Director Cannot Disable)

    [Fact]
    public async Task UpdateApprovalSettingsAsync_RequiredPolicy_NonAdminCannotDisable()
    {
        // Arrange
        var (context, org) = CreateTestContext(ApprovalPolicy.Required);
        var exercise = CreateExercise(context, org, requireApproval: true);
        var service = CreateService(context);

        _orgContextMock.Setup(x => x.IsSysAdmin).Returns(false);

        var request = new UpdateApprovalSettingsRequest(
            RequireInjectApproval: false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateApprovalSettingsAsync(
                exercise.Id,
                request,
                userId: "director-123"));

        // Verify no changes
        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.RequireInjectApproval.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateApprovalSettingsAsync_RequiredPolicy_AdminCanOverride()
    {
        // Arrange
        var (context, org) = CreateTestContext(ApprovalPolicy.Required);
        var exercise = CreateExercise(context, org, requireApproval: true);
        var service = CreateService(context);

        _orgContextMock.Setup(x => x.IsSysAdmin).Returns(true);

        var request = new UpdateApprovalSettingsRequest(
            RequireInjectApproval: false,
            IsOverride: true,
            OverrideReason: "Training exercise for new staff");

        // Act
        var result = await service.UpdateApprovalSettingsAsync(
            exercise.Id,
            request,
            userId: "admin-123");

        // Assert
        result.Should().NotBeNull();
        result.RequireInjectApproval.Should().BeFalse();
        result.ApprovalPolicyOverridden.Should().BeTrue();
        result.ApprovalOverrideReason.Should().Be("Training exercise for new staff");
        result.ApprovalOverriddenById.Should().Be("admin-123");
        result.ApprovalOverriddenAt.Should().NotBeNull();
        result.ApprovalOverriddenAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.RequireInjectApproval.Should().BeFalse();
        updated.ApprovalPolicyOverridden.Should().BeTrue();
        updated.ApprovalOverrideReason.Should().Be("Training exercise for new staff");
        updated.ApprovalOverriddenById.Should().Be("admin-123");
        updated.ApprovalOverriddenAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateApprovalSettingsAsync_RequiredPolicy_AdminCanRestorePolicy()
    {
        // Arrange
        var (context, org) = CreateTestContext(ApprovalPolicy.Required);
        var exercise = CreateExercise(context, org, requireApproval: false);
        exercise.ApprovalPolicyOverridden = true;
        exercise.ApprovalOverrideReason = "Previous override";
        exercise.ApprovalOverriddenById = "admin-123";
        exercise.ApprovalOverriddenAt = DateTime.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        _orgContextMock.Setup(x => x.IsSysAdmin).Returns(true);

        var request = new UpdateApprovalSettingsRequest(
            RequireInjectApproval: true);

        // Act
        var result = await service.UpdateApprovalSettingsAsync(
            exercise.Id,
            request,
            userId: "admin-456");

        // Assert
        result.Should().NotBeNull();
        result.RequireInjectApproval.Should().BeTrue();
        result.ApprovalPolicyOverridden.Should().BeFalse();
        result.ApprovalOverrideReason.Should().BeNull();
        result.ApprovalOverriddenById.Should().BeNull();
        result.ApprovalOverriddenAt.Should().BeNull();

        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.RequireInjectApproval.Should().BeTrue();
        updated.ApprovalPolicyOverridden.Should().BeFalse();
        updated.ApprovalOverrideReason.Should().BeNull();
        updated.ApprovalOverriddenById.Should().BeNull();
        updated.ApprovalOverriddenAt.Should().BeNull();
    }

    #endregion

    #region Policy: Disabled (Approval Cannot Be Enabled)

    [Fact]
    public async Task UpdateApprovalSettingsAsync_DisabledPolicy_CannotEnableApproval()
    {
        // Arrange
        var (context, org) = CreateTestContext(ApprovalPolicy.Disabled);
        var exercise = CreateExercise(context, org, requireApproval: false);
        var service = CreateService(context);

        _orgContextMock.Setup(x => x.IsSysAdmin).Returns(true);

        var request = new UpdateApprovalSettingsRequest(
            RequireInjectApproval: true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateApprovalSettingsAsync(
                exercise.Id,
                request,
                userId: "admin-123"));

        // Verify no changes
        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.RequireInjectApproval.Should().BeFalse();
    }

    #endregion

    #region Disabling Approval with Pending Injects

    [Fact]
    public async Task UpdateApprovalSettingsAsync_DisablingApproval_AutoApprovesSubmittedInjects()
    {
        // Arrange
        var (context, org) = CreateTestContext(ApprovalPolicy.Optional);
        var exercise = CreateExercise(context, org, requireApproval: true);
        var (msel, inject) = CreateInjectWithStatus(context, exercise, InjectStatus.Submitted);
        var service = CreateService(context);

        _orgContextMock.Setup(x => x.IsSysAdmin).Returns(false);

        var request = new UpdateApprovalSettingsRequest(
            RequireInjectApproval: false);

        // Act
        var result = await service.UpdateApprovalSettingsAsync(
            exercise.Id,
            request,
            userId: "director-123");

        // Assert
        result.RequireInjectApproval.Should().BeFalse();

        var updatedInject = await context.Injects.FindAsync(inject.Id);
        updatedInject!.Status.Should().Be(InjectStatus.Approved);
        updatedInject.ApprovedAt.Should().NotBeNull();
        updatedInject.ApprovedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        // ApprovedByUserId should be null to indicate auto-approval
        updatedInject.ApprovedByUserId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateApprovalSettingsAsync_DisablingApproval_KeepsApprovedInjects()
    {
        // Arrange
        var (context, org) = CreateTestContext(ApprovalPolicy.Optional);
        var exercise = CreateExercise(context, org, requireApproval: true);
        var (msel, inject) = CreateInjectWithStatus(context, exercise, InjectStatus.Approved);
        inject.ApprovedByUserId = "director-123";
        inject.ApprovedAt = DateTime.UtcNow.AddHours(-1);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        _orgContextMock.Setup(x => x.IsSysAdmin).Returns(false);

        var request = new UpdateApprovalSettingsRequest(
            RequireInjectApproval: false);

        // Act
        await service.UpdateApprovalSettingsAsync(
            exercise.Id,
            request,
            userId: "director-123");

        // Assert
        var updatedInject = await context.Injects.FindAsync(inject.Id);
        updatedInject!.Status.Should().Be(InjectStatus.Approved);
        updatedInject.ApprovedByUserId.Should().Be("director-123");
        updatedInject.ApprovedAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(-1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateApprovalSettingsAsync_DisablingApproval_KeepsDraftInjects()
    {
        // Arrange
        var (context, org) = CreateTestContext(ApprovalPolicy.Optional);
        var exercise = CreateExercise(context, org, requireApproval: true);
        var (msel, inject) = CreateInjectWithStatus(context, exercise, InjectStatus.Draft);
        var service = CreateService(context);

        _orgContextMock.Setup(x => x.IsSysAdmin).Returns(false);

        var request = new UpdateApprovalSettingsRequest(
            RequireInjectApproval: false);

        // Act
        await service.UpdateApprovalSettingsAsync(
            exercise.Id,
            request,
            userId: "director-123");

        // Assert
        var updatedInject = await context.Injects.FindAsync(inject.Id);
        updatedInject!.Status.Should().Be(InjectStatus.Draft);
    }

    #endregion

    #region GetApprovalSettingsAsync Tests

    [Fact]
    public async Task GetApprovalSettingsAsync_ReturnsSettings()
    {
        // Arrange
        var (context, org) = CreateTestContext(ApprovalPolicy.Required);
        var exercise = CreateExercise(context, org, requireApproval: true);
        var service = CreateService(context);

        // Act
        var result = await service.GetApprovalSettingsAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result.RequireInjectApproval.Should().BeTrue();
        result.ApprovalPolicyOverridden.Should().BeFalse();
        result.OrganizationPolicy.Should().Be(ApprovalPolicy.Required);
    }

    [Fact]
    public async Task GetApprovalSettingsAsync_ExerciseNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var service = CreateService(context);
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetApprovalSettingsAsync(nonExistentId));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task UpdateApprovalSettingsAsync_ExerciseNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var service = CreateService(context);
        var nonExistentId = Guid.NewGuid();

        var request = new UpdateApprovalSettingsRequest(RequireInjectApproval: true);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateApprovalSettingsAsync(nonExistentId, request, "user-123"));
    }

    [Fact]
    public async Task UpdateApprovalSettingsAsync_NoChange_DoesNotModifyOverrideFields()
    {
        // Arrange
        var (context, org) = CreateTestContext(ApprovalPolicy.Optional);
        var exercise = CreateExercise(context, org, requireApproval: true);
        var service = CreateService(context);

        _orgContextMock.Setup(x => x.IsSysAdmin).Returns(false);

        var request = new UpdateApprovalSettingsRequest(
            RequireInjectApproval: true); // No change

        // Act
        var result = await service.UpdateApprovalSettingsAsync(
            exercise.Id,
            request,
            userId: "director-123");

        // Assert
        result.RequireInjectApproval.Should().BeTrue();
        result.ApprovalPolicyOverridden.Should().BeFalse();

        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.ApprovalOverriddenById.Should().BeNull();
        updated.ApprovalOverriddenAt.Should().BeNull();
    }

    #endregion
}
