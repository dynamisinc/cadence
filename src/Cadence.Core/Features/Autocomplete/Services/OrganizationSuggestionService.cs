using Cadence.Core.Data;
using Cadence.Core.Features.Autocomplete.Mappers;
using Cadence.Core.Features.Autocomplete.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Autocomplete.Services;

/// <summary>
/// Service for CRUD operations on organization-curated autocomplete suggestions.
/// </summary>
public class OrganizationSuggestionService : IOrganizationSuggestionService
{
    private readonly AppDbContext _context;

    public OrganizationSuggestionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OrganizationSuggestionDto>> GetSuggestionsAsync(
        Guid organizationId, string fieldName, bool includeInactive = false)
    {
        if (!SuggestionFieldNames.IsValid(fieldName))
            throw new ArgumentException($"Invalid field name: {fieldName}", nameof(fieldName));

        var query = _context.OrganizationSuggestions
            .Where(s => s.OrganizationId == organizationId && s.FieldName == fieldName);

        if (!includeInactive)
            query = query.Where(s => s.IsActive);

        var suggestions = await query
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Value)
            .ToListAsync();

        return suggestions.Select(s => s.ToDto());
    }

    public async Task<OrganizationSuggestionDto?> GetSuggestionAsync(Guid organizationId, Guid id)
    {
        var suggestion = await _context.OrganizationSuggestions
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.Id == id);

        return suggestion?.ToDto();
    }

    public async Task<OrganizationSuggestionDto> CreateSuggestionAsync(
        Guid organizationId, CreateSuggestionRequest request)
    {
        if (!SuggestionFieldNames.IsValid(request.FieldName))
            throw new ArgumentException($"Invalid field name: {request.FieldName}");

        var trimmedValue = request.Value.Trim();
        if (string.IsNullOrEmpty(trimmedValue))
            throw new ArgumentException("Value cannot be empty.");

        // Check for duplicate
        var exists = await _context.OrganizationSuggestions
            .AnyAsync(s => s.OrganizationId == organizationId
                        && s.FieldName == request.FieldName
                        && s.Value.ToLowerInvariant() == trimmedValue.ToLowerInvariant());

        if (exists)
            throw new InvalidOperationException($"A suggestion with value '{trimmedValue}' already exists for this field.");

        var suggestion = new OrganizationSuggestion
        {
            OrganizationId = organizationId,
            FieldName = request.FieldName,
            Value = trimmedValue,
            SortOrder = request.SortOrder,
            IsActive = true,
        };

        _context.OrganizationSuggestions.Add(suggestion);
        await _context.SaveChangesAsync();

        return suggestion.ToDto();
    }

    public async Task<OrganizationSuggestionDto?> UpdateSuggestionAsync(
        Guid organizationId, Guid id, UpdateSuggestionRequest request)
    {
        var suggestion = await _context.OrganizationSuggestions
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.Id == id);

        if (suggestion == null)
            return null;

        var trimmedValue = request.Value.Trim();
        if (string.IsNullOrEmpty(trimmedValue))
            throw new ArgumentException("Value cannot be empty.");

        // Check for duplicate (excluding self)
        var duplicate = await _context.OrganizationSuggestions
            .AnyAsync(s => s.OrganizationId == organizationId
                        && s.FieldName == suggestion.FieldName
                        && s.Id != id
                        && s.Value.ToLowerInvariant() == trimmedValue.ToLowerInvariant());

        if (duplicate)
            throw new InvalidOperationException($"A suggestion with value '{trimmedValue}' already exists for this field.");

        suggestion.Value = trimmedValue;
        suggestion.SortOrder = request.SortOrder;
        suggestion.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return suggestion.ToDto();
    }

    public async Task<bool> DeleteSuggestionAsync(Guid organizationId, Guid id)
    {
        var suggestion = await _context.OrganizationSuggestions
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.Id == id);

        if (suggestion == null)
            return false;

        suggestion.IsDeleted = true;
        suggestion.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<BulkCreateSuggestionsResult> BulkCreateSuggestionsAsync(
        Guid organizationId, BulkCreateSuggestionsRequest request)
    {
        if (!SuggestionFieldNames.IsValid(request.FieldName))
            throw new ArgumentException($"Invalid field name: {request.FieldName}");

        // Clean and deduplicate input values
        var inputValues = request.Values
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrEmpty(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (inputValues.Count == 0)
            return new BulkCreateSuggestionsResult(request.Values.Count, 0, request.Values.Count);

        // Get existing values for this field (case-insensitive comparison)
        var existingValues = await _context.OrganizationSuggestions
            .Where(s => s.OrganizationId == organizationId && s.FieldName == request.FieldName)
            .Select(s => s.Value.ToLowerInvariant())
            .ToListAsync();

        var existingSet = new HashSet<string>(existingValues, StringComparer.OrdinalIgnoreCase);

        // Get max sort order to append after existing suggestions
        var maxSortOrder = await _context.OrganizationSuggestions
            .Where(s => s.OrganizationId == organizationId && s.FieldName == request.FieldName)
            .Select(s => (int?)s.SortOrder)
            .MaxAsync() ?? 0;

        var created = 0;
        var skipped = 0;

        foreach (var value in inputValues)
        {
            if (existingSet.Contains(value))
            {
                skipped++;
                continue;
            }

            maxSortOrder++;
            _context.OrganizationSuggestions.Add(new OrganizationSuggestion
            {
                OrganizationId = organizationId,
                FieldName = request.FieldName,
                Value = value,
                SortOrder = maxSortOrder,
                IsActive = true,
            });

            existingSet.Add(value);
            created++;
        }

        if (created > 0)
            await _context.SaveChangesAsync();

        return new BulkCreateSuggestionsResult(request.Values.Count, created, skipped);
    }

    public async Task ReorderSuggestionsAsync(Guid organizationId, string fieldName, List<Guid> orderedIds)
    {
        if (!SuggestionFieldNames.IsValid(fieldName))
            throw new ArgumentException($"Invalid field name: {fieldName}");

        var suggestions = await _context.OrganizationSuggestions
            .Where(s => s.OrganizationId == organizationId && s.FieldName == fieldName)
            .ToListAsync();

        var lookup = suggestions.ToDictionary(s => s.Id);

        for (var i = 0; i < orderedIds.Count; i++)
        {
            if (lookup.TryGetValue(orderedIds[i], out var suggestion))
            {
                suggestion.SortOrder = i;
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<OrganizationSuggestionDto> BlockValueAsync(
        Guid organizationId, BlockSuggestionRequest request)
    {
        if (!SuggestionFieldNames.IsValid(request.FieldName))
            throw new ArgumentException($"Invalid field name: {request.FieldName}");

        var trimmedValue = request.Value.Trim();
        if (string.IsNullOrEmpty(trimmedValue))
            throw new ArgumentException("Value cannot be empty.");

        // Check if already blocked or exists as a curated suggestion
        var existing = await _context.OrganizationSuggestions
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId
                                   && s.FieldName == request.FieldName
                                   && s.Value.ToLowerInvariant() == trimmedValue.ToLowerInvariant());

        if (existing != null)
        {
            if (existing.IsBlocked)
                throw new InvalidOperationException($"Value '{trimmedValue}' is already blocked.");

            throw new InvalidOperationException($"Value '{trimmedValue}' exists as a curated suggestion. Delete it instead.");
        }

        var blocked = new OrganizationSuggestion
        {
            OrganizationId = organizationId,
            FieldName = request.FieldName,
            Value = trimmedValue,
            SortOrder = 0,
            IsActive = false,
            IsBlocked = true,
        };

        _context.OrganizationSuggestions.Add(blocked);
        await _context.SaveChangesAsync();

        return blocked.ToDto();
    }

    public async Task<bool> UnblockAsync(Guid organizationId, Guid id)
    {
        var suggestion = await _context.OrganizationSuggestions
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId
                                   && s.Id == id
                                   && s.IsBlocked);

        if (suggestion == null)
            return false;

        suggestion.IsDeleted = true;
        suggestion.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }
}
