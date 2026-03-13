using System.Globalization;
using System.Text;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;

namespace Cadence.Core.Features.ExcelExport.Builders;

/// <summary>
/// Builds the MSEL, Phases, and Objectives worksheets, and generates CSV output for MSEL data.
/// </summary>
internal static class MselWorksheetBuilder
{
    /// <summary>
    /// Adds a "MSEL" worksheet to the workbook populated with inject data.
    /// </summary>
    /// <param name="workbook">The workbook to add the worksheet to.</param>
    /// <param name="injects">The ordered list of injects to write.</param>
    /// <param name="includeFormatting">Whether to apply header styling and alternating row colors.</param>
    /// <param name="includeConductData">Whether to append conduct columns (Status, Fired At, Fired By).</param>
    internal static void AddMselWorksheet(
        XLWorkbook workbook,
        List<Inject> injects,
        bool includeFormatting,
        bool includeConductData)
    {
        var ws = workbook.Worksheets.Add("MSEL");

        // Determine columns to include
        var columns = includeConductData
            ? ExcelFormattingHelper.MselColumns.Concat(ExcelFormattingHelper.ConductColumns).ToArray()
            : ExcelFormattingHelper.MselColumns;

        ExcelFormattingHelper.ApplyHeaderRow(ws, columns, XLColor.LightBlue, includeFormatting);

        if (includeFormatting)
        {
            ws.Row(1).Style.Alignment.WrapText = true;
        }

        // Add data rows
        var row = 2;
        foreach (var inject in injects)
        {
            var col = 1;

            // Core fields
            ws.Cell(row, col++).Value = inject.InjectNumber;
            ws.Cell(row, col++).Value = inject.Title;
            ws.Cell(row, col++).Value = inject.Description;
            ws.Cell(row, col++).Value = inject.ScheduledTime.ToString("HH:mm", CultureInfo.InvariantCulture);
            ws.Cell(row, col++).Value = inject.ScenarioDay?.ToString(CultureInfo.InvariantCulture) ?? "";
            ws.Cell(row, col++).Value = inject.ScenarioTime?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "";
            ws.Cell(row, col++).Value = inject.Source ?? "";
            ws.Cell(row, col++).Value = inject.Target;
            ws.Cell(row, col++).Value = ExcelFormattingHelper.GetDeliveryMethodDisplay(inject);
            ws.Cell(row, col++).Value = inject.Track ?? "";
            ws.Cell(row, col++).Value = inject.Phase?.Name ?? "";
            ws.Cell(row, col++).Value = inject.ExpectedAction ?? "";
            ws.Cell(row, col++).Value = inject.ControllerNotes ?? "";
            ws.Cell(row, col++).Value = inject.Priority?.ToString(CultureInfo.InvariantCulture) ?? "";
            ws.Cell(row, col++).Value = inject.LocationName ?? "";
            ws.Cell(row, col++).Value = inject.ResponsibleController ?? "";

            // Conduct data
            if (includeConductData)
            {
                ws.Cell(row, col++).Value = inject.Status.ToString();
                ws.Cell(row, col++).Value = inject.FiredAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "";
                ws.Cell(row, col++).Value = inject.FiredByUser?.DisplayName ?? "";
            }

            // Alternating row colors
            if (includeFormatting && row % 2 == 0)
            {
                for (int c = 1; c <= columns.Length; c++)
                {
                    ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.AliceBlue;
                }
            }

            row++;
        }

        // Add auto-filter
        if (includeFormatting && injects.Count > 0)
        {
            ws.RangeUsed()?.SetAutoFilter();
        }
    }

    /// <summary>
    /// Adds a "Phases" worksheet to the workbook populated with exercise phase data.
    /// </summary>
    /// <param name="workbook">The workbook to add the worksheet to.</param>
    /// <param name="phases">The ordered list of phases to write.</param>
    /// <param name="includeFormatting">Whether to apply header styling.</param>
    internal static void AddPhasesWorksheet(
        XLWorkbook workbook,
        List<Phase> phases,
        bool includeFormatting)
    {
        var ws = workbook.Worksheets.Add("Phases");

        var headers = new[] { "Sequence", "Name", "Description", "Start Time", "End Time" };
        var widths = new[] { 10, 30, 50, 12, 12 };

        // Add header row
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];

