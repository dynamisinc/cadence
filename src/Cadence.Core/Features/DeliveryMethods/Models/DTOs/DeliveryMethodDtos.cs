using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.DeliveryMethods.Models.DTOs;

/// <summary>
/// DTO for delivery method lookup response (read operations).
/// </summary>
public record DeliveryMethodDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    int SortOrder,
    bool IsOther
);

/// <summary>
/// Extension methods for mapping between DeliveryMethodLookup entity and DTOs.
/// </summary>
public static class DeliveryMethodMapper
{
    public static DeliveryMethodDto ToDto(this DeliveryMethodLookup entity) => new(
        entity.Id,
        entity.Name,
        entity.Description,
        entity.IsActive,
        entity.SortOrder,
        entity.IsOther
    );
}
