using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Exercises;

public class ExerciseStatusServiceTests
{
    private readonly Mock<IExerciseHubContext> _hubContextMock;
    private readonly Mock<ILogger<ExerciseStatusService>> _loggerMock;

    public ExerciseStatusServiceTests()
    {
        _hubContextMock = new Mock<IExerciseHubContext>();
        _loggerMock = new Mock<ILogger<ExerciseStatusService>>();
    }

    private (AppDbContext context, Organization org) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };
        context.Organizations.Add(org);
        context.SaveChanges();

        return (context, org);
    }

    private Exercise CreateExercise(
        AppDbContext context,
        Organization org,
        ExerciseStatus status = ExerciseStatus.Draft,
        bool hasBeenPublished = false)
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
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();

        return exercise;
    }

    private (Msel msel, Inject inject) CreateInject(AppDbContext context, Exercise exercise)
    {
        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Version = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
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
            Status = InjectStatus.Pending,
            Sequence = 1,
            MselId = msel.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        context.Injects.Add(inject);
        context.SaveChanges();

        return (msel, inject);
    }

    private ExerciseStatusService CreateService(AppDbContext context)
    {
        return new ExerciseStatusService(context, _hubContextMock.Object, _loggerMock.Object);
    }

    #region CanTransition Tests

    [Fact]
    public void CanTransition_DraftToActive_ReturnsTrue()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = service.CanTransition(ExerciseStatus.Draft, ExerciseStatus.Active);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanTransition_ActiveToPaused_ReturnsTrue()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = service.CanTransition(ExerciseStatus.Active, ExerciseStatus.Paused);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanTransition_PausedToActive_ReturnsTrue()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = service.CanTransition(ExerciseStatus.Paused, ExerciseStatus.Active);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanTransition_ActiveToCompleted_ReturnsTrue()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = service.CanTransition(ExerciseStatus.Active, ExerciseStatus.Completed);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanTransition_CompletedToArchived_ReturnsTrue()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = service.CanTransition(ExerciseStatus.Completed, ExerciseStatus.Archived);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanTransition_DraftToCompleted_ReturnsFalse()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = service.CanTransition(ExerciseStatus.Draft, ExerciseStatus.Completed);

        result.Should().BeFalse();
    }

    [Fact]
    public void CanTransition_ArchivedToActive_ReturnsFalse()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = service.CanTransition(ExerciseStatus.Archived, ExerciseStatus.Active);

        result.Should().BeFalse();
    }

    #endregion

    #region GetAvailableTransitions Tests

    [Fact]
    public void GetAvailableTransitions_Draft_ReturnsActiveAndArchived()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = service.GetAvailableTransitions(ExerciseStatus.Draft);

        result.Should().Contain(ExerciseStatus.Active);
        result.Should().Contain(ExerciseStatus.Archived);
    }

    [Fact]
    public void GetAvailableTransitions_Active_ReturnsPausedCompletedArchived()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = service.GetAvailableTransitions(ExerciseStatus.Active);

        result.Should().Contain(ExerciseStatus.Paused);
        result.Should().Contain(ExerciseStatus.Completed);
        result.Should().Contain(ExerciseStatus.Archived);
    }

    [Fact]
    public void GetAvailableTransitions_Archived_ReturnsEmpty()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = service.GetAvailableTransitions(ExerciseStatus.Archived);

        result.Should().BeEmpty();
    }

    #endregion

    #region ActivateAsync Tests

    [Fact]
    public async Task ActivateAsync_ExerciseNotFound_ReturnsFailed()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.ActivateAsync(Guid.NewGuid(), userId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ActivateAsync_NotDraft_ReturnsFailed()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Active);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.ActivateAsync(exercise.Id, userId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Current status is Active");
    }

    [Fact]
    public async Task ActivateAsync_NoInjects_ReturnsFailed()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Draft);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.ActivateAsync(exercise.Id, userId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("without at least one inject");
    }

    [Fact]
    public async Task ActivateAsync_ValidExercise_ReturnsSuccess()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Draft);
        CreateInject(context, exercise);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.ActivateAsync(exercise.Id, userId);

        result.Success.Should().BeTrue();
        result.Exercise!.Status.Should().Be(ExerciseStatus.Active);
    }

    [Fact]
    public async Task ActivateAsync_SetsHasBeenPublishedTrue()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Draft, hasBeenPublished: false);
        CreateInject(context, exercise);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        await service.ActivateAsync(exercise.Id, userId);

        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.HasBeenPublished.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateAsync_SetsActivatedAtAndBy()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Draft);
        CreateInject(context, exercise);
        var service = CreateService(context);
        var userId = Guid.NewGuid();
        var beforeActivation = DateTime.UtcNow;

        await service.ActivateAsync(exercise.Id, userId);

        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.ActivatedAt.Should().NotBeNull();
        updated.ActivatedAt!.Value.Should().BeOnOrAfter(beforeActivation);
        updated.ActivatedBy.Should().Be(userId.ToString());
    }

    #endregion

    #region PauseAsync Tests

    [Fact]
    public async Task PauseAsync_ActiveExercise_ReturnsSuccess()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Active);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.PauseAsync(exercise.Id, userId);

        result.Success.Should().BeTrue();
        result.Exercise!.Status.Should().Be(ExerciseStatus.Paused);
    }

    [Fact]
    public async Task PauseAsync_NotActive_ReturnsFailed()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Draft);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.PauseAsync(exercise.Id, userId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Current status is Draft");
    }

    [Fact]
    public async Task PauseAsync_RunningClock_StopsAndSavesElapsed()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Active);
        exercise.ClockState = ExerciseClockState.Running;
        exercise.ClockStartedAt = DateTime.UtcNow.AddMinutes(-10);
        context.SaveChanges();

        var service = CreateService(context);
        var userId = Guid.NewGuid();

        await service.PauseAsync(exercise.Id, userId);

        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.ClockState.Should().Be(ExerciseClockState.Paused);
        updated.ClockStartedAt.Should().BeNull();
        updated.ClockElapsedBeforePause.Should().NotBeNull();
        updated.ClockElapsedBeforePause!.Value.TotalMinutes.Should().BeGreaterThan(9);
    }

    #endregion

    #region ResumeAsync Tests

    [Fact]
    public async Task ResumeAsync_PausedExercise_ReturnsSuccess()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Paused);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.ResumeAsync(exercise.Id, userId);

        result.Success.Should().BeTrue();
        result.Exercise!.Status.Should().Be(ExerciseStatus.Active);
    }

    [Fact]
    public async Task ResumeAsync_StartsClock()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Paused);
        exercise.ClockState = ExerciseClockState.Paused;
        exercise.ClockElapsedBeforePause = TimeSpan.FromMinutes(10);
        context.SaveChanges();

        var service = CreateService(context);
        var userId = Guid.NewGuid();

        await service.ResumeAsync(exercise.Id, userId);

        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.ClockState.Should().Be(ExerciseClockState.Running);
        updated.ClockStartedAt.Should().NotBeNull();
        updated.ClockStartedBy.Should().Be(userId.ToString());
    }

    #endregion

    #region CompleteAsync Tests

    [Fact]
    public async Task CompleteAsync_ActiveExercise_ReturnsSuccess()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Active);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.CompleteAsync(exercise.Id, userId);

        result.Success.Should().BeTrue();
        result.Exercise!.Status.Should().Be(ExerciseStatus.Completed);
    }

    [Fact]
    public async Task CompleteAsync_PausedExercise_ReturnsSuccess()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Paused);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.CompleteAsync(exercise.Id, userId);

        result.Success.Should().BeTrue();
        result.Exercise!.Status.Should().Be(ExerciseStatus.Completed);
    }

    [Fact]
    public async Task CompleteAsync_DraftExercise_ReturnsFailed()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Draft);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.CompleteAsync(exercise.Id, userId);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CompleteAsync_SetsCompletedAtAndBy()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Active);
        var service = CreateService(context);
        var userId = Guid.NewGuid();
        var beforeCompletion = DateTime.UtcNow;

        await service.CompleteAsync(exercise.Id, userId);

        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.CompletedAt.Should().NotBeNull();
        updated.CompletedAt!.Value.Should().BeOnOrAfter(beforeCompletion);
        updated.CompletedBy.Should().Be(userId.ToString());
    }

    [Fact]
    public async Task CompleteAsync_StopsClock()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Active);
        exercise.ClockState = ExerciseClockState.Running;
        exercise.ClockStartedAt = DateTime.UtcNow.AddMinutes(-30);
        context.SaveChanges();

        var service = CreateService(context);
        var userId = Guid.NewGuid();

        await service.CompleteAsync(exercise.Id, userId);

        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.ClockState.Should().Be(ExerciseClockState.Stopped);
        updated.ClockStartedAt.Should().BeNull();
    }

    #endregion

    #region ArchiveAsync Tests

    [Fact]
    public async Task ArchiveAsync_CompletedExercise_ReturnsSuccess()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Completed);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.ArchiveAsync(exercise.Id, userId);

        result.Success.Should().BeTrue();
        result.Exercise!.Status.Should().Be(ExerciseStatus.Archived);
    }

    [Fact]
    public async Task ArchiveAsync_DraftExercise_ReturnsSuccess()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Draft);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.ArchiveAsync(exercise.Id, userId);

        result.Success.Should().BeTrue();
        result.Exercise!.Status.Should().Be(ExerciseStatus.Archived);
    }

    [Fact]
    public async Task ArchiveAsync_ActiveExercise_ReturnsSuccess()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Active);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.ArchiveAsync(exercise.Id, userId);

        result.Success.Should().BeTrue();
        result.Exercise!.Status.Should().Be(ExerciseStatus.Archived);
    }

    [Fact]
    public async Task ArchiveAsync_AlreadyArchived_ReturnsFailed()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Archived);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.ArchiveAsync(exercise.Id, userId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already archived");
    }

    [Fact]
    public async Task ArchiveAsync_SavesPreviousStatus()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Active);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        await service.ArchiveAsync(exercise.Id, userId);

        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.PreviousStatus.Should().Be(ExerciseStatus.Active);
    }

    [Fact]
    public async Task ArchiveAsync_SetsArchivedAtAndBy()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Completed);
        var service = CreateService(context);
        var userId = Guid.NewGuid();
        var beforeArchive = DateTime.UtcNow;

        await service.ArchiveAsync(exercise.Id, userId);

        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.ArchivedAt.Should().NotBeNull();
        updated.ArchivedAt!.Value.Should().BeOnOrAfter(beforeArchive);
        updated.ArchivedBy.Should().Be(userId.ToString());
    }

    #endregion

    #region UnarchiveAsync Tests

    [Fact]
    public async Task UnarchiveAsync_ArchivedExercise_ReturnsSuccess()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Archived);
        exercise.PreviousStatus = ExerciseStatus.Completed;
        context.SaveChanges();

        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.UnarchiveAsync(exercise.Id, userId);

        result.Success.Should().BeTrue();
        result.Exercise!.Status.Should().Be(ExerciseStatus.Completed);
    }

    [Fact]
    public async Task UnarchiveAsync_NotArchived_ReturnsFailed()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Active);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.UnarchiveAsync(exercise.Id, userId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Current status is Active");
    }

    [Fact]
    public async Task UnarchiveAsync_NoPreviousStatus_RestoresToDraft()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Archived);
        exercise.PreviousStatus = null;
        context.SaveChanges();

        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.UnarchiveAsync(exercise.Id, userId);

        result.Success.Should().BeTrue();
        result.Exercise!.Status.Should().Be(ExerciseStatus.Draft);
    }

    [Fact]
    public async Task UnarchiveAsync_ClearsArchiveFields()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Archived);
        exercise.PreviousStatus = ExerciseStatus.Active;
        exercise.ArchivedAt = DateTime.UtcNow.AddDays(-1);
        exercise.ArchivedBy = Guid.NewGuid().ToString();
        context.SaveChanges();

        var service = CreateService(context);
        var userId = Guid.NewGuid();

        await service.UnarchiveAsync(exercise.Id, userId);

        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.ArchivedAt.Should().BeNull();
        updated.ArchivedBy.Should().BeNull();
        updated.PreviousStatus.Should().BeNull();
    }

    #endregion

    #region RevertToDraftAsync Tests

    [Fact]
    public async Task RevertToDraftAsync_PausedExercise_ReturnsSuccess()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Paused);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.RevertToDraftAsync(exercise.Id, userId);

        result.Success.Should().BeTrue();
        result.Exercise!.Status.Should().Be(ExerciseStatus.Draft);
    }

    [Fact]
    public async Task RevertToDraftAsync_NotPaused_ReturnsFailed()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Active);
        var service = CreateService(context);
        var userId = Guid.NewGuid();

        var result = await service.RevertToDraftAsync(exercise.Id, userId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Current status is Active");
    }

    [Fact]
    public async Task RevertToDraftAsync_ResetsInjectStatuses()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Paused);
        var (_, inject) = CreateInject(context, exercise);

        inject.Status = InjectStatus.Fired;
        inject.FiredAt = DateTime.UtcNow;
        inject.FiredByUserId = Guid.NewGuid().ToString();
        context.SaveChanges();

        var service = CreateService(context);
        var userId = Guid.NewGuid();

        await service.RevertToDraftAsync(exercise.Id, userId);

        var updatedInject = await context.Injects.FindAsync(inject.Id);
        updatedInject!.Status.Should().Be(InjectStatus.Pending);
        updatedInject.FiredAt.Should().BeNull();
        updatedInject.FiredByUserId.Should().BeNull();
    }

    [Fact]
    public async Task RevertToDraftAsync_SoftDeletesObservations()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Paused);

        var observation = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Test observation",
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        context.Observations.Add(observation);
        context.SaveChanges();

        var service = CreateService(context);
        var userId = Guid.NewGuid();

        await service.RevertToDraftAsync(exercise.Id, userId);

        // Normal query should not find it
        var found = await context.Observations.FirstOrDefaultAsync(o => o.Id == observation.Id);
        found.Should().BeNull();

        // But it should still exist with IgnoreQueryFilters
        var deleted = await context.Observations.IgnoreQueryFilters().FirstAsync(o => o.Id == observation.Id);
        deleted.IsDeleted.Should().BeTrue();
        deleted.DeletedBy.Should().Be(userId);
    }

    [Fact]
    public async Task RevertToDraftAsync_ResetsClock()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Paused);
        exercise.ClockState = ExerciseClockState.Paused;
        exercise.ClockElapsedBeforePause = TimeSpan.FromMinutes(45);
        exercise.ClockStartedBy = Guid.NewGuid().ToString();
        context.SaveChanges();

        var service = CreateService(context);
        var userId = Guid.NewGuid();

        await service.RevertToDraftAsync(exercise.Id, userId);

        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.ClockState.Should().Be(ExerciseClockState.Stopped);
        updated.ClockElapsedBeforePause.Should().BeNull();
        updated.ClockStartedBy.Should().BeNull();
    }

    [Fact]
    public async Task RevertToDraftAsync_ClearsActivationData()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, ExerciseStatus.Paused);
        exercise.ActivatedAt = DateTime.UtcNow.AddHours(-2);
        exercise.ActivatedBy = Guid.NewGuid().ToString();
        context.SaveChanges();

        var service = CreateService(context);
        var userId = Guid.NewGuid();

        await service.RevertToDraftAsync(exercise.Id, userId);

        var updated = await context.Exercises.FindAsync(exercise.Id);
        updated!.ActivatedAt.Should().BeNull();
        updated.ActivatedBy.Should().BeNull();
    }

    #endregion
}
