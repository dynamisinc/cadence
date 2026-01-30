using Cadence.Core.Features.Organizations.Models.DTOs;
using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Organizations.Mappers;

/// <summary>
/// Extension methods for mapping between Organization entities and DTOs.
/// </summary>
public static class OrganizationMapper
{
    /// <summary>
    /// Maps an Organization entity to a full OrganizationDto.
    /// </summary>
    public static OrganizationDto ToDto(this Organization organization)
    {
        return new OrganizationDto(
            organization.Id,
            organization.Name,
            organization.Slug,
            organization.Description,
            organization.ContactEmail,
            organization.Status.ToString(),
            organization.CreatedAt,
            organization.UpdatedAt
        );
    }

    /// <summary>
    /// Maps an Organization entity to an OrganizationListItemDto.
    /// </summary>
    /// <param name="organization">Organization entity</param>
    /// <param name="userCount">Number of active users in the organization</param>
    /// <param name="exerciseCount">Number of exercises in the organization</param>
    public static OrganizationListItemDto ToListItemDto(
        this Organization organization,
        int userCount,
        int exerciseCount)
    {
        return new OrganizationListItemDto(
            organization.Id,
            organization.Name,
            organization.Slug,
            organization.Status.ToString(),
            userCount,
            exerciseCount,
            organization.CreatedAt
        );
    }
}
