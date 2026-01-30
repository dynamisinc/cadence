using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;

namespace Cadence.WebApi.Services;

/// <summary>
/// Implementation of ICurrentOrganizationContext that reads organization context from HTTP request claims.
/// Provides access to the current user's organization ID and role from JWT claims.
/// </summary>
public class CurrentOrganizationContext : ICurrentOrganizationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentOrganizationContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public Guid? CurrentOrganizationId
    {
        get
        {
            var orgIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("org_id")?.Value;
            return Guid.TryParse(orgIdClaim, out var id) ? id : null;
        }
    }

    /// <inheritdoc />
    public OrgRole? CurrentOrgRole
    {
        get
        {
            var orgRoleClaim = _httpContextAccessor.HttpContext?.User.FindFirst("org_role")?.Value;
            return Enum.TryParse<OrgRole>(orgRoleClaim, out var role) ? role : null;
        }
    }

    /// <inheritdoc />
    public bool IsSysAdmin
    {
        get
        {
            var systemRoleClaim = _httpContextAccessor.HttpContext?.User.FindFirst("SystemRole")?.Value;
            return systemRoleClaim == SystemRole.Admin.ToString();
        }
    }
}
