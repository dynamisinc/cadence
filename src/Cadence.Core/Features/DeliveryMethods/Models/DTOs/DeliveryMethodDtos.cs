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
/// Request DTO for creating a new delivery method. Admin only.
/// </summary>
public class CreateDeliveryMethodRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int SortOrder { get; init; } = 0;
    public bool IsOther { get; init; } = false;
}

/// <summary>
/// Request DTO for updating an existing delivery method. Admin only.
/// </summary>
public class UpdateDeliveryMethodRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int SortOrder { get; init; } = 0;
    public bool IsActive { get; init; } = true;
    public bool IsOther { get; init; } = false;
}

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
