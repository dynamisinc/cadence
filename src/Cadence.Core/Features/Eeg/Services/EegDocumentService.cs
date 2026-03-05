using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Cadence.Core.Data;
using Cadence.Core.Features.Eeg.Models.DTOs;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Eeg.Services;

/// <summary>
/// Service for generating EEG documents (HSEEP-compliant Word documents).
/// </summary>
public partial class EegDocumentService : IEegDocumentService
{
    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationContext _orgContext;

    // HSEEP Rating definitions
    private static readonly Dictionary<PerformanceRating, string> RatingShortNames = new()
    {
        { PerformanceRating.Performed, "P: Performed without Challenges" },
        { PerformanceRating.SomeChallenges, "S: Performed with Some Challenges" },
        { PerformanceRating.MajorChallenges, "M: Performed with Major Challenges" },
        { PerformanceRating.UnableToPerform, "U: Unable to be Performed" }
    };

    private static readonly Dictionary<PerformanceRating, string> RatingFullDefinitions = new()
    {
        { PerformanceRating.Performed,
            "Performed without Challenges (P): The targets and critical tasks associated with the core capability were completed in a manner that achieved the objective(s) and met the performance measure(s)." },
        { PerformanceRating.SomeChallenges,
            "Performed with Some Challenges (S): The targets and critical tasks associated with the core capability were completed in a manner that achieved the objective(s) and met the performance measure(s); however, some challenges were noted." },
        { PerformanceRating.MajorChallenges,
            "Performed with Major Challenges (M): The targets and critical tasks associated with the core capability were completed in a manner that achieved the objective(s) and met the performance measure(s); however, significant challenges were noted." },
        { PerformanceRating.UnableToPerform,
            "Unable to be Performed (U): The targets and critical tasks associated with the core capability were not performed in a manner that achieved the objective(s) or met the performance measure(s)." }
    };

    public EegDocumentService(AppDbContext context, ICurrentOrganizationContext orgContext)
    {
        _context = context;
        _orgContext = orgContext;
    }

    public async Task<EegDocumentResult> GenerateAsync(Guid exerciseId, GenerateEegDocumentRequest request)
    {
        // Load exercise with organization - validate it belongs to current org
        var exercise = await _context.Exercises
            .Include(e => e.Organization)
            .FirstOrDefaultAsync(e => e.Id == exerciseId
                && e.OrganizationId == _orgContext.CurrentOrganizationId);

        if (exercise == null)
            throw new InvalidOperationException($"Exercise {exerciseId} not found");

        // Load capability targets with tasks and EEG entries - filter by organization
        var capabilityTargets = await _context.CapabilityTargets
            .Include(ct => ct.Capability)
            .Include(ct => ct.CriticalTasks.Where(t => !t.IsDeleted))
                .ThenInclude(task => task.EegEntries.Where(e => !e.IsDeleted))
                    .ThenInclude(e => e.Evaluator)
            .Where(ct => ct.ExerciseId == exerciseId
                && ct.OrganizationId == _orgContext.CurrentOrganizationId
                && !ct.IsDeleted)
            .OrderBy(ct => ct.SortOrder)
            .ToListAsync();

        if (capabilityTargets.Count == 0)
            throw new InvalidOperationException("Define at least one Capability Target before generating an EEG document");

        // Generate based on output format
        if (request.OutputFormat == EegDocumentOutputFormat.PerCapability)
        {
            return await GeneratePerCapabilityZipAsync(exercise, capabilityTargets, request);
        }

        return await GenerateSingleDocumentAsync(exercise, capabilityTargets, request);
    }

    private async Task<EegDocumentResult> GenerateSingleDocumentAsync(
        Exercise exercise,
        List<CapabilityTarget> capabilityTargets,
        GenerateEegDocumentRequest request)
    {
        using var stream = new MemoryStream();

        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Add document header
            AddDocumentHeader(body, exercise);

            // Add each capability target section
            foreach (var target in capabilityTargets)
            {
                AddCapabilityTargetSection(body, target, request.Mode, request.IncludeEvaluatorNames);
            }

            // Add ratings key and definitions
            AddRatingsSection(body);

            // Add page settings
            AddSectionProperties(body);
        }

        var totalTasks = capabilityTargets.Sum(ct => ct.CriticalTasks.Count);
        var filename = SanitizeFilename($"EEG_{exercise.Name}_{DateTime.UtcNow:yyyy-MM-dd}.docx");

