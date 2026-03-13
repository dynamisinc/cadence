using Cadence.Core.Features.Eeg.Builders;
using Cadence.Core.Features.Eeg.Services;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Eeg;

/// <summary>
/// Tests for EegCapabilityBuilder — builds the "By Capability" grouped worksheet
/// in EEG Excel exports.
/// </summary>
public class EegCapabilityBuilderTests
{
    private static CapabilityTarget CreateCapabilityTarget(
        string capabilityName = "Operational Communications",
        string targetDescription = "Establish comms within 30 min",
        int sortOrder = 1,
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
            SortOrder = sortOrder,
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

    private static ExportEegRequest DefaultRequest(bool formatting = false, bool includeEvaluatorNames = true) =>
        new(Guid.NewGuid(), IncludeFormatting: formatting, IncludeEvaluatorNames: includeEvaluatorNames);

    [Fact]
    public void AddByCapabilityWorksheet_EmptyTargets_AddsWorksheet()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        var capabilityTargets = new List<CapabilityTarget>();
        var eegEntries = new List<EegEntry>();

        // Act
        EegCapabilityBuilder.AddByCapabilityWorksheet(workbook, capabilityTargets, eegEntries, DefaultRequest());

        // Assert — worksheet is created even when there are no targets
        workbook.Worksheets.TryGetWorksheet("By Capability", out _).Should().BeTrue();
    }

    [Fact]
    public void AddByCapabilityWorksheet_WithTargetsAndEntries_GroupsByCapability()
    {
        // Arrange
        using var workbook = new XLWorkbook();
        var ct = CreateCapabilityTarget(
            "Mass Care",
            "Open shelters within 2 hours",
            sortOrder: 1,
            ("Activate Red Cross agreement", 1),
            ("Open designated shelter site", 2));

        var evaluator = new ApplicationUser
        {
            Id = "eval-1",
            DisplayName = "John Evaluator",
            Email = "john@example.com"
        };

        var eegEntries = new List<EegEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CriticalTaskId = ct.CriticalTasks.First().Id,
                Rating = PerformanceRating.Performed,
                ObservationText = "Agreement activated promptly",
                EvaluatorId = evaluator.Id,
                Evaluator = evaluator,
                ObservedAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                RecordedAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        // Act
        EegCapabilityBuilder.AddByCapabilityWorksheet(
            workbook, new List<CapabilityTarget> { ct }, eegEntries, DefaultRequest());

        // Assert — capability name appears at top, task column headers present
        var ws = workbook.Worksheet("By Capability");

        // Row 1: capability name in uppercase
        ws.Cell(1, 1).GetString().Should().Be("MASS CARE");

        // Row 2: target description
        ws.Cell(2, 1).GetString().Should().Contain("Open shelters within 2 hours");

        // Row 4: column headers (Task, Rating, Observation, Evaluator, Time)
        ws.Cell(4, 1).GetString().Should().Be("Task");
        ws.Cell(4, 2).GetString().Should().Be("Rating");
        ws.Cell(4, 3).GetString().Should().Be("Observation");
        ws.Cell(4, 4).GetString().Should().Be("Evaluator");
        ws.Cell(4, 5).GetString().Should().Be("Time");

        // First task row (row 5): evaluated with rating "P"
        ws.Cell(5, 1).GetString().Should().Be("Activate Red Cross agreement");
        ws.Cell(5, 2).GetString().Should().Be("P");
    }

    [Fact]
    public void AddByCapabilityWorksheet_NoEntriesForCapability_ShowsEmptySection()
    {
        // Arrange — capability target with tasks but no EEG entries
        using var workbook = new XLWorkbook();
        var ct = CreateCapabilityTarget(
            "Planning",
            "Update EOP within 30 days",
            sortOrder: 1,
            ("Draft updated annexes", 1),
            ("Conduct stakeholder review", 2));

        // Act
        EegCapabilityBuilder.AddByCapabilityWorksheet(
            workbook, new List<CapabilityTarget> { ct }, new List<EegEntry>(), DefaultRequest());

        // Assert — tasks still appear but marked as "Not Evaluated"
        var ws = workbook.Worksheet("By Capability");

        // Both tasks should be listed (rows 5 and 6 based on layout)
        ws.Cell(5, 1).GetString().Should().Be("Draft updated annexes");
        ws.Cell(5, 2).GetString().Should().Be("Not Evaluated");

        ws.Cell(6, 1).GetString().Should().Be("Conduct stakeholder review");
        ws.Cell(6, 2).GetString().Should().Be("Not Evaluated");
    }
}
