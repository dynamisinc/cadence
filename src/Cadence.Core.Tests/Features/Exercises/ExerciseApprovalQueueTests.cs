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
/// Tests for Exercise Approval Queue (S06: Approval Queue View).
/// Tests the GetApprovalStatusAsync service method.
/// </summary>
public class ExerciseApprovalQueueTests
{
    private readonly Mock<ICurrentOrganizationContext> _orgContextMock;
    private readonly Mock<ILogger<ExerciseApprovalQueueService>> _loggerMock;

    public ExerciseApprovalQueueTests()
    {
        _orgContextMock = new Mock<ICurrentOrganizationContext>();
        _loggerMock = new Mock<ILogger<ExerciseApprovalQueueService>>();
    }

    private (AppDbContext context, Organization org) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            Slug = "test-org",
            InjectApprovalPolicy = ApprovalPolicy.Optional,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Organizations.Add(org);
        context.SaveChanges();

        return (context, org);
    }

    private Exercise CreateExercise(AppDbContext context, Organization org)
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
            RequireInjectApproval = true,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();

        return exercise;
    }

    private Msel CreateMsel(AppDbContext context, Exercise exercise)
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
        context.SaveChanges();

        return msel;
    }

    private Inject CreateInject(
        AppDbContext context,
        Msel msel,
        int number,
        InjectStatus status)
    {
        var inject = new Inject
        {
            Id = Guid.NewGuid(),
            InjectNumber = number,
            Title = $"Test Inject {number}",
            Description = "Description",
            ScheduledTime = new TimeOnly(9, 0),
            Target = "Target",
            InjectType = InjectType.Standard,
            Status = status,
            Sequence = number,
            MselId = msel.Id,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Injects.Add(inject);
        context.SaveChanges();

        return inject;
    }

    /// <summary>
    /// AC: Given exercise with mixed status injects, when I get approval status,
    /// then it returns correct counts.
    /// </summary>
    [Fact]
    public async Task GetApprovalStatusAsync_MixedStatusInjects_ReturnsCorrectCounts()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var msel = CreateMsel(context, exercise);

        // Create injects: 2 Draft, 3 Submitted, 5 Approved
        CreateInject(context, msel, 1, InjectStatus.Draft);
        CreateInject(context, msel, 2, InjectStatus.Draft);
        CreateInject(context, msel, 3, InjectStatus.Submitted);
        CreateInject(context, msel, 4, InjectStatus.Submitted);
        CreateInject(context, msel, 5, InjectStatus.Submitted);
        CreateInject(context, msel, 6, InjectStatus.Approved);
        CreateInject(context, msel, 7, InjectStatus.Approved);
        CreateInject(context, msel, 8, InjectStatus.Approved);
        CreateInject(context, msel, 9, InjectStatus.Approved);
        CreateInject(context, msel, 10, InjectStatus.Approved);

        var service = new ExerciseApprovalQueueService(context, _orgContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetApprovalStatusAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result.TotalInjects.Should().Be(10);
        result.ApprovedCount.Should().Be(5);
        result.PendingApprovalCount.Should().Be(3);
        result.DraftCount.Should().Be(2);
        result.ApprovalPercentage.Should().Be(50);
        result.AllApproved.Should().BeFalse();
    }

    /// <summary>
    /// AC: Given all injects approved, when I get approval status,
    /// then AllApproved is true and percentage is 100.
    /// </summary>
    [Fact]
    public async Task GetApprovalStatusAsync_AllApproved_ReturnsAllApprovedTrue()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var msel = CreateMsel(context, exercise);

        CreateInject(context, msel, 1, InjectStatus.Approved);
        CreateInject(context, msel, 2, InjectStatus.Approved);
        CreateInject(context, msel, 3, InjectStatus.Approved);

        var service = new ExerciseApprovalQueueService(context, _orgContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetApprovalStatusAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result.TotalInjects.Should().Be(3);
        result.ApprovedCount.Should().Be(3);
        result.PendingApprovalCount.Should().Be(0);
        result.DraftCount.Should().Be(0);
        result.ApprovalPercentage.Should().Be(100);
        result.AllApproved.Should().BeTrue();
    }

    /// <summary>
    /// AC: Given exercise with no injects, when I get approval status,
    /// then counts are zero and AllApproved is true (vacuously true).
    /// </summary>
    [Fact]
    public async Task GetApprovalStatusAsync_NoInjects_ReturnsZeroCountsAndAllApproved()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var msel = CreateMsel(context, exercise);

        var service = new ExerciseApprovalQueueService(context, _orgContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetApprovalStatusAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result.TotalInjects.Should().Be(0);
        result.ApprovedCount.Should().Be(0);
        result.PendingApprovalCount.Should().Be(0);
        result.DraftCount.Should().Be(0);
        result.ApprovalPercentage.Should().Be(100); // Vacuously true
        result.AllApproved.Should().BeTrue();
    }

    /// <summary>
    /// AC: Given exercise with no active MSEL, when I get approval status,
    /// then counts are zero.
    /// </summary>
    [Fact]
    public async Task GetApprovalStatusAsync_NoActiveMsel_ReturnsZeroCounts()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        // No active MSEL

        var service = new ExerciseApprovalQueueService(context, _orgContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetApprovalStatusAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result.TotalInjects.Should().Be(0);
        result.ApprovedCount.Should().Be(0);
        result.PendingApprovalCount.Should().Be(0);
        result.DraftCount.Should().Be(0);
        result.ApprovalPercentage.Should().Be(100);
        result.AllApproved.Should().BeTrue();
    }

    /// <summary>
    /// AC: Given injects in Released status (fired), when I get approval status,
    /// then they count as approved.
    /// </summary>
    [Fact]
    public async Task GetApprovalStatusAsync_ReleasedInjects_CountAsApproved()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var msel = CreateMsel(context, exercise);

        CreateInject(context, msel, 1, InjectStatus.Approved);
        CreateInject(context, msel, 2, InjectStatus.Released);
        CreateInject(context, msel, 3, InjectStatus.Synchronized);
        CreateInject(context, msel, 4, InjectStatus.Submitted);

        var service = new ExerciseApprovalQueueService(context, _orgContextMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetApprovalStatusAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result.TotalInjects.Should().Be(4);
        result.ApprovedCount.Should().Be(3); // Approved, Released, Synchronized
        result.PendingApprovalCount.Should().Be(1); // Submitted
        result.DraftCount.Should().Be(0);
        result.ApprovalPercentage.Should().Be(75);
        result.AllApproved.Should().BeFalse();
    }

    /// <summary>
    /// AC: Given exercise not found, when I get approval status,
    /// then it throws InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task GetApprovalStatusAsync_ExerciseNotFound_ThrowsException()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var service = new ExerciseApprovalQueueService(context, _orgContextMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.GetApprovalStatusAsync(Guid.NewGuid()));
    }
}
