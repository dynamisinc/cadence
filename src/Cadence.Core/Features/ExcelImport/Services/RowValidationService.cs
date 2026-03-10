using Cadence.Core.Features.ExcelImport.Models.DTOs;

namespace Cadence.Core.Features.ExcelImport.Services;

/// <summary>
/// Stateless row-level validation logic for the Excel import wizard.
/// All methods are static so that they can be called without DI resolution.
/// </summary>
internal static class RowValidationService
{
    /// <summary>
    /// Validates every row in <paramref name="rows"/> against the configured
    /// <paramref name="mappings"/> and returns one <see cref="RowValidationResultDto"/>
    /// per input row.
    /// </summary>
    /// <remarks>
    /// Row numbers start at 2 because the header is assumed to occupy row 1.
    /// </remarks>
    public static List<RowValidationResultDto> ValidateRows(
        List<Dictionary<string, object?>> rows,
        IReadOnlyList<ColumnMappingDto> mappings)
    {
        var results = new List<RowValidationResultDto>(rows.Count);
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
    public static List<ValidationIssueDto> ValidateSingleRow(
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

        // Validate ScheduledTime format
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

        // Validate Priority range
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
            if (!string.IsNullOrEmpty(injectTypeStr) && !ColumnMappingStrategy.InjectTypeSynonyms.ContainsKey(injectTypeStr))
            {
                if (ColumnMappingStrategy.TriggerTypeLikeValues.Contains(injectTypeStr)
                    || ColumnMappingStrategy.TriggerTypeSynonyms.ContainsKey(injectTypeStr))
                {
                    issues.Add(new ValidationIssueDto
                    {
                        Field = "InjectType",
                        Severity = "Warning",
                        Message = "This value looks like a Trigger Type (e.g., Controller Action, Player Action). Consider mapping this column to Trigger Type instead.",
                        OriginalValue = injectTypeStr
                    });
                }
                else if (ColumnMappingStrategy.DeliveryMethodLikeValues.Contains(injectTypeStr))
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
            if (!string.IsNullOrEmpty(triggerTypeStr) && !ColumnMappingStrategy.TriggerTypeSynonyms.ContainsKey(triggerTypeStr))
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

    /// <summary>
    /// Returns <c>true</c> when <paramref name="value"/> is null or an all-whitespace string.
    /// </summary>
    public static bool IsEmpty(object? value) =>
        value == null || (value is string s && string.IsNullOrWhiteSpace(s));
}
