using Cadence.Core.Data;
using Cadence.Core.Features.Metrics.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Metrics;

public class InjectMetricsServiceTests
{
    private static InjectMetricsService CreateService(AppDbContext context) => new(context);

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
            ActivatedAt = DateTime.UtcNow.AddHours(-2),
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
    // GetInjectSummaryAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetInjectSummaryAsync_ReturnsNull_WhenExerciseNotFound()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = await service.GetInjectSummaryAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetInjectSummaryAsync_ReturnsCorrectCounts()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (_, injects) = CreateInjects(context, exercise, 5);

        injects[0].Status = InjectStatus.Released;
        injects[0].FiredAt = DateTime.UtcNow;
        injects[1].Status = InjectStatus.Released;
        injects[1].FiredAt = DateTime.UtcNow;
        injects[2].Status = InjectStatus.Deferred;
        // injects[3] and [4] remain Draft
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetInjectSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(5);
        result.FiredCount.Should().Be(2);
        result.SkippedCount.Should().Be(1);
        result.NotExecutedCount.Should().Be(2);
        result.FiredPercentage.Should().Be(40.0m);
        result.SkippedPercentage.Should().Be(20.0m);
        result.NotExecutedPercentage.Should().Be(40.0m);
    }

    [Fact]
    public async Task GetInjectSummaryAsync_CalculatesTimingMetrics()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var activatedAt = exercise.ActivatedAt!.Value;
        var (_, injects) = CreateInjects(context, exercise, 3);

        // Fire injects at various times relative to their scheduled DeliveryTime
        // Inject 1: DeliveryTime = 10 min, fired exactly on time
        injects[0].Status = InjectStatus.Released;
        injects[0].FiredAt = activatedAt + TimeSpan.FromMinutes(10);

        // Inject 2: DeliveryTime = 20 min, fired 3 min late (within tolerance)
        injects[1].Status = InjectStatus.Released;
        injects[1].FiredAt = activatedAt + TimeSpan.FromMinutes(23);

        // Inject 3: DeliveryTime = 30 min, fired 10 min late (outside 5-min tolerance)
        injects[2].Status = InjectStatus.Released;
        injects[2].FiredAt = activatedAt + TimeSpan.FromMinutes(40);

        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetInjectSummaryAsync(exercise.Id, onTimeToleranceMinutes: 5);

        result.Should().NotBeNull();
        result!.OnTimeCount.Should().Be(2); // Inject 1 (0 min var) and 2 (3 min var) are within 5 min
        result.OnTimeRate.Should().BeApproximately(66.7m, 0.1m); // 2/3 * 100
        result.AverageVariance.Should().NotBeNull();
    }

    [Fact]
    public async Task GetInjectSummaryAsync_GroupsByPhase()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (_, injects) = CreateInjects(context, exercise, 4);

        var phase1 = new Phase { Id = Guid.NewGuid(), Name = "Phase 1", Sequence = 1, ExerciseId = exercise.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() };
        var phase2 = new Phase { Id = Guid.NewGuid(), Name = "Phase 2", Sequence = 2, ExerciseId = exercise.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() };
        context.Phases.AddRange(phase1, phase2);

        injects[0].PhaseId = phase1.Id;
        injects[0].Status = InjectStatus.Released;
        injects[0].FiredAt = DateTime.UtcNow;
        injects[1].PhaseId = phase1.Id;
        injects[1].Status = InjectStatus.Released;
        injects[1].FiredAt = DateTime.UtcNow;
        injects[2].PhaseId = phase2.Id;
        injects[3].PhaseId = phase2.Id;
        injects[3].Status = InjectStatus.Deferred;
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetInjectSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.ByPhase.Should().HaveCount(2);
        var p1 = result.ByPhase.First(p => p.PhaseName == "Phase 1");
        p1.FiredCount.Should().Be(2);
        p1.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetInjectSummaryAsync_ReturnsSkippedInjectsWithReasons()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (_, injects) = CreateInjects(context, exercise, 2);

        injects[0].Status = InjectStatus.Deferred;
        injects[0].SkipReason = "Not applicable";
        injects[0].SkippedAt = DateTime.UtcNow;
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetInjectSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.SkippedInjects.Should().HaveCount(1);
        result.SkippedInjects[0].SkipReason.Should().Be("Not applicable");
    }

    [Fact]
    public async Task GetInjectSummaryAsync_NoTimingData_ReturnsNullTimingMetrics()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (_, injects) = CreateInjects(context, exercise, 2);

        // Fire inject but without DeliveryTime
        injects[0].Status = InjectStatus.Released;
        injects[0].FiredAt = DateTime.UtcNow;
        injects[0].DeliveryTime = null;
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetInjectSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.OnTimeRate.Should().BeNull();
        result.AverageVariance.Should().BeNull();
    }

    // =========================================================================
    // GetControllerActivityAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetControllerActivityAsync_ReturnsNull_WhenExerciseNotFound()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = await service.GetControllerActivityAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetControllerActivityAsync_GroupsByController()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (_, injects) = CreateInjects(context, exercise, 4);

        var user1Id = Guid.NewGuid().ToString();
        var user2Id = Guid.NewGuid().ToString();

        // Add users
        var user1 = new ApplicationUser { Id = user1Id, UserName = "user1@test.com", Email = "user1@test.com", DisplayName = "Controller A" };
        var user2 = new ApplicationUser { Id = user2Id, UserName = "user2@test.com", Email = "user2@test.com", DisplayName = "Controller B" };
        context.Users.AddRange(user1, user2);

        injects[0].Status = InjectStatus.Released;
        injects[0].FiredAt = DateTime.UtcNow;
        injects[0].FiredByUserId = user1Id;
        injects[1].Status = InjectStatus.Released;
        injects[1].FiredAt = DateTime.UtcNow;
        injects[1].FiredByUserId = user1Id;
        injects[2].Status = InjectStatus.Released;
        injects[2].FiredAt = DateTime.UtcNow;
        injects[2].FiredByUserId = user2Id;
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetControllerActivityAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.TotalControllers.Should().Be(2);
        result.TotalInjectsFired.Should().Be(3);

        var controllerA = result.Controllers.First(c => c.ControllerName == "Controller A");
        controllerA.InjectsFired.Should().Be(2);
        controllerA.WorkloadPercentage.Should().BeApproximately(66.7m, 0.1m);
    }

    [Fact]
    public async Task GetControllerActivityAsync_IncludesSkippedInjectCounts()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (_, injects) = CreateInjects(context, exercise, 3);

        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser { Id = userId, UserName = "ctrl@test.com", Email = "ctrl@test.com", DisplayName = "Controller" };
        context.Users.Add(user);

        injects[0].Status = InjectStatus.Released;
        injects[0].FiredAt = DateTime.UtcNow;
        injects[0].FiredByUserId = userId;
        injects[1].Status = InjectStatus.Deferred;
        injects[1].SkippedByUserId = userId;
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetControllerActivityAsync(exercise.Id);

        result.Should().NotBeNull();
        var ctrl = result!.Controllers.First();
        ctrl.InjectsFired.Should().Be(1);
        ctrl.InjectsSkipped.Should().Be(1);
        result.TotalInjectsSkipped.Should().Be(1);
    }

    [Fact]
    public async Task GetControllerActivityAsync_CalculatesOnTimeMetrics()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var activatedAt = exercise.ActivatedAt!.Value;
        var (_, injects) = CreateInjects(context, exercise, 2);

        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser { Id = userId, UserName = "ctrl@test.com", Email = "ctrl@test.com", DisplayName = "Controller" };
        context.Users.Add(user);

        // On time: fired within 5 min of expected
        injects[0].Status = InjectStatus.Released;
        injects[0].FiredAt = activatedAt + TimeSpan.FromMinutes(10);
        injects[0].FiredByUserId = userId;

        // Late: fired 15 min after expected
        injects[1].Status = InjectStatus.Released;
        injects[1].FiredAt = activatedAt + TimeSpan.FromMinutes(35); // expected 20, fired at 35
        injects[1].FiredByUserId = userId;

        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetControllerActivityAsync(exercise.Id, onTimeToleranceMinutes: 5);

        result.Should().NotBeNull();
        var ctrl = result!.Controllers.First();
        ctrl.OnTimeCount.Should().Be(1);
        ctrl.OnTimeRate.Should().Be(50.0m);
        ctrl.AverageVariance.Should().NotBeNull();
    }
}
