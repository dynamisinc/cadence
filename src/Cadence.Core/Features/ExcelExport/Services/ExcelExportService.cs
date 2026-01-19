using System.Text;
using Cadence.Core.Data;
using Cadence.Core.Features.ExcelExport.Models.DTOs;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.ExcelExport.Services;

/// <summary>
/// Service for exporting MSEL data to Excel format.
/// </summary>
public class ExcelExportService : IExcelExportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExcelExportService> _logger;

    // Column definitions for the MSEL worksheet
    private static readonly (string Field, string Header, int Width)[] MselColumns =
    {
        ("InjectNumber", "Inject #", 10),
        ("Title", "Title", 40),
        ("Description", "Description", 60),
        ("ScheduledTime", "Scheduled Time", 15),
        ("ScenarioDay", "Scenario Day", 12),
        ("ScenarioTime", "Scenario Time", 15),
        ("Source", "From / Source", 20),
        ("Target", "To / Target", 20),
        ("DeliveryMethod", "Delivery Method", 18),
        ("Track", "Track", 15),
        ("Phase", "Phase", 20),
        ("ExpectedAction", "Expected Action", 50),
        ("ControllerNotes", "Notes", 40),
        ("Priority", "Priority", 10),
        ("LocationName", "Location", 20),
        ("ResponsibleController", "Responsible Controller", 20),
    };

    // Additional columns for conduct data
    private static readonly (string Field, string Header, int Width)[] ConductColumns =
    {
        ("Status", "Status", 12),
        ("FiredAt", "Fired At", 20),
        ("FiredBy", "Fired By", 20),
    };

    public ExcelExportService(
        AppDbContext context,
        ILogger<ExcelExportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ExportResult> ExportMselAsync(ExportMselRequest request)
    {
        // Get exercise with related data
        var exercise = await _context.Exercises
            .Include(e => e.Msels)
            .FirstOrDefaultAsync(e => e.Id == request.ExerciseId);

        if (exercise == null)
        {
            throw new InvalidOperationException("Exercise not found.");
        }

        var activeMsel = exercise.Msels.FirstOrDefault(m => m.IsActive);
        if (activeMsel == null)
        {
            throw new InvalidOperationException("No active MSEL found for this exercise.");
        }

        // Get injects
        var injects = await _context.Injects
            .Include(i => i.Phase)
            .Include(i => i.DeliveryMethodLookup)
            .Include(i => i.FiredByUser)
            .Where(i => i.MselId == activeMsel.Id && !i.IsDeleted)
            .OrderBy(i => i.Sequence)
            .ToListAsync();

        // Get phases
        var phases = request.IncludePhases
            ? await _context.Phases
                .Where(p => p.ExerciseId == request.ExerciseId && !p.IsDeleted)
                .OrderBy(p => p.Sequence)
                .ToListAsync()
            : new List<Phase>();

        // Get objectives
        var objectives = request.IncludeObjectives
            ? await _context.Objectives
                .Where(o => o.ExerciseId == request.ExerciseId && !o.IsDeleted)
                .OrderBy(o => o.ObjectiveNumber)
                .ToListAsync()
            : new List<Objective>();

        // Generate filename
        var filename = request.Filename ?? GenerateFilename(exercise.Name);

        if (request.Format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            var csvContent = GenerateCsv(injects, request.IncludeConductData);
            return new ExportResult
            {
                Content = Encoding.UTF8.GetBytes(csvContent),
                Filename = $"{filename}.csv",
                ContentType = "text/csv",
                InjectCount = injects.Count,
                PhaseCount = phases.Count,
                ObjectiveCount = objectives.Count
            };
        }

        // Generate Excel file
        using var workbook = new XLWorkbook();

        // Add MSEL worksheet
        AddMselWorksheet(workbook, injects, request.IncludeFormatting, request.IncludeConductData);

        // Add Phases worksheet
        if (request.IncludePhases && phases.Count > 0)
        {
            AddPhasesWorksheet(workbook, phases, request.IncludeFormatting);
        }

        // Add Objectives worksheet
        if (request.IncludeObjectives && objectives.Count > 0)
        {
            AddObjectivesWorksheet(workbook, objectives, request.IncludeFormatting);
        }

        // Convert to bytes
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();

        return new ExportResult
        {
            Content = bytes,
            Filename = $"{filename}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            InjectCount = injects.Count,
            PhaseCount = phases.Count,
            ObjectiveCount = objectives.Count
        };
    }

    public Task<ExportResult> GenerateTemplateAsync(bool includeFormatting = true)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("MSEL");

        // Add header row
        var allColumns = MselColumns;
        for (int i = 0; i < allColumns.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = allColumns[i].Header;

            if (includeFormatting)
            {
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Column(i + 1).Width = allColumns[i].Width;
            }
        }

        // Add auto-filter
        if (includeFormatting)
        {
            ws.RangeUsed()?.SetAutoFilter();
            ws.Row(1).Style.Alignment.WrapText = true;
        }

        // Add example row
        var exampleData = new[]
        {
            "1",
            "Hurricane Warning Issued",
            "National Weather Service issues hurricane warning for the region.",
            "09:00",
            "1",
            "08:00",
            "National Weather Service",
            "Emergency Operations Center",
            "Phone",
            "EOC",
            "Initial Response",
            "EOC acknowledges receipt and initiates activation procedures.",
            "Verify EOC receives and acknowledges warning.",
            "2",
            "County EOC",
            "John Smith"
        };

        for (int i = 0; i < exampleData.Length && i < allColumns.Length; i++)
        {
            ws.Cell(2, i + 1).Value = exampleData[i];
        }

        if (includeFormatting)
        {
            ws.Row(2).Style.Fill.BackgroundColor = XLColor.LightYellow;
        }

        // Convert to bytes
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();

        return Task.FromResult(new ExportResult
        {
            Content = bytes,
            Filename = "Cadence_MSEL_Template.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            InjectCount = 0,
            PhaseCount = 0,
            ObjectiveCount = 0
        });
    }

    #region Private Methods

    private static string GenerateFilename(string exerciseName)
    {
        // Sanitize exercise name for filename
        var safeName = string.Join("_", exerciseName.Split(Path.GetInvalidFileNameChars()));
        safeName = safeName.Replace(" ", "_");
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return $"{safeName}_MSEL_{date}";
    }

    private void AddMselWorksheet(XLWorkbook workbook, List<Inject> injects, bool includeFormatting, bool includeConductData)
    {
        var ws = workbook.Worksheets.Add("MSEL");

        // Determine columns to include
        var columns = includeConductData
            ? MselColumns.Concat(ConductColumns).ToArray()
            : MselColumns;

        // Add header row
        for (int i = 0; i < columns.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = columns[i].Header;

            if (includeFormatting)
            {
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Column(i + 1).Width = columns[i].Width;
            }
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
            ws.Cell(row, col++).Value = inject.ScheduledTime.ToString("HH:mm");
            ws.Cell(row, col++).Value = inject.ScenarioDay?.ToString() ?? "";
            ws.Cell(row, col++).Value = inject.ScenarioTime?.ToString("HH:mm") ?? "";
            ws.Cell(row, col++).Value = inject.Source ?? "";
            ws.Cell(row, col++).Value = inject.Target;
            ws.Cell(row, col++).Value = GetDeliveryMethodDisplay(inject);
            ws.Cell(row, col++).Value = inject.Track ?? "";
            ws.Cell(row, col++).Value = inject.Phase?.Name ?? "";
            ws.Cell(row, col++).Value = inject.ExpectedAction ?? "";
            ws.Cell(row, col++).Value = inject.ControllerNotes ?? "";
            ws.Cell(row, col++).Value = inject.Priority?.ToString() ?? "";
            ws.Cell(row, col++).Value = inject.LocationName ?? "";
            ws.Cell(row, col++).Value = inject.ResponsibleController ?? "";

            // Conduct data
            if (includeConductData)
            {
                ws.Cell(row, col++).Value = inject.Status.ToString();
                ws.Cell(row, col++).Value = inject.FiredAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
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

    private static string GetDeliveryMethodDisplay(Inject inject)
    {
        if (inject.DeliveryMethodLookup != null)
        {
            if (inject.DeliveryMethodLookup.IsOther && !string.IsNullOrEmpty(inject.DeliveryMethodOther))
            {
                return inject.DeliveryMethodOther;
            }
            return inject.DeliveryMethodLookup.Name;
        }
        return inject.DeliveryMethod?.ToString() ?? "";
    }

    private void AddPhasesWorksheet(XLWorkbook workbook, List<Phase> phases, bool includeFormatting)
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
            ws.Cell(row, 4).Value = phase.StartTime?.ToString("HH:mm") ?? "";
            ws.Cell(row, 5).Value = phase.EndTime?.ToString("HH:mm") ?? "";
            row++;
        }

        if (includeFormatting && phases.Count > 0)
        {
            ws.RangeUsed()?.SetAutoFilter();
        }
    }

    private void AddObjectivesWorksheet(XLWorkbook workbook, List<Objective> objectives, bool includeFormatting)
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

    private string GenerateCsv(List<Inject> injects, bool includeConductData)
    {
        var sb = new StringBuilder();

        // Determine columns
        var columns = includeConductData
            ? MselColumns.Concat(ConductColumns).ToArray()
            : MselColumns;

        // Header row
        sb.AppendLine(string.Join(",", columns.Select(c => EscapeCsvField(c.Header))));

        // Data rows
        foreach (var inject in injects)
        {
            var values = new List<string>
            {
                inject.InjectNumber.ToString(),
                inject.Title,
                inject.Description,
                inject.ScheduledTime.ToString("HH:mm"),
                inject.ScenarioDay?.ToString() ?? "",
                inject.ScenarioTime?.ToString("HH:mm") ?? "",
                inject.Source ?? "",
                inject.Target,
                GetDeliveryMethodDisplay(inject),
                inject.Track ?? "",
                inject.Phase?.Name ?? "",
                inject.ExpectedAction ?? "",
                inject.ControllerNotes ?? "",
                inject.Priority?.ToString() ?? "",
                inject.LocationName ?? "",
                inject.ResponsibleController ?? ""
            };

            if (includeConductData)
            {
                values.Add(inject.Status.ToString());
                values.Add(inject.FiredAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "");
                values.Add(inject.FiredByUser?.DisplayName ?? "");
            }

            sb.AppendLine(string.Join(",", values.Select(EscapeCsvField)));
        }

        return sb.ToString();
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "\"\"";

        // If field contains comma, quote, or newline, wrap in quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            // Escape quotes by doubling them
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    #endregion
}
