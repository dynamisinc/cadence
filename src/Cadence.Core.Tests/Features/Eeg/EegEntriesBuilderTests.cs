using Cadence.Core.Features.Eeg.Builders;
using Cadence.Core.Features.Eeg.Services;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Eeg;

/// <summary>
/// Tests for EegEntriesBuilder — builds the "All Entries" flat-table worksheet
/// in EEG Excel exports.
/// </summary>
public class EegEntriesBuilderTests
{
    private static CapabilityTarget CreateCapabilityTarget(
        string capabilityName = "Operational Communications",
        string targetDescription = "Establish comms within 30 min",
        int sortOrder = 1)
    {
        var capability = new Capability
        {
            Id = Guid.NewGuid(),
            Name = capabilityName
        };

        return new CapabilityTarget
        {
            Id = Guid.NewGuid(),
            CapabilityId = capability.Id,
            Capability = capability,
            TargetDescription = targetDescription,
            SortOrder = sortOrder,
            CriticalTasks = new List<CriticalTask>()
        };
    }

    private static CriticalTask CreateTask(Guid capabilityTargetId, string description = "Issue EOC notification", int sortOrder = 1)
    {
        return new CriticalTask
        {
            Id = Guid.NewGuid(),
            CapabilityTargetId = capabilityTargetId,
            TaskDescription = description,
            SortOrder = sortOrder
        };
    }

    private static ExportEegRequest DefaultRequest(bool formatting = false, bool includeEvaluatorNames = true) =>
        new(Guid.NewGuid(), IncludeFormatting: formatting, IncludeEvaluatorNames: includeEvaluatorNames);

    [Fact]
    public void AddAllEntriesWorksheet_EmptyEntries_AddsWorksheetWithHeaders()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        var capabilityTargets = new List<CapabilityTarget>();
        var eegEntries = new List<EegEntry>();

        // Act
        EegEntriesBuilder.AddAllEntriesWorksheet(workbook, capabilityTargets, eegEntries, DefaultRequest());

        // Assert — worksheet exists with the expected column headers
        workbook.Worksheets.TryGetWorksheet("All Entries", out _).Should().BeTrue();
        var ws = workbook.Worksheet("All Entries");
        ws.Cell(1, 1).GetString().Should().Be("Timestamp");
        ws.Cell(1, 2).GetString().Should().Be("Capability");
        ws.Cell(1, 3).GetString().Should().Be("Target");
        ws.Cell(1, 4).GetString().Should().Be("Task");
        ws.Cell(1, 5).GetString().Should().Be("Rating");
        ws.Cell(1, 6).GetString().Should().Be("Observation");
        ws.Cell(1, 7).GetString().Should().Be("Evaluator");
        ws.Cell(1, 8).GetString().Should().Be("Triggering Inject");
    }

    [Fact]
    public void AddAllEntriesWorksheet_WithEntries_PopulatesAllRows()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        var ct = CreateCapabilityTarget();
        var task1 = CreateTask(ct.Id, "Activate EOC", 1);
        var task2 = CreateTask(ct.Id, "Notify partners", 2);
        ct.CriticalTasks.Add(task1);
        ct.CriticalTasks.Add(task2);

        var evaluator = new ApplicationUser
        {
            Id = "eval-1",
            DisplayName = "Jane Smith",
            Email = "jane@example.com"
        };

        var recordedTime1 = new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);
        var recordedTime2 = new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc);

        var eegEntries = new List<EegEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CriticalTaskId = task1.Id,
                CriticalTask = task1,
                Rating = PerformanceRating.Performed,
                ObservationText = "EOC activated on time",
                EvaluatorId = evaluator.Id,
                Evaluator = evaluator,
                ObservedAt = recordedTime1,
                RecordedAt = recordedTime1
            },
            new()
            {
                Id = Guid.NewGuid(),
                CriticalTaskId = task2.Id,
                CriticalTask = task2,
                Rating = PerformanceRating.SomeChallenges,
                ObservationText = "Notifications delayed by 10 min",
                EvaluatorId = evaluator.Id,
                Evaluator = evaluator,
                ObservedAt = recordedTime2,
                RecordedAt = recordedTime2
            }
        };

        // Act
        EegEntriesBuilder.AddAllEntriesWorksheet(
            workbook, new List<CapabilityTarget> { ct }, eegEntries, DefaultRequest());

        // Assert — two data rows present (rows 2 and 3), ordered by RecordedAt
        var ws = workbook.Worksheet("All Entries");

        // First data row: earlier recorded entry
        ws.Cell(2, 1).GetString().Should().Contain("2026-01-15");
        ws.Cell(2, 2).GetString().Should().Be("Operational Communications");
        ws.Cell(2, 3).GetString().Should().Be("Establish comms within 30 min");
        ws.Cell(2, 4).GetString().Should().Be("Activate EOC");
        ws.Cell(2, 5).GetString().Should().Be("P");
        ws.Cell(2, 6).GetString().Should().Be("EOC activated on time");
        ws.Cell(2, 7).GetString().Should().Be("Jane Smith");

        // Second data row: later recorded entry
        ws.Cell(3, 5).GetString().Should().Be("S");
    }

    [Fact]
    public void AddAllEntriesWorksheet_EntriesWithNullFields_HandlesGracefully()
    {
        // Arrange — entries where CriticalTask, Evaluator, and TriggeringInject are null
        using var workbook = new XLWorkbook();
        var taskId = Guid.NewGuid();
        var eegEntries = new List<EegEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CriticalTaskId = taskId,
                CriticalTask = null!, // null navigation property
                Rating = PerformanceRating.UnableToPerform,
                ObservationText = "No task context available",
                EvaluatorId = "eval-1",
                Evaluator = null, // null evaluator
                TriggeringInject = null, // null inject
                ObservedAt = DateTime.UtcNow,
                RecordedAt = DateTime.UtcNow
            }
        };

        // Act — should not throw
        var act = () => EegEntriesBuilder.AddAllEntriesWorksheet(
            workbook, new List<CapabilityTarget>(), eegEntries, DefaultRequest());

        // Assert
        act.Should().NotThrow();
        var ws = workbook.Worksheet("All Entries");
        ws.Cell(2, 4).GetString().Should().Be(""); // null task description falls back to empty
        ws.Cell(2, 7).GetString().Should().Be(""); // null evaluator falls back to empty
        ws.Cell(2, 8).GetString().Should().Be(""); // null inject falls back to empty
    }
}
