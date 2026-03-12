using Cadence.Core.Data;
using Cadence.Core.Features.Eeg.Services;
using Cadence.Core.Features.ExcelExport.Models.DTOs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Eeg;

/// <summary>
/// Tests for EegExportService — EEG data export to Excel/JSON for After-Action Review.
/// </summary>
public class EegExportServiceTests
{
    private readonly Mock<ILogger<EegExportService>> _loggerMock = new();

    private (AppDbContext context, EegExportService service, Guid exerciseId, Guid orgId) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Org",
            Slug = "test-org"
        };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            OrganizationId = org.Id,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = ExerciseStatus.Active
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();

        var service = new EegExportService(context, _loggerMock.Object);
        return (context, service, exercise.Id, org.Id);
    }

    private (Guid capTargetId, Guid taskId) SeedCapabilityWithTask(
        AppDbContext context, Guid exerciseId, Guid orgId,
        string capName = "Operational Communications",
        string targetDesc = "Establish comms within 30 min",
        string taskDesc = "Issue EOC activation notification")
    {
        var capability = new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Name = capName
        };
        context.Set<Capability>().Add(capability);

        var capTarget = new CapabilityTarget
        {
            Id = Guid.NewGuid(),
            ExerciseId = exerciseId,
            OrganizationId = orgId,
            CapabilityId = capability.Id,
            Capability = capability,
            TargetDescription = targetDesc,
            SortOrder = 1
        };
        context.Set<CapabilityTarget>().Add(capTarget);

        var task = new CriticalTask
        {
            Id = Guid.NewGuid(),
            CapabilityTargetId = capTarget.Id,
            OrganizationId = orgId,
            TaskDescription = taskDesc,
            SortOrder = 1
        };
        context.Set<CriticalTask>().Add(task);
        context.SaveChanges();

        return (capTarget.Id, task.Id);
    }

    private void SeedEegEntry(AppDbContext context, Guid taskId, Guid orgId,
        PerformanceRating rating = PerformanceRating.Performed,
        string observationText = "Good performance observed")
    {
        var evaluator = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"eval-{Guid.NewGuid():N}@test.com",
            Email = $"eval-{Guid.NewGuid():N}@test.com",
            DisplayName = "Test Evaluator"
        };
        context.ApplicationUsers.Add(evaluator);

        context.Set<EegEntry>().Add(new EegEntry
        {
            Id = Guid.NewGuid(),
            CriticalTaskId = taskId,
            OrganizationId = orgId,
            EvaluatorId = evaluator.Id,
            Evaluator = evaluator,
            Rating = rating,
            ObservationText = observationText,
            ObservedAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow
        });
        context.SaveChanges();
    }

    // =========================================================================
    // ExportEegDataAsync (Excel)
    // =========================================================================

    [Fact]
    public async Task ExportEegDataAsync_NonexistentExercise_ThrowsInvalidOperationException()
    {
        var (_, service, _, _) = CreateTestContext();
        var request = new ExportEegRequest(Guid.NewGuid());

        var act = () => service.ExportEegDataAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ExportEegDataAsync_ValidExercise_ReturnsXlsxBytes()
    {
        var (context, service, exerciseId, orgId) = CreateTestContext();
        var (_, taskId) = SeedCapabilityWithTask(context, exerciseId, orgId);
        SeedEegEntry(context, taskId, orgId);

        var request = new ExportEegRequest(exerciseId);
        var result = await service.ExportEegDataAsync(request);

        result.Should().NotBeNull();
        result.Content.Should().NotBeEmpty();
        result.Filename.Should().EndWith(".xlsx");
        result.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task ExportEegDataAsync_CustomFilename_UsesProvidedName()
    {
        var (context, service, exerciseId, orgId) = CreateTestContext();
        var (_, taskId) = SeedCapabilityWithTask(context, exerciseId, orgId);
        SeedEegEntry(context, taskId, orgId);

        var request = new ExportEegRequest(exerciseId, Filename: "My_Custom_Export");
        var result = await service.ExportEegDataAsync(request);

        result.Filename.Should().Be("My_Custom_Export.xlsx");
    }

    [Fact]
    public async Task ExportEegDataAsync_NoCapabilities_ReturnsValidXlsx()
    {
        var (_, service, exerciseId, _) = CreateTestContext();
        var request = new ExportEegRequest(exerciseId);

        var result = await service.ExportEegDataAsync(request);

        result.Content.Should().NotBeEmpty();

        // Verify it's a valid workbook
        using var stream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(stream);
        workbook.Worksheets.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportEegDataAsync_IncludeSummaryOnly_CreatesWorkbook()
    {
        var (context, service, exerciseId, orgId) = CreateTestContext();
        var (_, taskId) = SeedCapabilityWithTask(context, exerciseId, orgId);
        SeedEegEntry(context, taskId, orgId);

        var request = new ExportEegRequest(exerciseId,
            IncludeSummary: true,
            IncludeByCapability: false,
            IncludeAllEntries: false,
            IncludeCoverageGaps: false);

        var result = await service.ExportEegDataAsync(request);

        result.Content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportEegDataAsync_MultipleEntries_AllIncluded()
    {
        var (context, service, exerciseId, orgId) = CreateTestContext();
        var (_, taskId) = SeedCapabilityWithTask(context, exerciseId, orgId);
        SeedEegEntry(context, taskId, orgId, PerformanceRating.Performed);
        SeedEegEntry(context, taskId, orgId, PerformanceRating.SomeChallenges);
        SeedEegEntry(context, taskId, orgId, PerformanceRating.MajorChallenges);

        var request = new ExportEegRequest(exerciseId);
        var result = await service.ExportEegDataAsync(request);

        result.ObjectiveCount.Should().Be(3); // EEG entries count
    }

    // =========================================================================
    // ExportEegJsonAsync
    // =========================================================================

    [Fact]
    public async Task ExportEegJsonAsync_NonexistentExercise_ThrowsInvalidOperationException()
    {
        var (_, service, _, _) = CreateTestContext();

        var act = () => service.ExportEegJsonAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ExportEegJsonAsync_ValidExercise_ReturnsStructuredData()
    {
        var (context, service, exerciseId, orgId) = CreateTestContext();
        var (_, taskId) = SeedCapabilityWithTask(context, exerciseId, orgId);
        SeedEegEntry(context, taskId, orgId, PerformanceRating.Performed);

        var result = await service.ExportEegJsonAsync(exerciseId);

        result.Exercise.Should().NotBeNull();
        result.Exercise.Name.Should().Be("Test Exercise");
        result.Summary.TotalEntries.Should().Be(1);
        result.ByCapability.Should().NotBeEmpty();
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ExportEegJsonAsync_RatingDistribution_CountsCorrectly()
    {
        var (context, service, exerciseId, orgId) = CreateTestContext();
        var (_, taskId) = SeedCapabilityWithTask(context, exerciseId, orgId);
        SeedEegEntry(context, taskId, orgId, PerformanceRating.Performed);
        SeedEegEntry(context, taskId, orgId, PerformanceRating.Performed);
        SeedEegEntry(context, taskId, orgId, PerformanceRating.SomeChallenges);
        SeedEegEntry(context, taskId, orgId, PerformanceRating.UnableToPerform);

        var result = await service.ExportEegJsonAsync(exerciseId);

        result.Summary.RatingDistribution.P.Should().Be(2);
        result.Summary.RatingDistribution.S.Should().Be(1);
        result.Summary.RatingDistribution.M.Should().Be(0);
        result.Summary.RatingDistribution.U.Should().Be(1);
    }

    [Fact]
    public async Task ExportEegJsonAsync_TaskCoverage_CalculatesCorrectly()
    {
        var (context, service, exerciseId, orgId) = CreateTestContext();
        var (_, task1Id) = SeedCapabilityWithTask(context, exerciseId, orgId, taskDesc: "Task 1");

        // Add a second task under the same capability target
        var capTarget = context.Set<CapabilityTarget>().First();
        var task2 = new CriticalTask
        {
            Id = Guid.NewGuid(),
            CapabilityTargetId = capTarget.Id,
            OrganizationId = orgId,
            TaskDescription = "Task 2",
            SortOrder = 2
        };
        context.Set<CriticalTask>().Add(task2);
        context.SaveChanges();

        // Only evaluate task 1
        SeedEegEntry(context, task1Id, orgId);

        var result = await service.ExportEegJsonAsync(exerciseId);

        result.Summary.TasksCoverage.Evaluated.Should().Be(1);
        result.Summary.TasksCoverage.Total.Should().Be(2);
        result.Summary.TasksCoverage.Percentage.Should().Be(50);
    }

    [Fact]
    public async Task ExportEegJsonAsync_CoverageGaps_ListsUnevaluatedTasks()
    {
        var (context, service, exerciseId, orgId) = CreateTestContext();
        SeedCapabilityWithTask(context, exerciseId, orgId, taskDesc: "Unevaluated Task");
        // No EEG entries — the task is a coverage gap

        var result = await service.ExportEegJsonAsync(exerciseId);

        result.CoverageGaps.Should().NotBeEmpty();
        result.CoverageGaps.First().TaskDescription.Should().Be("Unevaluated Task");
    }

    [Fact]
    public async Task ExportEegJsonAsync_IncludeEvaluatorNames_False_OmitsNames()
    {
        var (context, service, exerciseId, orgId) = CreateTestContext();
        var (_, taskId) = SeedCapabilityWithTask(context, exerciseId, orgId);
        SeedEegEntry(context, taskId, orgId);

        var result = await service.ExportEegJsonAsync(exerciseId, includeEvaluatorNames: false);

        var entry = result.ByCapability.First().Tasks.First().Entries.First();
        entry.Evaluator.Should().BeNull();
    }

    [Fact]
    public async Task ExportEegJsonAsync_IncludeEvaluatorNames_True_IncludesNames()
    {
        var (context, service, exerciseId, orgId) = CreateTestContext();
        var (_, taskId) = SeedCapabilityWithTask(context, exerciseId, orgId);
        SeedEegEntry(context, taskId, orgId);

        var result = await service.ExportEegJsonAsync(exerciseId, includeEvaluatorNames: true);

        var entry = result.ByCapability.First().Tasks.First().Entries.First();
        entry.Evaluator.Should().Be("Test Evaluator");
    }

    [Fact]
    public async Task ExportEegJsonAsync_NoEntries_ReturnsZeroCoverage()
    {
        var (_, service, exerciseId, _) = CreateTestContext();

        var result = await service.ExportEegJsonAsync(exerciseId);

        result.Summary.TotalEntries.Should().Be(0);
        result.Summary.TasksCoverage.Total.Should().Be(0);
        result.Summary.TasksCoverage.Percentage.Should().Be(0);
    }

    [Fact]
    public async Task ExportEegJsonAsync_SoftDeletedTasks_Excluded()
    {
        var (context, service, exerciseId, orgId) = CreateTestContext();
        var (_, taskId) = SeedCapabilityWithTask(context, exerciseId, orgId);

        // Soft-delete the task
        var task = context.Set<CriticalTask>().Find(taskId)!;
        task.IsDeleted = true;
        task.DeletedAt = DateTime.UtcNow;
        context.SaveChanges();

        var result = await service.ExportEegJsonAsync(exerciseId);

        result.Summary.TasksCoverage.Total.Should().Be(0);
    }
}
