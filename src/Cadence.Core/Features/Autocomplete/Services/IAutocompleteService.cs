namespace Cadence.Core.Features.Autocomplete.Services;

/// <summary>
/// Service interface for organization-scoped autocomplete suggestions.
/// Provides "learn-as-you-go" suggestions based on previously used values.
/// </summary>
public interface IAutocompleteService
{
    /// <summary>
    /// Resolves the organization ID for an exercise by its ID.
    /// Returns null if the exercise does not exist.
    /// Used for pre-condition checks and organization-scoping of autocomplete queries.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The organization ID, or null if the exercise is not found</returns>
    Task<Guid?> GetExerciseOrganizationIdAsync(Guid exerciseId, CancellationToken ct = default);


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

    /// <summary>
    /// Gets historical values for a field, excluding curated and blocked values.
    /// Used by the management page to show blockable historical suggestions.
    /// </summary>
    Task<List<string>> GetHistoricalValuesAsync(Guid organizationId, string fieldName, int limit = 50);
}
