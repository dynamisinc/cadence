namespace Cadence.Core.Features.Autocomplete.Models.DTOs;

public record OrganizationSuggestionDto(
    Guid Id,
    string FieldName,
    string Value,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public class CreateSuggestionRequest
{
    public string FieldName { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public int SortOrder { get; init; } = 0;
}

public class UpdateSuggestionRequest
{
    public string Value { get; init; } = string.Empty;
    public int SortOrder { get; init; } = 0;
    public bool IsActive { get; init; } = true;
}

public class BulkCreateSuggestionsRequest
{
    public string FieldName { get; init; } = string.Empty;
    public List<string> Values { get; init; } = new();
}

public record BulkCreateSuggestionsResult(
    int TotalProvided,
    int Created,
    int SkippedDuplicates
);
