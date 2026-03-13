using System.Globalization;
using Cadence.Core.Data;
using Cadence.Core.Features.Eeg.Builders;
using Cadence.Core.Features.ExcelExport.Models.DTOs;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Eeg.Services;

/// <summary>
/// Orchestrates EEG data export for After-Action Review (AAR).
/// Queries data from the database and delegates worksheet construction
/// to the dedicated builder classes in <see cref="Cadence.Core.Features.Eeg.Builders"/>.
/// </summary>
public class EegExportService : IEegExportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EegExportService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="EegExportService"/>.
    /// </summary>
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
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var filename = request.Filename ?? $"EEG_Export_{safeName}_{date}";

        // Generate Excel file
        using var workbook = new XLWorkbook();

        if (request.IncludeSummary)
        {
            EegSummaryBuilder.AddSummaryWorksheet(workbook, exercise, capabilityTargets, eegEntries, request);
        }

        if (request.IncludeByCapability)
        {
            EegCapabilityBuilder.AddByCapabilityWorksheet(workbook, capabilityTargets, eegEntries, request);
        }

        if (request.IncludeAllEntries)
        {
            EegEntriesBuilder.AddAllEntriesWorksheet(workbook, capabilityTargets, eegEntries, request);
        }

        if (request.IncludeCoverageGaps)
        {
            EegCoverageGapsBuilder.AddCoverageGapsWorksheet(workbook, capabilityTargets, eegEntries, request);
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
                exercise.ScheduledDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
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
                            EegFormattingHelper.GetRatingShortCode(e.Rating),
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

    // =========================================================================
    // Private Helper Methods
    // =========================================================================

    /// <summary>
    /// Generates a filesystem-safe filename by replacing invalid characters and spaces with underscores.
    /// </summary>
    /// <param name="name">Raw exercise name to sanitize.</param>
    /// <returns>A filename-safe string.</returns>
    private static string GenerateSafeFilename(string name)
    {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        return safeName.Replace(" ", "_");
    }
}
