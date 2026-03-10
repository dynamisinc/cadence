using System.Globalization;
using Cadence.Core.Features.Eeg.Services;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;

namespace Cadence.Core.Features.Eeg.Builders;

/// <summary>
/// Builds the "All Entries" worksheet for an EEG Excel export.
/// Renders every EEG entry as a flat table with full context columns,
/// including capability, target, task, rating, observation, evaluator,
/// and triggering inject.
/// </summary>
internal static class EegEntriesBuilder
{
    /// <summary>
    /// Adds the "All Entries" worksheet to the provided workbook.
    /// </summary>
    /// <param name="workbook">Target workbook to add the worksheet to.</param>
    /// <param name="capabilityTargets">All capability targets used to resolve task context.</param>
    /// <param name="eegEntries">All EEG entries for the exercise, ordered by recorded time.</param>
    /// <param name="request">Export options (formatting, evaluator name visibility).</param>
    internal static void AddAllEntriesWorksheet(
        XLWorkbook workbook,
        List<CapabilityTarget> capabilityTargets,
        List<EegEntry> eegEntries,
        ExportEegRequest request)
    {
        var ws = workbook.Worksheets.Add("All Entries");

        // Header row
        var headers = new[] { "Timestamp", "Capability", "Target", "Task", "Rating", "Observation", "Evaluator", "Triggering Inject" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            if (request.IncludeFormatting)
            {
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }
        }

        // Build lookup for capability targets by critical task ID
        var taskToCapability = capabilityTargets
            .SelectMany(ct => ct.CriticalTasks.Where(t => !t.IsDeleted).Select(t => (t.Id, ct)))
            .ToDictionary(x => x.Id, x => x.ct);

        // Data rows
        var row = 2;
        foreach (var entry in eegEntries.OrderBy(e => e.RecordedAt))
        {
            var ct = taskToCapability.GetValueOrDefault(entry.CriticalTaskId);
            var task = entry.CriticalTask;

            ws.Cell(row, 1).Value = entry.RecordedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            ws.Cell(row, 2).Value = ct?.Capability.Name ?? "";
            ws.Cell(row, 3).Value = ct?.TargetDescription ?? "";
            ws.Cell(row, 4).Value = task?.TaskDescription ?? "";
            ws.Cell(row, 5).Value = EegFormattingHelper.GetRatingShortCode(entry.Rating);
            ws.Cell(row, 6).Value = entry.ObservationText;
            ws.Cell(row, 7).Value = request.IncludeEvaluatorNames ? (entry.Evaluator?.DisplayName ?? "") : "";
            ws.Cell(row, 8).Value = entry.TriggeringInject != null
                ? $"INJ-{entry.TriggeringInject.InjectNumber:D3}: {entry.TriggeringInject.Title}"
                : "";

            if (request.IncludeFormatting)
            {
                ws.Cell(row, 5).Style.Fill.BackgroundColor = EegFormattingHelper.RatingBackgroundColors[entry.Rating];

                if (row % 2 == 0)
                {
                    for (int c = 1; c <= headers.Length; c++)
                    {
                        if (c != 5) // Don't override rating color
                        {
                            ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.AliceBlue;
                        }
                    }
                }
            }

            row++;
        }

        // Format columns
        if (request.IncludeFormatting)
        {
            ws.Column(1).Width = 20;
            ws.Column(2).Width = 25;
            ws.Column(3).Width = 35;
            ws.Column(4).Width = 35;
            ws.Column(5).Width = 10;
            ws.Column(6).Width = 60;
            ws.Column(7).Width = 20;
            ws.Column(8).Width = 35;

            if (eegEntries.Count > 0)
            {
                ws.RangeUsed()?.SetAutoFilter();
            }
        }
    }
}
