using Cadence.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Autocomplete.Services;

/// <summary>
/// Service for organization-scoped autocomplete suggestions.
/// Queries inject data within the organization to find previously used values.
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
        return await GetSuggestionsAsync(organizationId, i => i.Track, filter, limit);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetTargetSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20)
    {
        return await GetSuggestionsAsync(organizationId, i => i.Target, filter, limit);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetSourceSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20)
    {
        return await GetSuggestionsAsync(organizationId, i => i.Source, filter, limit);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetLocationNameSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20)
    {
        return await GetSuggestionsAsync(organizationId, i => i.LocationName, filter, limit);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetLocationTypeSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20)
    {
        return await GetSuggestionsAsync(organizationId, i => i.LocationType, filter, limit);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetResponsibleControllerSuggestionsAsync(Guid organizationId, string? filter = null, int limit = 20)
    {
        return await GetSuggestionsAsync(organizationId, i => i.ResponsibleController, filter, limit);
    }

    /// <summary>
    /// Generic method to get suggestions for any inject field.
    /// Queries all injects within the organization's exercises and aggregates unique values.
    /// </summary>
    private async Task<List<string>> GetSuggestionsAsync(
        Guid organizationId,
        System.Linq.Expressions.Expression<Func<Models.Entities.Inject, string?>> fieldSelector,
        string? filter,
        int limit)
    {
        // Get all exercise IDs for this organization
        var exerciseIds = await _context.Exercises
            .Where(e => e.OrganizationId == organizationId)
            .Select(e => e.Id)
            .ToListAsync();

        if (exerciseIds.Count == 0)
        {
            return new List<string>();
        }

        // Get all MSEL IDs for these exercises
        var mselIds = await _context.Msels
            .Where(m => exerciseIds.Contains(m.ExerciseId))
            .Select(m => m.Id)
            .ToListAsync();

        if (mselIds.Count == 0)
        {
            return new List<string>();
        }

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
