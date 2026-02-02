using Cadence.Core.Data;
using Cadence.Core.Features.ExerciseClock.Models.DTOs;
using Cadence.Core.Features.ExerciseClock.Services;
using Cadence.Core.Features.Injects.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.ExerciseClock;

public class ExerciseClockServiceTests
{
    private readonly Mock<IExerciseHubContext> _hubContextMock;
    private readonly Mock<IInjectReadinessService> _injectReadinessServiceMock;
    private readonly Mock<ILogger<ExerciseClockService>> _loggerMock;

    public ExerciseClockServiceTests()
    {
        _hubContextMock = new Mock<IExerciseHubContext>();
        _injectReadinessServiceMock = new Mock<IInjectReadinessService>();
        _loggerMock = new Mock<ILogger<ExerciseClockService>>();
    }

    private (AppDbContext context, Organization org, Exercise exercise) CreateTestContext(
        ExerciseStatus status = ExerciseStatus.Draft,
        ExerciseClockState clockState = ExerciseClockState.Stopped)
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = status,
            ClockState = clockState,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();

        return (context, org, exercise);
    }

    private ExerciseClockService CreateService(AppDbContext context)
    {
        return new ExerciseClockService(context, _hubContextMock.Object, _injectReadinessServiceMock.Object, _loggerMock.Object);
    }

    #region GetClockStateAsync Tests

    [Fact]
    public async Task GetClockStateAsync_ExerciseNotFound_ReturnsNull()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        // Act
        var result = await service.GetClockStateAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetClockStateAsync_ExerciseExists_ReturnsClockStateDto()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetClockStateAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result!.ExerciseId.Should().Be(exercise.Id);
        result.State.Should().Be(ExerciseClockState.Stopped);
    }

    [Fact]
    public async Task GetClockStateAsync_StoppedClock_ReturnsZeroElapsed()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetClockStateAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result!.ElapsedTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetClockStateAsync_PausedClock_ReturnsAccumulatedElapsed()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Paused);
        exercise.ClockElapsedBeforePause = TimeSpan.FromMinutes(30);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetClockStateAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result!.ElapsedTime.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public async Task GetClockStateAsync_RunningClock_IncludesCurrentRunTime()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Running);
        exercise.ClockStartedAt = DateTime.UtcNow.AddMinutes(-10);
        exercise.ClockElapsedBeforePause = TimeSpan.FromMinutes(20);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetClockStateAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        // Should be approximately 30 minutes (20 accumulated + ~10 running)
        result!.ElapsedTime.TotalMinutes.Should().BeGreaterOrEqualTo(29);
        result.ElapsedTime.TotalMinutes.Should().BeLessThan(31);
    }

    #endregion

    #region StartClockAsync Tests

    [Fact]
    public async Task StartClockAsync_ExerciseNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.StartClockAsync(Guid.NewGuid(), userId.ToString());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task StartClockAsync_CompletedExercise_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(status: ExerciseStatus.Completed);
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.StartClockAsync(exercise.Id, userId.ToString());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Completed*");
    }

    [Fact]
    public async Task StartClockAsync_ArchivedExercise_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(status: ExerciseStatus.Archived);
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.StartClockAsync(exercise.Id, userId.ToString());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Archived*");
    }

    [Fact]
    public async Task StartClockAsync_ClockAlreadyRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Running);
        exercise.ClockStartedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.StartClockAsync(exercise.Id, userId.ToString());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already running*");
    }

    [Fact]
    public async Task StartClockAsync_DraftExercise_TransitionsToActive()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(status: ExerciseStatus.Draft);
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.StartClockAsync(exercise.Id, userId.ToString());

        // Assert
        var updatedExercise = await context.Exercises.FindAsync(exercise.Id);
        updatedExercise!.Status.Should().Be(ExerciseStatus.Active);
        result.State.Should().Be(ExerciseClockState.Running);
    }

    [Fact]
    public async Task StartClockAsync_ActiveExercise_DoesNotChangeStatus()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(status: ExerciseStatus.Active);
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.StartClockAsync(exercise.Id, userId.ToString());

        // Assert
        var updatedExercise = await context.Exercises.FindAsync(exercise.Id);
        updatedExercise!.Status.Should().Be(ExerciseStatus.Active);
        result.State.Should().Be(ExerciseClockState.Running);
    }

    [Fact]
    public async Task StartClockAsync_SetsClockStartedAtToUtcNow()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();
        var beforeStart = DateTime.UtcNow;

        // Act
        var result = await service.StartClockAsync(exercise.Id, userId.ToString());

        // Assert
        var updatedExercise = await context.Exercises.FindAsync(exercise.Id);
        updatedExercise!.ClockStartedAt.Should().NotBeNull();
        updatedExercise.ClockStartedAt!.Value.Should().BeOnOrAfter(beforeStart);
        updatedExercise.ClockStartedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task StartClockAsync_SetsClockStartedBy()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.StartClockAsync(exercise.Id, userId.ToString());

        // Assert
        var updatedExercise = await context.Exercises.FindAsync(exercise.Id);
        updatedExercise!.ClockStartedBy.Should().Be(userId.ToString());
    }

    [Fact]
    public async Task StartClockAsync_BroadcastsClockStartedEvent()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.StartClockAsync(exercise.Id, userId.ToString());

        // Assert
        _hubContextMock.Verify(
            h => h.NotifyClockStarted(exercise.Id, It.Is<ClockStateDto>(dto =>
                dto.ExerciseId == exercise.Id &&
                dto.State == ExerciseClockState.Running)),
            Times.Once);
    }

    [Fact]
    public async Task StartClockAsync_FromPausedState_StartsSuccessfully()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Paused);
        exercise.ClockElapsedBeforePause = TimeSpan.FromMinutes(15);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.StartClockAsync(exercise.Id, userId.ToString());

        // Assert
        result.State.Should().Be(ExerciseClockState.Running);
        var updatedExercise = await context.Exercises.FindAsync(exercise.Id);
        updatedExercise!.ClockStartedAt.Should().NotBeNull();
        // Previous elapsed time should still be preserved
        updatedExercise.ClockElapsedBeforePause.Should().Be(TimeSpan.FromMinutes(15));
    }

    #endregion

    #region PauseClockAsync Tests

    [Fact]
    public async Task PauseClockAsync_ExerciseNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.PauseClockAsync(Guid.NewGuid(), userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task PauseClockAsync_ClockNotRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Stopped);
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.PauseClockAsync(exercise.Id, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not running*");
    }

    [Fact]
    public async Task PauseClockAsync_ClockAlreadyPaused_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Paused);
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.PauseClockAsync(exercise.Id, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not running*");
    }

    [Fact]
    public async Task PauseClockAsync_RunningClock_AccumulatesElapsedTime()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Running);
        exercise.ClockStartedAt = DateTime.UtcNow.AddMinutes(-10);
        exercise.ClockElapsedBeforePause = TimeSpan.FromMinutes(5);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.PauseClockAsync(exercise.Id, userId);

        // Assert
        // Should be approximately 15 minutes (5 existing + ~10 from current run)
        result.ElapsedTime.TotalMinutes.Should().BeGreaterOrEqualTo(14);
        result.ElapsedTime.TotalMinutes.Should().BeLessThan(16);
    }

    [Fact]
    public async Task PauseClockAsync_ClearsClockStartedAt()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Running);
        exercise.ClockStartedAt = DateTime.UtcNow.AddMinutes(-5);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.PauseClockAsync(exercise.Id, userId);

        // Assert
        var updatedExercise = await context.Exercises.FindAsync(exercise.Id);
        updatedExercise!.ClockStartedAt.Should().BeNull();
    }

    [Fact]
    public async Task PauseClockAsync_SetsClockStateToPaused()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Running);
        exercise.ClockStartedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.PauseClockAsync(exercise.Id, userId);

        // Assert
        result.State.Should().Be(ExerciseClockState.Paused);
        var updatedExercise = await context.Exercises.FindAsync(exercise.Id);
        updatedExercise!.ClockState.Should().Be(ExerciseClockState.Paused);
    }

    [Fact]
    public async Task PauseClockAsync_BroadcastsClockPausedEvent()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Running);
        exercise.ClockStartedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.PauseClockAsync(exercise.Id, userId);

        // Assert
        _hubContextMock.Verify(
            h => h.NotifyClockPaused(exercise.Id, It.Is<ClockStateDto>(dto =>
                dto.ExerciseId == exercise.Id &&
                dto.State == ExerciseClockState.Paused)),
            Times.Once);
    }

    #endregion

    #region StopClockAsync Tests

    [Fact]
    public async Task StopClockAsync_ExerciseNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.StopClockAsync(Guid.NewGuid(), userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task StopClockAsync_ClockAlreadyStopped_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Stopped);
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.StopClockAsync(exercise.Id, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already stopped*");
    }

    [Fact]
    public async Task StopClockAsync_RunningClock_CapturesFinalElapsedTime()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Running);
        exercise.ClockStartedAt = DateTime.UtcNow.AddMinutes(-10);
        exercise.ClockElapsedBeforePause = TimeSpan.FromMinutes(20);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.StopClockAsync(exercise.Id, userId);

        // Assert
        // Should be approximately 30 minutes (20 existing + ~10 from current run)
        result.ElapsedTime.TotalMinutes.Should().BeGreaterOrEqualTo(29);
        result.ElapsedTime.TotalMinutes.Should().BeLessThan(31);
    }

    [Fact]
    public async Task StopClockAsync_PausedClock_PreservesElapsedTime()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Paused);
        exercise.ClockElapsedBeforePause = TimeSpan.FromMinutes(45);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.StopClockAsync(exercise.Id, userId);

        // Assert
        result.ElapsedTime.Should().Be(TimeSpan.FromMinutes(45));
    }

    [Fact]
    public async Task StopClockAsync_SetsExerciseStatusToCompleted()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Running);
        exercise.ClockStartedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.StopClockAsync(exercise.Id, userId);

        // Assert
        var updatedExercise = await context.Exercises.FindAsync(exercise.Id);
        updatedExercise!.Status.Should().Be(ExerciseStatus.Completed);
    }

    [Fact]
    public async Task StopClockAsync_SetsClockStateToStopped()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Running);
        exercise.ClockStartedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.StopClockAsync(exercise.Id, userId);

        // Assert
        result.State.Should().Be(ExerciseClockState.Stopped);
        var updatedExercise = await context.Exercises.FindAsync(exercise.Id);
        updatedExercise!.ClockState.Should().Be(ExerciseClockState.Stopped);
    }

    [Fact]
    public async Task StopClockAsync_ClearsClockStartedAt()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Running);
        exercise.ClockStartedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.StopClockAsync(exercise.Id, userId);

        // Assert
        var updatedExercise = await context.Exercises.FindAsync(exercise.Id);
        updatedExercise!.ClockStartedAt.Should().BeNull();
    }

    [Fact]
    public async Task StopClockAsync_BroadcastsClockStoppedEvent()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Running);
        exercise.ClockStartedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.StopClockAsync(exercise.Id, userId);

        // Assert
        _hubContextMock.Verify(
            h => h.NotifyClockStopped(exercise.Id, It.Is<ClockStateDto>(dto =>
                dto.ExerciseId == exercise.Id &&
                dto.State == ExerciseClockState.Stopped)),
            Times.Once);
    }

    #endregion

    #region ResetClockAsync Tests

    [Fact]
    public async Task ResetClockAsync_ExerciseNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.ResetClockAsync(Guid.NewGuid(), userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ResetClockAsync_ActiveExerciseWithRunningClock_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Running);
        exercise.ClockStartedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.ResetClockAsync(exercise.Id, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Draft or clock is Stopped*");
    }

    [Fact]
    public async Task ResetClockAsync_ActiveExerciseWithPausedClock_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Active,
            clockState: ExerciseClockState.Paused);
        exercise.ClockElapsedBeforePause = TimeSpan.FromMinutes(30);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => service.ResetClockAsync(exercise.Id, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Draft or clock is Stopped*");
    }

    [Fact]
    public async Task ResetClockAsync_DraftExercise_ResetsSuccessfully()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(status: ExerciseStatus.Draft);
        exercise.ClockElapsedBeforePause = TimeSpan.FromMinutes(15);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.ResetClockAsync(exercise.Id, userId);

        // Assert
        result.State.Should().Be(ExerciseClockState.Stopped);
        result.ElapsedTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task ResetClockAsync_StoppedClock_ResetsSuccessfully()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(
            status: ExerciseStatus.Completed,
            clockState: ExerciseClockState.Stopped);
        exercise.ClockElapsedBeforePause = TimeSpan.FromHours(2);
        exercise.ClockStartedBy = Guid.NewGuid().ToString();
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.ResetClockAsync(exercise.Id, userId);

        // Assert
        result.State.Should().Be(ExerciseClockState.Stopped);
        result.ElapsedTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task ResetClockAsync_ClearsAllClockFields()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(status: ExerciseStatus.Draft);
        exercise.ClockStartedAt = DateTime.UtcNow;
        exercise.ClockElapsedBeforePause = TimeSpan.FromMinutes(30);
        exercise.ClockStartedBy = Guid.NewGuid().ToString();
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.ResetClockAsync(exercise.Id, userId);

        // Assert
        var updatedExercise = await context.Exercises.FindAsync(exercise.Id);
        updatedExercise!.ClockStartedAt.Should().BeNull();
        updatedExercise.ClockElapsedBeforePause.Should().BeNull();
        updatedExercise.ClockStartedBy.Should().BeNull();
        updatedExercise.ClockState.Should().Be(ExerciseClockState.Stopped);
    }

    [Fact]
    public async Task ResetClockAsync_BroadcastsClockStoppedEvent()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(status: ExerciseStatus.Draft);
        var service = CreateService(context);
        var userId = Guid.NewGuid().ToString();

        // Act
        await service.ResetClockAsync(exercise.Id, userId);

        // Assert
        _hubContextMock.Verify(
            h => h.NotifyClockStopped(exercise.Id, It.Is<ClockStateDto>(dto =>
                dto.ExerciseId == exercise.Id &&
                dto.State == ExerciseClockState.Stopped)),
            Times.Once);
    }

    #endregion
}
