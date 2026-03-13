using Cadence.Core.Data;
using Cadence.Core.Features.Metrics.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Metrics;

public class ProgressMetricsServiceTests
{
    private static ProgressMetricsService CreateService(AppDbContext context) => new(context);

    private (AppDbContext context, Organization org) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);
        context.SaveChanges();
        return (context, org);
    }

    private Exercise CreateExercise(
        AppDbContext context,
        Organization org,
        ExerciseClockState clockState = ExerciseClockState.Stopped,
        decimal clockMultiplier = 1.0m)
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
            ClockMultiplier = clockMultiplier,
            ClockState = clockState,
            ActivatedAt = DateTime.UtcNow.AddHours(-1),
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();
        return exercise;
    }

    private (Msel msel, List<Inject> injects) CreateInjects(
        AppDbContext context, Exercise exercise, int count = 5)
    {
        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Version = 1,
            IsActive = true,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Msels.Add(msel);
        exercise.ActiveMselId = msel.Id;

        var injects = new List<Inject>();
        for (var i = 1; i <= count; i++)
        {
            var inject = new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = i,
                Title = $"Inject {i}",
                Description = "Desc",
                ScheduledTime = new TimeOnly(9 + i, 0),
                DeliveryTime = TimeSpan.FromMinutes(i * 10),
                Target = "Target",
                InjectType = InjectType.Standard,
                Status = InjectStatus.Draft,
                Sequence = i,
                MselId = msel.Id,
                CreatedBy = Guid.Empty.ToString(),
                ModifiedBy = Guid.Empty.ToString()
            };
            injects.Add(inject);
            context.Injects.Add(inject);
        }

        context.SaveChanges();
        return (msel, injects);
    }

    // =========================================================================
    // GetExerciseProgressAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetExerciseProgressAsync_ReturnsNull_WhenExerciseNotFound()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = await service.GetExerciseProgressAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExerciseProgressAsync_ReturnsZeroCounts_WhenNoMsel()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var service = CreateService(context);

        var result = await service.GetExerciseProgressAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.TotalInjects.Should().Be(0);
        result.FiredCount.Should().Be(0);
        result.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public async Task GetExerciseProgressAsync_CalculatesCorrectCounts()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (_, injects) = CreateInjects(context, exercise, 5);

        // Fire 2, skip 1, ready 1, draft 1
        injects[0].Status = InjectStatus.Released;
        injects[0].FiredAt = DateTime.UtcNow;
        injects[1].Status = InjectStatus.Released;
        injects[1].FiredAt = DateTime.UtcNow;
        injects[2].Status = InjectStatus.Deferred;
        injects[3].Status = InjectStatus.Synchronized;
        // injects[4] stays Draft
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetExerciseProgressAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.TotalInjects.Should().Be(5);
        result.FiredCount.Should().Be(2);
        result.SkippedCount.Should().Be(1);
        result.ReadyCount.Should().Be(1);
        result.PendingCount.Should().Be(2); // Draft + Synchronized
        result.ProgressPercentage.Should().Be(60.0m); // (2+1)/5 * 100
    }

    [Fact]
    public async Task GetExerciseProgressAsync_ReturnsRatingCounts()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        // Add observations with different ratings
        context.Observations.AddRange(
            new Observation { Id = Guid.NewGuid(), ExerciseId = exercise.Id, Content = "obs1", Rating = ObservationRating.Performed, ObservedAt = DateTime.UtcNow, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() },
            new Observation { Id = Guid.NewGuid(), ExerciseId = exercise.Id, Content = "obs2", Rating = ObservationRating.Satisfactory, ObservedAt = DateTime.UtcNow, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() },
            new Observation { Id = Guid.NewGuid(), ExerciseId = exercise.Id, Content = "obs3", Rating = ObservationRating.Marginal, ObservedAt = DateTime.UtcNow, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() },
            new Observation { Id = Guid.NewGuid(), ExerciseId = exercise.Id, Content = "obs4", Rating = null, ObservedAt = DateTime.UtcNow, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() }
        );
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetExerciseProgressAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.ObservationCount.Should().Be(4);
        result.RatingCounts.Performed.Should().Be(1);
        result.RatingCounts.Satisfactory.Should().Be(1);
        result.RatingCounts.Marginal.Should().Be(1);
        result.RatingCounts.Unsatisfactory.Should().Be(0);
        result.RatingCounts.Unrated.Should().Be(1);
    }

    [Fact]
    public async Task GetExerciseProgressAsync_ReturnsNextInjects_OrderedBySequence()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (_, injects) = CreateInjects(context, exercise, 5);

        // Fire first 2 so next 3 are returned
        injects[0].Status = InjectStatus.Released;
        injects[0].FiredAt = DateTime.UtcNow;
        injects[1].Status = InjectStatus.Released;
        injects[1].FiredAt = DateTime.UtcNow;
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetExerciseProgressAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.NextInjects.Should().HaveCount(3);
        result.NextInjects[0].InjectNumber.Should().Be(3);
        result.NextInjects[1].InjectNumber.Should().Be(4);
        result.NextInjects[2].InjectNumber.Should().Be(5);
    }

    [Fact]
    public async Task GetExerciseProgressAsync_AppliesClockMultiplier()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, clockMultiplier: 2.0m);
        exercise.ClockState = ExerciseClockState.Stopped;
        exercise.ClockElapsedBeforePause = TimeSpan.FromMinutes(30);
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetExerciseProgressAsync(exercise.Id);

        result.Should().NotBeNull();
        // 30 min wall clock * 2x multiplier = 60 min scenario time
        result!.ElapsedTime.Should().BeCloseTo(TimeSpan.FromMinutes(60), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetExerciseProgressAsync_CurrentPhase_ReturnsLastFiredInjectPhase()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (msel, injects) = CreateInjects(context, exercise, 3);

        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Phase Alpha",
            Sequence = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Phases.Add(phase);

        injects[0].PhaseId = phase.Id;
        injects[0].Status = InjectStatus.Released;
        injects[0].FiredAt = DateTime.UtcNow;
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetExerciseProgressAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.CurrentPhaseName.Should().Be("Phase Alpha");
    }

    [Fact]
    public async Task GetExerciseProgressAsync_ReturnsNull_WhenExerciseDeleted()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        exercise.IsDeleted = true;
        exercise.DeletedAt = DateTime.UtcNow;
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetExerciseProgressAsync(exercise.Id);

        result.Should().BeNull();
    }
}
