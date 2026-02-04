using System.IO.Compression;
using System.Text;
using System.Text.Json;
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

    // Column definitions for observations worksheet
    private static readonly (string Field, string Header, int Width)[] ObservationColumns =
    {
        ("ObservedAt", "Timestamp", 20),
        ("Observer", "Observer", 25),
        ("RelatedInject", "Related Inject", 30),
        ("Content", "Observation", 60),
        ("Rating", "Rating (P/S/M/U)", 18),
        ("Recommendation", "Recommendation", 50),
        ("Location", "Location", 20),
        ("RelatedObjective", "Related Objective", 30),
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

        // Add Instructions worksheet first (will appear as first tab)
        AddInstructionsWorksheet(workbook, includeFormatting);

        // Add MSEL worksheet
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

        // Add example row with proper data types
        // Columns: InjectNumber, Title, Description, ScheduledTime, ScenarioDay, ScenarioTime,
        //          Source, Target, DeliveryMethod, Track, Phase, ExpectedAction, ControllerNotes,
        //          Priority, LocationName, ResponsibleController
        ws.Cell(2, 1).Value = 1; // InjectNumber (number)
        ws.Cell(2, 2).Value = "Hurricane Warning Issued";
        ws.Cell(2, 3).Value = "National Weather Service issues hurricane warning for the region.";
        ws.Cell(2, 4).Value = "09:00";
        ws.Cell(2, 5).Value = 1; // ScenarioDay (number)
        ws.Cell(2, 6).Value = "08:00";
        ws.Cell(2, 7).Value = "National Weather Service";
        ws.Cell(2, 8).Value = "Emergency Operations Center";
        ws.Cell(2, 9).Value = "Phone";
        ws.Cell(2, 10).Value = "EOC";
        ws.Cell(2, 11).Value = "Initial Response";
        ws.Cell(2, 12).Value = "EOC acknowledges receipt and initiates activation procedures.";
        ws.Cell(2, 13).Value = "Verify EOC receives and acknowledges warning.";
        ws.Cell(2, 14).Value = 2; // Priority (number)
        ws.Cell(2, 15).Value = "County EOC";
        ws.Cell(2, 16).Value = "John Smith";

        if (includeFormatting)
        {
            ws.Row(2).Style.Fill.BackgroundColor = XLColor.LightYellow;
        }

        // Add Lookups worksheet with valid values
        AddLookupsWorksheet(workbook, includeFormatting);

        // Add data validation for Delivery Method column
        AddDataValidation(ws, allColumns);

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

    public async Task<ExportResult> ExportObservationsAsync(ExportObservationsRequest request)
    {
        // Get exercise
        var exercise = await _context.Exercises
            .FirstOrDefaultAsync(e => e.Id == request.ExerciseId);

        if (exercise == null)
        {
            throw new InvalidOperationException("Exercise not found.");
        }

        // Get observations with related data
        var observations = await _context.Observations
            .Include(o => o.CreatedByUser)
            .Include(o => o.Inject)
            .Include(o => o.Objective)
            .Where(o => o.ExerciseId == request.ExerciseId && !o.IsDeleted)
            .OrderBy(o => o.ObservedAt)
            .ToListAsync();

        // Generate filename
        var filename = request.Filename ?? GenerateObservationsFilename(exercise.Name);

        // Generate Excel file
        using var workbook = new XLWorkbook();
        AddObservationsWorksheet(workbook, observations, request.IncludeFormatting);

        // Convert to bytes
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();

        return new ExportResult
        {
            Content = bytes,
            Filename = $"{filename}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            InjectCount = 0,
            PhaseCount = 0,
            ObjectiveCount = observations.Count
        };
    }

    public async Task<ExportResult> ExportFullPackageAsync(ExportFullPackageRequest request)
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

        // Get injects
        var injects = activeMsel != null
            ? await _context.Injects
                .Include(i => i.Phase)
                .Include(i => i.DeliveryMethodLookup)
                .Include(i => i.FiredByUser)
                .Where(i => i.MselId == activeMsel.Id && !i.IsDeleted)
                .OrderBy(i => i.Sequence)
                .ToListAsync()
            : new List<Inject>();

        // Get observations
        var observations = await _context.Observations
            .Include(o => o.CreatedByUser)
            .Include(o => o.Inject)
            .Include(o => o.Objective)
            .Where(o => o.ExerciseId == request.ExerciseId && !o.IsDeleted)
            .OrderBy(o => o.ObservedAt)
            .ToListAsync();

        // Get phases
        var phases = await _context.Phases
            .Where(p => p.ExerciseId == request.ExerciseId && !p.IsDeleted)
            .OrderBy(p => p.Sequence)
            .ToListAsync();

        // Get objectives
        var objectives = await _context.Objectives
            .Where(o => o.ExerciseId == request.ExerciseId && !o.IsDeleted)
            .OrderBy(o => o.ObjectiveNumber)
            .ToListAsync();

        // Generate filename
        var safeName = GenerateSafeFilename(exercise.Name);
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var zipFilename = request.Filename ?? $"{safeName}_Package_{date}";

        // Create ZIP in memory
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            // Add MSEL.xlsx
            var mselEntry = archive.CreateEntry("MSEL.xlsx", CompressionLevel.Optimal);
            using (var mselEntryStream = mselEntry.Open())
            {
                using var mselWorkbook = new XLWorkbook();
                AddMselWorksheet(mselWorkbook, injects, request.IncludeFormatting, true);
                if (phases.Count > 0)
                {
                    AddPhasesWorksheet(mselWorkbook, phases, request.IncludeFormatting);
                }
                if (objectives.Count > 0)
                {
                    AddObjectivesWorksheet(mselWorkbook, objectives, request.IncludeFormatting);
                }
                mselWorkbook.SaveAs(mselEntryStream);
            }

            // Add Observations.xlsx
            var obsEntry = archive.CreateEntry("Observations.xlsx", CompressionLevel.Optimal);
            using (var obsEntryStream = obsEntry.Open())
            {
                using var obsWorkbook = new XLWorkbook();
                AddObservationsWorksheet(obsWorkbook, observations, request.IncludeFormatting);
                obsWorkbook.SaveAs(obsEntryStream);
            }

            // Add Summary.json
            var summaryEntry = archive.CreateEntry("Summary.json", CompressionLevel.Optimal);
            using (var summaryEntryStream = summaryEntry.Open())
            {
                var summary = new ExerciseSummaryDto
                {
                    Name = exercise.Name,
                    ExerciseType = exercise.ExerciseType.ToString(),
                    Description = exercise.Description,
                    ScheduledDate = exercise.ScheduledDate.ToString("yyyy-MM-dd"),
                    StartTime = exercise.StartTime?.ToString("HH:mm"),
                    EndTime = exercise.EndTime?.ToString("HH:mm"),
                    Status = exercise.Status.ToString(),
                    InjectCount = injects.Count,
                    InjectsFired = injects.Count(i => i.Status == InjectStatus.Released),
                    InjectsSkipped = injects.Count(i => i.Status == InjectStatus.Deferred),
                    InjectsPending = injects.Count(i => i.Status == InjectStatus.Draft),
                    ObservationCount = observations.Count,
                    PhaseCount = phases.Count,
                    ObjectiveCount = objectives.Count,
                    ExportedAt = DateTime.UtcNow
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(summary, options);
                using var writer = new StreamWriter(summaryEntryStream);
                await writer.WriteAsync(json);
            }
        }

        return new ExportResult
        {
            Content = zipStream.ToArray(),
            Filename = $"{zipFilename}.zip",
            ContentType = "application/zip",
            InjectCount = injects.Count,
            PhaseCount = phases.Count,
            ObjectiveCount = objectives.Count
        };
    }

    #region Private Methods

    private static string GenerateFilename(string exerciseName)
    {
        // Sanitize exercise name for filename
        var safeName = GenerateSafeFilename(exerciseName);
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return $"{safeName}_MSEL_{date}";
    }

    private static string GenerateObservationsFilename(string exerciseName)
    {
        var safeName = GenerateSafeFilename(exerciseName);
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return $"{safeName}_Observations_{date}";
    }

    private static string GenerateSafeFilename(string name)
    {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        return safeName.Replace(" ", "_");
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

    private void AddObservationsWorksheet(XLWorkbook workbook, List<Observation> observations, bool includeFormatting)
    {
        var ws = workbook.Worksheets.Add("Observations");

        // Add header row
        for (int i = 0; i < ObservationColumns.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = ObservationColumns[i].Header;

            if (includeFormatting)
            {
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightYellow;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Column(i + 1).Width = ObservationColumns[i].Width;
            }
        }

        // Add data rows
        var row = 2;
        foreach (var observation in observations)
        {
            var col = 1;

            // Timestamp
            ws.Cell(row, col++).Value = observation.ObservedAt.ToString("yyyy-MM-dd HH:mm:ss");

            // Observer
            ws.Cell(row, col++).Value = observation.CreatedByUser?.DisplayName ?? "";

            // Related Inject
            ws.Cell(row, col++).Value = observation.Inject != null
                ? $"#{observation.Inject.InjectNumber} - {observation.Inject.Title}"
                : "General";

            // Content
            ws.Cell(row, col++).Value = observation.Content;

            // Rating
            ws.Cell(row, col++).Value = GetRatingDisplay(observation.Rating);

            // Recommendation
            ws.Cell(row, col++).Value = observation.Recommendation ?? "";

            // Location
            ws.Cell(row, col++).Value = observation.Location ?? "";

            // Related Objective
            ws.Cell(row, col++).Value = observation.Objective?.Name ?? "";

            // Alternating row colors
            if (includeFormatting && row % 2 == 0)
            {
                for (int c = 1; c <= ObservationColumns.Length; c++)
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

    private static string GetRatingDisplay(ObservationRating? rating)
    {
        return rating switch
        {
            ObservationRating.Performed => "P - Performed",
            ObservationRating.Satisfactory => "S - Satisfactory",
            ObservationRating.Marginal => "M - Marginal",
            ObservationRating.Unsatisfactory => "U - Unsatisfactory",
            _ => ""
        };
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

    private void AddInstructionsWorksheet(XLWorkbook workbook, bool includeFormatting)
    {
        var ws = workbook.Worksheets.Add("Instructions");

        var instructions = new[]
        {
            ("Overview", "This template helps you create a Master Scenario Events List (MSEL) for import into Cadence."),
            ("", ""),
            ("How to Use", ""),
            ("", "1. Fill in the MSEL worksheet with your inject data"),
            ("", "2. Use the Lookups worksheet to see valid values for dropdown fields"),
            ("", "3. The example row (highlighted yellow) can be deleted or replaced"),
            ("", "4. Save as .xlsx and import into Cadence"),
            ("", ""),
            ("Required Fields", "The following fields are required for each inject:"),
            ("", "• Inject # - Unique number for the inject"),
            ("", "• Title - Short descriptive title"),
            ("", "• Description - Full inject content"),
            ("", "• Scheduled Time - When to deliver (HH:mm format)"),
            ("", "• To / Target - Who receives the inject"),
            ("", ""),
            ("Optional Fields", "The following fields are optional:"),
            ("", "• Scenario Day - Day number in multi-day exercises"),
            ("", "• Scenario Time - Time in the exercise scenario"),
            ("", "• From / Source - Who sends the inject"),
            ("", "• Delivery Method - How the inject is delivered (see Lookups)"),
            ("", "• Track - Functional area or team"),
            ("", "• Phase - Exercise phase name"),
            ("", "• Expected Action - What players should do"),
            ("", "• Notes - Controller notes"),
            ("", "• Priority - 1 (highest) to 5 (lowest)"),
            ("", "• Location - Where the inject occurs"),
            ("", "• Responsible Controller - Who fires this inject"),
            ("", ""),
            ("Tips", ""),
            ("", "• Use consistent time formats (HH:mm)"),
            ("", "• Delivery Method values must match the Lookups exactly"),
            ("", "• Leave cells blank for optional fields you don't need"),
            ("", "• Import will validate and report any errors"),
        };

        var row = 1;
        foreach (var (header, content) in instructions)
        {
            if (!string.IsNullOrEmpty(header))
            {
                var headerCell = ws.Cell(row, 1);
                headerCell.Value = header;
                if (includeFormatting)
                {
                    headerCell.Style.Font.Bold = true;
                    headerCell.Style.Font.FontSize = 12;
                }
            }

            if (!string.IsNullOrEmpty(content))
            {
                ws.Cell(row, string.IsNullOrEmpty(header) ? 1 : 2).Value = content;
            }

            row++;
        }

        if (includeFormatting)
        {
            ws.Column(1).Width = 20;
            ws.Column(2).Width = 80;
        }
    }

    private void AddLookupsWorksheet(XLWorkbook workbook, bool includeFormatting)
    {
        var ws = workbook.Worksheets.Add("Lookups");

        // Delivery Methods - populate the actual values for display and named range
        var deliveryMethods = new[] { "Verbal", "Phone", "Email", "Radio", "Written", "Simulation", "Other" };
        ws.Cell(1, 1).Value = "Delivery Methods";
        if (includeFormatting)
        {
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }
        for (int i = 0; i < deliveryMethods.Length; i++)
        {
            ws.Cell(i + 2, 1).Value = deliveryMethods[i];
        }

        // Priority values (as numbers to avoid "Number stored as text" warning)
        var priorities = new[] { 1, 2, 3, 4, 5 };
        ws.Cell(1, 2).Value = "Priority";
        if (includeFormatting)
        {
            ws.Cell(1, 2).Style.Font.Bold = true;
            ws.Cell(1, 2).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }
        for (int i = 0; i < priorities.Length; i++)
        {
            ws.Cell(i + 2, 2).Value = priorities[i];
        }

        // Inject Types (for reference)
        var injectTypes = new[] { "Standard", "Contingency", "Adaptive", "Complexity" };
        ws.Cell(1, 3).Value = "Inject Types";
        if (includeFormatting)
        {
            ws.Cell(1, 3).Style.Font.Bold = true;
            ws.Cell(1, 3).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }
        for (int i = 0; i < injectTypes.Length; i++)
        {
            ws.Cell(i + 2, 3).Value = injectTypes[i];
        }

        // Inject Statuses (for reference, used in conduct exports)
        var injectStatuses = new[] { "Draft", "Synchronized", "Released", "Deferred" };
        ws.Cell(1, 4).Value = "Inject Status";
        if (includeFormatting)
        {
            ws.Cell(1, 4).Style.Font.Bold = true;
            ws.Cell(1, 4).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }
        for (int i = 0; i < injectStatuses.Length; i++)
        {
            ws.Cell(i + 2, 4).Value = injectStatuses[i];
        }

        if (includeFormatting)
        {
            ws.Column(1).Width = 18;
            ws.Column(2).Width = 10;
            ws.Column(3).Width = 15;
            ws.Column(4).Width = 13;
        }

        // Define named ranges for data validation
        // Use the actual data rows only - Excel data validation with list references
        // correctly validates user input against these values regardless of which row
        // the user is editing in the MSEL worksheet
        var deliveryRange = ws.Range(2, 1, deliveryMethods.Length + 1, 1);
        deliveryRange.AddToNamed("DeliveryMethods", XLScope.Workbook);

        var priorityRange = ws.Range(2, 2, priorities.Length + 1, 2);
        priorityRange.AddToNamed("Priorities", XLScope.Workbook);
    }

    private void AddDataValidation(IXLWorksheet ws, (string Field, string Header, int Width)[] columns)
    {
        // Find the Delivery Method column index
        var deliveryMethodIndex = -1;
        var priorityIndex = -1;

        for (int i = 0; i < columns.Length; i++)
        {
            if (columns[i].Field == "DeliveryMethod")
            {
                deliveryMethodIndex = i + 1; // 1-based
            }
            else if (columns[i].Field == "Priority")
            {
                priorityIndex = i + 1; // 1-based
            }
        }

        // Apply validation to Delivery Method column (rows 2-1000)
        if (deliveryMethodIndex > 0)
        {
            var deliveryValidation = ws.Range(2, deliveryMethodIndex, 1000, deliveryMethodIndex)
                .CreateDataValidation();
            deliveryValidation.List("=DeliveryMethods", true);
            deliveryValidation.IgnoreBlanks = true;
            deliveryValidation.ShowErrorMessage = true;
            deliveryValidation.ErrorTitle = "Invalid Delivery Method";
            deliveryValidation.ErrorMessage = "Please select a delivery method from the dropdown list.";
        }

        // Apply validation to Priority column (rows 2-1000)
        if (priorityIndex > 0)
        {
            var priorityValidation = ws.Range(2, priorityIndex, 1000, priorityIndex)
                .CreateDataValidation();
            priorityValidation.List("=Priorities", true);
            priorityValidation.IgnoreBlanks = true;
            priorityValidation.ShowErrorMessage = true;
            priorityValidation.ErrorTitle = "Invalid Priority";
            priorityValidation.ErrorMessage = "Please select a priority value (1-5) from the dropdown list.";
        }
    }

    #endregion
}