            if (includeFormatting)
            {
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                ws.Column(i + 1).Width = widths[i];
            }
        }

        // Add data rows
        var row = 2;
        foreach (var phase in phases)
        {
            ws.Cell(row, 1).Value = phase.Sequence;
            ws.Cell(row, 2).Value = phase.Name;
            ws.Cell(row, 3).Value = phase.Description ?? "";
            ws.Cell(row, 4).Value = phase.StartTime?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "";
            ws.Cell(row, 5).Value = phase.EndTime?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "";
            row++;
        }

        if (includeFormatting && phases.Count > 0)
        {
            ws.RangeUsed()?.SetAutoFilter();
        }
    }

    /// <summary>
    /// Adds an "Objectives" worksheet to the workbook populated with exercise objective data.
    /// </summary>
    /// <param name="workbook">The workbook to add the worksheet to.</param>
    /// <param name="objectives">The ordered list of objectives to write.</param>
    /// <param name="includeFormatting">Whether to apply header styling.</param>
    internal static void AddObjectivesWorksheet(
        XLWorkbook workbook,
        List<Objective> objectives,
        bool includeFormatting)
    {
        var ws = workbook.Worksheets.Add("Objectives");

        var headers = new[] { "Objective #", "Name", "Description" };
        var widths = new[] { 12, 40, 60 };

        // Add header row
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];

            if (includeFormatting)
            {
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightCoral;
                ws.Column(i + 1).Width = widths[i];
            }
        }

        // Add data rows
        var row = 2;
        foreach (var objective in objectives)
        {
            ws.Cell(row, 1).Value = objective.ObjectiveNumber;
            ws.Cell(row, 2).Value = objective.Name;
            ws.Cell(row, 3).Value = objective.Description ?? "";
            row++;
        }

        if (includeFormatting && objectives.Count > 0)
        {
            ws.RangeUsed()?.SetAutoFilter();
        }
    }

    /// <summary>
    /// Generates a CSV string from a list of injects, using the standard MSEL column layout.
    /// </summary>
    /// <param name="injects">The ordered list of injects to serialise.</param>
    /// <param name="includeConductData">Whether to append conduct columns (Status, Fired At, Fired By).</param>
    /// <returns>A UTF-8 CSV string.</returns>
    internal static string GenerateCsv(List<Inject> injects, bool includeConductData)
    {
        var sb = new StringBuilder();

        // Determine columns
        var columns = includeConductData
            ? ExcelFormattingHelper.MselColumns.Concat(ExcelFormattingHelper.ConductColumns).ToArray()
            : ExcelFormattingHelper.MselColumns;

        // Header row
        sb.AppendLine(string.Join(",", columns.Select(c => ExcelFormattingHelper.EscapeCsvField(c.Header))));

        // Data rows
        foreach (var inject in injects)
        {
            var values = new List<string>
            {
                inject.InjectNumber.ToString(CultureInfo.InvariantCulture),
                inject.Title,
                inject.Description,
                inject.ScheduledTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                inject.ScenarioDay?.ToString(CultureInfo.InvariantCulture) ?? "",
                inject.ScenarioTime?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "",
                inject.Source ?? "",
                inject.Target,
                ExcelFormattingHelper.GetDeliveryMethodDisplay(inject),
                inject.Track ?? "",
                inject.Phase?.Name ?? "",
                inject.ExpectedAction ?? "",
                inject.ControllerNotes ?? "",
                inject.Priority?.ToString(CultureInfo.InvariantCulture) ?? "",
                inject.LocationName ?? "",
                inject.ResponsibleController ?? ""
            };

            if (includeConductData)
            {
                values.Add(inject.Status.ToString());
                values.Add(inject.FiredAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "");
                values.Add(inject.FiredByUser?.DisplayName ?? "");
            }

            sb.AppendLine(string.Join(",", values.Select(ExcelFormattingHelper.EscapeCsvField)));
        }

        return sb.ToString();
    }
}
