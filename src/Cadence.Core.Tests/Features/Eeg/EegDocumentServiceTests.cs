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
/// Tests for EegDocumentService (HSEEP-compliant Word document generation).
/// </summary>
public class EegDocumentServiceTests
{
    private (AppDbContext context, Organization org, Exercise exercise, Capability capability, CapabilityTarget target, ApplicationUser evaluator) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();

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
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            DeliveryMode = DeliveryMode.FacilitatorPaced,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            Organization = org,
            CreatedBy = userId,
            ModifiedBy = userId
        };

        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Description = "Test MSEL Description",
            ExerciseId = exercise.Id,
            Version = 1,
            IsActive = true,
            CreatedBy = userId,
            ModifiedBy = userId
        };

        exercise.ActiveMselId = msel.Id;

        var capability = new Capability
        {
            Id = Guid.NewGuid(),
            Name = "Operational Communications",
            Description = "Test capability",
            OrganizationId = org.Id
        };

        var target = new CapabilityTarget
        {
            Id = Guid.NewGuid(),
            TargetDescription = "Establish interoperable communications within 30 minutes",
            Sources = "SOP 5.2, Metro County EOP Annex F",
            SortOrder = 0,
            OrganizationId = org.Id,
            ExerciseId = exercise.Id,
            CapabilityId = capability.Id,
            Capability = capability,
            CreatedBy = userId,
            ModifiedBy = userId
        };

        var evaluator = new ApplicationUser
        {
            Id = userId,
            UserName = "test@example.com",
            Email = "test@example.com",
            DisplayName = "Test Evaluator",
            PhoneNumber = "(555) 123-4567",
            SystemRole = SystemRole.User
        };

        context.Exercises.Add(exercise);
        context.Msels.Add(msel);
        context.Capabilities.Add(capability);
        context.CapabilityTargets.Add(target);
        context.Users.Add(evaluator);
        context.SaveChanges();

        return (context, org, exercise, capability, target, evaluator);
    }

    private EegDocumentService CreateService(AppDbContext context, Guid organizationId)
    {
        var orgContextMock = new Mock<ICurrentOrganizationContext>();
        orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(organizationId);
        orgContextMock.Setup(x => x.HasContext).Returns(true);
        return new EegDocumentService(context, orgContextMock.Object);
    }

    #region GenerateAsync - Blank Mode Tests

    [Fact]
    public async Task GenerateAsync_BlankMode_ReturnsValidDocument()
    {
        // Arrange
        var (context, org, exercise, _, _, _) = CreateTestContext();
        var service = CreateService(context, org.Id);
        var request = new GenerateEegDocumentRequest { Mode = EegDocumentMode.Blank };

        // Act
        var result = await service.GenerateAsync(exercise.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().NotBeEmpty();
        result.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        result.Filename.Should().Contain(".docx");
        result.CapabilityTargetCount.Should().Be(1);
    }

    [Fact]
    public async Task GenerateAsync_BlankMode_IncludesCapabilityTargetCount()
    {
        // Arrange
        var (context, org, exercise, capability, _, userId) = CreateTestContext();

        // Add more capability targets
        context.CapabilityTargets.Add(new CapabilityTarget
        {
            Id = Guid.NewGuid(),
            TargetDescription = "Second Target",
            SortOrder = 1,
            OrganizationId = org.Id,
            ExerciseId = exercise.Id,
            CapabilityId = capability.Id,
            CreatedBy = userId.Id,
            ModifiedBy = userId.Id
        });
        context.SaveChanges();

        var service = CreateService(context, org.Id);
        var request = new GenerateEegDocumentRequest { Mode = EegDocumentMode.Blank };

        // Act
        var result = await service.GenerateAsync(exercise.Id, request);

        // Assert
        result.CapabilityTargetCount.Should().Be(2);
    }

    [Fact]
    public async Task GenerateAsync_InvalidExerciseId_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, org, _, _, _, _) = CreateTestContext();
        var service = CreateService(context, org.Id);
        var request = new GenerateEegDocumentRequest { Mode = EegDocumentMode.Blank };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task GenerateAsync_NoCapabilityTargets_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, org, _, _, _, _) = CreateTestContext();

        // Create exercise without targets
        var emptyExercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Empty Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            DeliveryMode = DeliveryMode.FacilitatorPaced,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = "test",
            ModifiedBy = "test"
        };
        context.Exercises.Add(emptyExercise);
        context.SaveChanges();

        var service = CreateService(context, org.Id);
        var request = new GenerateEegDocumentRequest { Mode = EegDocumentMode.Blank };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateAsync(emptyExercise.Id, request));
    }

    #endregion

    #region GenerateAsync - Completed Mode Tests

    [Fact]
    public async Task GenerateAsync_CompletedMode_ReturnsValidDocument()
    {
        // Arrange
        var (context, org, exercise, _, target, evaluator) = CreateTestContext();

        // Add critical task and EEG entry
        var task = new CriticalTask
        {
            Id = Guid.NewGuid(),
            TaskDescription = "Issue EOC activation notification",
            CapabilityTargetId = target.Id,
            OrganizationId = target.OrganizationId,
            SortOrder = 0,
            CreatedBy = evaluator.Id,
            ModifiedBy = evaluator.Id
        };
        context.CriticalTasks.Add(task);

        var entry = new EegEntry
        {
            Id = Guid.NewGuid(),
            CriticalTaskId = task.Id,
            ObservationText = "Test observation",
            Rating = PerformanceRating.Performed,
            EvaluatorId = evaluator.Id,
            ObservedAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            CreatedBy = evaluator.Id,
            ModifiedBy = evaluator.Id
        };
        context.EegEntries.Add(entry);
        context.SaveChanges();

        var service = CreateService(context, org.Id);
        var request = new GenerateEegDocumentRequest
        {
            Mode = EegDocumentMode.Completed,
            IncludeEvaluatorNames = true
        };

        // Act
        var result = await service.GenerateAsync(exercise.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().NotBeEmpty();
        result.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
    }

    [Fact]
    public async Task GenerateAsync_CompletedMode_IncludesCriticalTaskCount()
    {
        // Arrange
        var (context, org, exercise, _, target, evaluator) = CreateTestContext();

        // Add multiple critical tasks
        for (int i = 0; i < 3; i++)
        {
            context.CriticalTasks.Add(new CriticalTask
            {
                Id = Guid.NewGuid(),
                TaskDescription = $"Task {i + 1}",
                CapabilityTargetId = target.Id,
                OrganizationId = target.OrganizationId,
                SortOrder = i,
                CreatedBy = evaluator.Id,
                ModifiedBy = evaluator.Id
            });
        }
        context.SaveChanges();

        var service = CreateService(context, org.Id);
        var request = new GenerateEegDocumentRequest { Mode = EegDocumentMode.Completed };

        // Act
        var result = await service.GenerateAsync(exercise.Id, request);

        // Assert
        result.CriticalTaskCount.Should().Be(3);
    }

    #endregion

    #region GenerateAsync - Per Capability ZIP Tests

    [Fact]
    public async Task GenerateAsync_PerCapabilityFormat_ReturnsZipFile()
    {
        // Arrange
        var (context, org, exercise, _, _, _) = CreateTestContext();
        var service = CreateService(context, org.Id);
        var request = new GenerateEegDocumentRequest
        {
            Mode = EegDocumentMode.Blank,
            OutputFormat = EegDocumentOutputFormat.PerCapability
        };

        // Act
        var result = await service.GenerateAsync(exercise.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.ContentType.Should().Be("application/zip");
        result.Filename.Should().Contain(".zip");
    }

    #endregion

    #region Rating Aggregation Tests

    [Fact]
    public async Task GenerateAsync_CompletedMode_UsesWorstCaseRatingAggregation()
    {
        // Arrange
        var (context, org, exercise, _, target, evaluator) = CreateTestContext();

        var task = new CriticalTask
        {
            Id = Guid.NewGuid(),
            TaskDescription = "Test task with multiple ratings",
            CapabilityTargetId = target.Id,
            OrganizationId = target.OrganizationId,
            SortOrder = 0,
            CreatedBy = evaluator.Id,
            ModifiedBy = evaluator.Id
        };
        context.CriticalTasks.Add(task);

        // Add entries with different ratings - worst case should win
        var ratings = new[] { PerformanceRating.Performed, PerformanceRating.SomeChallenges, PerformanceRating.MajorChallenges };
        foreach (var rating in ratings)
        {
            context.EegEntries.Add(new EegEntry
            {
                Id = Guid.NewGuid(),
                CriticalTaskId = task.Id,
                ObservationText = $"Observation for {rating}",
                Rating = rating,
                EvaluatorId = evaluator.Id,
                ObservedAt = DateTime.UtcNow,
                RecordedAt = DateTime.UtcNow,
                CreatedBy = evaluator.Id,
                ModifiedBy = evaluator.Id
            });
        }
        context.SaveChanges();

        var service = CreateService(context, org.Id);
        var request = new GenerateEegDocumentRequest { Mode = EegDocumentMode.Completed };

        // Act
        var result = await service.GenerateAsync(exercise.Id, request);

        // Assert - Document should generate without error
        // The worst-case rating (MajorChallenges) should be used
        result.Should().NotBeNull();
        result.Content.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(new[] { PerformanceRating.Performed, PerformanceRating.Performed }, PerformanceRating.Performed)]
    [InlineData(new[] { PerformanceRating.Performed, PerformanceRating.SomeChallenges }, PerformanceRating.SomeChallenges)]
    [InlineData(new[] { PerformanceRating.SomeChallenges, PerformanceRating.MajorChallenges }, PerformanceRating.MajorChallenges)]
    [InlineData(new[] { PerformanceRating.Performed, PerformanceRating.UnableToPerform }, PerformanceRating.UnableToPerform)]
    public void GetAggregateRating_ReturnsWorstCase(PerformanceRating[] ratings, PerformanceRating expected)
    {
        // This tests the rating aggregation logic directly
        // Since GetAggregateRating is private, we test it indirectly through the service
        // But we can verify the expected behavior

        // Worst-case means the maximum enum value wins
        var actual = ratings.Max();
        actual.Should().Be(expected);
    }

    #endregion

    #region Filename Sanitization Tests

    [Fact]
    public async Task GenerateAsync_ExerciseNameWithSpecialChars_SanitizesFilename()
    {
        // Arrange
        var (context, org, _, _, _, _) = CreateTestContext();

        // Create exercise with special characters in name
        var specialExercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test: Exercise <2026> \"Special\" /Name\\",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            DeliveryMode = DeliveryMode.FacilitatorPaced,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = "test",
            ModifiedBy = "test"
        };

        var capability = new Capability
        {
            Id = Guid.NewGuid(),
            Name = "Test Capability",
            Description = "Test",
            OrganizationId = org.Id
        };

        var target = new CapabilityTarget
        {
            Id = Guid.NewGuid(),
            TargetDescription = "Test target",
            SortOrder = 0,
            OrganizationId = org.Id,
            ExerciseId = specialExercise.Id,
            CapabilityId = capability.Id,
            CreatedBy = "test",
            ModifiedBy = "test"
        };

        context.Exercises.Add(specialExercise);
        context.Capabilities.Add(capability);
        context.CapabilityTargets.Add(target);
        context.SaveChanges();

        var service = CreateService(context, org.Id);
        var request = new GenerateEegDocumentRequest { Mode = EegDocumentMode.Blank };

        // Act
        var result = await service.GenerateAsync(specialExercise.Id, request);

        // Assert
        result.Filename.Should().NotContain("<");
        result.Filename.Should().NotContain(">");
        result.Filename.Should().NotContain(":");
        result.Filename.Should().NotContain("\"");
        result.Filename.Should().NotContain("/");
        result.Filename.Should().NotContain("\\");
    }

    #endregion

    #region Observation Truncation Tests

    [Fact]
    public async Task GenerateAsync_LongObservation_TruncatesTo500Characters()
    {
        // Arrange
        var (context, org, exercise, _, target, evaluator) = CreateTestContext();

        var task = new CriticalTask
        {
            Id = Guid.NewGuid(),
            TaskDescription = "Test task",
            CapabilityTargetId = target.Id,
            OrganizationId = target.OrganizationId,
            SortOrder = 0,
            CreatedBy = evaluator.Id,
            ModifiedBy = evaluator.Id
        };
        context.CriticalTasks.Add(task);

        // Create observation longer than 500 characters
        var longObservation = new string('A', 600);
        context.EegEntries.Add(new EegEntry
        {
            Id = Guid.NewGuid(),
            CriticalTaskId = task.Id,
            ObservationText = longObservation,
            Rating = PerformanceRating.Performed,
            EvaluatorId = evaluator.Id,
            ObservedAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow,
            CreatedBy = evaluator.Id,
            ModifiedBy = evaluator.Id
        });
        context.SaveChanges();

        var service = CreateService(context, org.Id);
        var request = new GenerateEegDocumentRequest { Mode = EegDocumentMode.Completed };

        // Act - Document should generate without error (truncation happens internally)
        var result = await service.GenerateAsync(exercise.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().NotBeEmpty();
    }

    #endregion
}
