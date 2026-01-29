using Cadence.Core.Features.Capabilities.Models.DTOs;
using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Capabilities.Mappers;

/// <summary>
/// Extension methods for mapping between Capability entity and DTOs.
/// </summary>
public static class CapabilityMapper
{
    /// <summary>
    /// Maps a Capability entity to a CapabilityDto.
    /// </summary>
    public static CapabilityDto ToDto(this Capability entity) => new(
        entity.Id,
        entity.OrganizationId,
        entity.Name,
        entity.Description,
        entity.Category,
        entity.SortOrder,
        entity.IsActive,
        entity.SourceLibrary,
        entity.CreatedAt,
        entity.UpdatedAt
    );

    /// <summary>
    /// Maps a Capability entity to a CapabilitySummaryDto.
    /// </summary>
    public static CapabilitySummaryDto ToSummaryDto(this Capability entity) => new(
        entity.Id,
        entity.Name,
        entity.Category,
        entity.IsActive
    );

    /// <summary>
    /// Maps a CreateCapabilityRequest to a new Capability entity.
    /// </summary>
    public static Capability ToEntity(this CreateCapabilityRequest request, Guid organizationId) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        Name = request.Name.Trim(),
        Description = request.Description?.Trim(),
        Category = request.Category?.Trim(),
        SortOrder = request.SortOrder,
        IsActive = true,
        SourceLibrary = request.SourceLibrary?.Trim(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    /// <summary>
    /// Updates a Capability entity from an UpdateCapabilityRequest.
    /// </summary>
    public static void UpdateFromRequest(this Capability entity, UpdateCapabilityRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.Category = request.Category?.Trim();
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}
