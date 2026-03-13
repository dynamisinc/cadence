using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Cadence.Core.Data;
using Cadence.Core.Features.ExcelExport.Builders;
using Cadence.Core.Features.ExcelExport.Models.DTOs;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.ExcelExport.Services;

/// <summary>
/// Orchestrates MSEL and exercise data exports to Excel, CSV, and ZIP package formats.
/// Worksheet construction is delegated to the builder classes in
/// <c>Cadence.Core.Features.ExcelExport.Builders</c>.
/// </summary>
public class ExcelExportService : IExcelExportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExcelExportService> _logger;

    // Cached serializer options for JSON export summary
    private static readonly JsonSerializerOptions JsonSummaryOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initialises a new instance of <see cref="ExcelExportService"/>.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    /// <param name="logger">The logger.</param>
    public ExcelExportService(
        AppDbContext context,
        ILogger<ExcelExportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
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
        var filename = request.Filename ?? ExcelFormattingHelper.GenerateMselFilename(exercise.Name);

        if (request.Format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            var csvContent = MselWorksheetBuilder.GenerateCsv(injects, request.IncludeConductData);
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

        MselWorksheetBuilder.AddMselWorksheet(workbook, injects, request.IncludeFormatting, request.IncludeConductData);

        if (request.IncludePhases && phases.Count > 0)
        {
            MselWorksheetBuilder.AddPhasesWorksheet(workbook, phases, request.IncludeFormatting);
        }

        if (request.IncludeObjectives && objectives.Count > 0)
        {
            MselWorksheetBuilder.AddObjectivesWorksheet(workbook, objectives, request.IncludeFormatting);
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

    /// <inheritdoc/>
    public Task<ExportResult> GenerateTemplateAsync(bool includeFormatting = true)
    {
        using var workbook = new XLWorkbook();

        // Instructions worksheet appears as the first tab
        ExcelTemplateBuilder.AddInstructionsWorksheet(workbook, includeFormatting);

        // Add MSEL worksheet with headers and one example row
        var ws = workbook.Worksheets.Add("MSEL");
        var allColumns = ExcelFormattingHelper.MselColumns;

        ExcelFormattingHelper.ApplyHeaderRow(ws, allColumns, XLColor.LightBlue, includeFormatting);

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

        // Add Lookups worksheet with valid values and named ranges
        ExcelTemplateBuilder.AddLookupsWorksheet(workbook, includeFormatting);

        // Add data validation for Delivery Method and Priority columns
        ExcelTemplateBuilder.AddDataValidation(ws, allColumns);

        // Convert to bytes
        using var templateStream = new MemoryStream();
        workbook.SaveAs(templateStream);
        var bytes = templateStream.ToArray();

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

    /// <inheritdoc/>
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
        var filename = request.Filename ?? ExcelFormattingHelper.GenerateObservationsFilename(exercise.Name);

        // Generate Excel file
        using var workbook = new XLWorkbook();
        ObservationsWorksheetBuilder.AddObservationsWorksheet(workbook, observations, request.IncludeFormatting);

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

    /// <inheritdoc/>
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
        var safeName = ExcelFormattingHelper.GenerateSafeFilename(exercise.Name);
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
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
                MselWorksheetBuilder.AddMselWorksheet(mselWorkbook, injects, request.IncludeFormatting, true);
                if (phases.Count > 0)
                {
                    MselWorksheetBuilder.AddPhasesWorksheet(mselWorkbook, phases, request.IncludeFormatting);
                }
                if (objectives.Count > 0)
                {
                    MselWorksheetBuilder.AddObjectivesWorksheet(mselWorkbook, objectives, request.IncludeFormatting);
                }
                mselWorkbook.SaveAs(mselEntryStream);
            }

            // Add Observations.xlsx
            var obsEntry = archive.CreateEntry("Observations.xlsx", CompressionLevel.Optimal);
            using (var obsEntryStream = obsEntry.Open())
            {
                using var obsWorkbook = new XLWorkbook();
                ObservationsWorksheetBuilder.AddObservationsWorksheet(obsWorkbook, observations, request.IncludeFormatting);
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
                    ScheduledDate = exercise.ScheduledDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    StartTime = exercise.StartTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                    EndTime = exercise.EndTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
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

                var json = JsonSerializer.Serialize(summary, JsonSummaryOptions);
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
}
