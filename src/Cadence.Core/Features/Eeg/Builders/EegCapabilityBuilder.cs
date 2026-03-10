using System.Globalization;
using Cadence.Core.Features.Eeg.Services;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;

namespace Cadence.Core.Features.Eeg.Builders;

/// <summary>
/// Builds the "By Capability" worksheet for an EEG Excel export.
/// Groups EEG entries under their parent capability target and critical task,
/// making it easy to review performance against each exercise objective.
/// </summary>
internal static class EegCapabilityBuilder
{
    /// <summary>
    /// Adds the "By Capability" worksheet to the provided workbook.
    /// </summary>
    /// <param name="workbook">Target workbook to add the worksheet to.</param>
    /// <param name="capabilityTargets">All capability targets for the exercise, in sort order.</param>
    /// <param name="eegEntries">All EEG entries for the exercise.</param>
    /// <param name="request">Export options (formatting, evaluator name visibility).</param>
    internal static void AddByCapabilityWorksheet(
        XLWorkbook workbook,
        List<CapabilityTarget> capabilityTargets,
        List<EegEntry> eegEntries,
        ExportEegRequest request)
    {
        var ws = workbook.Worksheets.Add("By Capability");

        var row = 1;

        foreach (var ct in capabilityTargets)
        {
            // Capability Target header
            ws.Cell(row, 1).Value = ct.Capability.Name.ToUpperInvariant();
            if (request.IncludeFormatting)
            {
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 12;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                ws.Range(row, 1, row, 5).Merge();
            }
            row++;

            ws.Cell(row, 1).Value = $"Target: {ct.TargetDescription}";
            if (request.IncludeFormatting)
            {
                ws.Cell(row, 1).Style.Font.Italic = true;
                ws.Range(row, 1, row, 5).Merge();
            }
            row += 2;

            // Column headers
            ws.Cell(row, 1).Value = "Task";
            ws.Cell(row, 2).Value = "Rating";
            ws.Cell(row, 3).Value = "Observation";
            ws.Cell(row, 4).Value = "Evaluator";
            ws.Cell(row, 5).Value = "Time";
            if (request.IncludeFormatting)
            {
                ws.Range(row, 1, row, 5).Style.Font.Bold = true;
                ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.LightGray;
            }
            row++;

            var tasks = ct.CriticalTasks.Where(t => !t.IsDeleted).OrderBy(t => t.SortOrder).ToList();

            foreach (var task in tasks)
            {
                var taskEntries = eegEntries
                    .Where(e => e.CriticalTaskId == task.Id)
                    .OrderBy(e => e.RecordedAt)
                    .ToList();

                if (taskEntries.Count == 0)
                {
                    // Task with no entries
                    ws.Cell(row, 1).Value = task.TaskDescription;
                    ws.Cell(row, 2).Value = "Not Evaluated";
                    if (request.IncludeFormatting)
                    {
                        ws.Cell(row, 2).Style.Font.Italic = true;
                        ws.Cell(row, 2).Style.Font.FontColor = XLColor.Gray;
                    }
                    row++;
                }
                else
                {
                    // First entry with task name
                    var firstEntry = taskEntries[0];
                    ws.Cell(row, 1).Value = task.TaskDescription;
                    ws.Cell(row, 2).Value = EegFormattingHelper.GetRatingShortCode(firstEntry.Rating);
                    ws.Cell(row, 3).Value = EegFormattingHelper.TruncateText(firstEntry.ObservationText, 100);
                    ws.Cell(row, 4).Value = request.IncludeEvaluatorNames ? (firstEntry.Evaluator?.DisplayName ?? "") : "";
                    ws.Cell(row, 5).Value = firstEntry.ObservedAt.ToString("HH:mm", CultureInfo.InvariantCulture);

                    if (request.IncludeFormatting)
                    {
                        ws.Cell(row, 2).Style.Fill.BackgroundColor = EegFormattingHelper.RatingBackgroundColors[firstEntry.Rating];
                    }
                    row++;

                    // Additional entries without task name
                    foreach (var entry in taskEntries.Skip(1))
                    {
                        ws.Cell(row, 2).Value = EegFormattingHelper.GetRatingShortCode(entry.Rating);
                        ws.Cell(row, 3).Value = EegFormattingHelper.TruncateText(entry.ObservationText, 100);
                        ws.Cell(row, 4).Value = request.IncludeEvaluatorNames ? (entry.Evaluator?.DisplayName ?? "") : "";
                        ws.Cell(row, 5).Value = entry.ObservedAt.ToString("HH:mm", CultureInfo.InvariantCulture);

                        if (request.IncludeFormatting)
                        {
                            ws.Cell(row, 2).Style.Fill.BackgroundColor = EegFormattingHelper.RatingBackgroundColors[entry.Rating];
                        }
                        row++;
                    }
                }
            }

            row += 2; // Space between capability targets
        }

        // Format columns
        if (request.IncludeFormatting)
        {
            ws.Column(1).Width = 40;
            ws.Column(2).Width = 10;
            ws.Column(3).Width = 60;
            ws.Column(4).Width = 20;
            ws.Column(5).Width = 12;
        }
    }
}
