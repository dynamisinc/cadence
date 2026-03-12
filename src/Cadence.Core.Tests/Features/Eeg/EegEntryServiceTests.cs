using Cadence.Core.Data;
using Cadence.Core.Features.Eeg.Models.DTOs;
using Cadence.Core.Features.Eeg.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cadence.Core.Tests.Features.Eeg;

/// <summary>
/// Tests for EegEntryService covering CRUD operations, org isolation, pagination/filtering,
/// coverage calculation, and SignalR broadcast verification.
/// </summary>
public class EegEntryServiceTests
{
    private readonly Mock<ICurrentOrganizationContext> _orgContextMock = new();
    private readonly Mock<IExerciseHubContext> _hubContextMock = new();

    public EegEntryServiceTests()
    {
        // Default hub methods to return completed tasks so they don't throw
        _hubContextMock
            .Setup(x => x.NotifyEegEntryCreated(It.IsAny<Guid>(), It.IsAny<EegEntryDto>()))
            .Returns(Task.CompletedTask);
        _hubContextMock
            .Setup(x => x.NotifyEegEntryUpdated(It.IsAny<Guid>(), It.IsAny<EegEntryDto>()))
            .Returns(Task.CompletedTask);
        _hubContextMock
            .Setup(x => x.NotifyEegEntryDeleted(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
    }

    // =========================================================================
    // Test Context Helpers
    // =========================================================================

    /// <summary>
    /// Builds the full entity chain required for EegEntry tests:
    /// Organization -> Exercise -> Capability -> CapabilityTarget -> CriticalTask.
    /// </summary>
    private TestChain CreateTestChain(AppDbContext context, string? userId = null)
    {
        userId ??= Guid.NewGuid().ToString();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Hurricane Response TTX",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            DeliveryMode = DeliveryMode.FacilitatorPaced,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.Exercises.Add(exercise);

        var capability = new Capability
        {
            Id = Guid.NewGuid(),
            Name = "Operational Communications",
            OrganizationId = org.Id
        };
        context.Capabilities.Add(capability);

        var capabilityTarget = new CapabilityTarget
        {
            Id = Guid.NewGuid(),
            TargetDescription = "Establish interoperable communications within 30 minutes",
            SortOrder = 0,
            OrganizationId = org.Id,
            ExerciseId = exercise.Id,
            CapabilityId = capability.Id,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.CapabilityTargets.Add(capabilityTarget);

        var criticalTask = new CriticalTask
        {
            Id = Guid.NewGuid(),
            TaskDescription = "Issue EOC activation notification",
            Standard = "Per SOP 5.2",
            SortOrder = 0,
            OrganizationId = org.Id,
            CapabilityTargetId = capabilityTarget.Id,
            CreatedBy = userId,
            ModifiedBy = userId
        };
        context.CriticalTasks.Add(criticalTask);

        context.SaveChanges();

        return new TestChain(org, exercise, capability, capabilityTarget, criticalTask, userId);
    }

    private EegEntry CreateEegEntry(
        AppDbContext context,
        TestChain chain,
        string? evaluatorId = null,
        PerformanceRating rating = PerformanceRating.Performed,
        string? observationText = null,
        DateTime? observedAt = null)
    {
        evaluatorId ??= chain.UserId;
        var now = DateTime.UtcNow;

        var entry = new EegEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = chain.Org.Id,
            CriticalTaskId = chain.CriticalTask.Id,
            ObservationText = observationText ?? "EOC activated per notification procedures.",
            Rating = rating,
            ObservedAt = observedAt ?? now,
            RecordedAt = now,
            EvaluatorId = evaluatorId,
            CreatedBy = evaluatorId,
            ModifiedBy = evaluatorId
        };
        context.EegEntries.Add(entry);
        context.SaveChanges();
        return entry;
    }

    private EegEntryService CreateService(AppDbContext context)
    {
        return new EegEntryService(context, _orgContextMock.Object, _hubContextMock.Object);
    }

    private void SetupOrgContext(Guid orgId)
    {
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(orgId);
    }

    // =========================================================================
    // Inner helper record
    // =========================================================================

    private record TestChain(
        Organization Org,
        Exercise Exercise,
        Capability Capability,
        CapabilityTarget CapabilityTarget,
        CriticalTask CriticalTask,
        string UserId);

    // =========================================================================
    // GetByExerciseAsync Tests
    // =========================================================================

    #region GetByExerciseAsync

    [Fact]
    public async Task GetByExerciseAsync_WithEntries_ReturnsMatchingEntries()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        CreateEegEntry(context, chain, observationText: "First observation");
        CreateEegEntry(context, chain, observationText: "Second observation");

        var service = CreateService(context);

        // Act
        var result = await service.GetByExerciseAsync(chain.Exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByExerciseAsync_EmptyExercise_ReturnsEmptyList()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        // Act
        var result = await service.GetByExerciseAsync(chain.Exercise.Id);

        // Assert
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByExerciseAsync_DifferentOrg_DoesNotReturnOtherOrgEntries()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);

        // Create a second org with its own exercise chain and entries
        var otherChain = CreateTestChain(context);
        CreateEegEntry(context, otherChain);
        CreateEegEntry(context, chain);

        // Only give context to the first org
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        // Act
        var result = await service.GetByExerciseAsync(chain.Exercise.Id);

        // Assert - should only see entries for chain.Org, not otherChain.Org
        result.TotalCount.Should().Be(1);
        result.Items.Should().AllSatisfy(e => e.EvaluatorId.Should().Be(chain.UserId));
    }

    [Fact]
    public async Task GetByExerciseAsync_FilterByRating_ReturnsOnlyMatchingRating()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        CreateEegEntry(context, chain, rating: PerformanceRating.Performed);
        CreateEegEntry(context, chain, rating: PerformanceRating.SomeChallenges);
        CreateEegEntry(context, chain, rating: PerformanceRating.MajorChallenges);

        var service = CreateService(context);

        // Act
        var result = await service.GetByExerciseAsync(chain.Exercise.Id, new EegEntryQueryParams
        {
            Rating = "Performed"
        });

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().AllSatisfy(e => e.Rating.Should().Be(PerformanceRating.Performed));
    }

