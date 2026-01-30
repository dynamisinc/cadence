using Cadence.Core.Models.Entities;

namespace Cadence.Core.Hubs;

/// <summary>
/// Interface for accessing the current organization context.
/// Provides organization-scoped authorization information for the current request.
/// Implementation lives in WebApi layer, interface is in Core for service consumption.
/// </summary>
public interface ICurrentOrganizationContext
{
    /// <summary>
    /// Gets the ID of the current organization context.
    /// Null if user has no organization context (e.g., pending user).
    /// </summary>
    Guid? CurrentOrganizationId { get; }

    /// <summary>
    /// Gets the current user's role in the current organization.
    /// Null if user has no organization context.
    /// </summary>
    OrgRole? CurrentOrgRole { get; }

    /// <summary>
    /// Gets whether the current user is a system administrator.
    /// System admins bypass organization-level restrictions.
    /// </summary>
    bool IsSysAdmin { get; }
}
