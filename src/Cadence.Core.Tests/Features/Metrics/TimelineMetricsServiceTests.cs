using Cadence.Core.Data;
using Cadence.Core.Features.Metrics.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Metrics;

public class TimelineMetricsServiceTests
{
    private static TimelineMetricsService CreateService(AppDbContext context) => new(context);

    private (AppDbContext context, Organization org) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
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
            Status = ExerciseStatus.Active,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            ClockMultiplier = 1.0m,
            ClockState = ExerciseClockState.Stopped,
            ClockElapsedBeforePause = TimeSpan.FromHours(1),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();
        return exercise;
    }

    // =========================================================================
    // GetTimelineSummaryAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetTimelineSummaryAsync_ReturnsNull_WhenExerciseNotFound()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = await service.GetTimelineSummaryAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTimelineSummaryAsync_CalculatesPlannedDuration()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var service = CreateService(context);

        var result = await service.GetTimelineSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        // StartTime 09:00 to EndTime 12:00 = 3 hours
        result!.PlannedDuration.Should().Be(TimeSpan.FromHours(3));
    }

    [Fact]
    public async Task GetTimelineSummaryAsync_CalculatesActualDuration()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        exercise.ClockElapsedBeforePause = TimeSpan.FromMinutes(90);
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetTimelineSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.ActualDuration.Should().Be(TimeSpan.FromMinutes(90));
    }

    [Fact]
    public async Task GetTimelineSummaryAsync_CalculatesDurationVariance()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        // Planned: 3 hours, Actual: 1 hour (from ClockElapsedBeforePause)
        var service = CreateService(context);

        var result = await service.GetTimelineSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.DurationVariance.Should().NotBeNull();
        // Actual (1h) - Planned (3h) = -2h (ran shorter than planned)
        result.DurationVariance!.Value.Should().Be(TimeSpan.FromHours(-2));
    }

    [Fact]
    public async Task GetTimelineSummaryAsync_IdentifiesStartAndEndFromClockEvents()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        var startTime = DateTime.UtcNow.AddHours(-3);
        var stopTime = DateTime.UtcNow.AddHours(-1);

        context.ClockEvents.AddRange(
            new ClockEvent { Id = Guid.NewGuid(), ExerciseId = exercise.Id, EventType = ClockEventType.Started, OccurredAt = startTime, ElapsedTimeAtEvent = TimeSpan.Zero },
            new ClockEvent { Id = Guid.NewGuid(), ExerciseId = exercise.Id, EventType = ClockEventType.Stopped, OccurredAt = stopTime, ElapsedTimeAtEvent = TimeSpan.FromHours(2) }
        );
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetTimelineSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.StartedAt.Should().Be(startTime);
        result.EndedAt.Should().Be(stopTime);
        result.WallClockDuration.Should().BeCloseTo(TimeSpan.FromHours(2), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetTimelineSummaryAsync_CalculatesPauseMetrics()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        var baseTime = DateTime.UtcNow.AddHours(-3);
        context.ClockEvents.AddRange(
            new ClockEvent { Id = Guid.NewGuid(), ExerciseId = exercise.Id, EventType = ClockEventType.Started, OccurredAt = baseTime, ElapsedTimeAtEvent = TimeSpan.Zero },
            new ClockEvent { Id = Guid.NewGuid(), ExerciseId = exercise.Id, EventType = ClockEventType.Paused, OccurredAt = baseTime.AddMinutes(30), ElapsedTimeAtEvent = TimeSpan.FromMinutes(30) },
            new ClockEvent { Id = Guid.NewGuid(), ExerciseId = exercise.Id, EventType = ClockEventType.Started, OccurredAt = baseTime.AddMinutes(40), ElapsedTimeAtEvent = TimeSpan.FromMinutes(30) },
            new ClockEvent { Id = Guid.NewGuid(), ExerciseId = exercise.Id, EventType = ClockEventType.Paused, OccurredAt = baseTime.AddMinutes(60), ElapsedTimeAtEvent = TimeSpan.FromMinutes(50) },
            new ClockEvent { Id = Guid.NewGuid(), ExerciseId = exercise.Id, EventType = ClockEventType.Started, OccurredAt = baseTime.AddMinutes(80), ElapsedTimeAtEvent = TimeSpan.FromMinutes(50) }
        );
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetTimelineSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.PauseCount.Should().Be(2);
        result.PauseEvents.Should().HaveCount(2);
        // First pause: 10 min, second pause: 20 min
        result.PauseEvents[0].Duration.Should().Be(TimeSpan.FromMinutes(10));
        result.PauseEvents[1].Duration.Should().Be(TimeSpan.FromMinutes(20));
        result.TotalPauseTime.Should().Be(TimeSpan.FromMinutes(30));
        result.LongestPauseDuration.Should().Be(TimeSpan.FromMinutes(20));
    }

    [Fact]
    public async Task GetTimelineSummaryAsync_CalculatesPhaseTimings()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        var phase = new Phase { Id = Guid.NewGuid(), Name = "Phase 1", Sequence = 1, ExerciseId = exercise.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() };
        context.Phases.Add(phase);

        var msel = new Msel { Id = Guid.NewGuid(), Name = "MSEL", Version = 1, IsActive = true, ExerciseId = exercise.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() };
        context.Msels.Add(msel);
        exercise.ActiveMselId = msel.Id;

        var baseTime = DateTime.UtcNow.AddHours(-2);
        var injects = new[]
        {
            new Inject { Id = Guid.NewGuid(), InjectNumber = 1, Title = "I1", Description = "D", ScheduledTime = new TimeOnly(9, 0), Target = "T", InjectType = InjectType.Standard, Status = InjectStatus.Released, FiredAt = baseTime, PhaseId = phase.Id, Sequence = 1, MselId = msel.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() },
            new Inject { Id = Guid.NewGuid(), InjectNumber = 2, Title = "I2", Description = "D", ScheduledTime = new TimeOnly(9, 30), Target = "T", InjectType = InjectType.Standard, Status = InjectStatus.Released, FiredAt = baseTime.AddMinutes(30), PhaseId = phase.Id, Sequence = 2, MselId = msel.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() }
        };
        context.Injects.AddRange(injects);
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetTimelineSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.PhaseTimings.Should().HaveCount(1);
        var timing = result.PhaseTimings[0];
        timing.PhaseName.Should().Be("Phase 1");
        timing.InjectsFired.Should().Be(2);
        timing.Duration.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public async Task GetTimelineSummaryAsync_CalculatesInjectPacing()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        exercise.ClockElapsedBeforePause = TimeSpan.FromHours(1);
        context.SaveChanges();

        var msel = new Msel { Id = Guid.NewGuid(), Name = "MSEL", Version = 1, IsActive = true, ExerciseId = exercise.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() };
        context.Msels.Add(msel);
        exercise.ActiveMselId = msel.Id;

        var baseTime = DateTime.UtcNow.AddHours(-1);
        context.Injects.AddRange(
            new Inject { Id = Guid.NewGuid(), InjectNumber = 1, Title = "I1", Description = "D", ScheduledTime = new TimeOnly(9, 0), Target = "T", InjectType = InjectType.Standard, Status = InjectStatus.Released, FiredAt = baseTime, Sequence = 1, MselId = msel.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() },
            new Inject { Id = Guid.NewGuid(), InjectNumber = 2, Title = "I2", Description = "D", ScheduledTime = new TimeOnly(9, 15), Target = "T", InjectType = InjectType.Standard, Status = InjectStatus.Released, FiredAt = baseTime.AddMinutes(15), Sequence = 2, MselId = msel.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() },
            new Inject { Id = Guid.NewGuid(), InjectNumber = 3, Title = "I3", Description = "D", ScheduledTime = new TimeOnly(9, 45), Target = "T", InjectType = InjectType.Standard, Status = InjectStatus.Released, FiredAt = baseTime.AddMinutes(45), Sequence = 3, MselId = msel.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() }
        );
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetTimelineSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        var pacing = result!.InjectPacing;
        pacing.TotalFired.Should().Be(3);
        pacing.AverageTimeBetweenInjects.Should().NotBeNull();
        pacing.ShortestGap.Should().Be(TimeSpan.FromMinutes(15));
        pacing.LongestGap.Should().Be(TimeSpan.FromMinutes(30));
        pacing.InjectsPerHour.Should().Be(3.0m); // 3 injects in 1 hour
    }

    [Fact]
    public async Task GetTimelineSummaryAsync_NoPauseEvents_ReturnsZeroPauseMetrics()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        context.ClockEvents.Add(
            new ClockEvent { Id = Guid.NewGuid(), ExerciseId = exercise.Id, EventType = ClockEventType.Started, OccurredAt = DateTime.UtcNow.AddHours(-1), ElapsedTimeAtEvent = TimeSpan.Zero }
        );
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetTimelineSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.PauseCount.Should().Be(0);
        result.TotalPauseTime.Should().Be(TimeSpan.Zero);
        result.AveragePauseDuration.Should().BeNull();
        result.LongestPauseDuration.Should().BeNull();
    }

    [Fact]
    public async Task GetTimelineSummaryAsync_NoFiredInjects_ReturnsEmptyPacing()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var service = CreateService(context);

        var result = await service.GetTimelineSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.InjectPacing.TotalFired.Should().Be(0);
        result.InjectPacing.AverageTimeBetweenInjects.Should().BeNull();
        result.PhaseTimings.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTimelineSummaryAsync_HandlesOvernightExercise()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        // 22:00 to 02:00 = overnight
        exercise.StartTime = new TimeOnly(22, 0);
        exercise.EndTime = new TimeOnly(2, 0);
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetTimelineSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.PlannedDuration.Should().Be(TimeSpan.FromHours(4));
    }

    [Fact]
    public async Task GetTimelineSummaryAsync_MultipleFiredInjects_FindsBusiestPeriod()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        exercise.ClockElapsedBeforePause = TimeSpan.FromHours(2);
        context.SaveChanges();

        var msel = new Msel { Id = Guid.NewGuid(), Name = "MSEL", Version = 1, IsActive = true, ExerciseId = exercise.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() };
        context.Msels.Add(msel);
        exercise.ActiveMselId = msel.Id;

        var baseTime = DateTime.UtcNow.AddHours(-2);
        // Cluster 4 injects within 10 minutes, then 1 sparse inject 60 min later
        context.Injects.AddRange(
            new Inject { Id = Guid.NewGuid(), InjectNumber = 1, Title = "I1", Description = "D", ScheduledTime = new TimeOnly(9, 0), Target = "T", InjectType = InjectType.Standard, Status = InjectStatus.Released, FiredAt = baseTime, Sequence = 1, MselId = msel.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() },
            new Inject { Id = Guid.NewGuid(), InjectNumber = 2, Title = "I2", Description = "D", ScheduledTime = new TimeOnly(9, 3), Target = "T", InjectType = InjectType.Standard, Status = InjectStatus.Released, FiredAt = baseTime.AddMinutes(3), Sequence = 2, MselId = msel.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() },
            new Inject { Id = Guid.NewGuid(), InjectNumber = 3, Title = "I3", Description = "D", ScheduledTime = new TimeOnly(9, 6), Target = "T", InjectType = InjectType.Standard, Status = InjectStatus.Released, FiredAt = baseTime.AddMinutes(6), Sequence = 3, MselId = msel.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() },
            new Inject { Id = Guid.NewGuid(), InjectNumber = 4, Title = "I4", Description = "D", ScheduledTime = new TimeOnly(9, 9), Target = "T", InjectType = InjectType.Standard, Status = InjectStatus.Released, FiredAt = baseTime.AddMinutes(9), Sequence = 4, MselId = msel.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() },
            new Inject { Id = Guid.NewGuid(), InjectNumber = 5, Title = "I5", Description = "D", ScheduledTime = new TimeOnly(10, 30), Target = "T", InjectType = InjectType.Standard, Status = InjectStatus.Released, FiredAt = baseTime.AddMinutes(90), Sequence = 5, MselId = msel.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() }
        );
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetTimelineSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.InjectPacing.BusiestPeriod.Should().NotBeNull();
        result.InjectPacing.BusiestPeriod!.InjectCount.Should().BeGreaterOrEqualTo(4);
    }
}
