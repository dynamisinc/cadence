using Cadence.Core.Data;
using Cadence.Core.Features.ExcelImport.Models;
using Cadence.Core.Features.ExcelImport.Models.DTOs;
using Cadence.Core.Features.Injects.Services;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.ExcelImport.Services;

/// <summary>
/// Orchestrates the multi-step Excel import wizard: file analysis, worksheet selection,
/// column mapping, validation, inline row editing, and final import execution.
/// </summary>
/// <remarks>
/// Business logic is intentionally delegated to focused helper classes:
/// <list type="bullet">
///   <item><see cref="ImportSessionStore"/> / <see cref="IImportSessionStore"/> — session lifecycle</item>
///   <item><see cref="ExcelFileReader"/> — format-specific file reading</item>
///   <item><see cref="ColumnMappingStrategy"/> — column-pattern dictionaries and auto-mapping</item>
///   <item><see cref="RowValidationService"/> — per-row validation rules</item>
/// </list>
/// </remarks>
public class ExcelImportService : IExcelImportService
{
    private readonly AppDbContext _context;
    private readonly IInjectService _injectService;
    private readonly ILogger<ExcelImportService> _logger;
    private readonly IImportSessionStore _sessionStore;

    // Session timeout in minutes
    private const int SessionTimeoutMinutes = 30;

    // Maximum file size in bytes (10 MB)
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    // Maximum columns to process (prevents phantom column issues in some Excel files)
    private const int MaxColumns = 100;

    /// <summary>
    /// Initializes the service.
    /// </summary>
    /// <param name="context">EF Core database context.</param>
    /// <param name="injectService">Inject domain service.</param>
    /// <param name="logger">Structured logger.</param>
    /// <param name="sessionStore">
    /// Optional session store. When omitted (e.g., in unit tests that construct the
    /// service directly) the process-wide <see cref="ImportSessionStore.Default"/>
    /// instance is used so that sessions still work correctly.
    /// </param>
    public ExcelImportService(
        AppDbContext context,
        IInjectService injectService,
        ILogger<ExcelImportService> logger,
        IImportSessionStore? sessionStore = null)
    {
        _context = context;
        _injectService = injectService;
        _logger = logger;
        _sessionStore = sessionStore ?? ImportSessionStore.Default;
    }