    [Fact]
    public async Task GetByExerciseAsync_FilterByMultipleRatings_ReturnsAllMatching()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        CreateEegEntry(context, chain, rating: PerformanceRating.Performed);
        CreateEegEntry(context, chain, rating: PerformanceRating.SomeChallenges);
        CreateEegEntry(context, chain, rating: PerformanceRating.UnableToPerform);

        var service = CreateService(context);

        // Act
        var result = await service.GetByExerciseAsync(chain.Exercise.Id, new EegEntryQueryParams
        {
            Rating = "Performed,SomeChallenges"
        });

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Select(e => e.Rating)
            .Should().BeEquivalentTo(new[] { PerformanceRating.Performed, PerformanceRating.SomeChallenges });
    }

    [Fact]
    public async Task GetByExerciseAsync_FilterByEvaluatorId_ReturnsOnlyThatEvaluator()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        var evaluatorA = Guid.NewGuid().ToString();
        var evaluatorB = Guid.NewGuid().ToString();

        CreateEegEntry(context, chain, evaluatorId: evaluatorA);
        CreateEegEntry(context, chain, evaluatorId: evaluatorA);
        CreateEegEntry(context, chain, evaluatorId: evaluatorB);

        var service = CreateService(context);

        // Act
        var result = await service.GetByExerciseAsync(chain.Exercise.Id, new EegEntryQueryParams
        {
            EvaluatorId = evaluatorA
        });

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(e => e.EvaluatorId.Should().Be(evaluatorA));
    }

    [Fact]
    public async Task GetByExerciseAsync_FilterByCriticalTaskId_ReturnsOnlyThatTask()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        // Create a second critical task
        var secondTask = new CriticalTask
        {
            Id = Guid.NewGuid(),
            TaskDescription = "Staff EOC positions per roster",
            SortOrder = 1,
            OrganizationId = chain.Org.Id,
            CapabilityTargetId = chain.CapabilityTarget.Id,
            CreatedBy = chain.UserId,
            ModifiedBy = chain.UserId
        };
        context.CriticalTasks.Add(secondTask);
        context.SaveChanges();

        CreateEegEntry(context, chain); // against chain.CriticalTask
        var entryForSecondTask = new EegEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = chain.Org.Id,
            CriticalTaskId = secondTask.Id,
            ObservationText = "EOC staffed per roster.",
            Rating = PerformanceRating.Performed,
            ObservedAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            EvaluatorId = chain.UserId,
            CreatedBy = chain.UserId,
            ModifiedBy = chain.UserId
        };
        context.EegEntries.Add(entryForSecondTask);
        context.SaveChanges();

        var service = CreateService(context);

        // Act
        var result = await service.GetByExerciseAsync(chain.Exercise.Id, new EegEntryQueryParams
        {
            CriticalTaskId = secondTask.Id
        });

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.First().CriticalTaskId.Should().Be(secondTask.Id);
    }

    [Fact]
    public async Task GetByExerciseAsync_FilterByCapabilityTargetId_ReturnsEntriesForTarget()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        // Create a second capability target with its own critical task
        var secondTarget = new CapabilityTarget
        {
            Id = Guid.NewGuid(),
            TargetDescription = "Second target",
            SortOrder = 1,
            OrganizationId = chain.Org.Id,
            ExerciseId = chain.Exercise.Id,
            CapabilityId = chain.Capability.Id,
            CreatedBy = chain.UserId,
            ModifiedBy = chain.UserId
        };
        var secondTask = new CriticalTask
        {
            Id = Guid.NewGuid(),
            TaskDescription = "Task for second target",
            SortOrder = 0,
            OrganizationId = chain.Org.Id,
            CapabilityTargetId = secondTarget.Id,
            CreatedBy = chain.UserId,
            ModifiedBy = chain.UserId
        };
        context.CapabilityTargets.Add(secondTarget);
        context.CriticalTasks.Add(secondTask);
        context.SaveChanges();

        CreateEegEntry(context, chain); // against first target

        var entryForSecondTarget = new EegEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = chain.Org.Id,
            CriticalTaskId = secondTask.Id,
            ObservationText = "Observation for second target.",
            Rating = PerformanceRating.SomeChallenges,
            ObservedAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            EvaluatorId = chain.UserId,
            CreatedBy = chain.UserId,
            ModifiedBy = chain.UserId
        };
        context.EegEntries.Add(entryForSecondTarget);
        context.SaveChanges();

        var service = CreateService(context);

        // Act
        var result = await service.GetByExerciseAsync(chain.Exercise.Id, new EegEntryQueryParams
        {
            CapabilityTargetId = secondTarget.Id
        });

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.First().CriticalTask.CapabilityTargetId.Should().Be(secondTarget.Id);
    }

    [Fact]
    public async Task GetByExerciseAsync_FilterByDateRange_ReturnsEntriesWithinRange()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        var baseTime = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        CreateEegEntry(context, chain, observedAt: baseTime.AddHours(-2)); // before range
        CreateEegEntry(context, chain, observedAt: baseTime);              // in range
        CreateEegEntry(context, chain, observedAt: baseTime.AddHours(2));  // in range
        CreateEegEntry(context, chain, observedAt: baseTime.AddHours(5));  // after range

        var service = CreateService(context);

        // Act
        var result = await service.GetByExerciseAsync(chain.Exercise.Id, new EegEntryQueryParams
        {
            FromDate = baseTime.AddMinutes(-1),
            ToDate = baseTime.AddHours(3)
        });

        // Assert
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetByExerciseAsync_FilterBySearch_ReturnsMatchingObservationText()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        CreateEegEntry(context, chain, observationText: "EOC activated within time window.");
        CreateEegEntry(context, chain, observationText: "Communications established successfully.");

        var service = CreateService(context);

        // Act
        var result = await service.GetByExerciseAsync(chain.Exercise.Id, new EegEntryQueryParams
        {
            Search = "communications"
        });

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.First().ObservationText.Should().Contain("Communications");
    }

    [Fact]
    public async Task GetByExerciseAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        for (var i = 0; i < 5; i++)
        {
            CreateEegEntry(context, chain, observationText: $"Observation {i}");
        }

        var service = CreateService(context);

        // Act
        var result = await service.GetByExerciseAsync(chain.Exercise.Id, new EegEntryQueryParams
        {
            Page = 2,
            PageSize = 2
        });

        // Assert
        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetByExerciseAsync_PageSizeExceedsMax_ClampsTo100()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        CreateEegEntry(context, chain);

        var service = CreateService(context);

        // Act
        var result = await service.GetByExerciseAsync(chain.Exercise.Id, new EegEntryQueryParams
        {
            PageSize = 9999
        });

        // Assert - PageSize should be clamped to 100
        result.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task GetByExerciseAsync_SortByRatingAscending_ReturnsSortedResults()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        CreateEegEntry(context, chain, rating: PerformanceRating.UnableToPerform);
        CreateEegEntry(context, chain, rating: PerformanceRating.Performed);
        CreateEegEntry(context, chain, rating: PerformanceRating.MajorChallenges);

        var service = CreateService(context);

        // Act
        var result = await service.GetByExerciseAsync(chain.Exercise.Id, new EegEntryQueryParams
        {
            SortBy = "rating",
            SortOrder = "asc"
        });

        // Assert
        var ratings = result.Items.Select(e => (int)e.Rating).ToList();
        ratings.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetByExerciseAsync_PopulatesEegEntryDtoFields()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        var evaluator = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "evaluator@test.com",
            Email = "evaluator@test.com",
            DisplayName = "Jane Evaluator"
        };
        context.Users.Add(evaluator);
        context.SaveChanges();

        CreateEegEntry(context, chain, evaluatorId: evaluator.Id, rating: PerformanceRating.SomeChallenges,
            observationText: "Some challenges observed during EOC activation.");

        var service = CreateService(context);

        // Act
        var result = await service.GetByExerciseAsync(chain.Exercise.Id);

        // Assert
        var dto = result.Items.Should().ContainSingle().Subject;
        dto.ObservationText.Should().Be("Some challenges observed during EOC activation.");
        dto.Rating.Should().Be(PerformanceRating.SomeChallenges);
        dto.RatingDisplay.Should().Be("S - Performed with Some Challenges");
        dto.EvaluatorId.Should().Be(evaluator.Id);
        dto.EvaluatorName.Should().Be("Jane Evaluator");
        dto.CriticalTask.Should().NotBeNull();
        dto.CriticalTask.TaskDescription.Should().Be(chain.CriticalTask.TaskDescription);
        dto.CriticalTask.CapabilityTargetId.Should().Be(chain.CapabilityTarget.Id);
        dto.CriticalTask.CapabilityName.Should().Be(chain.Capability.Name);
    }

    #endregion

    // =========================================================================
    // GetByCriticalTaskAsync Tests
    // =========================================================================

    #region GetByCriticalTaskAsync

    [Fact]
    public async Task GetByCriticalTaskAsync_ValidTask_ReturnsEntries()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        CreateEegEntry(context, chain);
        CreateEegEntry(context, chain);

        var service = CreateService(context);

        // Act
        var result = await service.GetByCriticalTaskAsync(chain.CriticalTask.Id);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(e => e.CriticalTaskId.Should().Be(chain.CriticalTask.Id));
    }

    [Fact]
    public async Task GetByCriticalTaskAsync_TaskNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetByCriticalTaskAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByCriticalTaskAsync_TaskBelongsToOtherOrg_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);

        // Set context to a different org
        SetupOrgContext(Guid.NewGuid());
        var service = CreateService(context);

        // Act & Assert - task exists but is owned by a different org
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetByCriticalTaskAsync(chain.CriticalTask.Id));
    }

    [Fact]
    public async Task GetByCriticalTaskAsync_ReturnsEntriesOrderedByObservedAtDescending()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        var baseTime = DateTime.UtcNow;
        CreateEegEntry(context, chain, observedAt: baseTime.AddHours(-2), observationText: "Oldest");
        CreateEegEntry(context, chain, observedAt: baseTime, observationText: "Newest");
        CreateEegEntry(context, chain, observedAt: baseTime.AddHours(-1), observationText: "Middle");

        var service = CreateService(context);

        // Act
        var result = await service.GetByCriticalTaskAsync(chain.CriticalTask.Id);

        // Assert - newest first
        result.Items.Select(e => e.ObservationText)
            .Should().ContainInOrder("Newest", "Middle", "Oldest");
    }

    [Fact]
    public async Task GetByCriticalTaskAsync_NoEntries_ReturnsEmptyList()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        // Act
        var result = await service.GetByCriticalTaskAsync(chain.CriticalTask.Id);

        // Assert
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    #endregion

    // =========================================================================
    // GetByIdAsync Tests
    // =========================================================================

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ExistingEntry_ReturnsDto()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        var entry = CreateEegEntry(context, chain, rating: PerformanceRating.MajorChallenges,
            observationText: "Major challenges observed.");
        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(entry.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entry.Id);
        result.ObservationText.Should().Be("Major challenges observed.");
        result.Rating.Should().Be(PerformanceRating.MajorChallenges);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WrongOrg_ReturnsNull()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        var entry = CreateEegEntry(context, chain);

        // Set context to a different org
        SetupOrgContext(Guid.NewGuid());
        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(entry.Id);

        // Assert
        result.Should().BeNull("org isolation must prevent access to entries from other orgs");
    }

    [Fact]
    public async Task GetByIdAsync_PopulatesCriticalTaskAndCapabilityChain()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        var entry = CreateEegEntry(context, chain);
        var service = CreateService(context);

        // Act
        var result = await service.GetByIdAsync(entry.Id);

        // Assert
        result.Should().NotBeNull();
        result!.CriticalTask.Should().NotBeNull();
        result.CriticalTask.Id.Should().Be(chain.CriticalTask.Id);
        result.CriticalTask.TaskDescription.Should().Be(chain.CriticalTask.TaskDescription);
        result.CriticalTask.CapabilityTargetId.Should().Be(chain.CapabilityTarget.Id);
        result.CriticalTask.CapabilityTargetDescription.Should().Be(chain.CapabilityTarget.TargetDescription);
        result.CriticalTask.CapabilityName.Should().Be(chain.Capability.Name);
    }

    #endregion

    // =========================================================================
    // CreateAsync Tests
    // =========================================================================

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsCreatedEntry()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        var evaluatorId = Guid.NewGuid().ToString();
        var request = new CreateEegEntryRequest
        {
            CriticalTaskId = chain.CriticalTask.Id,
            ObservationText = "EOC activated well within the 60-minute target.",
            Rating = PerformanceRating.Performed
        };

        // Act
        var result = await service.CreateAsync(request, evaluatorId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.ObservationText.Should().Be("EOC activated well within the 60-minute target.");
        result.Rating.Should().Be(PerformanceRating.Performed);
        result.EvaluatorId.Should().Be(evaluatorId);
        result.TriggeringInjectId.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsToDatabase()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        var evaluatorId = Guid.NewGuid().ToString();
        var request = new CreateEegEntryRequest
        {
            CriticalTaskId = chain.CriticalTask.Id,
            ObservationText = "Observation text.",
            Rating = PerformanceRating.SomeChallenges
        };

        // Act
        var result = await service.CreateAsync(request, evaluatorId);

        // Assert - verify persisted
        var persisted = await context.EegEntries.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.OrganizationId.Should().Be(chain.Org.Id);
        persisted.CriticalTaskId.Should().Be(chain.CriticalTask.Id);
        persisted.EvaluatorId.Should().Be(evaluatorId);
        persisted.CreatedBy.Should().Be(evaluatorId);
        persisted.ModifiedBy.Should().Be(evaluatorId);
    }

    [Fact]
    public async Task CreateAsync_SetsOrganizationIdFromCriticalTaskChain()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        var request = new CreateEegEntryRequest
        {
            CriticalTaskId = chain.CriticalTask.Id,
            ObservationText = "Observation.",
            Rating = PerformanceRating.Performed
        };

        // Act
        var result = await service.CreateAsync(request, chain.UserId);

        // Assert - org ID comes from CapabilityTarget, not from orgContext
        var persisted = await context.EegEntries.IgnoreQueryFilters().FirstAsync(e => e.Id == result.Id);
        persisted.OrganizationId.Should().Be(chain.Org.Id);
    }

    [Fact]
    public async Task CreateAsync_DefaultsObservedAtToNowWhenNotProvided()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        var before = DateTime.UtcNow;
        var request = new CreateEegEntryRequest
        {
            CriticalTaskId = chain.CriticalTask.Id,
            ObservationText = "Observation.",
            Rating = PerformanceRating.Performed,
            ObservedAt = null
        };

        // Act
        var result = await service.CreateAsync(request, chain.UserId);
        var after = DateTime.UtcNow;

        // Assert
        result.ObservedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task CreateAsync_WithExplicitObservedAt_UsesProvidedTime()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        var scenarioTime = new DateTime(2025, 3, 15, 9, 30, 0, DateTimeKind.Utc);
        var request = new CreateEegEntryRequest
        {
            CriticalTaskId = chain.CriticalTask.Id,
            ObservationText = "Observation.",
            Rating = PerformanceRating.Performed,
            ObservedAt = scenarioTime
        };

        // Act
        var result = await service.CreateAsync(request, chain.UserId);

        // Assert
        result.ObservedAt.Should().Be(scenarioTime);
    }

    [Fact]
    public async Task CreateAsync_InvalidCriticalTaskId_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        var request = new CreateEegEntryRequest
        {
            CriticalTaskId = Guid.NewGuid(), // non-existent
            ObservationText = "Observation.",
            Rating = PerformanceRating.Performed
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(request, chain.UserId));
    }

    [Fact]
    public async Task CreateAsync_CriticalTaskFromOtherOrg_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);

        // Set context to a different org
        SetupOrgContext(Guid.NewGuid());
        var service = CreateService(context);

        var request = new CreateEegEntryRequest
        {
            CriticalTaskId = chain.CriticalTask.Id,
            ObservationText = "Observation.",
            Rating = PerformanceRating.Performed
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(request, chain.UserId));
    }

    [Fact]
    public async Task CreateAsync_InvalidTriggeringInjectId_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        var request = new CreateEegEntryRequest
        {
            CriticalTaskId = chain.CriticalTask.Id,
            ObservationText = "Observation.",
            Rating = PerformanceRating.Performed,
            TriggeringInjectId = Guid.NewGuid() // non-existent inject
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(request, chain.UserId));
    }

    [Fact]
    public async Task CreateAsync_BroadcastsEegEntryCreatedSignalR()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        var request = new CreateEegEntryRequest
        {
            CriticalTaskId = chain.CriticalTask.Id,
            ObservationText = "Observation.",
            Rating = PerformanceRating.Performed
        };

        // Act
        await service.CreateAsync(request, chain.UserId);

        // Assert
        _hubContextMock.Verify(
            x => x.NotifyEegEntryCreated(chain.Exercise.Id, It.Is<EegEntryDto>(e => e.CriticalTaskId == chain.CriticalTask.Id)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidTriggeringInject_LinksInjectToEntry()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        // Create a MSEL and inject for the exercise
        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Version = 1,
            ExerciseId = chain.Exercise.Id,
            CreatedBy = chain.UserId,
            ModifiedBy = chain.UserId
        };
        context.Msels.Add(msel);
        var inject = new Inject
        {
            Id = Guid.NewGuid(),
            MselId = msel.Id,
            InjectNumber = 1,
            Title = "Activate EOC",
            Description = "Director activates EOC.",
            ScheduledTime = TimeOnly.FromDateTime(DateTime.Now),
            Target = "EOC Director",
            Status = InjectStatus.Draft,
            Sequence = 1,
            CreatedBy = chain.UserId,
            ModifiedBy = chain.UserId
        };
        context.Injects.Add(inject);
        context.SaveChanges();

        var service = CreateService(context);

        var request = new CreateEegEntryRequest
        {
            CriticalTaskId = chain.CriticalTask.Id,
            ObservationText = "Observation triggered by inject.",
            Rating = PerformanceRating.Performed,
            TriggeringInjectId = inject.Id
        };

        // Act
        var result = await service.CreateAsync(request, chain.UserId);

        // Assert
        result.TriggeringInjectId.Should().Be(inject.Id);
        result.TriggeringInject.Should().NotBeNull();
        result.TriggeringInject!.Id.Should().Be(inject.Id);
        result.TriggeringInject.Title.Should().Be("Activate EOC");
    }

    #endregion

    // =========================================================================
    // UpdateAsync Tests
    // =========================================================================

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesEntry()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        var entry = CreateEegEntry(context, chain, observationText: "Original observation.");
        var service = CreateService(context);

        var modifierId = Guid.NewGuid().ToString();
        var request = new UpdateEegEntryRequest
        {
            ObservationText = "Updated observation with more detail.",
            Rating = PerformanceRating.SomeChallenges
        };

        // Act
        var result = await service.UpdateAsync(entry.Id, request, modifierId);

        // Assert
        result.Should().NotBeNull();
        result!.ObservationText.Should().Be("Updated observation with more detail.");
        result.Rating.Should().Be(PerformanceRating.SomeChallenges);
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_PersistsChangesToDatabase()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        var entry = CreateEegEntry(context, chain, rating: PerformanceRating.Performed);
        var service = CreateService(context);

        var request = new UpdateEegEntryRequest
        {
            ObservationText = "Revised observation.",
            Rating = PerformanceRating.MajorChallenges
        };

        // Act
        await service.UpdateAsync(entry.Id, request, chain.UserId);

        // Assert
        var persisted = await context.EegEntries.IgnoreQueryFilters().FirstAsync(e => e.Id == entry.Id);
        persisted.ObservationText.Should().Be("Revised observation.");
        persisted.Rating.Should().Be(PerformanceRating.MajorChallenges);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesModifiedByField()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        var entry = CreateEegEntry(context, chain);
        var service = CreateService(context);

        var supervisorId = Guid.NewGuid().ToString();
        var request = new UpdateEegEntryRequest
        {
            ObservationText = "Supervisor-revised observation.",
            Rating = PerformanceRating.Performed
        };

        // Act
        await service.UpdateAsync(entry.Id, request, supervisorId);

        // Assert
        var persisted = await context.EegEntries.IgnoreQueryFilters().FirstAsync(e => e.Id == entry.Id);
        persisted.ModifiedBy.Should().Be(supervisorId);
    }

    [Fact]
    public async Task UpdateAsync_WithObservedAt_UpdatesObservedAt()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        var entry = CreateEegEntry(context, chain);
        var service = CreateService(context);

        var newObservedAt = new DateTime(2025, 4, 1, 8, 0, 0, DateTimeKind.Utc);
        var request = new UpdateEegEntryRequest
        {
            ObservationText = "Updated.",
            Rating = PerformanceRating.Performed,
            ObservedAt = newObservedAt
        };

        // Act
        var result = await service.UpdateAsync(entry.Id, request, chain.UserId);

        // Assert
        result!.ObservedAt.Should().Be(newObservedAt);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        var request = new UpdateEegEntryRequest
        {
            ObservationText = "Observation.",
            Rating = PerformanceRating.Performed
        };

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), request, chain.UserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WrongOrg_ReturnsNull()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        var entry = CreateEegEntry(context, chain);

        // Set context to a different org
        SetupOrgContext(Guid.NewGuid());
        var service = CreateService(context);

        var request = new UpdateEegEntryRequest
        {
            ObservationText = "Attempt from wrong org.",
            Rating = PerformanceRating.Performed
        };

        // Act
        var result = await service.UpdateAsync(entry.Id, request, chain.UserId);

        // Assert
        result.Should().BeNull("org isolation must prevent updating entries from other orgs");
    }

    [Fact]
    public async Task UpdateAsync_BroadcastsEegEntryUpdatedSignalR()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        var entry = CreateEegEntry(context, chain);
        var service = CreateService(context);

        var request = new UpdateEegEntryRequest
        {
            ObservationText = "Updated observation.",
            Rating = PerformanceRating.Performed
        };

        // Act
        await service.UpdateAsync(entry.Id, request, chain.UserId);

        // Assert
        _hubContextMock.Verify(
            x => x.NotifyEegEntryUpdated(chain.Exercise.Id, It.Is<EegEntryDto>(e => e.Id == entry.Id)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_DoesNotBroadcastSignalR()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        var request = new UpdateEegEntryRequest
        {
            ObservationText = "Observation.",
            Rating = PerformanceRating.Performed
        };

        // Act
        await service.UpdateAsync(Guid.NewGuid(), request, chain.UserId);

        // Assert
        _hubContextMock.Verify(
            x => x.NotifyEegEntryUpdated(It.IsAny<Guid>(), It.IsAny<EegEntryDto>()),
            Times.Never);
    }

    #endregion

    // =========================================================================
    // DeleteAsync Tests
    // =========================================================================

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ExistingEntry_ReturnsTrue()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        var entry = CreateEegEntry(context, chain);
        var service = CreateService(context);

        // Act
        var result = await service.DeleteAsync(entry.Id, chain.UserId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesEntry()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        var entry = CreateEegEntry(context, chain);
        var service = CreateService(context);
        var deletedBy = Guid.NewGuid().ToString();

        // Act
        await service.DeleteAsync(entry.Id, deletedBy);

        // Assert - verify soft delete fields are set
        var persisted = await context.EegEntries.IgnoreQueryFilters().FirstAsync(e => e.Id == entry.Id);
        persisted.IsDeleted.Should().BeTrue();
        persisted.DeletedAt.Should().NotBeNull();
        persisted.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        persisted.DeletedBy.Should().Be(deletedBy);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletedEntryNotVisibleInNormalQueries()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        var entry = CreateEegEntry(context, chain);
        var service = CreateService(context);

        // Act
        await service.DeleteAsync(entry.Id, chain.UserId);

        // Assert - entry is gone from normal queries
        var result = await service.GetByIdAsync(entry.Id);
        result.Should().BeNull("soft-deleted entries must not appear in normal queries");
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsFalse()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid(), chain.UserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WrongOrg_ReturnsFalse()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        var entry = CreateEegEntry(context, chain);

        // Set context to a different org
        SetupOrgContext(Guid.NewGuid());
        var service = CreateService(context);

        // Act
        var result = await service.DeleteAsync(entry.Id, chain.UserId);

        // Assert
        result.Should().BeFalse("org isolation must prevent deleting entries from other orgs");
    }

    [Fact]
    public async Task DeleteAsync_BroadcastsEegEntryDeletedSignalR()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        var entry = CreateEegEntry(context, chain);
        var service = CreateService(context);

        // Act
        await service.DeleteAsync(entry.Id, chain.UserId);

        // Assert
        _hubContextMock.Verify(
            x => x.NotifyEegEntryDeleted(chain.Exercise.Id, entry.Id),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_DoesNotBroadcastSignalR()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(context);

        // Act
        await service.DeleteAsync(Guid.NewGuid(), chain.UserId);

        // Assert
        _hubContextMock.Verify(
            x => x.NotifyEegEntryDeleted(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    #endregion

    // =========================================================================
    // GetCoverageAsync Tests
    // =========================================================================

    #region GetCoverageAsync

    [Fact]
    public async Task GetCoverageAsync_NoTasks_ReturnsZeroCoverage()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        // Remove the critical task so there are none
        var task = await context.CriticalTasks.FindAsync(chain.CriticalTask.Id);
        context.CriticalTasks.Remove(task!);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.GetCoverageAsync(chain.Exercise.Id);

        // Assert
        result.TotalTasks.Should().Be(0);
        result.EvaluatedTasks.Should().Be(0);
        result.CoveragePercentage.Should().Be(0);
    }

    [Fact]
    public async Task GetCoverageAsync_AllTasksEvaluated_Returns100PercentCoverage()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        CreateEegEntry(context, chain);
        var service = CreateService(context);

        // Act
        var result = await service.GetCoverageAsync(chain.Exercise.Id);

        // Assert
        result.TotalTasks.Should().Be(1);
        result.EvaluatedTasks.Should().Be(1);
        result.CoveragePercentage.Should().Be(100.0m);
    }

    [Fact]
    public async Task GetCoverageAsync_SomeTasksUnevaluated_ReturnsPartialCoverage()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        // Add a second critical task with no entries
        var unevaluatedTask = new CriticalTask
        {
            Id = Guid.NewGuid(),
            TaskDescription = "Staff EOC positions",
            SortOrder = 1,
            OrganizationId = chain.Org.Id,
            CapabilityTargetId = chain.CapabilityTarget.Id,
            CreatedBy = chain.UserId,
            ModifiedBy = chain.UserId
        };
        context.CriticalTasks.Add(unevaluatedTask);
        context.SaveChanges();

        // Only evaluate the first task
        CreateEegEntry(context, chain);
        var service = CreateService(context);

        // Act
        var result = await service.GetCoverageAsync(chain.Exercise.Id);

        // Assert
        result.TotalTasks.Should().Be(2);
        result.EvaluatedTasks.Should().Be(1);
        result.CoveragePercentage.Should().Be(50.0m);
    }

    [Fact]
    public async Task GetCoverageAsync_RatingDistribution_CountsCorrectly()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        // Add additional critical tasks for each rating
        var taskS = new CriticalTask
        {
            Id = Guid.NewGuid(),
            TaskDescription = "Task for SomeChallenges",
            SortOrder = 1,
            OrganizationId = chain.Org.Id,
            CapabilityTargetId = chain.CapabilityTarget.Id,
            CreatedBy = chain.UserId,
            ModifiedBy = chain.UserId
        };
        var taskM = new CriticalTask
        {
            Id = Guid.NewGuid(),
            TaskDescription = "Task for MajorChallenges",
            SortOrder = 2,
            OrganizationId = chain.Org.Id,
            CapabilityTargetId = chain.CapabilityTarget.Id,
            CreatedBy = chain.UserId,
            ModifiedBy = chain.UserId
        };
        context.CriticalTasks.AddRange(taskS, taskM);
        context.SaveChanges();

        CreateEegEntry(context, chain, rating: PerformanceRating.Performed);
        var entryS = new EegEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = chain.Org.Id,
            CriticalTaskId = taskS.Id,
            ObservationText = "Some challenges.",
            Rating = PerformanceRating.SomeChallenges,
            ObservedAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            EvaluatorId = chain.UserId,
            CreatedBy = chain.UserId,
            ModifiedBy = chain.UserId
        };
        var entryM = new EegEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = chain.Org.Id,
            CriticalTaskId = taskM.Id,
            ObservationText = "Major challenges.",
            Rating = PerformanceRating.MajorChallenges,
            ObservedAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            EvaluatorId = chain.UserId,
            CreatedBy = chain.UserId,
            ModifiedBy = chain.UserId
        };
        context.EegEntries.AddRange(entryS, entryM);
        context.SaveChanges();

        var service = CreateService(context);

        // Act
        var result = await service.GetCoverageAsync(chain.Exercise.Id);

        // Assert
        result.RatingDistribution[PerformanceRating.Performed].Should().Be(1);
        result.RatingDistribution[PerformanceRating.SomeChallenges].Should().Be(1);
        result.RatingDistribution[PerformanceRating.MajorChallenges].Should().Be(1);
        result.RatingDistribution[PerformanceRating.UnableToPerform].Should().Be(0);
    }

    [Fact]
    public async Task GetCoverageAsync_ByCapabilityTarget_GroupsTasksCorrectly()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        // Second capability target with its own task
        var secondTarget = new CapabilityTarget
        {
            Id = Guid.NewGuid(),
            TargetDescription = "Activate EOC within 60 minutes",
            SortOrder = 1,
            OrganizationId = chain.Org.Id,
            ExerciseId = chain.Exercise.Id,
            CapabilityId = chain.Capability.Id,
            CreatedBy = chain.UserId,
            ModifiedBy = chain.UserId
        };
        var secondTask = new CriticalTask
        {
            Id = Guid.NewGuid(),
            TaskDescription = "Staff EOC positions",
            SortOrder = 0,
            OrganizationId = chain.Org.Id,
            CapabilityTargetId = secondTarget.Id,
            CreatedBy = chain.UserId,
            ModifiedBy = chain.UserId
        };
        context.CapabilityTargets.Add(secondTarget);
        context.CriticalTasks.Add(secondTask);
        context.SaveChanges();

        // Evaluate first target's task, leave second unevaluated
        CreateEegEntry(context, chain);
        var service = CreateService(context);

        // Act
        var result = await service.GetCoverageAsync(chain.Exercise.Id);

        // Assert
        result.ByCapabilityTarget.Should().HaveCount(2);

        var firstTargetCoverage = result.ByCapabilityTarget
            .First(ct => ct.Id == chain.CapabilityTarget.Id);
        firstTargetCoverage.EvaluatedTasks.Should().Be(1);
        firstTargetCoverage.TotalTasks.Should().Be(1);

        var secondTargetCoverage = result.ByCapabilityTarget
            .First(ct => ct.Id == secondTarget.Id);
        secondTargetCoverage.EvaluatedTasks.Should().Be(0);
        secondTargetCoverage.TotalTasks.Should().Be(1);
    }

    [Fact]
    public async Task GetCoverageAsync_UnevaluatedTasks_ListsTasksWithoutEntries()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        // Add a second task but don't evaluate it
        var unevaluatedTask = new CriticalTask
        {
            Id = Guid.NewGuid(),
            TaskDescription = "Unevaluated task description",
            SortOrder = 1,
            OrganizationId = chain.Org.Id,
            CapabilityTargetId = chain.CapabilityTarget.Id,
            CreatedBy = chain.UserId,
            ModifiedBy = chain.UserId
        };
        context.CriticalTasks.Add(unevaluatedTask);
        context.SaveChanges();

        // Evaluate only the first task
        CreateEegEntry(context, chain);
        var service = CreateService(context);

        // Act
        var result = await service.GetCoverageAsync(chain.Exercise.Id);

        // Assert
        result.UnevaluatedTasks.Should().ContainSingle();
        result.UnevaluatedTasks.First().TaskId.Should().Be(unevaluatedTask.Id);
        result.UnevaluatedTasks.First().TaskDescription.Should().Be("Unevaluated task description");
        result.UnevaluatedTasks.First().CapabilityTargetId.Should().Be(chain.CapabilityTarget.Id);
    }

    [Fact]
    public async Task GetCoverageAsync_OtherOrgTasks_NotIncludedInCoverage()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        var otherChain = CreateTestChain(context); // different org

        SetupOrgContext(chain.Org.Id);

        CreateEegEntry(context, chain);
        CreateEegEntry(context, otherChain); // should not be counted

        var service = CreateService(context);

        // Act
        var result = await service.GetCoverageAsync(chain.Exercise.Id);

        // Assert - only sees current org's tasks
        result.TotalTasks.Should().Be(1);
        result.EvaluatedTasks.Should().Be(1);
    }

    [Fact]
    public async Task GetCoverageAsync_SoftDeletedEntriesNotCountedAsEvaluated()
    {
        // Arrange - use a named DB and separate contexts to avoid change tracker caching
        var dbName = Guid.NewGuid().ToString();
        var seedContext = TestDbContextFactory.Create(dbName);
        var chain = CreateTestChain(seedContext);

        // Create the entry and then soft-delete it in the seed context
        var entry = CreateEegEntry(seedContext, chain);
        entry.IsDeleted = true;
        entry.DeletedAt = DateTime.UtcNow;
        entry.DeletedBy = chain.UserId;
        await seedContext.SaveChangesAsync();

        // Use a fresh context for querying to ensure clean change tracker
        var queryContext = TestDbContextFactory.Create(dbName);
        SetupOrgContext(chain.Org.Id);
        var service = CreateService(queryContext);

        // Act
        var result = await service.GetCoverageAsync(chain.Exercise.Id);

        // Assert - soft-deleted entry should not count as evaluated
        result.TotalTasks.Should().Be(1);
        result.EvaluatedTasks.Should().Be(0);
        result.CoveragePercentage.Should().Be(0);
    }

    [Fact]
    public async Task GetCoverageAsync_LatestRatingInTaskRatings_ReflectsNewestEntry()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var chain = CreateTestChain(context);
        SetupOrgContext(chain.Org.Id);

        var olderTime = DateTime.UtcNow.AddHours(-1);
        var newerTime = DateTime.UtcNow;

        CreateEegEntry(context, chain, rating: PerformanceRating.UnableToPerform, observedAt: olderTime);
        CreateEegEntry(context, chain, rating: PerformanceRating.Performed, observedAt: newerTime);

        var service = CreateService(context);

        // Act
        var result = await service.GetCoverageAsync(chain.Exercise.Id);

        // Assert - TaskRatings should show most recent rating
        var targetCoverage = result.ByCapabilityTarget.First();
        var taskRating = targetCoverage.TaskRatings.First(tr => tr.TaskId == chain.CriticalTask.Id);
        taskRating.LatestRating.Should().Be(PerformanceRating.Performed,
            "the most recently observed entry should determine the latest rating");
    }

    #endregion
}
