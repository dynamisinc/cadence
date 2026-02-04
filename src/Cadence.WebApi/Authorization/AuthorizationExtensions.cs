using Cadence.Core.Features.Authorization.Services;
using Cadence.Core.Models.Entities;
using Cadence.WebApi.Authorization.Handlers;
using Cadence.WebApi.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection;

namespace Cadence.WebApi.Authorization;

/// <summary>
/// Extension methods for configuring Cadence authorization policies.
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Add Cadence authorization services and policies.
    /// </summary>
    public static IServiceCollection AddCadenceAuthorization(this IServiceCollection services)
    {
        // Register authorization handlers
        services.AddScoped<IAuthorizationHandler, ExerciseAccessHandler>();
        services.AddScoped<IAuthorizationHandler, ExerciseRoleHandler>();

        // Register authorization logging handler (logs 401/403 failures to App Insights)
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationLoggingHandler>();

        // Register role resolver
        services.AddScoped<IRoleResolver, RoleResolver>();

        // Configure authorization policies
        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.RequireAdmin, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    var systemRoleClaim = context.User.FindFirst("SystemRole");
                    return systemRoleClaim?.Value == SystemRole.Admin.ToString();
                });
            })
            .AddPolicy(PolicyNames.RequireManager, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    var systemRoleClaim = context.User.FindFirst("SystemRole");
                    return systemRoleClaim?.Value == SystemRole.Admin.ToString() ||
                           systemRoleClaim?.Value == SystemRole.Manager.ToString();
                });
            })
            .AddPolicy(PolicyNames.RequireOrgAdmin, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                {
                    // System Admins always have org admin access
                    var systemRoleClaim = context.User.FindFirst("SystemRole");
                    if (systemRoleClaim?.Value == SystemRole.Admin.ToString())
                    {
                        return true;
                    }

                    // Check organization role from JWT claim
                    var orgRoleClaim = context.User.FindFirst("org_role");
                    return orgRoleClaim?.Value == OrgRole.OrgAdmin.ToString();
                });
            })
            .AddPolicy(PolicyNames.ExerciseAccess, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ExerciseAccessRequirement());
            })
            .AddPolicy(PolicyNames.ExerciseController, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ExerciseRoleRequirement(ExerciseRole.Controller));
            })
            .AddPolicy(PolicyNames.ExerciseDirector, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ExerciseRoleRequirement(ExerciseRole.ExerciseDirector));
            })
            .AddPolicy(PolicyNames.ExerciseEvaluator, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ExerciseRoleRequirement(ExerciseRole.Evaluator));
            });

        return services;
    }
}
