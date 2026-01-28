using Cadence.Core.Data;
using Cadence.Core.Features.Metrics.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Metrics;

public class ExerciseMetricsServiceTests
{
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
        ExerciseStatus status = ExerciseStatus.Active,
        decimal clockMultiplier = 1.0m)
    {
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = status,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            ClockMultiplier = clockMultiplier,
            ClockState = ExerciseClockState.Stopped,
            ActivatedAt = DateTime.UtcNow.AddHours(-1),
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();

        return exercise;
    }

    private (Msel msel, List<Inject> injects) CreateInjects(
        AppDbContext context,
        Exercise exercise,
        int count = 5)
    {
        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Version = 1,
            IsActive = true,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
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
                Title = $"Test Inject {i}",
                Description = "Description",
                ScheduledTime = new TimeOnly(9 + i, 0),
                DeliveryTime = TimeSpan.FromMinutes(i * 10), // 10, 20, 30, etc minutes
                Target = "Target",
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = i,
                MselId = msel.Id,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
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
        // Arrange
        var (context, _) = CreateTestContext();
        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetExerciseProgressAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExerciseProgressAsync_ReturnsProgress_WithCorrectCounts()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (msel, injects) = CreateInjects(context, exercise, 5);

        // Fire 2 injects, skip 1
        injects[0].Status = InjectStatus.Fired;
        injects[0].FiredAt = DateTime.UtcNow;
        injects[1].Status = InjectStatus.Fired;
        injects[1].FiredAt = DateTime.UtcNow;
        injects[2].Status = InjectStatus.Skipped;
        injects[2].SkippedAt = DateTime.UtcNow;
        context.SaveChanges();

        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetExerciseProgressAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result!.TotalInjects.Should().Be(5);
        result.FiredCount.Should().Be(2);
        result.SkippedCount.Should().Be(1);
        result.PendingCount.Should().Be(2);
    }

    [Fact]
    public async Task GetExerciseProgressAsync_AppliesClockMultiplier_ToElapsedTime()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, clockMultiplier: 2.0m);

        // Set clock state to paused with known elapsed time
        exercise.ClockState = ExerciseClockState.Paused;
        exercise.ClockElapsedBeforePause = TimeSpan.FromMinutes(30); // 30 minutes wall clock
        context.SaveChanges();

        var (msel, _) = CreateInjects(context, exercise, 1);
        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetExerciseProgressAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        // With 2x multiplier, 30 minutes wall clock should equal 60 minutes scenario time
        result!.ElapsedTime.Should().Be(TimeSpan.FromMinutes(60));
    }

    [Fact]
    public async Task GetExerciseProgressAsync_CalculatesElapsedTime_WhenClockPaused()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, clockMultiplier: 1.0m);

        exercise.ClockState = ExerciseClockState.Paused;
        exercise.ClockElapsedBeforePause = TimeSpan.FromMinutes(45);
        context.SaveChanges();

        var (msel, _) = CreateInjects(context, exercise, 1);
        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetExerciseProgressAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result!.ElapsedTime.Should().Be(TimeSpan.FromMinutes(45));
    }

    // =========================================================================
    // GetEvaluatorCoverageAsync Tests - TotalCapabilities fix
    // =========================================================================

    [Fact]
    public async Task GetEvaluatorCoverageAsync_ReturnsNull_WhenExerciseNotFound()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetEvaluatorCoverageAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEvaluatorCoverageAsync_TotalCapabilities_CountsAllActiveCapabilities()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (msel, _) = CreateInjects(context, exercise, 1);

        // The DbContext should have seeded CoreCapabilities
        var totalActiveCapabilities = context.CoreCapabilities.Count(c => c.IsActive);
        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetEvaluatorCoverageAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result!.TotalCapabilities.Should().Be(totalActiveCapabilities);
        // CapabilitiesCovered should be 0 since we have no observations
        result.CapabilitiesCovered.Should().Be(0);
    }

    // =========================================================================
    // GetControllerActivityAsync Tests - Variance calculation fix
    // =========================================================================

    [Fact]
    public async Task GetControllerActivityAsync_ReturnsNull_WhenExerciseNotFound()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetControllerActivityAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetControllerActivityAsync_CalculatesVariance_UsingDeliveryTime()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var activatedAt = DateTime.UtcNow.AddHours(-2);
        exercise.ActivatedAt = activatedAt;
        context.SaveChanges();

        var (msel, injects) = CreateInjects(context, exercise, 2);

        // Create a test user
        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testcontroller",
            DisplayName = "Test Controller"
        };
        context.ApplicationUsers.Add(user);

        // Fire inject 1 exactly on time (DeliveryTime = 10 minutes from activation)
        var inject1 = injects[0];
        inject1.Status = InjectStatus.Fired;
        inject1.FiredAt = activatedAt + inject1.DeliveryTime!.Value; // Exactly on time
        inject1.FiredByUserId = userId;

        // Fire inject 2 late (DeliveryTime = 20 minutes, but fired at 25 minutes)
        var inject2 = injects[1];
        inject2.Status = InjectStatus.Fired;
        inject2.FiredAt = activatedAt + inject2.DeliveryTime!.Value + TimeSpan.FromMinutes(5); // 5 min late
        inject2.FiredByUserId = userId;

        context.SaveChanges();

        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetControllerActivityAsync(exercise.Id, onTimeToleranceMinutes: 2);

        // Assert
        result.Should().NotBeNull();
        result!.Controllers.Should().HaveCount(1);

        var controller = result.Controllers[0];
        controller.InjectsFired.Should().Be(2);
        // One on-time (within 2 min tolerance), one late (5 min late > 2 min tolerance)
        controller.OnTimeCount.Should().Be(1);
        controller.OnTimeRate.Should().Be(50); // 1/2 = 50%
    }

    [Fact]
    public async Task GetControllerActivityAsync_SkipsVarianceCalculation_WhenNoDeliveryTime()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        exercise.ActivatedAt = DateTime.UtcNow.AddHours(-1);
        context.SaveChanges();

        var (msel, injects) = CreateInjects(context, exercise, 1);

        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testcontroller",
            DisplayName = "Test Controller"
        };
        context.ApplicationUsers.Add(user);

        // Fire inject without DeliveryTime set
        var inject = injects[0];
        inject.DeliveryTime = null; // No delivery time
        inject.Status = InjectStatus.Fired;
        inject.FiredAt = DateTime.UtcNow;
        inject.FiredByUserId = userId;
        context.SaveChanges();

        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetControllerActivityAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Controllers.Should().HaveCount(1);
        var controller = result.Controllers[0];
        controller.InjectsFired.Should().Be(1);
        // No variance calculation because DeliveryTime is null
        controller.OnTimeCount.Should().Be(0);
        controller.AverageVariance.Should().BeNull();
    }
}
