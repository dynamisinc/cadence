using Cadence.Core.Data;
using Cadence.Core.Features.Injects.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Injects;

public class InjectReadinessServiceTests
{
    private readonly Mock<IExerciseHubContext> _hubContextMock;
    private readonly Mock<ILogger<InjectReadinessService>> _loggerMock;

    public InjectReadinessServiceTests()
    {
        _hubContextMock = new Mock<IExerciseHubContext>();
        _loggerMock = new Mock<ILogger<InjectReadinessService>>();
    }

    private (AppDbContext context, Organization org, Exercise exercise, Msel msel) CreateTestContext(
        ExerciseStatus status = ExerciseStatus.Active,
        ExerciseClockState clockState = ExerciseClockState.Running,
        DeliveryMode deliveryMode = DeliveryMode.ClockDriven,
        DateTime? clockStartedAt = null,
        TimeSpan? clockElapsedBeforePause = null)
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

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = status,
            ClockState = clockState,
            DeliveryMode = deliveryMode,
            ClockStartedAt = clockStartedAt ?? (clockState == ExerciseClockState.Running ? DateTime.UtcNow.AddMinutes(-30) : null),
            ClockElapsedBeforePause = clockElapsedBeforePause,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };

        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Description = "Test MSEL Description",
            ExerciseId = exercise.Id,
            Version = 1,
            IsActive = true,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };

        exercise.ActiveMselId = msel.Id;

        context.Exercises.Add(exercise);
        context.Msels.Add(msel);
        context.SaveChanges();

        return (context, org, exercise, msel);
    }

    private InjectReadinessService CreateService(AppDbContext context)
    {
        return new InjectReadinessService(context, _hubContextMock.Object, _loggerMock.Object);
    }

    private Inject CreateInject(
        Guid mselId,
        int injectNumber,
        InjectStatus status = InjectStatus.Pending,
        TimeSpan? deliveryTime = null)
    {
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
            DeliveryTime = deliveryTime,
            Sequence = injectNumber,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };
    }

    #region EvaluateExerciseAsync Tests

    [Fact]
    public async Task EvaluateExercise_ClockDriven_TransitionsPendingToReady()
    {
        // Arrange - Exercise running for 30 minutes
        var (context, _, exercise, msel) = CreateTestContext(
            clockStartedAt: DateTime.UtcNow.AddMinutes(-30));

        // Inject due at 15 minutes (past due)
        var inject = CreateInject(msel.Id, 1, InjectStatus.Pending, TimeSpan.FromMinutes(15));
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.EvaluateExerciseAsync(exercise.Id);

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Ready);
        updated.ReadyAt.Should().NotBeNull();
        updated.ReadyAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _hubContextMock.Verify(
            h => h.NotifyInjectReadyToFire(exercise.Id, It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateExercise_FacilitatorPaced_NoTransitions()
    {
        // Arrange - Facilitator-paced exercise
        var (context, _, exercise, msel) = CreateTestContext(
            deliveryMode: DeliveryMode.FacilitatorPaced,
            clockStartedAt: DateTime.UtcNow.AddMinutes(-30));

        var inject = CreateInject(msel.Id, 1, InjectStatus.Pending, TimeSpan.FromMinutes(15));
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.EvaluateExerciseAsync(exercise.Id);

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Pending);
        updated.ReadyAt.Should().BeNull();

        _hubContextMock.Verify(
            h => h.NotifyInjectReadyToFire(It.IsAny<Guid>(), It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateExercise_ClockPaused_NoTransitions()
    {
        // Arrange - Clock is paused
        var (context, _, exercise, msel) = CreateTestContext(
            clockState: ExerciseClockState.Paused,
            clockStartedAt: null,
            clockElapsedBeforePause: TimeSpan.FromMinutes(30));

        var inject = CreateInject(msel.Id, 1, InjectStatus.Pending, TimeSpan.FromMinutes(15));
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.EvaluateExerciseAsync(exercise.Id);

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Pending);
        updated.ReadyAt.Should().BeNull();

        _hubContextMock.Verify(
            h => h.NotifyInjectReadyToFire(It.IsAny<Guid>(), It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateExercise_MultipleInjectsReady_AllTransition()
    {
        // Arrange - Exercise running for 60 minutes
        var (context, _, exercise, msel) = CreateTestContext(
            clockStartedAt: DateTime.UtcNow.AddMinutes(-60));

        var inject1 = CreateInject(msel.Id, 1, InjectStatus.Pending, TimeSpan.FromMinutes(15));
        var inject2 = CreateInject(msel.Id, 2, InjectStatus.Pending, TimeSpan.FromMinutes(30));
        var inject3 = CreateInject(msel.Id, 3, InjectStatus.Pending, TimeSpan.FromMinutes(45));
        context.Injects.AddRange(inject1, inject2, inject3);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.EvaluateExerciseAsync(exercise.Id);

        // Assert
        var updated1 = await context.Injects.FindAsync(inject1.Id);
        var updated2 = await context.Injects.FindAsync(inject2.Id);
        var updated3 = await context.Injects.FindAsync(inject3.Id);

        updated1!.Status.Should().Be(InjectStatus.Ready);
        updated2!.Status.Should().Be(InjectStatus.Ready);
        updated3!.Status.Should().Be(InjectStatus.Ready);

        _hubContextMock.Verify(
            h => h.NotifyInjectReadyToFire(exercise.Id, It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task EvaluateExercise_AlreadyFired_NoChange()
    {
        // Arrange - Exercise running for 30 minutes
        var (context, _, exercise, msel) = CreateTestContext(
            clockStartedAt: DateTime.UtcNow.AddMinutes(-30));

        var inject = CreateInject(msel.Id, 1, InjectStatus.Fired, TimeSpan.FromMinutes(15));
        inject.FiredAt = DateTime.UtcNow.AddMinutes(-10);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.EvaluateExerciseAsync(exercise.Id);

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Fired);
        updated.ReadyAt.Should().BeNull();

        _hubContextMock.Verify(
            h => h.NotifyInjectReadyToFire(It.IsAny<Guid>(), It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateExercise_NoDeliveryTime_SkipsInject()
    {
        // Arrange - Exercise running for 30 minutes
        var (context, _, exercise, msel) = CreateTestContext(
            clockStartedAt: DateTime.UtcNow.AddMinutes(-30));

        var inject = CreateInject(msel.Id, 1, InjectStatus.Pending, deliveryTime: null);
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.EvaluateExerciseAsync(exercise.Id);

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Pending);
        updated.ReadyAt.Should().BeNull();

        _hubContextMock.Verify(
            h => h.NotifyInjectReadyToFire(It.IsAny<Guid>(), It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateExercise_DeliveryTimeNotReached_NoTransition()
    {
        // Arrange - Exercise running for 30 minutes
        var (context, _, exercise, msel) = CreateTestContext(
            clockStartedAt: DateTime.UtcNow.AddMinutes(-30));

        // Inject due at 60 minutes (not yet reached)
        var inject = CreateInject(msel.Id, 1, InjectStatus.Pending, TimeSpan.FromMinutes(60));
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.EvaluateExerciseAsync(exercise.Id);

        // Assert
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Pending);
        updated.ReadyAt.Should().BeNull();

        _hubContextMock.Verify(
            h => h.NotifyInjectReadyToFire(It.IsAny<Guid>(), It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateExercise_ExerciseNotFound_LogsWarning()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        // Act
        await service.EvaluateExerciseAsync(Guid.NewGuid());

        // Assert - Should not throw, just log warning
        _hubContextMock.Verify(
            h => h.NotifyInjectReadyToFire(It.IsAny<Guid>(), It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateExercise_NoActiveMsel_DoesNothing()
    {
        // Arrange
        var (context, _, exercise, _) = CreateTestContext();
        exercise.ActiveMselId = null;
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.EvaluateExerciseAsync(exercise.Id);

        // Assert
        _hubContextMock.Verify(
            h => h.NotifyInjectReadyToFire(It.IsAny<Guid>(), It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateExercise_WithElapsedBeforePause_CalculatesCorrectly()
    {
        // Arrange - Clock was running, paused at 20 minutes, resumed 10 minutes ago
        var (context, _, exercise, msel) = CreateTestContext(
            clockStartedAt: DateTime.UtcNow.AddMinutes(-10),
            clockElapsedBeforePause: TimeSpan.FromMinutes(20));

        // Inject due at 25 minutes (20 before pause + 5 after resume = 25 total)
        var inject = CreateInject(msel.Id, 1, InjectStatus.Pending, TimeSpan.FromMinutes(25));
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.EvaluateExerciseAsync(exercise.Id);

        // Assert - Should be ready (30 minutes total: 20 before pause + 10 after resume)
        var updated = await context.Injects.FindAsync(inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Ready);
        updated.ReadyAt.Should().NotBeNull();

        _hubContextMock.Verify(
            h => h.NotifyInjectReadyToFire(exercise.Id, It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Once);
    }

    #endregion

    #region EvaluateAllExercisesAsync Tests

    [Fact]
    public async Task EvaluateAllExercises_MultipleActiveExercises_EvaluatesAll()
    {
        // Arrange - Create two active exercises with running clocks
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };
        context.Organizations.Add(org);

        var exercise1 = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Exercise 1",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            ClockState = ExerciseClockState.Running,
            DeliveryMode = DeliveryMode.ClockDriven,
            ClockStartedAt = DateTime.UtcNow.AddMinutes(-30),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };

        var msel1 = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "MSEL 1",
            Description = "Test",
            ExerciseId = exercise1.Id,
            Version = 1,
            IsActive = true,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };
        exercise1.ActiveMselId = msel1.Id;

        var exercise2 = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Exercise 2",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            ClockState = ExerciseClockState.Running,
            DeliveryMode = DeliveryMode.ClockDriven,
            ClockStartedAt = DateTime.UtcNow.AddMinutes(-45),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };

        var msel2 = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "MSEL 2",
            Description = "Test",
            ExerciseId = exercise2.Id,
            Version = 1,
            IsActive = true,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };
        exercise2.ActiveMselId = msel2.Id;

        context.Exercises.AddRange(exercise1, exercise2);
        context.Msels.AddRange(msel1, msel2);

        // Add injects to both MSELs
        var inject1 = CreateInject(msel1.Id, 1, InjectStatus.Pending, TimeSpan.FromMinutes(15));
        var inject2 = CreateInject(msel2.Id, 1, InjectStatus.Pending, TimeSpan.FromMinutes(20));
        context.Injects.AddRange(inject1, inject2);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.EvaluateAllExercisesAsync();

        // Assert - Both injects should be ready
        var updated1 = await context.Injects.FindAsync(inject1.Id);
        var updated2 = await context.Injects.FindAsync(inject2.Id);

        updated1!.Status.Should().Be(InjectStatus.Ready);
        updated2!.Status.Should().Be(InjectStatus.Ready);

        _hubContextMock.Verify(
            h => h.NotifyInjectReadyToFire(It.IsAny<Guid>(), It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task EvaluateAllExercises_NoActiveExercises_DoesNothing()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        // Act
        await service.EvaluateAllExercisesAsync();

        // Assert
        _hubContextMock.Verify(
            h => h.NotifyInjectReadyToFire(It.IsAny<Guid>(), It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateAllExercises_OnlyActiveClockDrivenRunning_IgnoresOthers()
    {
        // Arrange
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };
        context.Organizations.Add(org);

        // Active + ClockDriven + Running (should be evaluated)
        var exercise1 = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Active Running",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            ClockState = ExerciseClockState.Running,
            DeliveryMode = DeliveryMode.ClockDriven,
            ClockStartedAt = DateTime.UtcNow.AddMinutes(-30),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };

        // Draft (should be ignored)
        var exercise2 = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Draft Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ClockState = ExerciseClockState.Stopped,
            DeliveryMode = DeliveryMode.ClockDriven,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };

        // Active but Paused (should be ignored)
        var exercise3 = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Active Paused",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            ClockState = ExerciseClockState.Paused,
            DeliveryMode = DeliveryMode.ClockDriven,
            ClockElapsedBeforePause = TimeSpan.FromMinutes(30),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };

        // Active + Running but FacilitatorPaced (should be ignored)
        var exercise4 = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Facilitator Paced",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            ClockState = ExerciseClockState.Running,
            DeliveryMode = DeliveryMode.FacilitatorPaced,
            ClockStartedAt = DateTime.UtcNow.AddMinutes(-30),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };

        context.Exercises.AddRange(exercise1, exercise2, exercise3, exercise4);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        await service.EvaluateAllExercisesAsync();

        // Assert - Only exercise1 should have been queried
        var exercises = await context.Exercises
            .Where(e => e.Status == ExerciseStatus.Active)
            .Where(e => e.DeliveryMode == DeliveryMode.ClockDriven)
            .Where(e => e.ClockState == ExerciseClockState.Running)
            .ToListAsync();

        exercises.Should().HaveCount(1);
        exercises[0].Id.Should().Be(exercise1.Id);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task EvaluateExercise_InjectFiredConcurrently_DoesNotOverwriteFiredStatus()
    {
        // Arrange - Use a named database so we can create a second context
        var dbName = Guid.NewGuid().ToString();
        var context = TestDbContextFactory.Create(dbName);

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            ClockState = ExerciseClockState.Running,
            DeliveryMode = DeliveryMode.ClockDriven,
            ClockStartedAt = DateTime.UtcNow.AddMinutes(-30),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };

        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Description = "Test MSEL Description",
            ExerciseId = exercise.Id,
            Version = 1,
            IsActive = true,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };

        exercise.ActiveMselId = msel.Id;
        context.Exercises.Add(exercise);
        context.Msels.Add(msel);

        // Inject due at 15 minutes (past due, should become Ready)
        var inject = CreateInject(msel.Id, 1, InjectStatus.Pending, TimeSpan.FromMinutes(15));
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Simulate race condition: Fire the inject before background service saves
        // Create a second context sharing the same in-memory database
        using var concurrentContext = TestDbContextFactory.Create(dbName);
        var injectToFire = await concurrentContext.Injects.FindAsync(inject.Id);
        injectToFire!.Status = InjectStatus.Fired;
        injectToFire.FiredAt = DateTime.UtcNow;
        injectToFire.FiredBy = Guid.NewGuid();
        await concurrentContext.SaveChangesAsync();

        // Act - Background service attempts to mark as Ready
        await service.EvaluateExerciseAsync(exercise.Id);

        // Assert - Should NOT overwrite Fired status
        var updated = await context.Injects.AsNoTracking().FirstOrDefaultAsync(i => i.Id == inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Fired, "fired status should not be overwritten by background service");
        updated.FiredAt.Should().NotBeNull("FiredAt should not be cleared");
        updated.FiredBy.Should().NotBeNull("FiredBy should not be cleared");
        updated.ReadyAt.Should().BeNull("ReadyAt should not be set if inject was already fired");

        // Should not broadcast Ready notification if inject was already fired
        _hubContextMock.Verify(
            h => h.NotifyInjectReadyToFire(exercise.Id, It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateExercise_InjectSkippedConcurrently_DoesNotOverwriteSkippedStatus()
    {
        // Arrange - Use a named database so we can create a second context
        var dbName = Guid.NewGuid().ToString();
        var context = TestDbContextFactory.Create(dbName);

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            ClockState = ExerciseClockState.Running,
            DeliveryMode = DeliveryMode.ClockDriven,
            ClockStartedAt = DateTime.UtcNow.AddMinutes(-30),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };

        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Description = "Test MSEL Description",
            ExerciseId = exercise.Id,
            Version = 1,
            IsActive = true,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };

        exercise.ActiveMselId = msel.Id;
        context.Exercises.Add(exercise);
        context.Msels.Add(msel);

        var inject = CreateInject(msel.Id, 1, InjectStatus.Pending, TimeSpan.FromMinutes(15));
        context.Injects.Add(inject);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Simulate concurrent skip - Create a second context sharing the same in-memory database
        using var concurrentContext = TestDbContextFactory.Create(dbName);
        var injectToSkip = await concurrentContext.Injects.FindAsync(inject.Id);
        injectToSkip!.Status = InjectStatus.Skipped;
        injectToSkip.SkippedAt = DateTime.UtcNow;
        injectToSkip.SkippedBy = Guid.NewGuid();
        injectToSkip.SkipReason = "No longer relevant";
        await concurrentContext.SaveChangesAsync();

        // Act
        await service.EvaluateExerciseAsync(exercise.Id);

        // Assert - Should NOT overwrite Skipped status
        var updated = await context.Injects.AsNoTracking().FirstOrDefaultAsync(i => i.Id == inject.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(InjectStatus.Skipped);
        updated.SkippedAt.Should().NotBeNull();
        updated.SkippedBy.Should().NotBeNull();
        updated.ReadyAt.Should().BeNull();

        _hubContextMock.Verify(
            h => h.NotifyInjectReadyToFire(It.IsAny<Guid>(), It.IsAny<Cadence.Core.Features.Injects.Models.DTOs.InjectDto>()),
            Times.Never);
    }

    #endregion
}
