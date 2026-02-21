using Cadence.Core.Features.Autocomplete.Models.DTOs;

namespace Cadence.Core.Features.Autocomplete.Services;

/// <summary>
/// Service for managing organization-curated autocomplete suggestions.
/// </summary>
public interface IOrganizationSuggestionService
{
    /// <summary>
    /// Get all suggestions for a field in the current organization.
    /// </summary>
    Task<IEnumerable<OrganizationSuggestionDto>> GetSuggestionsAsync(
        Guid organizationId, string fieldName, bool includeInactive = false);

    /// <summary>
    /// Get a single suggestion by ID.
    /// </summary>
    Task<OrganizationSuggestionDto?> GetSuggestionAsync(Guid organizationId, Guid id);

    /// <summary>
    /// Create a new managed suggestion.
    /// </summary>
    Task<OrganizationSuggestionDto> CreateSuggestionAsync(
        Guid organizationId, CreateSuggestionRequest request);

    /// <summary>
    /// Update an existing suggestion.
    /// </summary>
    Task<OrganizationSuggestionDto?> UpdateSuggestionAsync(
        Guid organizationId, Guid id, UpdateSuggestionRequest request);

    /// <summary>
    /// Soft-delete a suggestion.
    /// </summary>
    Task<bool> DeleteSuggestionAsync(Guid organizationId, Guid id);

    /// <summary>
    /// Bulk-create suggestions from a list of values.
    /// Skips duplicates within the field.
    /// </summary>
    Task<BulkCreateSuggestionsResult> BulkCreateSuggestionsAsync(
        Guid organizationId, BulkCreateSuggestionsRequest request);

    /// <summary>
    /// Reorder suggestions within a field.
    /// </summary>
    Task ReorderSuggestionsAsync(Guid organizationId, string fieldName, List<Guid> orderedIds);

    /// <summary>
    /// Block a historical value from appearing in autocomplete suggestions.
    /// Creates an OrganizationSuggestion with IsBlocked=true, IsActive=false.
    /// </summary>
    Task<OrganizationSuggestionDto> BlockValueAsync(Guid organizationId, BlockSuggestionRequest request);

    /// <summary>
    /// Unblock a previously blocked value by deleting the blocked entry.
    /// </summary>
    Task<bool> UnblockAsync(Guid organizationId, Guid id);
}
