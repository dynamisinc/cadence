using System.Linq.Expressions;
using Cadence.Core.Data;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

using InjectEntity = Cadence.Core.Models.Entities.Inject;

namespace Cadence.Core.Features.Autocomplete.Services;

/// <summary>
/// Service for organization-scoped autocomplete suggestions.
/// Merges admin-managed suggestions (first, by SortOrder) with historical inject data
/// (deduplicated, by frequency).
/// </summary>
public class AutocompleteService : IAutocompleteService
{
    private readonly AppDbContext _context;

    public AutocompleteService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetTrackSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20)
    {
        return await GetSuggestionsAsync(organizationId, i => i.Track, SuggestionFieldNames.Track, filter, limit);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetTargetSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20)
    {
        return await GetSuggestionsAsync(organizationId, i => i.Target, SuggestionFieldNames.Target, filter, limit);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetSourceSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20)
    {
        return await GetSuggestionsAsync(organizationId, i => i.Source, SuggestionFieldNames.Source, filter, limit);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetLocationNameSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20)
    {
        return await GetSuggestionsAsync(organizationId, i => i.LocationName, SuggestionFieldNames.LocationName, filter, limit);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetLocationTypeSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20)
    {
        return await GetSuggestionsAsync(organizationId, i => i.LocationType, SuggestionFieldNames.LocationType, filter, limit);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetResponsibleControllerSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20)
    {
        return await GetSuggestionsAsync(organizationId, i => i.ResponsibleController, SuggestionFieldNames.ResponsibleController, filter, limit);
    }

    /// <summary>
    /// Merges managed suggestions (by SortOrder) with historical inject data (by frequency).
    /// Managed suggestions appear first; historical values are deduplicated against them.
    /// </summary>
    private async Task<List<string>> GetSuggestionsAsync(
        Guid organizationId,
        Expression<Func<InjectEntity, string?>> fieldSelector,
        string fieldName,
        string? filter,
        int limit)
    {
        // 1. Get managed suggestions (active, ordered by SortOrder)
        var managedQuery = _context.OrganizationSuggestions
            .Where(s => s.OrganizationId == organizationId
                     && s.FieldName == fieldName
                     && s.IsActive);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLower();
            managedQuery = managedQuery.Where(s => s.Value.ToLower().Contains(lowerFilter));
        }

        var managedSuggestions = await managedQuery
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Value)
            .Select(s => s.Value)
            .ToListAsync();

        // If managed suggestions already fill the limit, return early
        if (managedSuggestions.Count >= limit)
            return managedSuggestions.Take(limit).ToList();

        // 2. Get historical suggestions from inject data
        var historicalSuggestions = await GetHistoricalSuggestionsAsync(
            organizationId, fieldSelector, filter, limit);

        // 3. Merge: managed first, then historical (deduplicated)
        var managedSet = new HashSet<string>(managedSuggestions, StringComparer.OrdinalIgnoreCase);
        var merged = new List<string>(managedSuggestions);

        foreach (var historical in historicalSuggestions)
        {
            if (merged.Count >= limit) break;
            if (!managedSet.Contains(historical))
            {
                merged.Add(historical);
            }
        }

        return merged;
    }

    /// <summary>
    /// Queries historical inject data for previously used values, ordered by frequency.
    /// </summary>
    private async Task<List<string>> GetHistoricalSuggestionsAsync(
        Guid organizationId,
        Expression<Func<InjectEntity, string?>> fieldSelector,
        string? filter,
        int limit)
    {
        // Get all exercise IDs for this organization
        var exerciseIds = await _context.Exercises
            .Where(e => e.OrganizationId == organizationId)
            .Select(e => e.Id)
            .ToListAsync();

        if (exerciseIds.Count == 0)
            return new List<string>();

        // Get all MSEL IDs for these exercises
        var mselIds = await _context.Msels
            .Where(m => exerciseIds.Contains(m.ExerciseId))
            .Select(m => m.Id)
            .ToListAsync();

        if (mselIds.Count == 0)
            return new List<string>();

        // Query for unique values, ordered by frequency
        var query = _context.Injects
            .Where(i => mselIds.Contains(i.MselId))
            .Select(fieldSelector)
            .Where(v => v != null && v != "");

        // Apply filter if provided
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var lowerFilter = filter.ToLower();
            query = query.Where(v => v!.ToLower().Contains(lowerFilter));
        }

        // Group by value, count occurrences, and order by frequency
        var suggestions = await query
            .GroupBy(v => v)
            .Select(g => new { Value = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Value)
            .Take(limit)
            .Select(x => x.Value!)
            .ToListAsync();

        return suggestions;
    }
}
