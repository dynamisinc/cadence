using Cadence.Core.Data;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Cadence.Core.Data.Interceptors;

/// <summary>
/// EF Core interceptor that validates organization scope on all write operations.
/// Prevents creating or modifying entities in organizations the user doesn't have access to.
///
/// This provides defense-in-depth alongside query filters:
/// - Query filters protect reads (can't see other org's data)
/// - This interceptor protects writes (can't modify other org's data)
///
/// SysAdmins bypass this validation as they have cross-organization access.
///
/// Note: This interceptor uses IServiceProvider to resolve ICurrentOrganizationContext
/// at runtime because interceptors are singletons but org context is scoped.
/// </summary>
public class OrganizationValidationInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public OrganizationValidationInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets the current organization context from the scoped service provider.
    /// Returns null if no scope is active (e.g., during migrations).
    /// </summary>
    private ICurrentOrganizationContext? GetOrgContext()
    {
        try
        {
            // Create a scope to resolve scoped services from the singleton interceptor
            using var scope = _serviceProvider.CreateScope();
            return scope.ServiceProvider.GetService<ICurrentOrganizationContext>();
        }
        catch
        {
            // During migrations or design-time, there may be no service provider
            return null;
        }
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ValidateOrganizationScope(eventData.Context!);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ValidateOrganizationScope(eventData.Context!);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Validates that all modified IOrganizationScoped entities belong to the current user's organization.
    /// </summary>
    /// <exception cref="OrganizationAccessException">
    /// Thrown when attempting to modify an entity outside the user's organization context.
    /// </exception>
    private void ValidateOrganizationScope(DbContext context)
    {
        // Allow explicit bypass for cross-org operations (e.g., invitation acceptance)
        if (context is AppDbContext appDb && appDb.BypassOrgValidation)
        {
            return;
        }

        var orgContext = GetOrgContext();

        // If no org context available (migrations, design-time, tests), skip validation
        if (orgContext == null)
        {
            return;
        }

        // If no HTTP context (seeding, background jobs), skip validation
        // This allows data seeding to run without user authentication
        if (!orgContext.HasContext)
        {
            return;
        }

        // If the request is unauthenticated (registration, invitation accept), skip validation.
        // These flows create org-scoped entities (e.g., OrganizationMembership) before the user
        // has JWT claims. The controller-level [Authorize] / [AllowAnonymous] attributes
        // and service-layer logic are responsible for authorization in these flows.
        if (!orgContext.IsAuthenticated)
        {
            return;
        }

        // SysAdmins can modify any organization's data
        if (orgContext.IsSysAdmin)
        {
            return;
        }

        var currentOrgId = orgContext.CurrentOrganizationId;

        // Get all added or modified entities that implement IOrganizationScoped
        var entries = context.ChangeTracker
            .Entries<IOrganizationScoped>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var entityOrgId = entry.Entity.OrganizationId;
            var entityType = entry.Entity.GetType().Name;

            // User must have an organization context to modify org-scoped entities
            if (!currentOrgId.HasValue)
            {
                throw new OrganizationAccessException(
                    $"Cannot modify {entityType}: no organization context. " +
                    "User must be assigned to an organization.");
            }

            // Entity must belong to the user's current organization
            if (entityOrgId != currentOrgId.Value)
            {
                throw new OrganizationAccessException(
                    $"Cannot modify {entityType} in organization {entityOrgId}. " +
                    $"User's current organization is {currentOrgId.Value}.");
            }
        }
    }
}

/// <summary>
/// Exception thrown when a user attempts to access or modify data outside their organization context.
/// </summary>
public class OrganizationAccessException : UnauthorizedAccessException
{
    public OrganizationAccessException(string message) : base(message)
    {
    }

    public OrganizationAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
