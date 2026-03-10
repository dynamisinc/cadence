using System.Globalization;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;

namespace Cadence.Core.Features.ExcelExport.Builders;

/// <summary>
/// Builds the Observations worksheet for exercise after-action review exports.
/// </summary>
internal static class ObservationsWorksheetBuilder
{
    /// <summary>
    /// Adds an "Observations" worksheet to the workbook populated with evaluator observation data.
    /// </summary>
    /// <param name="workbook">The workbook to add the worksheet to.</param>
    /// <param name="observations">The ordered list of observations to write.</param>
    /// <param name="includeFormatting">Whether to apply header styling and alternating row colors.</param>
    internal static void AddObservationsWorksheet(
        XLWorkbook workbook,
        List<Observation> observations,
        bool includeFormatting)
    {
        var ws = workbook.Worksheets.Add("Observations");

        ExcelFormattingHelper.ApplyHeaderRow(
            ws,
            ExcelFormattingHelper.ObservationColumns,
            XLColor.LightYellow,
            includeFormatting);

        // Add data rows
        var row = 2;
        foreach (var observation in observations)
        {
            var col = 1;

            // Timestamp
            ws.Cell(row, col++).Value = observation.ObservedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            // Observer
            ws.Cell(row, col++).Value = observation.CreatedByUser?.DisplayName ?? "";

            // Related Inject
            ws.Cell(row, col++).Value = observation.Inject != null
                ? $"#{observation.Inject.InjectNumber} - {observation.Inject.Title}"
                : "General";

            // Content
            ws.Cell(row, col++).Value = observation.Content;

            // Rating
            ws.Cell(row, col++).Value = ExcelFormattingHelper.GetRatingDisplay(observation.Rating);

            // Recommendation
            ws.Cell(row, col++).Value = observation.Recommendation ?? "";

            // Location
            ws.Cell(row, col++).Value = observation.Location ?? "";

            // Related Objective
            ws.Cell(row, col++).Value = observation.Objective?.Name ?? "";

            // Alternating row colors
            if (includeFormatting && row % 2 == 0)
            {
                for (int c = 1; c <= ExcelFormattingHelper.ObservationColumns.Length; c++)
                {
                    ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.LightGoldenrodYellow;
                }
            }

            row++;
        }

        // Add auto-filter
        if (includeFormatting && observations.Count > 0)
        {
            ws.RangeUsed()?.SetAutoFilter();
        }
    }
}
