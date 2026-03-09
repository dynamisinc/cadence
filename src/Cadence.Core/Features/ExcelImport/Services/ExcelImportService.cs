using System.Collections.Concurrent;
using System.Data;
using Cadence.Core.Data;
using Cadence.Core.Features.ExcelImport.Models.DTOs;
using Cadence.Core.Features.Injects.Services;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.ExcelImport.Services;

/// <summary>
/// Service for handling Excel file import operations.
/// </summary>
public class ExcelImportService : IExcelImportService
{
    private readonly AppDbContext _context;
    private readonly IInjectService _injectService;
    private readonly ILogger<ExcelImportService> _logger;

    // TODO (CD-C01): Migrate _sessions to IDistributedCache (Redis or SQL) so import state
    // survives App Service restarts and scale-out scenarios. The static dictionary is
    // single-instance only and will lose sessions on restart or when running multiple instances.
    // In-memory session storage - sessions are stored in static memory for simplicity.
    // LIMITATION: This does not work in multi-instance deployments (Azure Scale Out, K8s replicas).
    // For production with multiple instances, replace with Redis or database-backed session storage.
    // Current use case: Single-instance App Service deployment where this is acceptable.
    private static readonly ConcurrentDictionary<Guid, ImportSession> _sessions = new();

    // Session timeout in minutes
    private const int SessionTimeoutMinutes = 30;

    // Maximum file size in bytes (10 MB)
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    // Maximum rows to process
    private const int MaxRows = 5000;

    // Maximum columns to process (prevents phantom column issues in some Excel files)
    private const int MaxColumns = 100;

