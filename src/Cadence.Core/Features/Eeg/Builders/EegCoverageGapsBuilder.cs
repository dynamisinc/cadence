using Cadence.Core.Features.Eeg.Services;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;

namespace Cadence.Core.Features.Eeg.Builders;

/// <summary>
/// Builds the "Coverage Gaps" worksheet for an EEG Excel export.
/// Identifies critical tasks that have received no EEG entries so evaluators
/// can prioritize remaining observations before the exercise ends.
/// </summary>
internal static class EegCoverageGapsBuilder
{
    /// <summary>
    /// Adds the "Coverage Gaps" worksheet to the provided workbook.
    /// </summary>
    /// <param name="workbook">Target workbook to add the worksheet to.</param>
    /// <param name="capabilityTargets">All capability targets for the exercise.</param>
    /// <param name="eegEntries">All EEG entries recorded so far.</param>
    /// <param name="request">Export options (formatting).</param>
    internal static void AddCoverageGapsWorksheet(
        XLWorkbook workbook,
        List<CapabilityTarget> capabilityTargets,
        List<EegEntry> eegEntries,
        ExportEegRequest request)
    {
        var ws = workbook.Worksheets.Add("Coverage Gaps");

        var evaluatedTaskIds = eegEntries.Select(e => e.CriticalTaskId).Distinct().ToHashSet();

        var unevaluatedTasks = capabilityTargets
            .SelectMany(ct => ct.CriticalTasks
                .Where(t => !t.IsDeleted && !evaluatedTaskIds.Contains(t.Id))
                .Select(t => (CapabilityTarget: ct, Task: t)))
            .OrderBy(x => x.CapabilityTarget.SortOrder)
            .ThenBy(x => x.Task.SortOrder)
            .ToList();

        if (unevaluatedTasks.Count == 0)
        {
            // All tasks evaluated
            ws.Cell(1, 1).Value = "All Critical Tasks Evaluated";
            if (request.IncludeFormatting)
            {
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 14;
                ws.Cell(1, 1).Style.Font.FontColor = XLColor.Green;
            }

            var totalTasks = capabilityTargets.SelectMany(ct => ct.CriticalTasks.Where(t => !t.IsDeleted)).Count();
            ws.Cell(3, 1).Value = $"{totalTasks} of {totalTasks} tasks have at least one EEG entry.";
            ws.Cell(5, 1).Value = "Consider reviewing tasks with M or U ratings before exercise ends.";

            if (request.IncludeFormatting)
            {
                ws.Column(1).Width = 60;
            }
            return;
        }

        // Header
        ws.Cell(1, 1).Value = "TASKS NEEDING EVALUATION";
        if (request.IncludeFormatting)
        {
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.Orange;
        }

        ws.Cell(2, 1).Value = $"{unevaluatedTasks.Count} critical tasks have no EEG entries";
        var row = 4;

        // Column headers
        ws.Cell(row, 1).Value = "Capability";
        ws.Cell(row, 2).Value = "Target";
        ws.Cell(row, 3).Value = "Task";
        if (request.IncludeFormatting)
        {
            ws.Range(row, 1, row, 3).Style.Font.Bold = true;
            ws.Range(row, 1, row, 3).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        row++;

        foreach (var (ct, task) in unevaluatedTasks)
        {
            ws.Cell(row, 1).Value = ct.Capability.Name;
            ws.Cell(row, 2).Value = ct.TargetDescription;
            ws.Cell(row, 3).Value = task.TaskDescription;

            if (request.IncludeFormatting && row % 2 == 0)
            {
                ws.Range(row, 1, row, 3).Style.Fill.BackgroundColor = XLColor.AliceBlue;
            }

            row++;
        }

        // Format columns
        if (request.IncludeFormatting)
        {
            ws.Column(1).Width = 25;
            ws.Column(2).Width = 40;
            ws.Column(3).Width = 40;

            if (unevaluatedTasks.Count > 0)
            {
                ws.Range(4, 1, row - 1, 3).SetAutoFilter();
            }
        }
    }
}
