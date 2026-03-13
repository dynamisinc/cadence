using Cadence.Core.Data;
using Cadence.Core.Features.Metrics.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Metrics;

public class ObservationMetricsServiceTests
{
    private static ObservationMetricsService CreateService(AppDbContext context) => new(context);

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
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();
        return exercise;
    }

    private Observation CreateObservation(
        AppDbContext context,
        Guid exerciseId,
        ObservationRating? rating = null,
        string? createdByUserId = null,
        Guid? objectiveId = null,
        Guid? injectId = null)
    {
        var obs = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exerciseId,
            Content = "Test observation",
            Rating = rating,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            ObjectiveId = objectiveId,
            InjectId = injectId,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Observations.Add(obs);
        return obs;
    }

    // =========================================================================
    // GetObservationSummaryAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetObservationSummaryAsync_ReturnsNull_WhenExerciseNotFound()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = await service.GetObservationSummaryAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetObservationSummaryAsync_ReturnsRatingDistribution()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        CreateObservation(context, exercise.Id, ObservationRating.Performed);
        CreateObservation(context, exercise.Id, ObservationRating.Performed);
        CreateObservation(context, exercise.Id, ObservationRating.Satisfactory);
        CreateObservation(context, exercise.Id, ObservationRating.Marginal);
        CreateObservation(context, exercise.Id, ObservationRating.Unsatisfactory);
        CreateObservation(context, exercise.Id, rating: null);
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetObservationSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(6);
        result.RatingDistribution.PerformedCount.Should().Be(2);
        result.RatingDistribution.SatisfactoryCount.Should().Be(1);
        result.RatingDistribution.MarginalCount.Should().Be(1);
        result.RatingDistribution.UnsatisfactoryCount.Should().Be(1);
        result.RatingDistribution.UnratedCount.Should().Be(1);
        result.RatingDistribution.AverageRating.Should().NotBeNull();
    }

    [Fact]
    public async Task GetObservationSummaryAsync_CalculatesObjectiveCoverage()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        var obj1 = new Objective { Id = Guid.NewGuid(), ExerciseId = exercise.Id, ObjectiveNumber = "1", Name = "Obj 1", CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() };
        var obj2 = new Objective { Id = Guid.NewGuid(), ExerciseId = exercise.Id, ObjectiveNumber = "2", Name = "Obj 2", CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() };
        var obj3 = new Objective { Id = Guid.NewGuid(), ExerciseId = exercise.Id, ObjectiveNumber = "3", Name = "Obj 3", CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() };
        context.Set<Objective>().AddRange(obj1, obj2, obj3);

        CreateObservation(context, exercise.Id, objectiveId: obj1.Id);
        CreateObservation(context, exercise.Id, objectiveId: obj2.Id);
        // obj3 has no observations
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetObservationSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.TotalObjectives.Should().Be(3);
        result.ObjectivesCovered.Should().Be(2);
        result.CoverageRate.Should().BeApproximately(66.7m, 0.1m);
        result.UncoveredObjectives.Should().HaveCount(1);
        result.UncoveredObjectives[0].Name.Should().Be("Obj 3");
    }

    [Fact]
    public async Task GetObservationSummaryAsync_GroupsByEvaluator()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        var eval1 = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "eval1@test.com", Email = "eval1@test.com", DisplayName = "Evaluator 1" };
        var eval2 = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "eval2@test.com", Email = "eval2@test.com", DisplayName = "Evaluator 2" };
        context.Users.AddRange(eval1, eval2);

        CreateObservation(context, exercise.Id, ObservationRating.Performed, createdByUserId: eval1.Id);
        CreateObservation(context, exercise.Id, ObservationRating.Satisfactory, createdByUserId: eval1.Id);
        CreateObservation(context, exercise.Id, ObservationRating.Marginal, createdByUserId: eval2.Id);
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetObservationSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.ByEvaluator.Should().HaveCount(2);
        var e1 = result.ByEvaluator.First(e => e.EvaluatorName == "Evaluator 1");
        e1.ObservationCount.Should().Be(2);
        e1.AverageRating.Should().Be(1.5m); // (P=1 + S=2) / 2
    }

    [Fact]
    public async Task GetObservationSummaryAsync_TracksLinkingStatistics()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        var msel = new Msel { Id = Guid.NewGuid(), Name = "MSEL", Version = 1, IsActive = true, ExerciseId = exercise.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() };
        context.Msels.Add(msel);
        var inject = new Inject { Id = Guid.NewGuid(), InjectNumber = 1, Title = "Inj", Description = "D", ScheduledTime = new TimeOnly(9, 0), Target = "T", InjectType = InjectType.Standard, Status = InjectStatus.Draft, Sequence = 1, MselId = msel.Id, CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() };
        context.Injects.Add(inject);

        var obj = new Objective { Id = Guid.NewGuid(), ExerciseId = exercise.Id, ObjectiveNumber = "1", Name = "Obj", CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() };
        context.Set<Objective>().Add(obj);

        CreateObservation(context, exercise.Id, injectId: inject.Id); // linked to inject
        CreateObservation(context, exercise.Id, objectiveId: obj.Id); // linked to objective
        CreateObservation(context, exercise.Id); // unlinked
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetObservationSummaryAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.LinkedToInjectCount.Should().Be(1);
        result.LinkedToObjectiveCount.Should().Be(1);
        result.UnlinkedCount.Should().Be(1);
    }

    // =========================================================================
    // GetEvaluatorCoverageAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetEvaluatorCoverageAsync_ReturnsNull_WhenExerciseNotFound()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = await service.GetEvaluatorCoverageAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEvaluatorCoverageAsync_CalculatesObjectiveCoverageMatrix()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        var obj1 = new Objective { Id = Guid.NewGuid(), ExerciseId = exercise.Id, ObjectiveNumber = "1", Name = "Obj 1", CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() };
        var obj2 = new Objective { Id = Guid.NewGuid(), ExerciseId = exercise.Id, ObjectiveNumber = "2", Name = "Obj 2", CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() };
        context.Set<Objective>().AddRange(obj1, obj2);

        var eval1 = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "e1@t.com", Email = "e1@t.com", DisplayName = "Eval 1" };
        context.Users.Add(eval1);

        // Eval1 covers obj1 with 3 observations, obj2 uncovered
        CreateObservation(context, exercise.Id, ObservationRating.Performed, eval1.Id, obj1.Id);
        CreateObservation(context, exercise.Id, ObservationRating.Satisfactory, eval1.Id, obj1.Id);
        CreateObservation(context, exercise.Id, ObservationRating.Performed, eval1.Id, obj1.Id);
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetEvaluatorCoverageAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.ObjectivesCovered.Should().Be(1);
        result.TotalObjectives.Should().Be(2);
        result.ObjectiveCoverageRate.Should().Be(50.0m);
        result.UncoveredObjectives.Should().HaveCount(1);
        result.CoverageMatrix.Should().HaveCount(2);

        var obj1Row = result.CoverageMatrix.First(r => r.ObjectiveName == "Obj 1");
        obj1Row.TotalObservations.Should().Be(3);
        obj1Row.CoverageStatus.Should().Be("Good");

        var obj2Row = result.CoverageMatrix.First(r => r.ObjectiveName == "Obj 2");
        obj2Row.TotalObservations.Should().Be(0);
        obj2Row.CoverageStatus.Should().Be("None");
    }

    [Fact]
    public async Task GetEvaluatorCoverageAsync_IdentifiesLowCoverageObjectives()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        var obj1 = new Objective { Id = Guid.NewGuid(), ExerciseId = exercise.Id, ObjectiveNumber = "1", Name = "Low Coverage Obj", CreatedBy = Guid.Empty.ToString(), ModifiedBy = Guid.Empty.ToString() };
        context.Set<Objective>().Add(obj1);

        var eval = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "e@t.com", Email = "e@t.com", DisplayName = "Eval" };
        context.Users.Add(eval);

        // Only 1 observation for this objective
        CreateObservation(context, exercise.Id, ObservationRating.Performed, eval.Id, obj1.Id);
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetEvaluatorCoverageAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.LowCoverageObjectives.Should().HaveCount(1);
        result.LowCoverageObjectives[0].ObservationCount.Should().Be(1);
    }

    [Fact]
    public async Task GetEvaluatorCoverageAsync_CalculatesConsistency_WithMultipleEvaluators()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        var eval1 = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "e1@t.com", Email = "e1@t.com", DisplayName = "Lenient Eval" };
        var eval2 = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "e2@t.com", Email = "e2@t.com", DisplayName = "Harsh Eval" };
        context.Users.AddRange(eval1, eval2);

        // Lenient evaluator gives mostly P/S (avg ~1.5)
        CreateObservation(context, exercise.Id, ObservationRating.Performed, eval1.Id);
        CreateObservation(context, exercise.Id, ObservationRating.Satisfactory, eval1.Id);

        // Harsh evaluator gives mostly M/U (avg ~3.5)
        CreateObservation(context, exercise.Id, ObservationRating.Marginal, eval2.Id);
        CreateObservation(context, exercise.Id, ObservationRating.Unsatisfactory, eval2.Id);
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetEvaluatorCoverageAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.Consistency.Should().NotBeNull();
        result.Consistency!.Level.Should().Be("Low"); // StdDev > 0.6
    }

    // =========================================================================
    // GetCapabilityPerformanceAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetCapabilityPerformanceAsync_ReturnsNull_WhenExerciseNotFound()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);

        var result = await service.GetCapabilityPerformanceAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCapabilityPerformanceAsync_CalculatesPerformanceLevels()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        var capability = new Capability { Id = Guid.NewGuid(), Name = "Mass Care", Category = "Response", IsActive = true };
        context.Capabilities.Add(capability);

        // Create observations tagged with the capability
        var obs1 = CreateObservation(context, exercise.Id, ObservationRating.Performed);
        var obs2 = CreateObservation(context, exercise.Id, ObservationRating.Satisfactory);
        context.SaveChanges();

        // Link observations to capability via ObservationCapabilities
        context.Set<ObservationCapability>().AddRange(
            new ObservationCapability { ObservationId = obs1.Id, CapabilityId = capability.Id },
            new ObservationCapability { ObservationId = obs2.Id, CapabilityId = capability.Id }
        );
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetCapabilityPerformanceAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.CapabilitiesEvaluated.Should().Be(1);
        var cap = result.Capabilities.First();
        cap.Name.Should().Be("Mass Care");
        cap.AverageRating.Should().Be(1.5m); // (P=1 + S=2) / 2
        cap.PerformanceLevel.Should().Be("Good"); // avg <= 1.5
        cap.RatingCategory.Should().Be("Performed"); // avg <= 1.5
    }

    [Fact]
    public async Task GetCapabilityPerformanceAsync_TracksTargetCapabilities()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        var targetCap = new Capability { Id = Guid.NewGuid(), Name = "Target Cap", Category = "Response", IsActive = true };
        var unevalCap = new Capability { Id = Guid.NewGuid(), Name = "Unevaluated Cap", Category = "Protection", IsActive = true };
        context.Capabilities.AddRange(targetCap, unevalCap);

        // Set both as target capabilities
        context.ExerciseTargetCapabilities.AddRange(
            new ExerciseTargetCapability { ExerciseId = exercise.Id, CapabilityId = targetCap.Id },
            new ExerciseTargetCapability { ExerciseId = exercise.Id, CapabilityId = unevalCap.Id }
        );

        // Only targetCap gets observations
        var obs = CreateObservation(context, exercise.Id, ObservationRating.Satisfactory);
        context.SaveChanges();
        context.Set<ObservationCapability>().Add(
            new ObservationCapability { ObservationId = obs.Id, CapabilityId = targetCap.Id }
        );
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetCapabilityPerformanceAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.TargetCapabilitiesCount.Should().Be(2);
        result.TargetCapabilitiesEvaluated.Should().Be(1);
        result.TargetCoverageRate.Should().Be(50.0m);
        result.UnevaluatedTargets.Should().HaveCount(1);
        result.UnevaluatedTargets[0].Name.Should().Be("Unevaluated Cap");
    }

    [Fact]
    public async Task GetCapabilityPerformanceAsync_CalculatesTaggingRate()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        var capability = new Capability { Id = Guid.NewGuid(), Name = "Cap", Category = "Test", IsActive = true };
        context.Capabilities.Add(capability);

        // 3 observations total, 1 tagged
        var taggedObs = CreateObservation(context, exercise.Id, ObservationRating.Performed);
        CreateObservation(context, exercise.Id, ObservationRating.Satisfactory); // untagged
        CreateObservation(context, exercise.Id, ObservationRating.Marginal); // untagged
        context.SaveChanges();

        context.Set<ObservationCapability>().Add(
            new ObservationCapability { ObservationId = taggedObs.Id, CapabilityId = capability.Id }
        );
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetCapabilityPerformanceAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.TotalObservations.Should().Be(3);
        result.TotalTaggedObservations.Should().Be(1);
        result.TaggingRate.Should().BeApproximately(33.3m, 0.1m);
    }

    [Fact]
    public async Task GetCapabilityPerformanceAsync_GroupsByCategory()
    {
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        var cap1 = new Capability { Id = Guid.NewGuid(), Name = "Cap A", Category = "Response", IsActive = true };
        var cap2 = new Capability { Id = Guid.NewGuid(), Name = "Cap B", Category = "Response", IsActive = true };
        var cap3 = new Capability { Id = Guid.NewGuid(), Name = "Cap C", Category = "Protection", IsActive = true };
        context.Capabilities.AddRange(cap1, cap2, cap3);

        var obs1 = CreateObservation(context, exercise.Id, ObservationRating.Performed);
        var obs2 = CreateObservation(context, exercise.Id, ObservationRating.Satisfactory);
        var obs3 = CreateObservation(context, exercise.Id, ObservationRating.Marginal);
        context.SaveChanges();

        context.Set<ObservationCapability>().AddRange(
            new ObservationCapability { ObservationId = obs1.Id, CapabilityId = cap1.Id },
            new ObservationCapability { ObservationId = obs2.Id, CapabilityId = cap2.Id },
            new ObservationCapability { ObservationId = obs3.Id, CapabilityId = cap3.Id }
        );
        context.SaveChanges();

        var service = CreateService(context);
        var result = await service.GetCapabilityPerformanceAsync(exercise.Id);

        result.Should().NotBeNull();
        result!.ByCategory.Should().HaveCount(2);
        var response = result.ByCategory.First(c => c.Category == "Response");
        response.CapabilitiesEvaluated.Should().Be(2);
        response.ObservationCount.Should().Be(2);
    }
}
