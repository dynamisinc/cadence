using System.Net.Mail;
using System.Text;
using Cadence.Core.Features.BulkParticipantImport.Models.DTOs;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.BulkParticipantImport.Services;

/// <summary>
/// Parses CSV and XLSX files containing participant data.
/// Handles column synonym detection, row validation, and flexible header matching.
/// </summary>
public class ParticipantFileParser : IParticipantFileParser
{
    private readonly ILogger<ParticipantFileParser> _logger;

    private const int MaxRows = 500;

    // Column name patterns for auto-mapping (case-insensitive)
    private static readonly Dictionary<string, string[]> ColumnPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Email", new[] { "email", "e-mail", "email address", "e-mail address", "participant email", "contact email" } },
        { "ExerciseRole", new[] { "exercise role", "hseep role", "role", "participant role", "assignment", "exrole" } },
        { "DisplayName", new[] { "display name", "name", "full name", "participant name", "displayname", "contact name" } },
        { "OrganizationRole", new[] { "organization role", "org role", "orgrole", "organization" } },
    };

    // Exercise role value synonyms (case-insensitive)
    private static readonly Dictionary<string, ExerciseRole> ExerciseRoleSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        { "exercisedirector", ExerciseRole.ExerciseDirector },
        { "exercise director", ExerciseRole.ExerciseDirector },
        { "director", ExerciseRole.ExerciseDirector },
        { "ed", ExerciseRole.ExerciseDirector },
        { "controller", ExerciseRole.Controller },
        { "ctrl", ExerciseRole.Controller },
        { "evaluator", ExerciseRole.Evaluator },
        { "eval", ExerciseRole.Evaluator },
        { "observer", ExerciseRole.Observer },
        { "obs", ExerciseRole.Observer },
        { "administrator", ExerciseRole.Administrator },
        { "admin", ExerciseRole.Administrator },
    };

    // OrgRole value synonyms (case-insensitive)
    private static readonly Dictionary<string, OrgRole> OrgRoleSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        { "orgadmin", OrgRole.OrgAdmin },
        { "org admin", OrgRole.OrgAdmin },
        { "orgmanager", OrgRole.OrgManager },
        { "org manager", OrgRole.OrgManager },
        { "orguser", OrgRole.OrgUser },
        { "org user", OrgRole.OrgUser },
    };

    public ParticipantFileParser(ILogger<ParticipantFileParser> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FileParseResult> ParseAsync(Stream fileStream, string fileName)
    {
        var sessionId = Guid.NewGuid();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        try
        {
            if (extension == ".csv")
            {
                return await ParseCsvAsync(fileStream, fileName, sessionId);
            }
            else if (extension == ".xlsx" || extension == ".xls")
            {
                return await ParseXlsxAsync(fileStream, fileName, sessionId);
            }
            else
            {
                return CreateErrorResult(sessionId, fileName, $"Unsupported file format: {extension}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse participant file {FileName}", fileName);
            return CreateErrorResult(sessionId, fileName, $"Failed to parse file: {ex.Message}");
        }
    }

    // ============================================================================
    // CSV Parsing
    // ============================================================================

    private async Task<FileParseResult> ParseCsvAsync(Stream fileStream, string fileName, Guid sessionId)
    {
        var lines = await ReadCsvLinesAsync(fileStream);

        if (lines.Count == 0)
        {
            return CreateErrorResult(sessionId, fileName, "File is empty");
        }

        // Detect delimiter
        var delimiter = DetectDelimiter(lines[0]);

        // Parse header row
        var headers = ParseCsvLine(lines[0], delimiter);
        var (columnMappings, mappingErrors, warnings) = MapColumns(headers);

        if (mappingErrors.Count > 0)
        {
            return new FileParseResult
            {
                SessionId = sessionId,
                FileName = fileName,
                TotalRows = 0,
                ColumnMappings = columnMappings,
                Rows = [],
                Warnings = warnings,
                Errors = mappingErrors
            };
        }

        // Parse data rows
        var rows = new List<ParsedParticipantRow>();
        for (int i = 1; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue; // Skip empty rows

            var values = ParseCsvLine(line, delimiter);
            var row = ParseDataRow(i + 1, values, columnMappings);
            rows.Add(row);
        }

        // Check max rows
        if (rows.Count > MaxRows)
        {
            return new FileParseResult
            {
                SessionId = sessionId,
                FileName = fileName,
                TotalRows = rows.Count,
                ColumnMappings = columnMappings,
                Rows = [],
                Warnings = warnings,
                Errors = new List<string> { $"File exceeds maximum of {MaxRows} rows" }
            };
        }

        // Validate rows (including duplicate check)
        var validatedRows = ValidateRows(rows);

        return new FileParseResult
        {
            SessionId = sessionId,
            FileName = fileName,
            TotalRows = validatedRows.Count,
            ColumnMappings = columnMappings,
            Rows = validatedRows,
            Warnings = warnings,
            Errors = []
        };
    }

    private async Task<List<string>> ReadCsvLinesAsync(Stream fileStream)
    {
        var lines = new List<string>();

        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            lines.Add(line);
        }

        return lines;
    }

    private char DetectDelimiter(string headerLine)
    {
        var commaCount = headerLine.Count(c => c == ',');
        var semicolonCount = headerLine.Count(c => c == ';');
        var tabCount = headerLine.Count(c => c == '\t');

        if (semicolonCount > commaCount && semicolonCount > tabCount)
            return ';';
        if (tabCount > commaCount && tabCount > semicolonCount)
            return '\t';

        return ','; // Default to comma
    }

    private List<string> ParseCsvLine(string line, char delimiter)
    {
        var values = new List<string>();
        var currentValue = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == delimiter && !inQuotes)
            {
                values.Add(currentValue.ToString().Trim());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        // Add the last value
        values.Add(currentValue.ToString().Trim());

        return values;
    }

    // ============================================================================
    // XLSX Parsing
    // ============================================================================

    private async Task<FileParseResult> ParseXlsxAsync(Stream fileStream, string fileName, Guid sessionId)
    {
        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.First();

            // Find header row
            var headerRowIndex = FindHeaderRow(worksheet);
            if (headerRowIndex == -1)
            {
                return CreateErrorResult(sessionId, fileName, "Could not find header row with required columns");
            }

            // Parse headers
            var headers = new List<string>();
            var headerRow = worksheet.Row(headerRowIndex);
            var lastColUsed = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

            for (int col = 1; col <= lastColUsed; col++)
            {
                var cellValue = headerRow.Cell(col).GetString();
                headers.Add(cellValue);
            }

            var (columnMappings, mappingErrors, warnings) = MapColumns(headers);

            if (mappingErrors.Count > 0)
            {
                return new FileParseResult
                {
                    SessionId = sessionId,
                    FileName = fileName,
                    TotalRows = 0,
                    ColumnMappings = columnMappings,
                    Rows = [],
                    Warnings = warnings,
                    Errors = mappingErrors
                };
            }

            // Parse data rows
            var rows = new List<ParsedParticipantRow>();
            var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 0;

            for (int rowIndex = headerRowIndex + 1; rowIndex <= lastRowUsed; rowIndex++)
            {
                var row = worksheet.Row(rowIndex);
                var values = new List<string>();

                for (int col = 1; col <= lastColUsed; col++)
                {
                    values.Add(row.Cell(col).GetString());
                }

                // Skip empty rows
                if (values.All(string.IsNullOrWhiteSpace))
                    continue;

                var parsedRow = ParseDataRow(rowIndex, values, columnMappings);
                rows.Add(parsedRow);
            }

            // Check max rows
            if (rows.Count > MaxRows)
            {
                return new FileParseResult
                {
                    SessionId = sessionId,
                    FileName = fileName,
                    TotalRows = rows.Count,
                    ColumnMappings = columnMappings,
                    Rows = [],
                    Warnings = warnings,
                    Errors = new List<string> { $"File exceeds maximum of {MaxRows} rows" }
                };
            }

            // Validate rows
            var validatedRows = ValidateRows(rows);

            return new FileParseResult
            {
                SessionId = sessionId,
                FileName = fileName,
                TotalRows = validatedRows.Count,
                ColumnMappings = columnMappings,
                Rows = validatedRows,
                Warnings = warnings,
                Errors = []
            };
        });
    }

    private int FindHeaderRow(IXLWorksheet worksheet)
    {
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        // Look for a row containing "Email" and "Exercise Role" (or synonyms)
        for (int rowIndex = 1; rowIndex <= Math.Min(lastRowUsed, 10); rowIndex++)
        {
            var row = worksheet.Row(rowIndex);
            var lastColUsed = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            var cellValues = new List<string>();

            for (int col = 1; col <= lastColUsed; col++)
            {
                cellValues.Add(row.Cell(col).GetString());
            }

            var (_, errors, _) = MapColumns(cellValues);
            if (errors.Count == 0)
            {
                return rowIndex; // Found header row
            }
        }

        return -1; // Header not found
    }

    // ============================================================================
    // Column Mapping
    // ============================================================================

    private (List<ColumnMapping> mappings, List<string> errors, List<string> warnings) MapColumns(List<string> headers)
    {
        var mappings = new List<ColumnMapping>();
        var errors = new List<string>();
        var warnings = new List<string>();
        var mappedFields = new HashSet<string>();

        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i];
            var normalizedHeader = NormalizeHeader(header);

            // Try to match against known patterns
            foreach (var pattern in ColumnPatterns)
            {
                var fieldName = pattern.Key;
                var synonyms = pattern.Value;

                if (synonyms.Any(s => NormalizeHeader(s) == normalizedHeader))
                {
                    if (mappedFields.Contains(fieldName))
                    {
                        warnings.Add($"Multiple columns matched field '{fieldName}'. Using the first match.");
                        continue;
                    }

                    mappings.Add(new ColumnMapping
                    {
                        OriginalHeader = header,
                        MappedField = fieldName,
                        ColumnIndex = i
                    });

                    mappedFields.Add(fieldName);
                    break;
                }
            }
        }

        // Check for required fields
        if (!mappedFields.Contains("Email"))
        {
            errors.Add("Required column 'Email' not found");
        }

        if (!mappedFields.Contains("ExerciseRole"))
        {
            errors.Add("Required column 'Exercise Role' not found");
        }

        return (mappings, errors, warnings);
    }

    private string NormalizeHeader(string header)
    {
        // Remove whitespace, underscores, hyphens
        return header.Replace(" ", "")
                     .Replace("_", "")
                     .Replace("-", "")
                     .ToLowerInvariant();
    }

    // ============================================================================
    // Row Parsing and Validation
    // ============================================================================

    private ParsedParticipantRow ParseDataRow(int rowNumber, List<string> values, List<ColumnMapping> columnMappings)
    {
        string? email = null;
        string? exerciseRole = null;
        string? displayName = null;
        string? organizationRole = null;

        foreach (var mapping in columnMappings)
        {
            var value = mapping.ColumnIndex < values.Count ? values[mapping.ColumnIndex] : "";

            switch (mapping.MappedField)
            {
                case "Email":
                    email = value;
                    break;
                case "ExerciseRole":
                    exerciseRole = value;
                    break;
                case "DisplayName":
                    displayName = value;
                    break;
                case "OrganizationRole":
                    organizationRole = value;
                    break;
            }
        }

        // Normalize roles
        var normalizedExerciseRole = NormalizeExerciseRole(exerciseRole);
        var normalizedOrgRole = NormalizeOrgRole(organizationRole);

        return new ParsedParticipantRow
        {
            RowNumber = rowNumber,
            Email = email ?? "",
            ExerciseRole = exerciseRole ?? "",
            NormalizedExerciseRole = normalizedExerciseRole,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName,
            OrganizationRole = string.IsNullOrWhiteSpace(organizationRole) ? null : organizationRole,
            NormalizedOrgRole = normalizedOrgRole,
            ValidationErrors = [] // Validation happens later
        };
    }

    private ExerciseRole? NormalizeExerciseRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return null;

        var normalizedRole = role.Trim().Replace(" ", "").ToLowerInvariant();

        if (ExerciseRoleSynonyms.TryGetValue(normalizedRole, out var exerciseRole))
        {
            return exerciseRole;
        }

        return null;
    }

    private OrgRole? NormalizeOrgRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return null;

        var normalizedRole = role.Trim().Replace(" ", "").ToLowerInvariant();

        if (OrgRoleSynonyms.TryGetValue(normalizedRole, out var orgRole))
        {
            return orgRole;
        }

        return null;
    }

    private List<ParsedParticipantRow> ValidateRows(List<ParsedParticipantRow> rows)
    {
        var validatedRows = new List<ParsedParticipantRow>();
        var emailCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Count email occurrences
        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.Email))
            {
                emailCounts[row.Email] = emailCounts.GetValueOrDefault(row.Email, 0) + 1;
            }
        }

        // Validate each row
        foreach (var row in rows)
        {
            var errors = new List<string>();

            // Validate email format
            if (string.IsNullOrWhiteSpace(row.Email))
            {
                errors.Add("Email is required");
            }
            else if (!IsValidEmail(row.Email))
            {
                errors.Add("Invalid email format");
            }
            else if (emailCounts.GetValueOrDefault(row.Email, 0) > 1)
            {
                errors.Add("Duplicate email in file");
            }

            // Validate exercise role
            if (string.IsNullOrWhiteSpace(row.ExerciseRole))
            {
                errors.Add("Exercise role is required");
            }
            else if (!row.NormalizedExerciseRole.HasValue)
            {
                var validRoles = string.Join(", ", Enum.GetNames<ExerciseRole>());
                errors.Add($"Invalid exercise role '{row.ExerciseRole}'. Valid roles: {validRoles}");
            }

            validatedRows.Add(row with { ValidationErrors = errors });
        }

        return validatedRows;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    private static FileParseResult CreateErrorResult(Guid sessionId, string fileName, string error)
    {
        return new FileParseResult
        {
            SessionId = sessionId,
            FileName = fileName,
            TotalRows = 0,
            ColumnMappings = [],
            Rows = [],
            Warnings = [],
            Errors = new List<string> { error }
        };
    }
}
