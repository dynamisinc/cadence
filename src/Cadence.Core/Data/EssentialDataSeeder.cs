using Cadence.Core.Constants;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Data;

/// <summary>
/// Seeds essential data required for the application to function.
/// Runs in ALL environments (including production) and is idempotent.
///
/// Seeds:
/// - Default organization (required for user registration)
/// </summary>
public static class EssentialDataSeeder
{
    /// <summary>
    /// Seeds essential data if not already present. Idempotent - safe to call multiple times.
    /// </summary>
    public static async Task SeedAsync(AppDbContext context, ILogger? logger = null)
    {
        var seeded = false;

        // Ensure default organization exists
        if (!await context.Organizations.AnyAsync(o => o.Id == SystemConstants.DefaultOrganizationId))
        {
            // Check if ANY organization exists (migration may have seeded with different ID)
            var hasAnyOrg = await context.Organizations.AnyAsync();

            if (!hasAnyOrg)
            {
                var defaultOrg = new Organization
                {
                    Id = SystemConstants.DefaultOrganizationId,
                    Name = "Default Organization",
                    Description = "Default organization for Cadence users",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = SystemConstants.SystemUserId,
                    ModifiedBy = SystemConstants.SystemUserId
                };

                context.Organizations.Add(defaultOrg);
                seeded = true;
                logger?.LogInformation("Created default organization: {OrgId}", defaultOrg.Id);
            }
            else
            {
                logger?.LogDebug(
                    "Default organization with well-known ID not found, but other organizations exist. Skipping.");
            }
        }

        if (seeded)
        {
            await context.SaveChangesAsync();
            logger?.LogInformation("Essential data seeding completed");
        }
        else
        {
            logger?.LogDebug("Essential data already seeded, no changes needed");
        }
    }
}
