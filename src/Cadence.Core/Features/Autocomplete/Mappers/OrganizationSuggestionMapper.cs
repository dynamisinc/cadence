using Cadence.Core.Features.Autocomplete.Models.DTOs;
using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Autocomplete.Mappers;

public static class OrganizationSuggestionMapper
{
    public static OrganizationSuggestionDto ToDto(this OrganizationSuggestion entity) => new(
        entity.Id,
        entity.FieldName,
        entity.Value,
        entity.SortOrder,
        entity.IsActive,
        entity.CreatedAt,
        entity.UpdatedAt
    );
}
