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
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
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
                Title = $"Test Inject {i}",
                Description = "Description",
                ScheduledTime = new TimeOnly(9 + i, 0),
                DeliveryTime = TimeSpan.FromMinutes(i * 10), // 10, 20, 30, etc minutes
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
        injects[0].Status = InjectStatus.Released;
        injects[0].FiredAt = DateTime.UtcNow;
        injects[1].Status = InjectStatus.Released;
        injects[1].FiredAt = DateTime.UtcNow;
        injects[2].Status = InjectStatus.Deferred;
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

        // Capabilities are now organization-scoped, count active ones for this org
        var totalActiveCapabilities = context.Capabilities.Count(c => c.IsActive && c.OrganizationId == org.Id);
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
        inject1.Status = InjectStatus.Released;
        inject1.FiredAt = activatedAt + inject1.DeliveryTime!.Value; // Exactly on time
        inject1.FiredByUserId = userId;

        // Fire inject 2 late (DeliveryTime = 20 minutes, but fired at 25 minutes)
        var inject2 = injects[1];
        inject2.Status = InjectStatus.Released;
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
        inject.Status = InjectStatus.Released;
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

    // =========================================================================
    // GetCapabilityPerformanceAsync Tests (S06)
    // =========================================================================

    [Fact]
    public async Task GetCapabilityPerformanceAsync_ReturnsNull_WhenExerciseNotFound()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetCapabilityPerformanceAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCapabilityPerformanceAsync_ReturnsCorrectCounts()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (msel, injects) = CreateInjects(context, exercise, 1);

        // Create capabilities
        var capability1 = new Capability
        {
            Id = Guid.NewGuid(),
            Name = "Mass Care Services",
            Category = "Response",
            IsActive = true,
            OrganizationId = org.Id,
        };
        var capability2 = new Capability
        {
            Id = Guid.NewGuid(),
            Name = "Planning",
            Category = "All Areas",
            IsActive = true,
            OrganizationId = org.Id,


        };
        context.Capabilities.AddRange(capability1, capability2);

        // Create observations with capability tags
        var userId = Guid.NewGuid().ToString();
        var obs1 = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Observation 1",
            Rating = ObservationRating.Performed,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,


        };
        var obs2 = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Observation 2",
            Rating = ObservationRating.Satisfactory,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,


        };
        context.Observations.AddRange(obs1, obs2);

        // Link observations to capability
        var obsCapability1 = new ObservationCapability
        {
            ObservationId = obs1.Id,
            CapabilityId = capability1.Id
        };
        var obsCapability2 = new ObservationCapability
        {
            ObservationId = obs2.Id,
            CapabilityId = capability1.Id
        };
        context.Set<ObservationCapability>().AddRange(obsCapability1, obsCapability2);
        context.SaveChanges();

        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetCapabilityPerformanceAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result!.CapabilitiesEvaluated.Should().Be(1);
        result.TotalTaggedObservations.Should().Be(2);
        result.TotalObservations.Should().Be(2);
        result.TaggingRate.Should().Be(100m);
    }

    [Fact]
    public async Task GetCapabilityPerformanceAsync_CalculatesAverageRating()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (msel, injects) = CreateInjects(context, exercise, 1);

        var capability = new Capability
        {
            Id = Guid.NewGuid(),
            Name = "Operational Communications",
            Category = "Response",
            IsActive = true,
            OrganizationId = org.Id,


        };
        context.Capabilities.Add(capability);

        var userId = Guid.NewGuid().ToString();
        // Create observations with ratings: P=1, S=2, M=3, U=4
        // Average = (1 + 2 + 3 + 4) / 4 = 2.5
        var observations = new[]
        {
            ObservationRating.Performed,      // 1
            ObservationRating.Satisfactory,   // 2
            ObservationRating.Marginal,       // 3
            ObservationRating.Unsatisfactory  // 4
        }.Select(rating => new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = $"Observation {rating}",
            Rating = rating,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,


        }).ToList();

        context.Observations.AddRange(observations);

        foreach (var obs in observations)
        {
            context.Set<ObservationCapability>().Add(new ObservationCapability
            {
                ObservationId = obs.Id,
                CapabilityId = capability.Id
            });
        }
        context.SaveChanges();

        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetCapabilityPerformanceAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        var capPerf = result!.Capabilities.Should().ContainSingle().Subject;
        capPerf.Name.Should().Be("Operational Communications");
        capPerf.ObservationCount.Should().Be(4);
        capPerf.AverageRating.Should().Be(2.5m); // (1+2+3+4)/4
        capPerf.RatingCategory.Should().Be("Satisfactory"); // 2.5 is in Satisfactory range
        capPerf.RatingCounts.Performed.Should().Be(1);
        capPerf.RatingCounts.Satisfactory.Should().Be(1);
        capPerf.RatingCounts.Marginal.Should().Be(1);
        capPerf.RatingCounts.Unsatisfactory.Should().Be(1);
        capPerf.RatingCounts.Unrated.Should().Be(0);
    }

    [Fact]
    public async Task GetCapabilityPerformanceAsync_IdentifiesTargetCapabilities()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (msel, injects) = CreateInjects(context, exercise, 1);

        var targetCapability = new Capability
        {
            Id = Guid.NewGuid(),
            Name = "Mass Care Services",
            Category = "Response",
            IsActive = true,
            OrganizationId = org.Id,


        };
        var nonTargetCapability = new Capability
        {
            Id = Guid.NewGuid(),
            Name = "Planning",
            Category = "All Areas",
            IsActive = true,
            OrganizationId = org.Id,


        };
        context.Capabilities.AddRange(targetCapability, nonTargetCapability);

        // Mark first capability as target
        var exerciseTarget = new ExerciseTargetCapability
        {
            ExerciseId = exercise.Id,
            CapabilityId = targetCapability.Id,


        };
        context.Set<ExerciseTargetCapability>().Add(exerciseTarget);

        var userId = Guid.NewGuid().ToString();
        // Create observations for both
        var obs1 = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Target observation",
            Rating = ObservationRating.Performed,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,


        };
        var obs2 = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Non-target observation",
            Rating = ObservationRating.Satisfactory,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,


        };
        context.Observations.AddRange(obs1, obs2);

        context.Set<ObservationCapability>().AddRange(
            new ObservationCapability { ObservationId = obs1.Id, CapabilityId = targetCapability.Id },
            new ObservationCapability { ObservationId = obs2.Id, CapabilityId = nonTargetCapability.Id }
        );
        context.SaveChanges();

        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetCapabilityPerformanceAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result!.TargetCapabilitiesCount.Should().Be(1);
        result.TargetCapabilitiesEvaluated.Should().Be(1);
        result.TargetCoverageRate.Should().Be(100m);

        var targetCap = result.Capabilities.Should().Contain(c => c.Name == "Mass Care Services").Subject;
        targetCap.IsTargetCapability.Should().BeTrue();

        var nonTargetCap = result.Capabilities.Should().Contain(c => c.Name == "Planning").Subject;
        nonTargetCap.IsTargetCapability.Should().BeFalse();
    }

    [Fact]
    public async Task GetCapabilityPerformanceAsync_FindsUnevaluatedTargets()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (msel, injects) = CreateInjects(context, exercise, 1);

        var evaluatedCapability = new Capability
        {
            Id = Guid.NewGuid(),
            Name = "Mass Care Services",
            Category = "Response",
            IsActive = true,
            OrganizationId = org.Id,


        };
        var unevaluatedCapability = new Capability
        {
            Id = Guid.NewGuid(),
            Name = "Intelligence and Information Sharing",
            Category = "Prevention",
            IsActive = true,
            OrganizationId = org.Id,


        };
        context.Capabilities.AddRange(evaluatedCapability, unevaluatedCapability);

        // Mark both as targets
        context.Set<ExerciseTargetCapability>().AddRange(
            new ExerciseTargetCapability
            {
                ExerciseId = exercise.Id,
                CapabilityId = evaluatedCapability.Id,


            },
            new ExerciseTargetCapability
            {
                ExerciseId = exercise.Id,
                CapabilityId = unevaluatedCapability.Id,


            }
        );

        // Only create observation for first capability
        var userId = Guid.NewGuid().ToString();
        var obs = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Observation",
            Rating = ObservationRating.Satisfactory,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,


        };
        context.Observations.Add(obs);
        context.Set<ObservationCapability>().Add(new ObservationCapability
        {
            ObservationId = obs.Id,
            CapabilityId = evaluatedCapability.Id
        });
        context.SaveChanges();

        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetCapabilityPerformanceAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result!.TargetCapabilitiesCount.Should().Be(2);
        result.TargetCapabilitiesEvaluated.Should().Be(1);
        result.TargetCoverageRate.Should().Be(50m);

        result.UnevaluatedTargets.Should().ContainSingle();
        var gap = result.UnevaluatedTargets[0];
        gap.Name.Should().Be("Intelligence and Information Sharing");
        gap.Category.Should().Be("Prevention");
    }

    [Fact]
    public async Task GetCapabilityPerformanceAsync_SortsByWorstRatingFirst()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (msel, injects) = CreateInjects(context, exercise, 1);

        var capabilities = new[]
        {
            new Capability { Id = Guid.NewGuid(), Name = "Good Capability", Category = "Response", IsActive = true, OrganizationId = org.Id },
            new Capability { Id = Guid.NewGuid(), Name = "Bad Capability", Category = "Response", IsActive = true, OrganizationId = org.Id },
            new Capability { Id = Guid.NewGuid(), Name = "Medium Capability", Category = "Response", IsActive = true, OrganizationId = org.Id,   }
        };
        context.Capabilities.AddRange(capabilities);

        var userId = Guid.NewGuid().ToString();
        // Good capability: P rating (1.0)
        var obsGood = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Good",
            Rating = ObservationRating.Performed,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,


        };
        context.Observations.Add(obsGood);
        context.Set<ObservationCapability>().Add(new ObservationCapability
        {
            ObservationId = obsGood.Id,
            CapabilityId = capabilities[0].Id
        });

        // Bad capability: U rating (4.0)
        var obsBad = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Bad",
            Rating = ObservationRating.Unsatisfactory,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,


        };
        context.Observations.Add(obsBad);
        context.Set<ObservationCapability>().Add(new ObservationCapability
        {
            ObservationId = obsBad.Id,
            CapabilityId = capabilities[1].Id
        });

        // Medium capability: S rating (2.0)
        var obsMedium = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Medium",
            Rating = ObservationRating.Satisfactory,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,


        };
        context.Observations.Add(obsMedium);
        context.Set<ObservationCapability>().Add(new ObservationCapability
        {
            ObservationId = obsMedium.Id,
            CapabilityId = capabilities[2].Id
        });
        context.SaveChanges();

        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetCapabilityPerformanceAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Capabilities.Should().HaveCount(3);
        // Should be sorted by rating descending (worst first), then by name
        result.Capabilities[0].Name.Should().Be("Bad Capability");
        result.Capabilities[0].AverageRating.Should().Be(4m);
        result.Capabilities[1].Name.Should().Be("Medium Capability");
        result.Capabilities[1].AverageRating.Should().Be(2m);
        result.Capabilities[2].Name.Should().Be("Good Capability");
        result.Capabilities[2].AverageRating.Should().Be(1m);
    }

    [Fact]
    public async Task GetCapabilityPerformanceAsync_GroupsByCategory()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (msel, injects) = CreateInjects(context, exercise, 1);

        var responseCapability = new Capability
        {
            Id = Guid.NewGuid(),
            Name = "Mass Care Services",
            Category = "Response",
            IsActive = true,
            OrganizationId = org.Id,


        };
        var preventionCapability = new Capability
        {
            Id = Guid.NewGuid(),
            Name = "Intelligence",
            Category = "Prevention",
            IsActive = true,
            OrganizationId = org.Id,


        };
        context.Capabilities.AddRange(responseCapability, preventionCapability);

        var userId = Guid.NewGuid().ToString();
        // Two observations for Response (P and S)
        var obsResp1 = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Resp 1",
            Rating = ObservationRating.Performed,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,


        };
        var obsResp2 = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Resp 2",
            Rating = ObservationRating.Satisfactory,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,


        };
        context.Observations.AddRange(obsResp1, obsResp2);
        context.Set<ObservationCapability>().AddRange(
            new ObservationCapability { ObservationId = obsResp1.Id, CapabilityId = responseCapability.Id },
            new ObservationCapability { ObservationId = obsResp2.Id, CapabilityId = responseCapability.Id }
        );

        // One observation for Prevention (M)
        var obsPrev = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Prev",
            Rating = ObservationRating.Marginal,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,


        };
        context.Observations.Add(obsPrev);
        context.Set<ObservationCapability>().Add(new ObservationCapability
        {
            ObservationId = obsPrev.Id,
            CapabilityId = preventionCapability.Id
        });
        context.SaveChanges();

        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetCapabilityPerformanceAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result!.ByCategory.Should().HaveCount(2);

        var prevention = result.ByCategory.Should().Contain(c => c.Category == "Prevention").Subject;
        prevention.CapabilitiesEvaluated.Should().Be(1);
        prevention.ObservationCount.Should().Be(1);
        prevention.AverageRating.Should().Be(3m);

        var response = result.ByCategory.Should().Contain(c => c.Category == "Response").Subject;
        response.CapabilitiesEvaluated.Should().Be(1);
        response.ObservationCount.Should().Be(2);
        // Response average: (1 + 2) / 2 = 1.5
        response.AverageRating.Should().Be(1.5m);
    }

    [Fact]
    public async Task GetCapabilityPerformanceAsync_HandlesNoCapabilityTags()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var (msel, injects) = CreateInjects(context, exercise, 1);

        // Create observation WITHOUT capability tags
        var userId = Guid.NewGuid().ToString();
        var obs = new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Content = "Untagged observation",
            Rating = ObservationRating.Satisfactory,
            ObservedAt = DateTime.UtcNow,
            CreatedByUserId = userId,


        };
        context.Observations.Add(obs);
        context.SaveChanges();

        var service = new ExerciseMetricsService(context);

        // Act
        var result = await service.GetCapabilityPerformanceAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result!.TotalObservations.Should().Be(1);
        result.TotalTaggedObservations.Should().Be(0);
        result.TaggingRate.Should().Be(0m);
        result.CapabilitiesEvaluated.Should().Be(0);
        result.Capabilities.Should().BeEmpty();
    }
}
