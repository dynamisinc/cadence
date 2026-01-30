namespace Cadence.Core.Models.Entities;

/// <summary>
/// Interface for entities that belong to a specific organization.
/// Entities implementing this interface will automatically have organization-scoped
/// query filters applied, ensuring data isolation between organizations.
///
/// The global query filter will:
/// - Return only entities matching the current user's organization context
/// - Allow SysAdmins to see all organizations
/// - Return empty results for users without organization context
///
/// Use <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.IgnoreQueryFilters"/>
/// when explicit cross-organization access is needed (e.g., admin operations).
/// </summary>
public interface IOrganizationScoped
{
    /// <summary>
    /// The ID of the organization that owns this entity.
    /// This is the primary key for organization-based data isolation.
    /// </summary>
    Guid OrganizationId { get; set; }
}
