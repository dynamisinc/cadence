using Cadence.Core.Data;
using Cadence.Core.Features.ExcelExport.Models.DTOs;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Eeg.Services;

/// <summary>
/// Service for exporting EEG data for After-Action Review (AAR).
/// </summary>
public class EegExportService : IEegExportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EegExportService> _logger;

    // Rating colors for Excel formatting
    private static readonly Dictionary<PerformanceRating, XLColor> RatingColors = new()
    {
        { PerformanceRating.Performed, XLColor.FromHtml("#4caf50") },           // Green
        { PerformanceRating.SomeChallenges, XLColor.FromHtml("#ff9800") },      // Orange
        { PerformanceRating.MajorChallenges, XLColor.FromHtml("#f44336") },     // Red
        { PerformanceRating.UnableToPerform, XLColor.FromHtml("#9e9e9e") },     // Grey
    };

    private static readonly Dictionary<PerformanceRating, XLColor> RatingBackgroundColors = new()
    {
        { PerformanceRating.Performed, XLColor.FromHtml("#e8f5e9") },
        { PerformanceRating.SomeChallenges, XLColor.FromHtml("#fff3e0") },
        { PerformanceRating.MajorChallenges, XLColor.FromHtml("#ffebee") },
        { PerformanceRating.UnableToPerform, XLColor.FromHtml("#fafafa") },
    };

    public EegExportService(
        AppDbContext context,
        ILogger<EegExportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ExportResult> ExportEegDataAsync(ExportEegRequest request)
    {
        // Get exercise
        var exercise = await _context.Exercises
            .FirstOrDefaultAsync(e => e.Id == request.ExerciseId);

        if (exercise == null)
        {
            throw new InvalidOperationException("Exercise not found.");
        }

        // Get capability targets with critical tasks
        var capabilityTargets = await _context.Set<CapabilityTarget>()
            .Include(ct => ct.Capability)
            .Include(ct => ct.CriticalTasks)
            .Where(ct => ct.ExerciseId == request.ExerciseId && !ct.IsDeleted)
            .OrderBy(ct => ct.SortOrder)
            .ToListAsync();

        // Get all EEG entries for the exercise
        var criticalTaskIds = capabilityTargets
            .SelectMany(ct => ct.CriticalTasks.Where(t => !t.IsDeleted))
            .Select(t => t.Id)
            .ToList();

        var eegEntries = await _context.Set<EegEntry>()
            .Include(e => e.Evaluator)
            .Include(e => e.TriggeringInject)
            .Include(e => e.CriticalTask)
            .Where(e => criticalTaskIds.Contains(e.CriticalTaskId) && !e.IsDeleted)
            .OrderBy(e => e.RecordedAt)
            .ToListAsync();

        // Generate filename
        var safeName = GenerateSafeFilename(exercise.Name);
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var filename = request.Filename ?? $"EEG_Export_{safeName}_{date}";

        // Generate Excel file
        using var workbook = new XLWorkbook();

        if (request.IncludeSummary)
        {
            AddSummaryWorksheet(workbook, exercise, capabilityTargets, eegEntries, request);
        }

        if (request.IncludeByCapability)
        {
            AddByCapabilityWorksheet(workbook, capabilityTargets, eegEntries, request);
        }

        if (request.IncludeAllEntries)
        {
            AddAllEntriesWorksheet(workbook, capabilityTargets, eegEntries, request);
        }

        if (request.IncludeCoverageGaps)
        {
            AddCoverageGapsWorksheet(workbook, capabilityTargets, eegEntries, request);
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
            InjectCount = 0,
            PhaseCount = 0,
            ObjectiveCount = eegEntries.Count
        };
    }

    public async Task<EegExportJsonDto> ExportEegJsonAsync(Guid exerciseId, bool includeEvaluatorNames = true)
    {
        // Get exercise
        var exercise = await _context.Exercises
            .FirstOrDefaultAsync(e => e.Id == exerciseId);

        if (exercise == null)
        {
            throw new InvalidOperationException("Exercise not found.");
        }

        // Get capability targets with critical tasks
        var capabilityTargets = await _context.Set<CapabilityTarget>()
            .Include(ct => ct.Capability)
            .Include(ct => ct.CriticalTasks)
            .Where(ct => ct.ExerciseId == exerciseId && !ct.IsDeleted)
            .OrderBy(ct => ct.SortOrder)
            .ToListAsync();

        // Get all EEG entries
        var criticalTaskIds = capabilityTargets
            .SelectMany(ct => ct.CriticalTasks.Where(t => !t.IsDeleted))
            .Select(t => t.Id)
            .ToList();

        var eegEntries = await _context.Set<EegEntry>()
            .Include(e => e.Evaluator)
            .Include(e => e.CriticalTask)
            .Where(e => criticalTaskIds.Contains(e.CriticalTaskId) && !e.IsDeleted)
            .OrderBy(e => e.RecordedAt)
            .ToListAsync();

        // Calculate coverage
        var allTasks = capabilityTargets
            .SelectMany(ct => ct.CriticalTasks.Where(t => !t.IsDeleted))
            .ToList();

        var evaluatedTaskIds = eegEntries.Select(e => e.CriticalTaskId).Distinct().ToHashSet();
        var totalTasks = allTasks.Count;
        var evaluatedTasks = allTasks.Count(t => evaluatedTaskIds.Contains(t.Id));
        var coveragePercentage = totalTasks > 0 ? (int)Math.Round((decimal)evaluatedTasks / totalTasks * 100) : 0;

        // Rating distribution
        var ratingCounts = eegEntries
            .GroupBy(e => e.Rating)
            .ToDictionary(g => g.Key, g => g.Count());

        // Build JSON export
        return new EegExportJsonDto
        {
            Exercise = new ExerciseInfoDto(
                exercise.Name,
                exercise.ScheduledDate.ToString("yyyy-MM-dd"),
                exercise.Status.ToString()
            ),
            Summary = new EegSummaryDto(
                eegEntries.Count,
                new TaskCoverageDto(evaluatedTasks, totalTasks, coveragePercentage),
                new RatingDistributionDto(
                    ratingCounts.GetValueOrDefault(PerformanceRating.Performed, 0),
                    ratingCounts.GetValueOrDefault(PerformanceRating.SomeChallenges, 0),
                    ratingCounts.GetValueOrDefault(PerformanceRating.MajorChallenges, 0),
                    ratingCounts.GetValueOrDefault(PerformanceRating.UnableToPerform, 0)
                )
            ),
            ByCapability = capabilityTargets.Select(ct => new CapabilityExportDto
            {
                CapabilityName = ct.Capability.Name,
                TargetDescription = ct.TargetDescription,
                Tasks = ct.CriticalTasks.Where(t => !t.IsDeleted).Select(task => new TaskExportDto
                {
                    TaskDescription = task.TaskDescription,
                    Entries = eegEntries
                        .Where(e => e.CriticalTaskId == task.Id)
                        .Select(e => new EntryExportDto(
                            GetRatingShortCode(e.Rating),
                            e.ObservationText,
                            includeEvaluatorNames ? e.Evaluator?.DisplayName : null,
                            e.ObservedAt
                        ))
                })
            }),
            CoverageGaps = allTasks
                .Where(t => !evaluatedTaskIds.Contains(t.Id))
                .Select(t =>
                {
                    var ct = capabilityTargets.First(c => c.Id == t.CapabilityTargetId);
                    return new CoverageGapDto(
                        ct.Capability.Name,
                        ct.TargetDescription,
                        t.TaskDescription
                    );
                }),
            GeneratedAt = DateTime.UtcNow
        };
    }

    #region Private Methods

    private static string GenerateSafeFilename(string name)
    {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        return safeName.Replace(" ", "_");
    }

    private void AddSummaryWorksheet(
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
        ws.Cell(row, 2).Value = exercise.ScheduledDate.ToString("yyyy-MM-dd");
        row++;

        ws.Cell(row, 1).Value = "Status";
        ws.Cell(row, 2).Value = exercise.Status.ToString();
        row++;

        ws.Cell(row, 1).Value = "Generated";
        ws.Cell(row, 2).Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
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

            ws.Cell(row, 1).Value = GetRatingDisplay(rating);
            ws.Cell(row, 2).Value = count;
            ws.Cell(row, 3).Value = $"{percentage}%";

            if (request.IncludeFormatting)
            {
                ws.Cell(row, 1).Style.Fill.BackgroundColor = RatingBackgroundColors[rating];
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

    private void AddByCapabilityWorksheet(
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
            ws.Cell(row, 1).Value = ct.Capability.Name.ToUpper();
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
                    ws.Cell(row, 2).Value = GetRatingShortCode(firstEntry.Rating);
                    ws.Cell(row, 3).Value = TruncateText(firstEntry.ObservationText, 100);
                    ws.Cell(row, 4).Value = request.IncludeEvaluatorNames ? (firstEntry.Evaluator?.DisplayName ?? "") : "";
                    ws.Cell(row, 5).Value = firstEntry.ObservedAt.ToString("HH:mm");

                    if (request.IncludeFormatting)
                    {
                        ws.Cell(row, 2).Style.Fill.BackgroundColor = RatingBackgroundColors[firstEntry.Rating];
                    }
                    row++;

                    // Additional entries without task name
                    foreach (var entry in taskEntries.Skip(1))
                    {
                        ws.Cell(row, 2).Value = GetRatingShortCode(entry.Rating);
                        ws.Cell(row, 3).Value = TruncateText(entry.ObservationText, 100);
                        ws.Cell(row, 4).Value = request.IncludeEvaluatorNames ? (entry.Evaluator?.DisplayName ?? "") : "";
                        ws.Cell(row, 5).Value = entry.ObservedAt.ToString("HH:mm");

                        if (request.IncludeFormatting)
                        {
                            ws.Cell(row, 2).Style.Fill.BackgroundColor = RatingBackgroundColors[entry.Rating];
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

    private void AddAllEntriesWorksheet(
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

        // Build lookup for capability targets
        var taskToCapability = capabilityTargets
            .SelectMany(ct => ct.CriticalTasks.Where(t => !t.IsDeleted).Select(t => (t.Id, ct)))
            .ToDictionary(x => x.Id, x => x.ct);

        // Data rows
        var row = 2;
        foreach (var entry in eegEntries.OrderBy(e => e.RecordedAt))
        {
            var ct = taskToCapability.GetValueOrDefault(entry.CriticalTaskId);
            var task = entry.CriticalTask;

            ws.Cell(row, 1).Value = entry.RecordedAt.ToString("yyyy-MM-dd HH:mm:ss");
            ws.Cell(row, 2).Value = ct?.Capability.Name ?? "";
            ws.Cell(row, 3).Value = ct?.TargetDescription ?? "";
            ws.Cell(row, 4).Value = task?.TaskDescription ?? "";
            ws.Cell(row, 5).Value = GetRatingShortCode(entry.Rating);
            ws.Cell(row, 6).Value = entry.ObservationText;
            ws.Cell(row, 7).Value = request.IncludeEvaluatorNames ? (entry.Evaluator?.DisplayName ?? "") : "";
            ws.Cell(row, 8).Value = entry.TriggeringInject != null
                ? $"INJ-{entry.TriggeringInject.InjectNumber:D3}: {entry.TriggeringInject.Title}"
                : "";

            if (request.IncludeFormatting)
            {
                ws.Cell(row, 5).Style.Fill.BackgroundColor = RatingBackgroundColors[entry.Rating];

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

    private void AddCoverageGapsWorksheet(
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

    private static string GetRatingDisplay(PerformanceRating rating)
    {
        return rating switch
        {
            PerformanceRating.Performed => "P - Performed",
            PerformanceRating.SomeChallenges => "S - Some Challenges",
            PerformanceRating.MajorChallenges => "M - Major Challenges",
            PerformanceRating.UnableToPerform => "U - Unable to Perform",
            _ => ""
        };
    }

    private static string GetRatingShortCode(PerformanceRating rating)
    {
        return rating switch
        {
            PerformanceRating.Performed => "P",
            PerformanceRating.SomeChallenges => "S",
            PerformanceRating.MajorChallenges => "M",
            PerformanceRating.UnableToPerform => "U",
            _ => ""
        };
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.Length <= maxLength) return text;
        return text.Substring(0, maxLength - 3) + "...";
    }

    #endregion
}
