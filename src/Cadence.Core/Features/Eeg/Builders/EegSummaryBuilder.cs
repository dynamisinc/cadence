using System.Globalization;
using Cadence.Core.Features.Eeg.Services;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;

namespace Cadence.Core.Features.Eeg.Builders;

/// <summary>
/// Builds the "Summary" worksheet for an EEG Excel export.
/// Renders exercise metadata, coverage metrics, rating distribution,
/// and optional evaluator activity sections.
/// </summary>
internal static class EegSummaryBuilder
{
    /// <summary>
    /// Adds the "Summary" worksheet to the provided workbook.
    /// </summary>
    /// <param name="workbook">Target workbook to add the worksheet to.</param>
    /// <param name="exercise">Exercise being exported.</param>
    /// <param name="capabilityTargets">All capability targets for the exercise.</param>
    /// <param name="eegEntries">All EEG entries for the exercise.</param>
    /// <param name="request">Export options (formatting, evaluator name visibility).</param>
    internal static void AddSummaryWorksheet(
        XLWorkbook workbook,
        Exercise exercise,
        List<CapabilityTarget> capabilityTargets,
        List<EegEntry> eegEntries,
        ExportEegRequest request)
    {
        var ws = workbook.Worksheets.Add("Summary");

        // Calculate metrics
        var allTasks = capabilityTargets
            .SelectMany(ct => ct.CriticalTasks.Where(t => !t.IsDeleted))
            .ToList();

        var evaluatedTaskIds = eegEntries.Select(e => e.CriticalTaskId).Distinct().ToHashSet();
        var totalTasks = allTasks.Count;
        var evaluatedTasks = allTasks.Count(t => evaluatedTaskIds.Contains(t.Id));
        var coveragePercentage = totalTasks > 0 ? (int)Math.Round((decimal)evaluatedTasks / totalTasks * 100) : 0;

        var ratingCounts = eegEntries
            .GroupBy(e => e.Rating)
            .ToDictionary(g => g.Key, g => g.Count());

        var row = 1;

        // Exercise Summary section
        ws.Cell(row, 1).Value = "EXERCISE SUMMARY";
        if (request.IncludeFormatting)
        {
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 14;
        }
        row += 2;

        ws.Cell(row, 1).Value = "Exercise Name";
        ws.Cell(row, 2).Value = exercise.Name;
        row++;

        ws.Cell(row, 1).Value = "Exercise Date";
        ws.Cell(row, 2).Value = exercise.ScheduledDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        row++;

        ws.Cell(row, 1).Value = "Status";
        ws.Cell(row, 2).Value = exercise.Status.ToString();
        row++;

        ws.Cell(row, 1).Value = "Generated";
        ws.Cell(row, 2).Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";
        row += 2;

        // Coverage Metrics section
        ws.Cell(row, 1).Value = "COVERAGE METRICS";
        if (request.IncludeFormatting)
        {
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 14;
        }
        row += 2;

        ws.Cell(row, 1).Value = "Total Entries";
        ws.Cell(row, 2).Value = eegEntries.Count;
        row++;

        ws.Cell(row, 1).Value = "Tasks Evaluated";
        ws.Cell(row, 2).Value = $"{evaluatedTasks} of {totalTasks} ({coveragePercentage}%)";
        row += 2;

        // Rating Distribution section
        ws.Cell(row, 1).Value = "RATING DISTRIBUTION";
        if (request.IncludeFormatting)
        {
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 14;
        }
        row += 2;

        ws.Cell(row, 1).Value = "Rating";
        ws.Cell(row, 2).Value = "Count";
        ws.Cell(row, 3).Value = "Percentage";
        if (request.IncludeFormatting)
        {
            ws.Range(row, 1, row, 3).Style.Font.Bold = true;
            ws.Range(row, 1, row, 3).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        row++;

        var totalEntries = eegEntries.Count > 0 ? eegEntries.Count : 1;
        foreach (var rating in new[] { PerformanceRating.Performed, PerformanceRating.SomeChallenges, PerformanceRating.MajorChallenges, PerformanceRating.UnableToPerform })
        {
            var count = ratingCounts.GetValueOrDefault(rating, 0);
            var percentage = (int)Math.Round((decimal)count / totalEntries * 100);

            ws.Cell(row, 1).Value = EegFormattingHelper.GetRatingDisplay(rating);
            ws.Cell(row, 2).Value = count;
            ws.Cell(row, 3).Value = $"{percentage}%";

            if (request.IncludeFormatting)
            {
                ws.Cell(row, 1).Style.Fill.BackgroundColor = EegFormattingHelper.RatingBackgroundColors[rating];
            }
            row++;
        }

        // Evaluator Activity section (if including names)
        if (request.IncludeEvaluatorNames && eegEntries.Count > 0)
        {
            row += 2;
            ws.Cell(row, 1).Value = "EVALUATOR ACTIVITY";
            if (request.IncludeFormatting)
            {
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 14;
            }
            row += 2;

            ws.Cell(row, 1).Value = "Evaluator";
            ws.Cell(row, 2).Value = "Entries";
            if (request.IncludeFormatting)
            {
                ws.Range(row, 1, row, 2).Style.Font.Bold = true;
                ws.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.LightGray;
            }
            row++;

            var evaluatorCounts = eegEntries
                .GroupBy(e => e.Evaluator?.DisplayName ?? "Unknown")
                .OrderByDescending(g => g.Count())
                .ToList();

            foreach (var evaluator in evaluatorCounts)
            {
                ws.Cell(row, 1).Value = evaluator.Key;
                ws.Cell(row, 2).Value = evaluator.Count();
                row++;
            }
        }

        // Format columns
        if (request.IncludeFormatting)
        {
            ws.Column(1).Width = 25;
            ws.Column(2).Width = 40;
            ws.Column(3).Width = 15;
        }
    }
}
