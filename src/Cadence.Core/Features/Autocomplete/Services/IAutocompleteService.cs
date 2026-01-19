namespace Cadence.Core.Features.Autocomplete.Services;

/// <summary>
/// Service interface for organization-scoped autocomplete suggestions.
/// Provides "learn-as-you-go" suggestions based on previously used values.
/// </summary>
public interface IAutocompleteService
{
    /// <summary>
    /// Gets track suggestions for an organization, ordered by usage frequency.
    /// </summary>
    Task<List<string>> GetTrackSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20);

    /// <summary>
    /// Gets target suggestions for an organization, ordered by usage frequency.
    /// </summary>
    Task<List<string>> GetTargetSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20);

    /// <summary>
    /// Gets source suggestions for an organization, ordered by usage frequency.
    /// </summary>
    Task<List<string>> GetSourceSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20);

    /// <summary>
    /// Gets location name suggestions for an organization, ordered by usage frequency.
    /// </summary>
    Task<List<string>> GetLocationNameSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20);

    /// <summary>
    /// Gets location type suggestions for an organization, ordered by usage frequency.
    /// </summary>
    Task<List<string>> GetLocationTypeSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20);

    /// <summary>
    /// Gets responsible controller suggestions for an organization, ordered by usage frequency.
    /// </summary>
    Task<List<string>> GetResponsibleControllerSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20);
}