        return new EegDocumentResult(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            filename,
            capabilityTargets.Count,
            totalTasks
        );
    }

    private async Task<EegDocumentResult> GeneratePerCapabilityZipAsync(
        Exercise exercise,
        List<CapabilityTarget> capabilityTargets,
        GenerateEegDocumentRequest request)
    {
        using var zipStream = new MemoryStream();

        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            foreach (var target in capabilityTargets)
            {
                var docFilename = SanitizeFilename(
                    $"EEG_{exercise.Name}_{target.Capability.Name}_{DateTime.UtcNow:yyyy-MM-dd}.docx");

                var entry = archive.CreateEntry(docFilename);

                using var entryStream = entry.Open();
                using var docStream = new MemoryStream();

                using (var document = WordprocessingDocument.Create(docStream, WordprocessingDocumentType.Document))
                {
                    var mainPart = document.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    var body = mainPart.Document.AppendChild(new Body());

                    AddDocumentHeader(body, exercise);
                    AddCapabilityTargetSection(body, target, request.Mode, request.IncludeEvaluatorNames);
                    AddRatingsSection(body);
                    AddSectionProperties(body);
                }

                docStream.Position = 0;
                await docStream.CopyToAsync(entryStream);
            }
        }

        var totalTasks = capabilityTargets.Sum(ct => ct.CriticalTasks.Count);
        var zipFilename = SanitizeFilename($"EEG_{exercise.Name}_{DateTime.UtcNow:yyyy-MM-dd}.zip");

        return new EegDocumentResult(
            zipStream.ToArray(),
            "application/zip",
            zipFilename,
            capabilityTargets.Count,
            totalTasks
        );
    }

    private static void AddDocumentHeader(Body body, Exercise exercise)
    {
        // Title
        body.AppendChild(CreateParagraph("Exercise Evaluation Guide", bold: true, fontSize: 28, centered: true));
        body.AppendChild(CreateParagraph(""));

        // Exercise info table
        var table = new Table();
        table.AppendChild(CreateTableProperties(false));

        AddTableRow(table, "Exercise Name:", exercise.Name);
        AddTableRow(table, "Exercise Date:", exercise.ScheduledDate.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture));
        AddTableRow(table, "Organization:", exercise.Organization?.Name ?? "");
        AddTableRow(table, "Location:", exercise.Location ?? "");

        body.AppendChild(table);
        body.AppendChild(CreateParagraph(""));
    }

    private static void AddCapabilityTargetSection(
        Body body,
        CapabilityTarget target,
        EegDocumentMode mode,
        bool includeEvaluatorNames)
    {
        // Capability header
        body.AppendChild(CreateParagraph(
            target.Capability.Name.ToUpperInvariant(),
            bold: true,
            fontSize: 24,
            backgroundColor: "D9D9D9"));

        // Capability Target description
        body.AppendChild(CreateParagraph($"Capability Target: {target.TargetDescription}", bold: true));

        // Sources (if present)
        if (!string.IsNullOrWhiteSpace(target.Sources))
        {
            body.AppendChild(CreateParagraph($"Source(s): {target.Sources}", italic: true));
        }

        body.AppendChild(CreateParagraph(""));

        // Critical Tasks section
        body.AppendChild(CreateParagraph("Critical Tasks:", bold: true));
        foreach (var task in target.CriticalTasks.OrderBy(t => t.SortOrder))
        {
            body.AppendChild(CreateBulletParagraph(task.TaskDescription));
            if (!string.IsNullOrWhiteSpace(task.Standard))
            {
                body.AppendChild(CreateParagraph($"    Standard: {task.Standard}", italic: true, fontSize: 20));
            }
        }

        body.AppendChild(CreateParagraph(""));

        // Rating Chart Table
        AddRatingChartTable(body, target, mode, includeEvaluatorNames);

        // Evaluator Information section
        if (mode == EegDocumentMode.Blank)
        {
            AddBlankEvaluatorInfoSection(body);
        }
        else if (mode == EegDocumentMode.Completed && includeEvaluatorNames)
        {
            var evaluators = target.CriticalTasks
                .SelectMany(t => t.EegEntries)
                .Where(e => e.Evaluator != null)
                .Select(e => e.Evaluator!)
                .DistinctBy(e => e.Id)
                .ToList();
            AddCompletedEvaluatorInfoSection(body, evaluators);
        }

        body.AppendChild(CreateParagraph(""));
    }

    private static void AddRatingChartTable(
        Body body,
        CapabilityTarget target,
        EegDocumentMode mode,
        bool includeEvaluatorNames)
    {
        var table = new Table();
        table.AppendChild(CreateTableProperties(true));

        // Header row
        var headerRow = new TableRow();
        headerRow.AppendChild(CreateTableCell("Capability Target", true, "2000"));
        headerRow.AppendChild(CreateTableCell("Associated Critical Tasks", true, "2500"));
        headerRow.AppendChild(CreateTableCell("Observation Notes and Explanation of Rating", true, "3500"));
        headerRow.AppendChild(CreateTableCell("Target Rating", true, "1000"));
        table.AppendChild(headerRow);

        // Data row with target and tasks
        var dataRow = new TableRow();

        // Target column
        dataRow.AppendChild(CreateTableCell(target.TargetDescription, false, "2000"));

        // Tasks column - bullet list of tasks
        var tasksCell = new TableCell();
        tasksCell.AppendChild(new TableCellProperties(new TableCellWidth { Width = "2500" }));
        foreach (var task in target.CriticalTasks.OrderBy(t => t.SortOrder))
        {
            tasksCell.AppendChild(CreateBulletParagraph(task.TaskDescription, fontSize: 20));
        }
        if (target.CriticalTasks.Count == 0)
        {
            tasksCell.AppendChild(CreateParagraph("(No tasks defined)"));
        }
        dataRow.AppendChild(tasksCell);

        // Observation column
        if (mode == EegDocumentMode.Completed)
        {
            var obsCell = new TableCell();
            obsCell.AppendChild(new TableCellProperties(new TableCellWidth { Width = "3500" }));

            var entries = target.CriticalTasks
                .SelectMany(t => t.EegEntries)
                .OrderBy(e => e.ObservedAt)
                .ToList();

            foreach (var entry in entries)
            {
                var ratingLetter = GetRatingLetter(entry.Rating);
                var timeStr = entry.ObservedAt.ToString("HH:mm", CultureInfo.InvariantCulture);
                var evaluatorText = includeEvaluatorNames && entry.Evaluator != null
                    ? entry.Evaluator.DisplayName
                    : "Evaluator";

                // Format: [Evaluator Name, Time] Observation text...
                var obsText = entry.ObservationText;
                if (obsText.Length > 500)
                    obsText = obsText[..497] + "...";

                obsCell.AppendChild(CreateParagraph(
                    $"[{evaluatorText}, {timeStr}] ({ratingLetter}) {obsText}",
                    fontSize: 20));
                obsCell.AppendChild(CreateParagraph(""));
            }

            if (entries.Count == 0)
            {
                obsCell.AppendChild(CreateParagraph("(No observations recorded)"));
            }

            dataRow.AppendChild(obsCell);

            // Calculated rating for completed mode
            var finalRating = entries.Count > 0
                ? GetAggregateRating(entries.Select(e => e.Rating))
                : null;
            dataRow.AppendChild(CreateTableCell(
                finalRating.HasValue ? GetRatingLetter(finalRating.Value) : "",
                false,
                "1000"));
        }
        else
        {
            // Blank observation column
            dataRow.AppendChild(CreateTableCell("", false, "3500"));
            dataRow.AppendChild(CreateTableCell("", false, "1000"));
        }

        table.AppendChild(dataRow);

        // Final rating row
        var finalRow = new TableRow();
        finalRow.AppendChild(CreateTableCell("Final Core Capability Rating:", true, "8000", colSpan: 3));
        finalRow.AppendChild(CreateTableCell("", false, "1000"));
        table.AppendChild(finalRow);

        body.AppendChild(table);
    }

    private static void AddBlankEvaluatorInfoSection(Body body)
    {
        body.AppendChild(CreateParagraph(""));
        body.AppendChild(CreateParagraph("Evaluator Information", bold: true));

        var table = new Table();
        table.AppendChild(CreateTableProperties(true));

        AddTableRow(table, "Name:", "______________________________");
        AddTableRow(table, "Email:", "______________________________");
        AddTableRow(table, "Phone:", "______________________________");

        body.AppendChild(table);
    }

    private static void AddCompletedEvaluatorInfoSection(Body body, List<ApplicationUser> evaluators)
    {
        if (evaluators.Count == 0) return;

        body.AppendChild(CreateParagraph(""));
        body.AppendChild(CreateParagraph("Evaluator Information", bold: true));

        var table = new Table();
        table.AppendChild(CreateTableProperties(true));

        // Header row
        var headerRow = new TableRow();
        headerRow.AppendChild(CreateTableCell("Name", true, "3000"));
        headerRow.AppendChild(CreateTableCell("Email", true, "3500"));
        headerRow.AppendChild(CreateTableCell("Phone", true, "2500"));
        table.AppendChild(headerRow);

        // Evaluator rows
        foreach (var evaluator in evaluators.OrderBy(e => e.DisplayName))
        {
            var row = new TableRow();
            row.AppendChild(CreateTableCell(evaluator.DisplayName, false, "3000"));
            row.AppendChild(CreateTableCell(evaluator.Email ?? "", false, "3500"));
            row.AppendChild(CreateTableCell(evaluator.PhoneNumber ?? "[Not provided]", false, "2500"));
            table.AppendChild(row);
        }

        body.AppendChild(table);
    }

    private static void AddRatingsSection(Body body)
    {
        body.AppendChild(CreateParagraph(""));
        body.AppendChild(CreateParagraph("Ratings Key", bold: true, backgroundColor: "D9D9D9"));

        foreach (var rating in RatingShortNames)
        {
            body.AppendChild(CreateParagraph(rating.Value));
        }

        body.AppendChild(CreateParagraph(""));
        body.AppendChild(CreateParagraph("Rating Definitions", bold: true));

        foreach (var rating in RatingFullDefinitions)
        {
            body.AppendChild(CreateParagraph(rating.Value, fontSize: 20));
            body.AppendChild(CreateParagraph(""));
        }
    }

    private static void AddSectionProperties(Body body)
    {
        var sectionProps = new SectionProperties(
            new PageSize { Width = 12240, Height = 15840 }, // Letter size in twentieths of a point
            new PageMargin
            {
                Top = 1440,    // 1 inch
                Right = 1440,
                Bottom = 1440,
                Left = 1440
            }
        );
        body.AppendChild(sectionProps);
    }

    // Helper methods for creating document elements

    private static Paragraph CreateParagraph(
        string text,
        bool bold = false,
        bool italic = false,
        int fontSize = 22,  // 11pt in half-points
        bool centered = false,
        string? backgroundColor = null)
    {
        var paragraph = new Paragraph();

        if (centered || backgroundColor != null)
        {
            var pProps = new ParagraphProperties();
            if (centered)
                pProps.AppendChild(new Justification { Val = JustificationValues.Center });
            if (backgroundColor != null)
                pProps.AppendChild(new Shading { Fill = backgroundColor });
            paragraph.AppendChild(pProps);
        }

        if (!string.IsNullOrEmpty(text))
        {
            var run = new Run();
            var runProps = new RunProperties();

            runProps.AppendChild(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" });
            runProps.AppendChild(new FontSize { Val = fontSize.ToString(CultureInfo.InvariantCulture) });

            if (bold)
                runProps.AppendChild(new Bold());
            if (italic)
                runProps.AppendChild(new Italic());

            run.AppendChild(runProps);
            run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            paragraph.AppendChild(run);
        }

        return paragraph;
    }

    private static Paragraph CreateBulletParagraph(string text, int fontSize = 22)
    {
        var paragraph = new Paragraph();

        var pProps = new ParagraphProperties();
        pProps.AppendChild(new Indentation { Left = "720" }); // 0.5 inch indent

        paragraph.AppendChild(pProps);

        var run = new Run();
        var runProps = new RunProperties();
        runProps.AppendChild(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" });
        runProps.AppendChild(new FontSize { Val = fontSize.ToString(CultureInfo.InvariantCulture) });

        run.AppendChild(runProps);
        run.AppendChild(new Text($"\u2022 {text}") { Space = SpaceProcessingModeValues.Preserve });
        paragraph.AppendChild(run);

        return paragraph;
    }

    private static TableProperties CreateTableProperties(bool withBorders)
    {
        var props = new TableProperties();
        props.AppendChild(new TableWidth { Width = "9000", Type = TableWidthUnitValues.Dxa });

        if (withBorders)
        {
            props.AppendChild(new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" }
            ));
        }

        return props;
    }

    private static void AddTableRow(Table table, string label, string value)
    {
        var row = new TableRow();
        row.AppendChild(CreateTableCell(label, true, "2500"));
        row.AppendChild(CreateTableCell(value, false, "6500"));
        table.AppendChild(row);
    }

    private static TableCell CreateTableCell(
        string text,
        bool bold,
        string width,
        int colSpan = 1)
    {
        var cell = new TableCell();

        var cellProps = new TableCellProperties();
        cellProps.AppendChild(new TableCellWidth { Width = width });

        if (bold)
        {
            cellProps.AppendChild(new Shading { Fill = "F2F2F2" }); // Light gray for headers
        }

        if (colSpan > 1)
        {
            cellProps.AppendChild(new GridSpan { Val = colSpan });
        }

        cell.AppendChild(cellProps);
        cell.AppendChild(CreateParagraph(text, bold: bold));

        return cell;
    }

    private static PerformanceRating? GetAggregateRating(IEnumerable<PerformanceRating> ratings)
    {
        var list = ratings.ToList();
        if (list.Count == 0) return null;

        // Use worst-case aggregation per HSEEP guidelines
        // Higher enum value = worse rating, so max gives worst case
        return list.Max();
    }

    private static string GetRatingLetter(PerformanceRating rating) => rating switch
    {
        PerformanceRating.Performed => "P",
        PerformanceRating.SomeChallenges => "S",
        PerformanceRating.MajorChallenges => "M",
        PerformanceRating.UnableToPerform => "U",
        _ => "?"
    };

    private static string SanitizeFilename(string filename)
    {
        return InvalidFilenameCharsRegex().Replace(filename, "_");
    }

    [GeneratedRegex(@"[<>:""/\\|?*]")]
    private static partial Regex InvalidFilenameCharsRegex();
}