    /// <inheritdoc />
    public async Task<FileAnalysisResultDto> AnalyzeFileAsync(string fileName, Stream fileStream)
    {
        var sessionId = Guid.NewGuid();
        var warnings = new List<string>();

        // Validate file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".xls" && extension != ".csv")
        {
            throw new InvalidOperationException(
                $"Unsupported file format: {extension}. Supported formats: .xlsx, .xls, .csv");
        }

        // Validate file size
        if (fileStream.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"File size exceeds the maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        // Buffer the stream so it can be read multiple times
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
                var lines = await ExcelFileReader.ReadCsvLinesAsync(memoryStream);
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
                var dataSet = LegacyExcelReader.ReadToDataSet(memoryStream);

                for (int i = 0; i < dataSet.Tables.Count; i++)
                {
                    var table = dataSet.Tables[i];
                    var lastRow = LegacyExcelReader.GetLastUsedRow(table);
                    var lastCol = Math.Min(LegacyExcelReader.GetLastUsedColumn(table) + 1, MaxColumns);

                    var (looksLikeMsel, confidence, suggestedHeaderRow) =
                        ExcelFileReader.AnalyzeDataTableHeaders(table);

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
                using var workbook = new XLWorkbook(memoryStream);

                foreach (var worksheet in workbook.Worksheets)
                {
                    var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                    var lastCol = Math.Min(worksheet.LastColumnUsed()?.ColumnNumber() ?? 0, MaxColumns);

                    var (looksLikeMsel, confidence, suggestedHeaderRow) =
                        ExcelFileReader.AnalyzeWorksheetHeaders(worksheet);

                    worksheets.Add(new WorksheetInfoDto
                    {
                        Index = worksheet.Position - 1, // ClosedXML uses 1-based position
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

        // Persist buffered file to temp storage for subsequent wizard steps
        memoryStream.Position = 0;
        var tempFilePath = GetTempFilePath(sessionId);
        await using (var fileWriteStream = File.Create(tempFilePath))
        {
            await memoryStream.CopyToAsync(fileWriteStream);
        }

        // Create and store the session
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
        _sessionStore.CreateSession(session);

        // Clean up expired sessions in the background
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

    /// <inheritdoc />
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

        return await ProcessXlsxSelectionAsync(session, request);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ColumnMappingDto>> GetSuggestedMappingsAsync(Guid sessionId)
    {
        var session = GetSession(sessionId);

        if (session.Columns == null || session.Columns.Count == 0)
        {
            throw new InvalidOperationException("No worksheet selected. Please select a worksheet first.");
        }

        var mappings = new List<ColumnMappingDto>();

        // All Cadence importable fields with metadata
        var cadenceFields = new[]
        {
            ("InjectNumber",          "Inject Number",          false, "Unique identifier for the inject within the MSEL"),
            ("Title",                 "Title",                  true,  "Brief title of the inject"),
            ("Description",           "Description",            false, "Detailed description of the inject"),
            ("ScheduledTime",         "Scheduled Time",         false, "Wall clock time when inject should be delivered (defaults to 00:00 if not provided)"),
            ("ScenarioDay",           "Scenario Day",           false, "Day number in the exercise scenario"),
            ("ScenarioTime",          "Scenario Time",          false, "Time in the exercise scenario"),
            ("Source",                "Source / From",          false, "Who is sending or initiating this inject"),
            ("Target",                "Target / To",            false, "Who is receiving or responding to this inject"),
            ("Track",                 "Track",                  false, "Functional area or track this inject belongs to"),
            ("DeliveryMethod",        "Delivery Method",        false, "How the inject will be delivered"),
            ("ExpectedAction",        "Expected Action",        false, "Expected response from participants"),
            ("Notes",                 "Notes",                  false, "Additional notes for controllers"),
            ("Phase",                 "Phase",                  false, "Exercise phase this inject belongs to"),
            ("Priority",              "Priority",               false, "Priority level (1-5)"),
            ("LocationName",          "Location Name",          false, "Name of the location"),
            ("LocationType",          "Location Type",          false, "Type of location"),
            ("ResponsibleController", "Responsible Controller", false, "Controller assigned to this inject"),
            ("InjectType",            "Inject Type",            false, "Type of inject (Standard, Contingency, Adaptive, Complexity)"),
            ("TriggerType",           "Trigger Type",           false, "How the inject is triggered (Manual, Scheduled, Conditional)"),
        };

        foreach (var (fieldName, displayName, isRequired, description) in cadenceFields)
        {
            var (suggestedIndex, confidence) =
                ColumnMappingStrategy.FindBestMatchingColumn(fieldName, session.Columns);

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

    /// <inheritdoc />
    public async Task<ValidationResultDto> ValidateImportAsync(ConfigureMappingsRequestDto request)
    {
        var session = GetSession(request.SessionId);

        session.Mappings = request.Mappings.ToList();
        session.TimeFormat = request.TimeFormat;
        session.DateFormat = request.DateFormat;

        // Check that all required fields have a source column
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

        var rows = await ExcelFileReader.ReadAllRowsAsync(session, request.Mappings);
        var validationResults = RowValidationService.ValidateRows(rows, request.Mappings);

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

    /// <inheritdoc />
    public async Task<ImportResultDto> ExecuteImportAsync(ExecuteImportRequestDto request)
    {
        var session = GetSession(request.SessionId);

        if (session.ValidationResults == null || session.Mappings == null)
        {
            throw new InvalidOperationException(
                "Import has not been validated. Please validate before importing.");
        }

        // Resolve the exercise and ensure an active MSEL exists
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
            // Auto-create a MSEL if the exercise has none
            msel = new Cadence.Core.Models.Entities.Msel
            {
                Id = Guid.NewGuid(),
                Name = "Primary MSEL",
                Description = "Automatically created during import",
                Version = 1,
                IsActive = true,
                ExerciseId = exercise.Id,
                OrganizationId = exercise.OrganizationId
            };
            _context.Msels.Add(msel);

            // Set the exercise's ActiveMselId so the API can find the injects
            exercise.ActiveMselId = msel.Id;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Auto-created MSEL {MselId} for exercise {ExerciseId}", msel.Id, exercise.Id);
        }
        else if (exercise.ActiveMselId == null)
        {
            // Fix: existing active MSEL but ActiveMselId was not set on the exercise
            exercise.ActiveMselId = msel.Id;
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Fixed ActiveMselId for exercise {ExerciseId} to MSEL {MselId}", exercise.Id, msel.Id);
        }

        var errors = new List<string>();
        var warnings = new List<string>();
        var injectsCreated = 0;
        var injectsUpdated = 0;
        var rowsSkipped = 0;
        var phasesCreated = 0;

        // Replace strategy: soft-delete all existing injects first
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

        // Load lookup data once outside the loop
        var phases = await _context.Phases
            .Where(p => p.ExerciseId == request.ExerciseId && !p.IsDeleted)
            .ToDictionaryAsync(p => p.Name.ToLowerInvariant());

        var deliveryMethods = await _context.DeliveryMethods
            .Where(d => d.IsActive)
            .ToDictionaryAsync(d => d.Name.ToLowerInvariant());

        // Determine which rows to process
        var rowsToImport = request.SkipErrorRows
            ? session.ValidationResults.Where(r => r.Status != "Error")
            : session.ValidationResults;

        // Read initial max values once before the loop
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
                currentMaxSequence++;
                currentMaxInjectNumber++;

                var inject = new Inject
                {
                    Id = Guid.NewGuid(),
                    MselId = msel.Id,
                    InjectNumber = currentMaxInjectNumber,
                    Sequence = currentMaxSequence,
                    Status = InjectStatus.Draft,
                    TriggerType = TriggerType.Manual
                };

                MapRowToInject(inject, row.Values, session.Mappings, phases, deliveryMethods,
                    request.ExerciseId, exercise.OrganizationId, request.CreateMissingPhases,
                    ref phasesCreated, warnings);

                // Title fallback: populate from Description when Title is empty
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

    /// <inheritdoc />
    public Task CancelImportAsync(Guid sessionId)
    {
        var session = _sessionStore.RemoveSession(sessionId);
        if (session != null && File.Exists(session.TempFilePath))
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

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ImportSessionStateDto?> GetSessionStateAsync(Guid sessionId)
    {
        var session = _sessionStore.GetSession(sessionId);

        if (session == null)
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

    /// <inheritdoc />
    public Task<UpdateRowsResultDto> UpdateRowsAsync(UpdateRowsRequestDto request)
    {
        var session = GetSession(request.SessionId);

        if (session.ValidationResults == null || session.Mappings == null)
        {
            throw new InvalidOperationException(
                "No validation results. Please validate before updating rows.");
        }

        // Lock session state to prevent concurrent read-modify-write on ValidationResults
        lock (session.SyncRoot)
        {
            // Build rowNumber → index lookup
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
                var issues = RowValidationService.ValidateSingleRow(existing.Values, session.Mappings);
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

            var validRows   = session.ValidationResults.Count(r => r.Status == "Valid");
            var errorRows   = session.ValidationResults.Count(r => r.Status == "Error");
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

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Retrieves and validates a session, extending its expiry on access.
    /// Throws <see cref="InvalidOperationException"/> when the session is missing or expired.
    /// </summary>
    private ImportSession GetSession(Guid sessionId)
    {
        var session = _sessionStore.GetSession(sessionId);

        if (session == null)
        {
            throw new InvalidOperationException("Import session not found. Please upload a file first.");
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            _ = CancelImportAsync(sessionId);
            throw new InvalidOperationException("Import session has expired. Please upload the file again.");
        }

        // Rolling expiry: each interaction extends the session
        session.ExpiresAt = DateTime.UtcNow.AddMinutes(SessionTimeoutMinutes);

        return session;
    }

    private static string GetTempFilePath(Guid sessionId)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "cadence-imports");
        Directory.CreateDirectory(tempDir);
        return Path.Combine(tempDir, $"{sessionId}.tmp");
    }

    private async Task CleanupExpiredSessionsAsync()
    {
        var expired = _sessionStore.CleanupExpiredSessions();
        foreach (var session in expired)
        {
            if (File.Exists(session.TempFilePath))
            {
                try
                {
                    File.Delete(session.TempFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temp file {Path} during cleanup", session.TempFilePath);
                }
            }
        }

        await Task.CompletedTask;
    }

    // -----------------------------------------------------------------------
    // Worksheet selection helpers (format-specific)
    // -----------------------------------------------------------------------

    private async Task<WorksheetSelectionResultDto> ProcessXlsxSelectionAsync(
        ImportSession session,
        SelectWorksheetRequestDto request)
    {
        await using var fileStream = File.OpenRead(session.TempFilePath);
        using var workbook = new XLWorkbook(fileStream);

        var worksheet = workbook.Worksheet(request.WorksheetIndex + 1); // ClosedXML is 1-based
        var worksheetInfo = session.Worksheets[request.WorksheetIndex];

        var columns = new List<ColumnInfoDto>();
        var headerRow = worksheet.Row(request.HeaderRow);
        var lastColumn = Math.Min(worksheet.LastColumnUsed()?.ColumnNumber() ?? 0, MaxColumns);

        for (int col = 1; col <= lastColumn; col++)
        {
            var headerText = headerRow.Cell(col).GetString().Trim();
            if (string.IsNullOrEmpty(headerText))
            {
                headerText = $"Column {ExcelFileReader.GetColumnLetter(col)}";
            }

            var columnData = ExcelFileReader.GetColumnData(worksheet, col, request.DataStartRow);
            columns.Add(new ColumnInfoDto
            {
                Index = col - 1,
                Letter = ExcelFileReader.GetColumnLetter(col),
                Header = headerText,
                DataType = columnData.DataType,
                SampleValues = columnData.SampleValues,
                FillRate = columnData.FillRate
            });
        }

        var previewRows = new List<Dictionary<string, object?>>();
        var lastDataRow = worksheet.LastRowUsed()?.RowNumber() ?? request.DataStartRow;
        var previewEnd = Math.Min(request.DataStartRow + request.PreviewRowCount - 1, lastDataRow);

        for (int row = request.DataStartRow; row <= previewEnd; row++)
        {
            var rowData = new Dictionary<string, object?>();
            for (int col = 1; col <= lastColumn; col++)
            {
                var header = columns[col - 1].Header;
                rowData[header] = ExcelFileReader.GetCellValue(worksheet.Cell(row, col));
            }
            previewRows.Add(rowData);
        }

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

    private async Task<WorksheetSelectionResultDto> ProcessXlsSelectionAsync(
        ImportSession session,
        SelectWorksheetRequestDto request)
    {
        await using var fileStream = File.OpenRead(session.TempFilePath);
        var dataSet = LegacyExcelReader.ReadToDataSet(fileStream);
        var table = dataSet.Tables[request.WorksheetIndex];
        var worksheetInfo = session.Worksheets[request.WorksheetIndex];

        var columns = new List<ColumnInfoDto>();
        var headerRowIndex = request.HeaderRow - 1; // DataTable is 0-based
        var lastColumn = Math.Min(LegacyExcelReader.GetLastUsedColumn(table) + 1, MaxColumns);

        if (headerRowIndex < table.Rows.Count)
        {
            var headerDataRow = table.Rows[headerRowIndex];

            for (int col = 0; col < lastColumn; col++)
            {
                var headerText = LegacyExcelReader.GetCellString(headerDataRow, col);
                if (string.IsNullOrEmpty(headerText))
                {
                    headerText = $"Column {ExcelFileReader.GetColumnLetter(col + 1)}";
                }

                var columnData = ExcelFileReader.GetDataTableColumnData(table, col, request.DataStartRow - 1);
                columns.Add(new ColumnInfoDto
                {
                    Index = col,
                    Letter = ExcelFileReader.GetColumnLetter(col + 1),
                    Header = headerText,
                    DataType = columnData.DataType,
                    SampleValues = columnData.SampleValues,
                    FillRate = columnData.FillRate
                });
            }
        }

        var previewRows = new List<Dictionary<string, object?>>();
        var startRowIndex = request.DataStartRow - 1; // DataStartRow is 1-based
        var lastDataRow = LegacyExcelReader.GetLastUsedRow(table);
        var previewEnd = Math.Min(startRowIndex + request.PreviewRowCount - 1, lastDataRow);

        for (int row = startRowIndex; row <= previewEnd; row++)
        {
            var dataRow = table.Rows[row];
            var rowData = new Dictionary<string, object?>();
            for (int col = 0; col < lastColumn && col < columns.Count; col++)
            {
                rowData[columns[col].Header] = LegacyExcelReader.GetCellValue(dataRow, col);
            }
            previewRows.Add(rowData);
        }

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

    private async Task<WorksheetSelectionResultDto> ProcessCsvSelectionAsync(
        ImportSession session,
        SelectWorksheetRequestDto request)
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

        var headers = ExcelFileReader.ParseCsvLine(lines[request.HeaderRow - 1]);
        for (int i = 0; i < headers.Count; i++)
        {
            columns.Add(new ColumnInfoDto
            {
                Index = i,
                Letter = ExcelFileReader.GetColumnLetter(i + 1),
                Header = headers[i],
                DataType = "text",
                SampleValues = Array.Empty<string>(),
                FillRate = 100
            });
        }

        var startRow = Math.Max(1, request.DataStartRow);
        var endRow = Math.Min(startRow + request.PreviewRowCount - 1, lines.Length);
        for (int row = startRow; row <= endRow; row++)
        {
            var values = ExcelFileReader.ParseCsvLine(lines[row - 1]);
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

    // -----------------------------------------------------------------------
    // Inject entity mapping
    // -----------------------------------------------------------------------

    /// <summary>
    /// Maps row values from the import session into the properties of an <see cref="Inject"/>
    /// entity according to the configured column mappings.
    /// </summary>
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
            if (!values.TryGetValue(mapping.CadenceField, out var value) || RowValidationService.IsEmpty(value))
            {
                continue;
            }

            var stringValue = value?.ToString()?.Trim();

            switch (mapping.CadenceField)
            {
                case "InjectNumber":
                    // InjectNumber is auto-assigned; store the source value as a reference
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
                        // Populate ScenarioDay from the date portion if not already set
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
                    if (stringValue != null
                        && deliveryMethods.TryGetValue(stringValue.ToLowerInvariant(), out var method))
                    {
                        inject.DeliveryMethodId = method.Id;
                    }
                    else if (!string.IsNullOrEmpty(stringValue)
                             && ColumnMappingStrategy.DeliveryMethodSynonyms.TryGetValue(stringValue, out var canonicalName)
                             && deliveryMethods.TryGetValue(canonicalName.ToLowerInvariant(), out var synonymMethod))
                    {
                        inject.DeliveryMethodId = synonymMethod.Id;
                    }
                    else if (!string.IsNullOrEmpty(stringValue))
                    {
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
                            var newPhase = new Phase
                            {
                                Id = Guid.NewGuid(),
                                ExerciseId = exerciseId,
                                OrganizationId = organizationId,
                                Name = stringValue,
                                Sequence = phases.Count + 1
                            };
                            _context.Phases.Add(newPhase);
                            phases[phaseLower] = newPhase;
                            inject.PhaseId = newPhase.Id;
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
                        if (ColumnMappingStrategy.InjectTypeSynonyms.TryGetValue(stringValue, out var injectType))
                        {
                            inject.InjectType = injectType;
                        }
                        else if (ColumnMappingStrategy.TriggerTypeLikeValues.Contains(stringValue)
                                 || ColumnMappingStrategy.TriggerTypeSynonyms.ContainsKey(stringValue))
                        {
                            warnings.Add(
                                $"Row value '{stringValue}' in Inject Type column looks like a trigger type " +
                                "(e.g., Controller Action, Player Action). Consider mapping this column to " +
                                "Trigger Type instead. Defaulting to Standard.");
                            inject.InjectType = InjectType.Standard;
                        }
                        else if (ColumnMappingStrategy.DeliveryMethodLikeValues.Contains(stringValue))
                        {
                            warnings.Add(
                                $"Row value '{stringValue}' in Inject Type column looks like a delivery method. " +
                                "Consider mapping to Delivery Method instead. Defaulting to Standard.");
                            inject.InjectType = InjectType.Standard;
                        }
                        else
                        {
                            warnings.Add($"Unrecognized inject type '{stringValue}', defaulting to Standard.");
                            inject.InjectType = InjectType.Standard;
                        }
                    }
                    break;

                case "TriggerType":
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        if (ColumnMappingStrategy.TriggerTypeSynonyms.TryGetValue(stringValue, out var triggerType))
                        {
                            inject.TriggerType = triggerType;
                        }
                        else
                        {
                            warnings.Add($"Unrecognized trigger type '{stringValue}', defaulting to Manual.");
                            inject.TriggerType = TriggerType.Manual;
                        }
                    }
                    break;
            }
        }
    }
}
