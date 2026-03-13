using Cadence.Core.Features.Eeg.Builders;
using Cadence.Core.Features.Eeg.Services;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Eeg;

/// <summary>
/// Tests for EegCoverageGapsBuilder — identifies unevaluated critical tasks in EEG exports.
/// </summary>
public class EegCoverageGapsBuilderTests
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

    private static ExportEegRequest DefaultRequest(bool formatting = false) =>
        new(Guid.NewGuid(), IncludeFormatting: formatting, IncludeCoverageGaps: true);

    [Fact]
    public void AddCoverageGapsWorksheet_AllTasksEvaluated_ShowsSuccessMessage()
    {
        using var workbook = new XLWorkbook();
        var ct = CreateCapabilityTarget(tasks: ("Task 1", 1));
        var entries = new List<EegEntry>
        {
            new() { Id = Guid.NewGuid(), CriticalTaskId = ct.CriticalTasks.First().Id }
        };

        EegCoverageGapsBuilder.AddCoverageGapsWorksheet(
            workbook, new List<CapabilityTarget> { ct }, entries, DefaultRequest());

        var ws = workbook.Worksheet("Coverage Gaps");
        ws.Cell(1, 1).GetString().Should().Be("All Critical Tasks Evaluated");
        ws.Cell(3, 1).GetString().Should().Contain("1 of 1");
    }

    [Fact]
    public void AddCoverageGapsWorksheet_UnevaluatedTasks_ShowsWarning()
    {
        using var workbook = new XLWorkbook();
        var ct = CreateCapabilityTarget(tasks: ("Unevaluated Task", 1));

        EegCoverageGapsBuilder.AddCoverageGapsWorksheet(
            workbook, new List<CapabilityTarget> { ct }, new List<EegEntry>(), DefaultRequest());

        var ws = workbook.Worksheet("Coverage Gaps");
        ws.Cell(1, 1).GetString().Should().Be("TASKS NEEDING EVALUATION");
        ws.Cell(2, 1).GetString().Should().Contain("1 critical tasks");
    }

    [Fact]
    public void AddCoverageGapsWorksheet_UnevaluatedTasks_ListsCapabilityAndTask()
    {
        using var workbook = new XLWorkbook();
        var ct = CreateCapabilityTarget(
            capabilityName: "Mass Care",
            targetDescription: "Open shelters",
            tasks: ("Activate Red Cross", 1));

        EegCoverageGapsBuilder.AddCoverageGapsWorksheet(
            workbook, new List<CapabilityTarget> { ct }, new List<EegEntry>(), DefaultRequest());

        var ws = workbook.Worksheet("Coverage Gaps");
        // Row 4 = column headers, Row 5 = first data row
        ws.Cell(5, 1).GetString().Should().Be("Mass Care");
        ws.Cell(5, 2).GetString().Should().Be("Open shelters");
        ws.Cell(5, 3).GetString().Should().Be("Activate Red Cross");
    }

    [Fact]
    public void AddCoverageGapsWorksheet_SoftDeletedTasks_Excluded()
    {
        using var workbook = new XLWorkbook();
        var ct = CreateCapabilityTarget(tasks: [("Active Task", 1), ("Deleted Task", 2)]);
        ct.CriticalTasks.Last().IsDeleted = true;

        EegCoverageGapsBuilder.AddCoverageGapsWorksheet(
            workbook, new List<CapabilityTarget> { ct }, new List<EegEntry>(), DefaultRequest());

        var ws = workbook.Worksheet("Coverage Gaps");
        ws.Cell(2, 1).GetString().Should().Contain("1 critical tasks");
    }

    [Fact]
    public void AddCoverageGapsWorksheet_MultipleCapabilities_SortedBySortOrder()
    {
        using var workbook = new XLWorkbook();
        var ct1 = CreateCapabilityTarget("Bravo Cap", "B Target", 2, ("B Task", 1));
        var ct2 = CreateCapabilityTarget("Alpha Cap", "A Target", 1, ("A Task", 1));

        EegCoverageGapsBuilder.AddCoverageGapsWorksheet(
            workbook, new List<CapabilityTarget> { ct1, ct2 }, new List<EegEntry>(), DefaultRequest());

        var ws = workbook.Worksheet("Coverage Gaps");
        ws.Cell(5, 1).GetString().Should().Be("Alpha Cap");
        ws.Cell(6, 1).GetString().Should().Be("Bravo Cap");
    }

    [Fact]
    public void AddCoverageGapsWorksheet_WithFormatting_AppliesOrangeHeaderAndColumnWidths()
    {
        using var workbook = new XLWorkbook();
        var ct = CreateCapabilityTarget(tasks: ("Task 1", 1));

        EegCoverageGapsBuilder.AddCoverageGapsWorksheet(
            workbook, new List<CapabilityTarget> { ct }, new List<EegEntry>(), DefaultRequest(formatting: true));

        var ws = workbook.Worksheet("Coverage Gaps");
        ws.Cell(1, 1).Style.Font.FontColor.Should().Be(XLColor.Orange);
        ws.Cell(1, 1).Style.Font.Bold.Should().BeTrue();
        ws.Column(1).Width.Should().Be(25);
        ws.Column(2).Width.Should().Be(40);
    }

    [Fact]
    public void AddCoverageGapsWorksheet_AllEvaluatedWithFormatting_GreenHeader()
    {
        using var workbook = new XLWorkbook();
        var ct = CreateCapabilityTarget(tasks: ("Task 1", 1));
        var entries = new List<EegEntry>
        {
            new() { Id = Guid.NewGuid(), CriticalTaskId = ct.CriticalTasks.First().Id }
        };

        EegCoverageGapsBuilder.AddCoverageGapsWorksheet(
            workbook, new List<CapabilityTarget> { ct }, entries, DefaultRequest(formatting: true));

        var ws = workbook.Worksheet("Coverage Gaps");
        ws.Cell(1, 1).Style.Font.FontColor.Should().Be(XLColor.Green);
    }

    [Fact]
    public void AddCoverageGapsWorksheet_NoCapabilities_ShowsAllEvaluated()
    {
        using var workbook = new XLWorkbook();

        EegCoverageGapsBuilder.AddCoverageGapsWorksheet(
            workbook, new List<CapabilityTarget>(), new List<EegEntry>(), DefaultRequest());

        var ws = workbook.Worksheet("Coverage Gaps");
        ws.Cell(1, 1).GetString().Should().Be("All Critical Tasks Evaluated");
    }

    // -------------------------------------------------------------------------
    // Additional coverage-gaps tests
    // -------------------------------------------------------------------------

    [Fact]
    public void AddCoverageGapsWorksheet_AllTargetsCovered_ShowsNone()
    {
        // Arrange — every task has at least one entry
        using var workbook = new XLWorkbook();
        var ct = CreateCapabilityTarget("Operational Communications", "Establish comms within 30 min", 1, ("Task A", 1), ("Task B", 2));
        var entries = ct.CriticalTasks
            .Select(t => new EegEntry { Id = Guid.NewGuid(), CriticalTaskId = t.Id })
            .ToList();

        // Act
        EegCoverageGapsBuilder.AddCoverageGapsWorksheet(
            workbook, new List<CapabilityTarget> { ct }, entries, DefaultRequest());

        // Assert — success message, not the warning header
        var ws = workbook.Worksheet("Coverage Gaps");
        ws.Cell(1, 1).GetString().Should().Be("All Critical Tasks Evaluated");
        ws.Cell(2, 1).GetString().Should().BeEmpty(
            because: "there should be no unevaluated count message when all tasks are covered");
    }

    [Fact]
    public void AddCoverageGapsWorksheet_UncoveredTargets_ListsGaps()
    {
        // Arrange — one task has entries, two tasks do not
        using var workbook = new XLWorkbook();
        var ct = CreateCapabilityTarget(
            "Public Health",
            "Activate health operations",
            1,
            ("Notify health officer", 1),
            ("Open dispensing sites", 2),
            ("Coordinate with hospitals", 3));

        // Only the first task has an entry
        var entries = new List<EegEntry>
        {
            new() { Id = Guid.NewGuid(), CriticalTaskId = ct.CriticalTasks.First().Id }
        };

        // Act
        EegCoverageGapsBuilder.AddCoverageGapsWorksheet(
            workbook, new List<CapabilityTarget> { ct }, entries, DefaultRequest());

        // Assert — two uncovered tasks listed
        var ws = workbook.Worksheet("Coverage Gaps");
        ws.Cell(1, 1).GetString().Should().Be("TASKS NEEDING EVALUATION");
        ws.Cell(2, 1).GetString().Should().Contain("2 critical tasks");

        // Data rows start at row 5 (row 4 = column headers)
        ws.Cell(5, 3).GetString().Should().Be("Open dispensing sites");
        ws.Cell(6, 3).GetString().Should().Be("Coordinate with hospitals");
    }

    [Fact]
    public void AddCoverageGapsWorksheet_EmptyData_AddsWorksheet()
    {
        // Arrange — no targets, no entries
        using var workbook = new XLWorkbook();

        // Act — should not throw
        var act = () => EegCoverageGapsBuilder.AddCoverageGapsWorksheet(
            workbook, new List<CapabilityTarget>(), new List<EegEntry>(), DefaultRequest());

        // Assert
        act.Should().NotThrow();
        workbook.Worksheets.TryGetWorksheet("Coverage Gaps", out _).Should().BeTrue();
    }
}
