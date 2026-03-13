using Cadence.Core.Features.Eeg.Builders;
using Cadence.Core.Features.Eeg.Services;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Eeg;

/// <summary>
/// Tests for EegSummaryBuilder — builds the "Summary" worksheet in EEG Excel exports.
/// </summary>
public class EegSummaryBuilderTests
{
    private static Exercise CreateExercise(string name = "Test Exercise") =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            ScheduledDate = new DateOnly(2026, 1, 15)
        };

    private static CapabilityTarget CreateCapabilityTarget(
        string capabilityName = "Operational Communications",
        string targetDescription = "Establish comms within 30 min",
        params (string description, int sortOrder)[] tasks)
    {
        var capability = new Capability
        {
            Id = Guid.NewGuid(),
            Name = capabilityName
        };

        var ct = new CapabilityTarget
        {
            Id = Guid.NewGuid(),
            CapabilityId = capability.Id,
            Capability = capability,
            TargetDescription = targetDescription,
            SortOrder = 1,
            CriticalTasks = new List<CriticalTask>()
        };

        foreach (var (desc, so) in tasks)
        {
            ct.CriticalTasks.Add(new CriticalTask
            {
                Id = Guid.NewGuid(),
                CapabilityTargetId = ct.Id,
                TaskDescription = desc,
                SortOrder = so
            });
        }

        return ct;
    }

    private static ExportEegRequest DefaultRequest(bool formatting = false) =>
        new(Guid.NewGuid(), IncludeFormatting: formatting, IncludeSummary: true);

    [Fact]
    public void AddSummaryWorksheet_EmptyData_AddsWorksheetWithHeaders()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        var exercise = CreateExercise();
        var capabilityTargets = new List<CapabilityTarget>();
        var eegEntries = new List<EegEntry>();

        // Act
        EegSummaryBuilder.AddSummaryWorksheet(
            workbook, exercise, capabilityTargets, eegEntries, DefaultRequest());

        // Assert — worksheet is present with expected section headers
        workbook.Worksheets.TryGetWorksheet("Summary", out _).Should().BeTrue();
        var ws = workbook.Worksheet("Summary");
        ws.Cell(1, 1).GetString().Should().Be("EXERCISE SUMMARY");
        ws.Cell(3, 1).GetString().Should().Be("Exercise Name");
        ws.Cell(3, 2).GetString().Should().Be(exercise.Name);
    }

    [Fact]
    public void AddSummaryWorksheet_WithEntries_PopulatesData()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        var exercise = CreateExercise("Ops Exercise");
        var ct = CreateCapabilityTarget("Operational Communications", "Establish comms within 30 min", ("Activate EOC", 1), ("Establish ICS", 2));
        var evaluatorId = "eval-1";

        var entries = new List<EegEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CriticalTaskId = ct.CriticalTasks.First().Id,
                Rating = PerformanceRating.Performed,
                ObservationText = "Good performance",
                EvaluatorId = evaluatorId,
                ObservedAt = DateTime.UtcNow,
                RecordedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                CriticalTaskId = ct.CriticalTasks.Last().Id,
                Rating = PerformanceRating.SomeChallenges,
                ObservationText = "Minor issues noted",
                EvaluatorId = evaluatorId,
                ObservedAt = DateTime.UtcNow,
                RecordedAt = DateTime.UtcNow
            }
        };

        // Act
        EegSummaryBuilder.AddSummaryWorksheet(
            workbook, exercise, new List<CapabilityTarget> { ct }, entries, DefaultRequest());

        // Assert — entry count and coverage appear in the worksheet
        var ws = workbook.Worksheet("Summary");

        // Find "Total Entries" row (row 9 based on structure: header+blank+4 meta+blank+coverage header+blank)
        // We look across all used cells for the value we expect rather than hardcoding row numbers
        var usedRows = ws.RowsUsed().ToList();
        var totalEntriesRow = usedRows.FirstOrDefault(r => r.Cell(1).GetString() == "Total Entries");
        totalEntriesRow.Should().NotBeNull(because: "the worksheet should contain a 'Total Entries' row");
        totalEntriesRow!.Cell(2).GetDouble().Should().Be(2);
    }

    [Fact]
    public void AddSummaryWorksheet_IncludeRatingBreakdown_ShowsRatingColumns()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        var exercise = CreateExercise();
        var ct = CreateCapabilityTarget(tasks: ("Task A", 1));
        var entries = new List<EegEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CriticalTaskId = ct.CriticalTasks.First().Id,
                Rating = PerformanceRating.MajorChallenges,
                ObservationText = "Significant gaps",
                EvaluatorId = "eval-1",
                ObservedAt = DateTime.UtcNow,
                RecordedAt = DateTime.UtcNow
            }
        };

        // Act
        EegSummaryBuilder.AddSummaryWorksheet(
            workbook, exercise, new List<CapabilityTarget> { ct }, entries, DefaultRequest());

        // Assert — RATING DISTRIBUTION section is present and shows a header row with
        // "Rating", "Count", "Percentage" columns
        var ws = workbook.Worksheet("Summary");
        var ratingHeaderRow = ws.RowsUsed()
            .FirstOrDefault(r => r.Cell(1).GetString() == "Rating");
        ratingHeaderRow.Should().NotBeNull(because: "RATING DISTRIBUTION section should have column headers");
        ratingHeaderRow!.Cell(2).GetString().Should().Be("Count");
        ratingHeaderRow.Cell(3).GetString().Should().Be("Percentage");
    }
}