    // Common column name patterns for auto-mapping
    private static readonly Dictionary<string, string[]> ColumnPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        { "InjectNumber", new[] { "#", "number", "inject number", "inj #", "inject #", "inject no", "no", "id", "msel number", "msel #", "msel no", "inj#" } },
        { "Title", new[] { "title", "inject title", "name", "inject name", "event", "event title", "inject", "subject" } },
        { "Description", new[] { "description", "desc", "details", "inject description", "narrative", "inject details", "text", "description/script", "detailed statement", "script" } },
        { "ScheduledTime", new[] { "time", "scheduled time", "scheduled", "wall clock", "wall time", "delivery time", "inject dtg", "dtg", "date time", "actual dtg" } },
        { "ScenarioDay", new[] { "day", "scenario day", "exercise day", "sim day" } },
        { "ScenarioTime", new[] { "scenario time", "sim time", "story time", "exercise time" } },
        { "Source", new[] { "source", "from", "sender", "originator", "sent by", "send from", "sent from", "initiated by" } },
        { "Target", new[] { "target", "to", "recipient", "receiver", "sent to", "for", "send to" } },
        { "Track", new[] { "track", "lane", "functional area", "area", "storyline/thread", "storyline/ thread", "storyline", "thread", "esf" } },
        { "DeliveryMethod", new[] { "method", "delivery method", "delivery", "means", "inject mode", "modality" } },
        { "ExpectedAction", new[] { "expected action", "expected response", "response", "action", "anticipated response", "remarks/expected outcome", "expected outcome" } },
        { "Notes", new[] { "notes", "comments", "remarks", "controller notes", "notes/remarks", "comments/notes" } },
        { "Phase", new[] { "phase", "exercise phase", "phase name" } },
        { "Priority", new[] { "priority", "importance" } },
        { "LocationName", new[] { "location", "location name", "place", "venue" } },
        { "LocationType", new[] { "location type", "venue type" } },
        { "ResponsibleController", new[] { "controller", "responsible controller", "assigned to", "owner", "injected by", "poc", "inject author" } },
        { "InjectType", new[] { "inject type", "category", "inject category" } },
        { "TriggerType", new[] { "trigger", "trigger type", "activation", "fire mode" } },
    };

    // Synonyms for InjectType enum values
    private static readonly Dictionary<string, InjectType> InjectTypeSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        // Standard synonyms
        { "standard", InjectType.Standard },
        { "normal", InjectType.Standard },
        { "scheduled", InjectType.Standard },
        { "planned", InjectType.Standard },
        { "regular", InjectType.Standard },
        { "primary", InjectType.Standard },
        // Contingency synonyms
        { "contingency", InjectType.Contingency },
        { "backup", InjectType.Contingency },
        { "alternate", InjectType.Contingency },
        { "fallback", InjectType.Contingency },
        { "reserve", InjectType.Contingency },
        // Adaptive synonyms
        { "adaptive", InjectType.Adaptive },
        { "branch", InjectType.Adaptive },
        { "branching", InjectType.Adaptive },
        { "conditional", InjectType.Adaptive },
        { "decision", InjectType.Adaptive },
        // Complexity synonyms
        { "complexity", InjectType.Complexity },
        { "advanced", InjectType.Complexity },
        { "challenge", InjectType.Complexity },
        { "escalation", InjectType.Complexity },
        { "difficult", InjectType.Complexity },
        // Legacy MSEL type values
        { "administrative", InjectType.Standard },
        { "contextual", InjectType.Standard },
        { "expected action", InjectType.Standard },
        { "contingent", InjectType.Contingency },
        { "information", InjectType.Standard },
        { "operational", InjectType.Standard },
    };

    // Synonyms for TriggerType enum values
    private static readonly Dictionary<string, TriggerType> TriggerTypeSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        // Manual synonyms - controller/staff initiated
        { "manual", TriggerType.Manual },
        { "controller", TriggerType.Manual },
        { "controller action", TriggerType.Manual },
        { "actor action", TriggerType.Manual },
        { "staff action", TriggerType.Manual },
        { "human", TriggerType.Manual },
        { "hand", TriggerType.Manual },
        // Scheduled synonyms - time-based automatic
        { "scheduled", TriggerType.Scheduled },
        { "auto", TriggerType.Scheduled },
        { "automatic", TriggerType.Scheduled },
        { "timed", TriggerType.Scheduled },
        { "time-based", TriggerType.Scheduled },
        { "time", TriggerType.Scheduled },
        // Conditional synonyms - triggered by events/actions
        { "conditional", TriggerType.Conditional },
        { "triggered", TriggerType.Conditional },
        { "event", TriggerType.Conditional },
        { "event-based", TriggerType.Conditional },
        { "player action", TriggerType.Conditional },
        { "inject", TriggerType.Conditional },
        { "dependent", TriggerType.Conditional },
        { "contingent", TriggerType.Conditional },
    };

    // Values that look like delivery methods (to warn user about possible mapping mistake)
    private static readonly HashSet<string> DeliveryMethodLikeValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "radio", "phone", "email", "verbal", "written", "in person", "in-person", "face to face",
        "text", "sms", "fax", "simulation", "sim", "messenger", "call"
    };

    // Values that look like trigger types (to warn when mapped to InjectType)
    private static readonly HashSet<string> TriggerTypeLikeValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "controller action", "actor action", "player action", "staff action", "inject",
        "manual", "automatic", "auto", "scheduled", "timed", "conditional", "triggered",
        "time-based", "event-based", "dependent", "contingent"
    };

    // Delivery method synonyms for resolving legacy MSEL delivery method values
    private static readonly Dictionary<string, string> DeliveryMethodSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        // Verbal synonyms
        { "in person", "Verbal" },
        { "in-person", "Verbal" },
        { "face to face", "Verbal" },
        { "face-to-face", "Verbal" },
        { "spoken", "Verbal" },
        { "oral", "Verbal" },
        { "runner", "Verbal" },
        // Phone synonyms
        { "call", "Phone" },
        { "telephone", "Phone" },
        { "text", "Phone" },
        { "sms", "Phone" },
        { "message", "Phone" },
        { "cell phone call", "Phone" },
        { "cell phone", "Phone" },
        { "mobile", "Phone" },
        // Email synonyms
        { "e-mail", "Email" },
        { "electronic mail", "Email" },
        { "msg", "Email" },
        // Written synonyms
        { "fax", "Written" },
        { "document", "Written" },
        { "paper", "Written" },
        { "memo", "Written" },
        { "letter", "Written" },
        { "handout", "Written" },
        { "courier", "Written" },
        // Simulation synonyms
        { "sim", "Simulation" },
        { "cax", "Simulation" },
        { "computer aided", "Simulation" },
        { "simulated", "Simulation" },
        { "emits", "Simulation" },
        { "jiee", "Simulation" },
    };

    public ExcelImportService(
        AppDbContext context,
        IInjectService injectService,
        ILogger<ExcelImportService> logger)
    {
        _context = context;
        _injectService = injectService;
        _logger = logger;
    }

    public async Task<FileAnalysisResultDto> AnalyzeFileAsync(string fileName, Stream fileStream)
    {
        var sessionId = Guid.NewGuid();
        var warnings = new List<string>();

        // Check file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".xls" && extension != ".csv")
        {
            throw new InvalidOperationException($"Unsupported file format: {extension}. Supported formats: .xlsx, .xls, .csv");
        }

        // Check file size
        if (fileStream.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File size exceeds the maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        // Copy stream to memory for reuse
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var worksheets = new List<WorksheetInfoDto>();
        var fileFormat = extension.TrimStart('.');
        bool isPasswordProtected = false;

        try
        {
            if (extension == ".csv")
            {
                // Handle CSV as a single-sheet workbook
                var lines = await ReadCsvLinesAsync(memoryStream);
                worksheets.Add(new WorksheetInfoDto
                {
                    Index = 0,
                    Name = Path.GetFileNameWithoutExtension(fileName),
                    RowCount = lines.Count,
                    ColumnCount = lines.FirstOrDefault()?.Split(',').Length ?? 0,
                    LooksLikeMsel = true,
                    MselConfidence = 50
                });
            }
            else if (extension == ".xls")
            {
                // Handle legacy .xls files using ExcelDataReader
                var dataSet = LegacyExcelReader.ReadToDataSet(memoryStream);

                for (int i = 0; i < dataSet.Tables.Count; i++)
                {
                    var table = dataSet.Tables[i];
                    var lastRow = LegacyExcelReader.GetLastUsedRow(table);
                    var lastCol = Math.Min(LegacyExcelReader.GetLastUsedColumn(table) + 1, MaxColumns);

                    var (looksLikeMsel, confidence, suggestedHeaderRow) = AnalyzeDataTableHeaders(table);

                    worksheets.Add(new WorksheetInfoDto
                    {
                        Index = i,
                        Name = table.TableName,
                        RowCount = Math.Max(0, lastRow), // lastRow is 0-based
                        ColumnCount = lastCol,
                        LooksLikeMsel = looksLikeMsel,
                        MselConfidence = confidence,
                        SuggestedHeaderRow = suggestedHeaderRow,
                        SuggestedDataStartRow = suggestedHeaderRow + 1
                    });
                }

                if (worksheets.Count == 0)
                {
                    warnings.Add("The workbook contains no worksheets.");
                }
            }
            else
            {
                // Handle .xlsx files using ClosedXML
                using var workbook = new XLWorkbook(memoryStream);

                foreach (var worksheet in workbook.Worksheets)
                {
                    var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                    var lastCol = Math.Min(worksheet.LastColumnUsed()?.ColumnNumber() ?? 0, MaxColumns);

                    // Analyze headers across first 10 rows for MSEL patterns
                    var (looksLikeMsel, confidence, suggestedHeaderRow) = AnalyzeWorksheetHeaders(worksheet);

                    worksheets.Add(new WorksheetInfoDto
                    {
                        Index = worksheet.Position - 1, // ClosedXML uses 1-based
                        Name = worksheet.Name,
                        RowCount = Math.Max(0, lastRow - 1), // Subtract header row
                        ColumnCount = lastCol,
                        LooksLikeMsel = looksLikeMsel,
                        MselConfidence = confidence,
                        SuggestedHeaderRow = suggestedHeaderRow,
                        SuggestedDataStartRow = suggestedHeaderRow + 1
                    });
                }

                if (worksheets.Count == 0)
                {
                    warnings.Add("The workbook contains no worksheets.");
                }
            }
        }
        catch (System.IO.InvalidDataException)
        {
            isPasswordProtected = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing Excel file {FileName}", fileName);
            throw new InvalidOperationException($"Failed to read file: {ex.Message}");
        }

        // Save file to temp storage for later use
        memoryStream.Position = 0;
        var tempFilePath = GetTempFilePath(sessionId);
        await using (var fileWriteStream = File.Create(tempFilePath))
        {
            await memoryStream.CopyToAsync(fileWriteStream);
        }

        // Create session
        var session = new ImportSession
        {
            SessionId = sessionId,
            FileName = fileName,
            FileFormat = fileFormat,
            TempFilePath = tempFilePath,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(SessionTimeoutMinutes),
            CurrentStep = "Upload",
            Worksheets = worksheets
        };
        _sessions[sessionId] = session;

        // Cleanup expired sessions in background
        _ = Task.Run(CleanupExpiredSessionsAsync);

        return new FileAnalysisResultDto
        {
            SessionId = sessionId,
            FileName = fileName,
            FileSize = fileStream.Length,
            FileFormat = fileFormat,
            Worksheets = worksheets,
            IsPasswordProtected = isPasswordProtected,
            Warnings = warnings.Count > 0 ? warnings : null
        };
    }

    public async Task<WorksheetSelectionResultDto> SelectWorksheetAsync(SelectWorksheetRequestDto request)
    {
        var session = GetSession(request.SessionId);

        if (session.FileFormat == "csv")
        {
            return await ProcessCsvSelectionAsync(session, request);
        }

        if (session.FileFormat == "xls")
        {
            return await ProcessXlsSelectionAsync(session, request);
        }

        await using var fileStream = File.OpenRead(session.TempFilePath);
        using var workbook = new XLWorkbook(fileStream);

        var worksheet = workbook.Worksheet(request.WorksheetIndex + 1); // ClosedXML uses 1-based
        var worksheetInfo = session.Worksheets[request.WorksheetIndex];

        // Get column information
        var columns = new List<ColumnInfoDto>();
        var headerRow = worksheet.Row(request.HeaderRow);
        var lastColumn = Math.Min(worksheet.LastColumnUsed()?.ColumnNumber() ?? 0, MaxColumns);

        for (int col = 1; col <= lastColumn; col++)
        {
            var cell = headerRow.Cell(col);
            var headerText = cell.GetString().Trim();

            if (string.IsNullOrEmpty(headerText))
            {
                headerText = $"Column {GetColumnLetter(col)}";
            }

            var columnData = GetColumnData(worksheet, col, request.DataStartRow);

            columns.Add(new ColumnInfoDto
            {
                Index = col - 1,
                Letter = GetColumnLetter(col),
                Header = headerText,
                DataType = columnData.DataType,
                SampleValues = columnData.SampleValues,
                FillRate = columnData.FillRate
            });
        }

        // Get preview rows
        var previewRows = new List<Dictionary<string, object?>>();
        var lastDataRow = worksheet.LastRowUsed()?.RowNumber() ?? request.DataStartRow;
        var previewEnd = Math.Min(request.DataStartRow + request.PreviewRowCount - 1, lastDataRow);

        for (int row = request.DataStartRow; row <= previewEnd; row++)
        {
            var rowData = new Dictionary<string, object?>();
            for (int col = 1; col <= lastColumn; col++)
            {
                var header = columns[col - 1].Header;
                var cell = worksheet.Cell(row, col);
                rowData[header] = GetCellValue(cell);
            }
            previewRows.Add(rowData);
        }

        // Update session
        session.SelectedWorksheetIndex = request.WorksheetIndex;
        session.HeaderRow = request.HeaderRow;
        session.DataStartRow = request.DataStartRow;
        session.Columns = columns;
        session.CurrentStep = "SheetSelection";

        return new WorksheetSelectionResultDto
        {
            SessionId = request.SessionId,
            Worksheet = worksheetInfo,
            Columns = columns,
            PreviewRows = previewRows,
            PreviewRowCount = previewRows.Count
        };
    }

    public Task<IReadOnlyList<ColumnMappingDto>> GetSuggestedMappingsAsync(Guid sessionId)
    {
        var session = GetSession(sessionId);

        if (session.Columns == null || session.Columns.Count == 0)
        {
            throw new InvalidOperationException("No worksheet selected. Please select a worksheet first.");
        }

        var mappings = new List<ColumnMappingDto>();

        // Define all Cadence fields with their properties
        var cadenceFields = new[]
        {
            ("InjectNumber", "Inject Number", false, "Unique identifier for the inject within the MSEL"),
            ("Title", "Title", true, "Brief title of the inject"),
            ("Description", "Description", false, "Detailed description of the inject"),
            ("ScheduledTime", "Scheduled Time", false, "Wall clock time when inject should be delivered (defaults to 00:00 if not provided)"),
            ("ScenarioDay", "Scenario Day", false, "Day number in the exercise scenario"),
            ("ScenarioTime", "Scenario Time", false, "Time in the exercise scenario"),
            ("Source", "Source / From", false, "Who is sending or initiating this inject"),
            ("Target", "Target / To", false, "Who is receiving or responding to this inject"),
            ("Track", "Track", false, "Functional area or track this inject belongs to"),
            ("DeliveryMethod", "Delivery Method", false, "How the inject will be delivered"),
            ("ExpectedAction", "Expected Action", false, "Expected response from participants"),
            ("Notes", "Notes", false, "Additional notes for controllers"),
            ("Phase", "Phase", false, "Exercise phase this inject belongs to"),
            ("Priority", "Priority", false, "Priority level (1-5)"),
            ("LocationName", "Location Name", false, "Name of the location"),
            ("LocationType", "Location Type", false, "Type of location"),
            ("ResponsibleController", "Responsible Controller", false, "Controller assigned to this inject"),
            ("InjectType", "Inject Type", false, "Type of inject (Standard, Contingency, Adaptive, Complexity)"),
            ("TriggerType", "Trigger Type", false, "How the inject is triggered (Manual, Scheduled, Conditional)"),
        };

        foreach (var (fieldName, displayName, isRequired, description) in cadenceFields)
        {
            var (suggestedIndex, confidence) = FindBestMatchingColumn(fieldName, session.Columns);

            mappings.Add(new ColumnMappingDto
            {
                CadenceField = fieldName,
                DisplayName = displayName,
                IsRequired = isRequired,
                Description = description,
                SourceColumnIndex = suggestedIndex >= 0 ? suggestedIndex : null,
                SuggestedColumnIndex = suggestedIndex >= 0 ? suggestedIndex : null,
                SuggestedMappingConfidence = confidence
            });
        }

        session.Mappings = mappings;
        session.CurrentStep = "Mapping";

        return Task.FromResult<IReadOnlyList<ColumnMappingDto>>(mappings);
    }

    public async Task<ValidationResultDto> ValidateImportAsync(ConfigureMappingsRequestDto request)
    {
        var session = GetSession(request.SessionId);

        // Update session mappings
        session.Mappings = request.Mappings.ToList();
        session.TimeFormat = request.TimeFormat;
        session.DateFormat = request.DateFormat;

        // Check required mappings
        var missingRequired = request.Mappings
            .Where(m => m.IsRequired && !m.SourceColumnIndex.HasValue)
            .Select(m => m.DisplayName)
            .ToList();

        if (missingRequired.Count > 0)
        {
            return new ValidationResultDto
            {
                SessionId = request.SessionId,
                TotalRows = 0,
                ValidRows = 0,
                ErrorRows = 0,
                WarningRows = 0,
                Rows = Array.Empty<RowValidationResultDto>(),
                AllRequiredMappingsConfigured = false,
                MissingRequiredMappings = missingRequired
            };
        }

        // Read and validate all rows
        var rows = await ReadAllRowsAsync(session, request.Mappings);
        var validationResults = ValidateRows(rows, request.Mappings);

        var validRows = validationResults.Count(r => r.Status == "Valid");
        var errorRows = validationResults.Count(r => r.Status == "Error");
        var warningRows = validationResults.Count(r => r.Status == "Warning");

        session.ValidationResults = validationResults;
        session.CurrentStep = "Validation";

        return new ValidationResultDto
        {
            SessionId = request.SessionId,
            TotalRows = validationResults.Count,
            ValidRows = validRows,
            ErrorRows = errorRows,
            WarningRows = warningRows,
            Rows = validationResults,
            AllRequiredMappingsConfigured = true
        };
    }

    public async Task<ImportResultDto> ExecuteImportAsync(ExecuteImportRequestDto request)
    {
        var session = GetSession(request.SessionId);

        if (session.ValidationResults == null || session.Mappings == null)
        {
            throw new InvalidOperationException("Import has not been validated. Please validate before importing.");
        }

        // Get the exercise and its active MSEL
        var exercise = await _context.Exercises
            .Include(e => e.Msels)
            .FirstOrDefaultAsync(e => e.Id == request.ExerciseId);

        if (exercise == null)
        {
            throw new InvalidOperationException("Exercise not found.");
        }

        var msel = exercise.Msels.FirstOrDefault(m => m.IsActive);
        if (msel == null)
        {
            // Auto-create MSEL if one doesn't exist for this exercise
            msel = new Cadence.Core.Models.Entities.Msel
            {
                Id = Guid.NewGuid(),
                Name = "Primary MSEL",
                Description = "Automatically created during import",
                Version = 1,
                IsActive = true,
                ExerciseId = exercise.Id,
                OrganizationId = exercise.OrganizationId // Data isolation
            };
            _context.Msels.Add(msel);

            // IMPORTANT: Set the exercise's ActiveMselId so the API can find the injects
            exercise.ActiveMselId = msel.Id;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Auto-created MSEL {MselId} for exercise {ExerciseId}", msel.Id, exercise.Id);
        }
        else if (exercise.ActiveMselId == null)
        {
            // Fix: Existing active MSEL but exercise.ActiveMselId wasn't set
            exercise.ActiveMselId = msel.Id;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Fixed ActiveMselId for exercise {ExerciseId} to MSEL {MselId}", exercise.Id, msel.Id);
        }

        var errors = new List<string>();
        var warnings = new List<string>();
        var injectsCreated = 0;
        var injectsUpdated = 0;
        var rowsSkipped = 0;
        var phasesCreated = 0;

        // Handle Replace strategy
        if (request.Strategy == ImportStrategy.Replace)
        {
            var existingInjects = await _context.Injects
                .Where(i => i.MselId == msel.Id && !i.IsDeleted)
                .ToListAsync();

            foreach (var inject in existingInjects)
            {
                inject.IsDeleted = true;
                inject.DeletedAt = DateTime.UtcNow;
            }
        }

        // Get existing phases
        var phases = await _context.Phases
            .Where(p => p.ExerciseId == request.ExerciseId && !p.IsDeleted)
            .ToDictionaryAsync(p => p.Name.ToLowerInvariant());

        // Get existing delivery methods
        var deliveryMethods = await _context.DeliveryMethods
            .Where(d => d.IsActive)
            .ToDictionaryAsync(d => d.Name.ToLowerInvariant());

        // Process each valid row
        var rowsToImport = request.SkipErrorRows
            ? session.ValidationResults.Where(r => r.Status != "Error")
            : session.ValidationResults;

        // Get initial max values ONCE before the loop (not inside)
        var currentMaxSequence = await _context.Injects
            .Where(i => i.MselId == msel.Id && !i.IsDeleted)
            .MaxAsync(i => (int?)i.Sequence) ?? 0;

        var currentMaxInjectNumber = await _context.Injects
            .Where(i => i.MselId == msel.Id && !i.IsDeleted)
            .MaxAsync(i => (int?)i.InjectNumber) ?? 0;

        foreach (var row in rowsToImport)
        {
            if (row.Status == "Error" && !request.SkipErrorRows)
            {
                errors.Add($"Row {row.RowNumber}: Contains validation errors");
                rowsSkipped++;
                continue;
            }

            try
            {
                // Increment counters for each new inject
                currentMaxSequence++;
                currentMaxInjectNumber++;

                // Create inject entity
                var inject = new Inject
                {
                    Id = Guid.NewGuid(),
                    MselId = msel.Id,
                    InjectNumber = currentMaxInjectNumber,
                    Sequence = currentMaxSequence,
                    Status = InjectStatus.Draft,
                    TriggerType = TriggerType.Manual
                };

                // Map values from row
                MapRowToInject(inject, row.Values, session.Mappings, phases, deliveryMethods,
                    request.ExerciseId, exercise.OrganizationId, request.CreateMissingPhases, ref phasesCreated, warnings);

                // Title fallback: if Title is empty, populate from Description
                if (string.IsNullOrWhiteSpace(inject.Title) && !string.IsNullOrWhiteSpace(inject.Description))
                {
                    inject.Title = inject.Description.Length > 200
                        ? inject.Description[..197] + "..."
                        : inject.Description;
                }

                _context.Injects.Add(inject);
                injectsCreated++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing row {RowNumber}", row.RowNumber);
                errors.Add($"Row {row.RowNumber}: {ex.Message}");
                rowsSkipped++;
            }
        }

        await _context.SaveChangesAsync();

        session.CurrentStep = "Complete";

        return new ImportResultDto
        {
            Success = errors.Count == 0,
            InjectsCreated = injectsCreated,
            InjectsUpdated = injectsUpdated,
            RowsSkipped = rowsSkipped,
            PhasesCreated = phasesCreated,
            Errors = errors.Count > 0 ? errors : null,
            Warnings = warnings.Count > 0 ? warnings : null,
            MselId = msel.Id
        };
    }

    public Task CancelImportAsync(Guid sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            // Delete temp file
            if (File.Exists(session.TempFilePath))
            {
                try
                {
                    File.Delete(session.TempFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temp file {Path}", session.TempFilePath);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<ImportSessionStateDto?> GetSessionStateAsync(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult<ImportSessionStateDto?>(null);
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            _ = CancelImportAsync(sessionId);
            return Task.FromResult<ImportSessionStateDto?>(null);
        }

        return Task.FromResult<ImportSessionStateDto?>(new ImportSessionStateDto
        {
            SessionId = session.SessionId,
            FileName = session.FileName,
            CurrentStep = session.CurrentStep,
            SelectedWorksheetIndex = session.SelectedWorksheetIndex,
            Mappings = session.Mappings,
            CreatedAt = session.CreatedAt,
            ExpiresAt = session.ExpiresAt
        });
    }

    public Task<UpdateRowsResultDto> UpdateRowsAsync(UpdateRowsRequestDto request)
    {
        var session = GetSession(request.SessionId);

        if (session.ValidationResults == null || session.Mappings == null)
        {
            throw new InvalidOperationException("No validation results. Please validate before updating rows.");
        }

        // Lock session state to prevent concurrent read-modify-write on ValidationResults
        lock (session.SyncRoot)
        {
            // Build rowNumber -> index lookup
            var rowIndex = new Dictionary<int, int>();
            for (int i = 0; i < session.ValidationResults.Count; i++)
            {
                rowIndex[session.ValidationResults[i].RowNumber] = i;
            }

            var updatedRows = new List<RowValidationResultDto>();
            var affectedRowNumbers = request.Updates.Select(u => u.RowNumber).Distinct();

            foreach (var rowNumber in affectedRowNumbers)
            {
                if (!rowIndex.TryGetValue(rowNumber, out var idx))
                    continue;

                var existing = session.ValidationResults[idx];

                // Apply value updates
                foreach (var update in request.Updates.Where(u => u.RowNumber == rowNumber))
                {
                    existing.Values[update.Field] = update.Value;
                }

                // Re-validate
                var issues = ValidateSingleRow(existing.Values, session.Mappings);
                var status = issues.Any(i => i.Severity == "Error")
                    ? "Error"
                    : issues.Any(i => i.Severity == "Warning")
                        ? "Warning"
                        : "Valid";

                var newRow = new RowValidationResultDto
                {
                    RowNumber = existing.RowNumber,
                    Status = status,
                    Values = existing.Values,
                    Issues = issues.Count > 0 ? issues : null
                };

                session.ValidationResults[idx] = newRow;
                updatedRows.Add(newRow);
            }

            // Recompute counts
            var validRows = session.ValidationResults.Count(r => r.Status == "Valid");
            var errorRows = session.ValidationResults.Count(r => r.Status == "Error");
            var warningRows = session.ValidationResults.Count(r => r.Status == "Warning");

            return Task.FromResult(new UpdateRowsResultDto
            {
                SessionId = request.SessionId,
                TotalRows = session.ValidationResults.Count,
                ValidRows = validRows,
                ErrorRows = errorRows,
                WarningRows = warningRows,
                UpdatedRows = updatedRows
            });
        }
    }

    #region Private Methods

    private ImportSession GetSession(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException("Import session not found. Please upload a file first.");
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            _ = CancelImportAsync(sessionId);
            throw new InvalidOperationException("Import session has expired. Please upload the file again.");
        }

        // Extend session
        session.ExpiresAt = DateTime.UtcNow.AddMinutes(SessionTimeoutMinutes);

        return session;
    }

    private static string GetTempFilePath(Guid sessionId)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "cadence-imports");
        Directory.CreateDirectory(tempDir);
        return Path.Combine(tempDir, $"{sessionId}.tmp");
    }

    private static async Task<List<string>> ReadCsvLinesAsync(Stream stream)
    {
        var lines = new List<string>();
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync() is { } line)
        {
            lines.Add(line);
        }
        return lines;
    }

    /// <summary>
    /// Analyzes a DataTable (from .xls file) for MSEL-like header patterns.
    /// Returns 1-based header row number for consistency with the rest of the code.
    /// </summary>
    private static (bool LooksLikeMsel, int Confidence, int SuggestedHeaderRow) AnalyzeDataTableHeaders(DataTable table)
    {
        var bestRow = 1; // 1-based
        var bestConfidence = 0;
        var bestMatches = 0;

        var maxScanRow = Math.Min(10, table.Rows.Count);
        var lastCol = Math.Min(LegacyExcelReader.GetLastUsedColumn(table) + 1, MaxColumns);

        var keyPatterns = new[] { "title", "subject", "time", "dtg", "inject", "description", "text", "from", "to", "msel" };

        for (int row = 0; row < maxScanRow; row++)
        {
            var headers = new List<string>();
            var dataRow = table.Rows[row];
            for (int col = 0; col < lastCol; col++)
            {
                headers.Add(LegacyExcelReader.GetCellString(dataRow, col).ToLowerInvariant());
            }

            var matchedPatterns = 0;
            foreach (var pattern in keyPatterns)
            {
                if (headers.Any(h => h.Contains(pattern)))
                {
                    matchedPatterns++;
                }
            }

            if (matchedPatterns > bestMatches)
            {
                bestMatches = matchedPatterns;
                bestRow = row + 1; // Convert to 1-based
                bestConfidence = (matchedPatterns * 100) / keyPatterns.Length;
            }
        }

        return (bestMatches >= 2, Math.Min(100, bestConfidence + 20), bestRow);
    }

    /// <summary>
    /// Gets column data (type, samples, fill rate) from a DataTable.
    /// startRow is 0-based.
    /// </summary>
    private static (string DataType, IReadOnlyList<string?> SampleValues, int FillRate) GetDataTableColumnData(
        DataTable table, int column, int startRow)
    {
        var samples = new List<string?>();
        var types = new List<string>();
        var filledCount = 0;
        var lastRow = Math.Min(table.Rows.Count - 1, startRow + 100);

        for (int row = startRow; row <= lastRow; row++)
        {
            var dataRow = table.Rows[row];
            if (!LegacyExcelReader.IsCellEmpty(dataRow, column))
            {
                filledCount++;

                if (samples.Count < 3)
                {
                    samples.Add(LegacyExcelReader.GetCellValue(dataRow, column)?.ToString());
                }

                types.Add(LegacyExcelReader.InferCellType(LegacyExcelReader.GetCellValue(dataRow, column)));
            }
        }

        var totalRows = lastRow - startRow + 1;
        var fillRate = totalRows > 0 ? (filledCount * 100) / totalRows : 0;

        var dataType = types.Count > 0
            ? types.GroupBy(t => t).OrderByDescending(g => g.Count()).First().Key
            : "text";

        return (dataType, samples, fillRate);
    }

    private static (bool LooksLikeMsel, int Confidence, int SuggestedHeaderRow) AnalyzeWorksheetHeaders(IXLWorksheet worksheet)
    {
        var bestRow = 1;
        var bestConfidence = 0;
        var bestMatches = 0;

        var maxScanRow = Math.Min(10, worksheet.LastRowUsed()?.RowNumber() ?? 1);
        var lastCol = Math.Min(worksheet.LastColumnUsed()?.ColumnNumber() ?? 0, MaxColumns);

        // Expanded patterns for legacy MSEL detection
        var keyPatterns = new[] { "title", "subject", "time", "dtg", "inject", "description", "text", "from", "to", "msel" };

        for (int row = 1; row <= maxScanRow; row++)
        {
            var headers = new List<string>();
            for (int col = 1; col <= lastCol; col++)
            {
                headers.Add(worksheet.Row(row).Cell(col).GetString().Trim().ToLowerInvariant());
            }

            var matchedPatterns = 0;
            foreach (var pattern in keyPatterns)
            {
                if (headers.Any(h => h.Contains(pattern)))
                {
                    matchedPatterns++;
                }
            }

            if (matchedPatterns > bestMatches)
            {
                bestMatches = matchedPatterns;
                bestRow = row;
                bestConfidence = (matchedPatterns * 100) / keyPatterns.Length;
            }
        }

        return (bestMatches >= 2, Math.Min(100, bestConfidence + 20), bestRow);
    }

    private static (string DataType, IReadOnlyList<string?> SampleValues, int FillRate) GetColumnData(
        IXLWorksheet worksheet, int column, int startRow)
    {
        var samples = new List<string?>();
        var types = new List<string>();
        var filledCount = 0;
        var lastRow = Math.Min(worksheet.LastRowUsed()?.RowNumber() ?? startRow, startRow + 100);

        for (int row = startRow; row <= lastRow; row++)
        {
            var cell = worksheet.Cell(row, column);
            if (!cell.IsEmpty())
            {
                filledCount++;

                if (samples.Count < 3)
                {
                    samples.Add(cell.GetString());
                }

                types.Add(InferCellType(cell));
            }
        }

        var totalRows = lastRow - startRow + 1;
        var fillRate = totalRows > 0 ? (filledCount * 100) / totalRows : 0;

        var dataType = types.Count > 0
            ? types.GroupBy(t => t).OrderByDescending(g => g.Count()).First().Key
            : "text";

        return (dataType, samples, fillRate);
    }

    private static string InferCellType(IXLCell cell)
    {
        if (cell.IsEmpty()) return "empty";
        if (cell.DataType == XLDataType.DateTime) return "date";
        if (cell.DataType == XLDataType.Number) return "number";
        if (cell.DataType == XLDataType.Boolean) return "boolean";
        if (cell.DataType == XLDataType.TimeSpan) return "time";
        return "text";
    }

    private static object? GetCellValue(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;

        return cell.DataType switch
        {
            XLDataType.DateTime => cell.GetDateTime(),
            XLDataType.Number => cell.GetDouble(),
            XLDataType.Boolean => cell.GetBoolean(),
            XLDataType.TimeSpan => cell.GetTimeSpan().ToString(),
            _ => cell.GetString()
        };
    }

    private static string GetColumnLetter(int columnNumber)
    {
        var result = "";
        while (columnNumber > 0)
        {
            columnNumber--;
            result = (char)('A' + columnNumber % 26) + result;
            columnNumber /= 26;
        }
        return result;
    }

    private static (int Index, int Confidence) FindBestMatchingColumn(string fieldName, IReadOnlyList<ColumnInfoDto> columns)
    {
        if (!ColumnPatterns.TryGetValue(fieldName, out var patterns))
        {
            return (-1, 0);
        }

        foreach (var column in columns)
        {
            var headerLower = column.Header.ToLowerInvariant();

            foreach (var pattern in patterns)
            {
                if (headerLower == pattern)
                {
                    return (column.Index, 100); // Exact match
                }

                if (headerLower.Contains(pattern))
                {
                    return (column.Index, 80); // Contains match
                }
            }
        }

        return (-1, 0);
    }

    private async Task<WorksheetSelectionResultDto> ProcessXlsSelectionAsync(ImportSession session, SelectWorksheetRequestDto request)
    {
        await using var fileStream = File.OpenRead(session.TempFilePath);
        var dataSet = LegacyExcelReader.ReadToDataSet(fileStream);
        var table = dataSet.Tables[request.WorksheetIndex];
        var worksheetInfo = session.Worksheets[request.WorksheetIndex];

        // Get column information (HeaderRow is 1-based, DataTable is 0-based)
        var columns = new List<ColumnInfoDto>();
        var headerRowIndex = request.HeaderRow - 1;
        var lastColumn = Math.Min(LegacyExcelReader.GetLastUsedColumn(table) + 1, MaxColumns);

        if (headerRowIndex < table.Rows.Count)
        {
            var headerDataRow = table.Rows[headerRowIndex];

            for (int col = 0; col < lastColumn; col++)
            {
                var headerText = LegacyExcelReader.GetCellString(headerDataRow, col);

                if (string.IsNullOrEmpty(headerText))
                {
                    headerText = $"Column {GetColumnLetter(col + 1)}";
                }

                var columnData = GetDataTableColumnData(table, col, request.DataStartRow - 1);

                columns.Add(new ColumnInfoDto
                {
                    Index = col,
                    Letter = GetColumnLetter(col + 1),
                    Header = headerText,
                    DataType = columnData.DataType,
                    SampleValues = columnData.SampleValues,
                    FillRate = columnData.FillRate
                });
            }
        }

        // Get preview rows (DataStartRow is 1-based)
        var previewRows = new List<Dictionary<string, object?>>();
        var startRowIndex = request.DataStartRow - 1;
        var lastDataRow = LegacyExcelReader.GetLastUsedRow(table);
        var previewEnd = Math.Min(startRowIndex + request.PreviewRowCount - 1, lastDataRow);

        for (int row = startRowIndex; row <= previewEnd; row++)
        {
            var dataRow = table.Rows[row];
            var rowData = new Dictionary<string, object?>();
            for (int col = 0; col < lastColumn && col < columns.Count; col++)
            {
                var header = columns[col].Header;
                rowData[header] = LegacyExcelReader.GetCellValue(dataRow, col);
            }
            previewRows.Add(rowData);
        }

        // Update session
        session.SelectedWorksheetIndex = request.WorksheetIndex;
        session.HeaderRow = request.HeaderRow;
        session.DataStartRow = request.DataStartRow;
        session.Columns = columns;
        session.CurrentStep = "SheetSelection";

        return new WorksheetSelectionResultDto
        {
            SessionId = request.SessionId,
            Worksheet = worksheetInfo,
            Columns = columns,
            PreviewRows = previewRows,
            PreviewRowCount = previewRows.Count
        };
    }

    private async Task<WorksheetSelectionResultDto> ProcessCsvSelectionAsync(ImportSession session, SelectWorksheetRequestDto request)
    {
        var lines = await File.ReadAllLinesAsync(session.TempFilePath);
        var columns = new List<ColumnInfoDto>();
        var previewRows = new List<Dictionary<string, object?>>();

        if (lines.Length == 0 || request.HeaderRow < 1 || request.HeaderRow > lines.Length)
        {
            return new WorksheetSelectionResultDto
            {
                SessionId = request.SessionId,
                Worksheet = session.Worksheets[0],
                Columns = columns,
                PreviewRows = previewRows,
                PreviewRowCount = 0
            };
        }

        // Parse header row
        var headers = ParseCsvLine(lines[request.HeaderRow - 1]);
        for (int i = 0; i < headers.Count; i++)
        {
            columns.Add(new ColumnInfoDto
            {
                Index = i,
                Letter = GetColumnLetter(i + 1),
                Header = headers[i],
                DataType = "text",
                SampleValues = Array.Empty<string>(),
                FillRate = 100
            });
        }

        // Get preview rows (ensure DataStartRow is valid)
        var startRow = Math.Max(1, request.DataStartRow);
        var endRow = Math.Min(startRow + request.PreviewRowCount - 1, lines.Length);
        for (int row = startRow; row <= endRow; row++)
        {
            var values = ParseCsvLine(lines[row - 1]);
            var rowData = new Dictionary<string, object?>();
            for (int col = 0; col < headers.Count && col < values.Count; col++)
            {
                rowData[headers[col]] = values[col];
            }
            previewRows.Add(rowData);
        }

        session.SelectedWorksheetIndex = 0;
        session.HeaderRow = request.HeaderRow;
        session.DataStartRow = startRow;
        session.Columns = columns;
        session.CurrentStep = "SheetSelection";

        return new WorksheetSelectionResultDto
        {
            SessionId = request.SessionId,
            Worksheet = session.Worksheets[0],
            Columns = columns,
            PreviewRows = previewRows,
            PreviewRowCount = previewRows.Count
        };
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var inQuotes = false;
        var current = "";

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current.Trim());
                current = "";
            }
            else
            {
                current += c;
            }
        }
        values.Add(current.Trim());

        return values;
    }

    private async Task<List<Dictionary<string, object?>>> ReadAllRowsAsync(
        ImportSession session, IReadOnlyList<ColumnMappingDto> mappings)
    {
        var rows = new List<Dictionary<string, object?>>();

        if (session.FileFormat == "csv")
        {
            var lines = await File.ReadAllLinesAsync(session.TempFilePath);
            for (int i = session.DataStartRow - 1; i < lines.Length && i < MaxRows; i++)
            {
                var values = ParseCsvLine(lines[i]);
                var rowData = new Dictionary<string, object?>();

                foreach (var mapping in mappings.Where(m => m.SourceColumnIndex.HasValue))
                {
                    var colIndex = mapping.SourceColumnIndex!.Value;
                    rowData[mapping.CadenceField] = colIndex < values.Count ? values[colIndex] : null;
                }

                // Skip rows where all mapped values are empty (legend rows, spacers)
                if (rowData.Values.All(v => IsEmpty(v)))
                    continue;

                rows.Add(rowData);
            }
        }
        else if (session.FileFormat == "xls")
        {
            if (!session.SelectedWorksheetIndex.HasValue)
            {
                throw new InvalidOperationException("Worksheet index not set for Excel file import");
            }

            await using var fileStream = File.OpenRead(session.TempFilePath);
            var dataSet = LegacyExcelReader.ReadToDataSet(fileStream);
            var table = dataSet.Tables[session.SelectedWorksheetIndex.Value];

            // DataTable rows are 0-based; session.DataStartRow is 1-based
            var startRowIndex = session.DataStartRow - 1;
            var lastRowIndex = Math.Min(LegacyExcelReader.GetLastUsedRow(table), startRowIndex + MaxRows - 1);

            for (int row = startRowIndex; row <= lastRowIndex; row++)
            {
                var dataRow = table.Rows[row];
                var rowData = new Dictionary<string, object?>();

                foreach (var mapping in mappings.Where(m => m.SourceColumnIndex.HasValue))
                {
                    rowData[mapping.CadenceField] = LegacyExcelReader.GetCellValue(dataRow, mapping.SourceColumnIndex!.Value);
                }

                // Skip rows where all mapped values are empty (legend rows, spacers)
                if (rowData.Values.All(v => IsEmpty(v)))
                    continue;

                rows.Add(rowData);
            }
        }
        else
        {
            if (!session.SelectedWorksheetIndex.HasValue)
            {
                throw new InvalidOperationException("Worksheet index not set for Excel file import");
            }

            await using var fileStream = File.OpenRead(session.TempFilePath);
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheet(session.SelectedWorksheetIndex.Value + 1);

            var lastRow = Math.Min(worksheet.LastRowUsed()?.RowNumber() ?? 0, session.DataStartRow + MaxRows - 1);

            for (int row = session.DataStartRow; row <= lastRow; row++)
            {
                var rowData = new Dictionary<string, object?>();

                foreach (var mapping in mappings.Where(m => m.SourceColumnIndex.HasValue))
                {
                    var cell = worksheet.Cell(row, mapping.SourceColumnIndex!.Value + 1);
                    rowData[mapping.CadenceField] = GetCellValue(cell);
                }

                // Skip rows where all mapped values are empty (legend rows, spacers)
                if (rowData.Values.All(v => IsEmpty(v)))
                    continue;

                rows.Add(rowData);
            }
        }

        return rows;
    }

    private static List<RowValidationResultDto> ValidateRows(
        List<Dictionary<string, object?>> rows,
        IReadOnlyList<ColumnMappingDto> mappings)
    {
        var results = new List<RowValidationResultDto>();
        var rowNumber = 2; // Assume header is row 1

        foreach (var row in rows)
        {
            var issues = ValidateSingleRow(row, mappings);

            var status = issues.Any(i => i.Severity == "Error")
                ? "Error"
                : issues.Any(i => i.Severity == "Warning")
                    ? "Warning"
                    : "Valid";

            results.Add(new RowValidationResultDto
            {
                RowNumber = rowNumber++,
                Status = status,
                Values = row,
                Issues = issues.Count > 0 ? issues : null
            });
        }

        return results;
    }

    /// <summary>
    /// Validates a single row of data against the configured mappings.
    /// Returns a list of validation issues found.
    /// </summary>
    private static List<ValidationIssueDto> ValidateSingleRow(
        Dictionary<string, object?> row,
        IReadOnlyList<ColumnMappingDto> mappings)
    {
        var issues = new List<ValidationIssueDto>();

        // Validate required fields
        foreach (var mapping in mappings.Where(m => m.IsRequired))
        {
            if (!row.TryGetValue(mapping.CadenceField, out var value) || IsEmpty(value))
            {
                issues.Add(new ValidationIssueDto
                {
                    Field = mapping.CadenceField,
                    Severity = "Error",
                    Message = $"{mapping.DisplayName} is required",
                    OriginalValue = value?.ToString()
                });
            }
        }

        // Validate specific field formats
        if (row.TryGetValue("ScheduledTime", out var timeValue) && !IsEmpty(timeValue))
        {
            if (!TimeParsingHelper.TryParseTime(timeValue, out _) && !TimeParsingHelper.TryParseDateTime(timeValue, out _))
            {
                issues.Add(new ValidationIssueDto
                {
                    Field = "ScheduledTime",
                    Severity = "Error",
                    Message = "Cannot parse time value",
                    OriginalValue = timeValue?.ToString()
                });
            }
        }
        else if (!row.ContainsKey("ScheduledTime") || IsEmpty(timeValue))
        {
            // ScheduledTime not mapped or empty - warn but allow import
            var hasScheduledTimeMapping = mappings.Any(m => m.CadenceField == "ScheduledTime" && m.SourceColumnIndex.HasValue);
            if (!hasScheduledTimeMapping || IsEmpty(timeValue))
            {
                issues.Add(new ValidationIssueDto
                {
                    Field = "ScheduledTime",
                    Severity = "Warning",
                    Message = "Scheduled Time is not provided. Will default to 00:00.",
                    OriginalValue = null
                });
            }
        }

        if (row.TryGetValue("Priority", out var priorityValue) && !IsEmpty(priorityValue))
        {
            if (!int.TryParse(priorityValue?.ToString(), out var priority) || priority < 1 || priority > 5)
            {
                issues.Add(new ValidationIssueDto
                {
                    Field = "Priority",
                    Severity = "Warning",
                    Message = "Priority should be between 1 and 5",
                    OriginalValue = priorityValue?.ToString()
                });
            }
        }

        // Validate InjectType - warn if value looks like TriggerType
        if (row.TryGetValue("InjectType", out var injectTypeValue) && !IsEmpty(injectTypeValue))
        {
            var injectTypeStr = injectTypeValue?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(injectTypeStr) && !InjectTypeSynonyms.ContainsKey(injectTypeStr))
            {
                if (TriggerTypeLikeValues.Contains(injectTypeStr) || TriggerTypeSynonyms.ContainsKey(injectTypeStr))
                {
                    issues.Add(new ValidationIssueDto
                    {
                        Field = "InjectType",
                        Severity = "Warning",
                        Message = "This value looks like a Trigger Type (e.g., Controller Action, Player Action). Consider mapping this column to Trigger Type instead.",
                        OriginalValue = injectTypeStr
                    });
                }
                else if (DeliveryMethodLikeValues.Contains(injectTypeStr))
                {
                    issues.Add(new ValidationIssueDto
                    {
                        Field = "InjectType",
                        Severity = "Warning",
                        Message = "This value looks like a Delivery Method. Consider mapping this column to Delivery Method instead.",
                        OriginalValue = injectTypeStr
                    });
                }
            }
        }

        // Validate TriggerType - warn if unrecognized
        if (row.TryGetValue("TriggerType", out var triggerTypeValue) && !IsEmpty(triggerTypeValue))
        {
            var triggerTypeStr = triggerTypeValue?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(triggerTypeStr) && !TriggerTypeSynonyms.ContainsKey(triggerTypeStr))
            {
                issues.Add(new ValidationIssueDto
                {
                    Field = "TriggerType",
                    Severity = "Warning",
                    Message = "Unrecognized trigger type value. Will default to Manual.",
                    OriginalValue = triggerTypeStr
                });
            }
        }

        return issues;
    }

    private static bool IsEmpty(object? value)
    {
        return value == null || (value is string s && string.IsNullOrWhiteSpace(s));
    }

    // NOTE: TryParseTime logic has been extracted to TimeParsingHelper for testability.
    // All callers now use TimeParsingHelper.TryParseTime / TryParseDateTime.

    private void MapRowToInject(
        Inject inject,
        Dictionary<string, object?> values,
        IReadOnlyList<ColumnMappingDto> mappings,
        Dictionary<string, Phase> phases,
        Dictionary<string, DeliveryMethodLookup> deliveryMethods,
        Guid exerciseId,
        Guid organizationId,
        bool createMissingPhases,
        ref int phasesCreated,
        List<string> warnings)
    {
        foreach (var mapping in mappings.Where(m => m.SourceColumnIndex.HasValue))
        {
            if (!values.TryGetValue(mapping.CadenceField, out var value) || IsEmpty(value))
            {
                continue;
            }

            var stringValue = value?.ToString()?.Trim();

            switch (mapping.CadenceField)
            {
                case "InjectNumber":
                    // InjectNumber is auto-assigned; skip if provided in Excel
                    // (It could be stored in SourceReference for traceability)
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        inject.SourceReference = stringValue;
                    }
                    break;
                case "Title":
                    inject.Title = stringValue ?? "";
                    break;
                case "Description":
                    inject.Description = stringValue ?? "";
                    break;
                case "ScheduledTime":
                    if (TimeParsingHelper.TryParseTime(value, out var time))
                    {
                        inject.ScheduledTime = time;
                    }
                    else if (TimeParsingHelper.TryParseDateTime(value, out var fullDt))
                    {
                        inject.ScheduledTime = TimeOnly.FromDateTime(fullDt);
                        // Populate ScenarioDay from date if not already set
                        if (inject.ScenarioDay == null)
                        {
                            inject.ScenarioDay = fullDt.Day;
                        }
                    }
                    break;
                case "ScenarioDay":
                    if (int.TryParse(stringValue, out var day))
                    {
                        inject.ScenarioDay = day;
                    }
                    break;
                case "ScenarioTime":
                    if (TimeParsingHelper.TryParseTime(value, out var scenarioTime))
                    {
                        inject.ScenarioTime = scenarioTime;
                    }
                    break;
                case "Source":
                    inject.Source = stringValue;
                    break;
                case "Target":
                    inject.Target = stringValue ?? "";
                    break;
                case "Track":
                    inject.Track = stringValue;
                    break;
                case "DeliveryMethod":
                    if (stringValue != null && deliveryMethods.TryGetValue(stringValue.ToLowerInvariant(), out var method))
                    {
                        // Exact match in delivery methods lookup
                        inject.DeliveryMethodId = method.Id;
                    }
                    else if (!string.IsNullOrEmpty(stringValue) &&
                             DeliveryMethodSynonyms.TryGetValue(stringValue, out var canonicalName) &&
                             deliveryMethods.TryGetValue(canonicalName.ToLowerInvariant(), out var synonymMethod))
                    {
                        // Synonym resolved to a known delivery method
                        inject.DeliveryMethodId = synonymMethod.Id;
                    }
                    else if (!string.IsNullOrEmpty(stringValue))
                    {
                        // Use "Other" delivery method
                        var otherMethod = deliveryMethods.Values.FirstOrDefault(m => m.IsOther);
                        if (otherMethod != null)
                        {
                            inject.DeliveryMethodId = otherMethod.Id;
                            inject.DeliveryMethodOther = stringValue;
                        }
                    }
                    break;
                case "ExpectedAction":
                    inject.ExpectedAction = stringValue;
                    break;
                case "Notes":
                    inject.ControllerNotes = stringValue;
                    break;
                case "Phase":
                    if (stringValue != null)
                    {
                        var phaseLower = stringValue.ToLowerInvariant();
                        if (phases.TryGetValue(phaseLower, out var phase))
                        {
                            inject.PhaseId = phase.Id;
                        }
                        else if (createMissingPhases)
                        {
                            // Create new phase
                            var newPhase = new Phase
                            {
                                Id = Guid.NewGuid(),
                                ExerciseId = exerciseId,
                                OrganizationId = organizationId, // Data isolation
                                Name = stringValue,
                                Sequence = phases.Count + 1
                            };
                            _context.Phases.Add(newPhase);
                            phases[phaseLower] = newPhase;
                            phasesCreated++;
                        }
                        else
                        {
                            warnings.Add($"Phase '{stringValue}' not found and will not be assigned.");
                        }
                    }
                    break;
                case "Priority":
                    if (int.TryParse(stringValue, out var priority))
                    {
                        inject.Priority = Math.Clamp(priority, 1, 5);
                    }
                    break;
                case "LocationName":
                    inject.LocationName = stringValue;
                    break;
                case "LocationType":
                    inject.LocationType = stringValue;
                    break;
                case "ResponsibleController":
                    inject.ResponsibleController = stringValue;
                    break;
                case "InjectType":
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        if (InjectTypeSynonyms.TryGetValue(stringValue, out var injectType))
                        {
                            inject.InjectType = injectType;
                        }
                        else if (TriggerTypeLikeValues.Contains(stringValue) || TriggerTypeSynonyms.ContainsKey(stringValue))
                        {
                            // Warn user that this looks like a trigger type, not inject type
                            warnings.Add($"Row value '{stringValue}' in Inject Type column looks like a trigger type (e.g., Controller Action, Player Action). Consider mapping this column to Trigger Type instead. Defaulting to Standard.");
                            inject.InjectType = InjectType.Standard;
                        }
                        else if (DeliveryMethodLikeValues.Contains(stringValue))
                        {
                            // Warn user that this looks like a delivery method, not inject type
                            warnings.Add($"Row value '{stringValue}' in Inject Type column looks like a delivery method. Consider mapping to Delivery Method instead. Defaulting to Standard.");
                            inject.InjectType = InjectType.Standard;
                        }
                        else
                        {
                            // Unrecognized value - default to Standard with warning
                            warnings.Add($"Unrecognized inject type '{stringValue}', defaulting to Standard.");
                            inject.InjectType = InjectType.Standard;
                        }
                    }
                    break;
                case "TriggerType":
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        if (TriggerTypeSynonyms.TryGetValue(stringValue, out var triggerType))
                        {
                            inject.TriggerType = triggerType;
                        }
                        else
                        {
                            // Unrecognized value - default to Manual with warning
                            warnings.Add($"Unrecognized trigger type '{stringValue}', defaulting to Manual.");
                            inject.TriggerType = TriggerType.Manual;
                        }
                    }
                    break;
            }
        }
    }

    private async Task CleanupExpiredSessionsAsync()
    {
        var expiredSessions = _sessions
            .Where(kv => kv.Value.ExpiresAt < DateTime.UtcNow)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var sessionId in expiredSessions)
        {
            await CancelImportAsync(sessionId);
        }
    }

    #endregion

    #region Nested Types

    private sealed class ImportSession
    {
        // Lock object for thread-safe access to mutable session state.
        // Used for ExpiresAt and any read-modify-write on ValidationResults/Mappings.
        private readonly object _lock = new();
        private DateTime _expiresAt;

        /// <summary>
        /// Synchronization root for external callers that need to lock session state
        /// (e.g., UpdateRowsAsync mutating ValidationResults).
        /// </summary>
        public object SyncRoot => _lock;

        public Guid SessionId { get; init; }
        public required string FileName { get; init; }
        public required string FileFormat { get; init; }
        public required string TempFilePath { get; init; }
        public DateTime CreatedAt { get; init; }

        public DateTime ExpiresAt
        {
            get { lock (_lock) { return _expiresAt; } }
            set { lock (_lock) { _expiresAt = value; } }
        }

        public required string CurrentStep { get; set; }
        public required List<WorksheetInfoDto> Worksheets { get; init; }
        public int? SelectedWorksheetIndex { get; set; }
        public int HeaderRow { get; set; } = 1;
        public int DataStartRow { get; set; } = 2;
        public List<ColumnInfoDto>? Columns { get; set; }
        public List<ColumnMappingDto>? Mappings { get; set; }
        public string? TimeFormat { get; set; }
        public string? DateFormat { get; set; }
        public List<RowValidationResultDto>? ValidationResults { get; set; }
    }

    #endregion
}
