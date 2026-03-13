using System.Diagnostics.CodeAnalysis;
using Cadence.Core.Constants;
using Cadence.Core.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Data;

/// <summary>
/// Seeds demo users for UAT, staging, and demonstration environments.
/// Uses ASP.NET Core Identity UserManager for proper password hashing.
/// 
/// Works alongside DemoDataSeeder:
/// 1. DemoDataSeeder.SeedAsync() creates org, exercises, MSELs, phases, injects
/// 2. DemoUserSeeder.SeedAsync() creates users and exercise participants
/// 
/// Idempotent - safe to run multiple times.
/// Runs in ALL environments EXCEPT Production.
/// 
/// Demo Credentials (all use same password: Demo123!)
/// ============================================================
/// Email                           | SystemRole | HSEEP Roles
/// --------------------------------|------------|---------------------------
/// admin@metrocounty.gov           | Admin      | Administrator
/// jwashington@metrocounty.gov     | Manager    | Exercise Director (Hurricane)
/// tgarcia@metrocounty.gov         | Manager    | Exercise Director (Cyber)
/// smartinez@metrocounty.gov       | User       | Controller
/// mbrown@metrocounty.gov          | User       | Controller
/// kpatel@metrocounty.gov          | User       | Controller
/// ldavis@metrocounty.gov          | User       | Evaluator
/// awilson@metrocounty.gov         | User       | Evaluator
/// rjohnson@metrocounty.gov        | User       | Observer
/// ============================================================
/// </summary>
[ExcludeFromCodeCoverage]
public class DemoUserSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;
    private readonly ILogger<DemoUserSeeder> _logger;

    /// <summary>
    /// Default password for all demo users.
    /// Meets ASP.NET Core Identity requirements.
    /// </summary>
    public const string DemoPassword = "Demo123!";

    public DemoUserSeeder(
        UserManager<ApplicationUser> userManager,
        AppDbContext context,
        ILogger<DemoUserSeeder> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Seeds demo users and exercise participants. Idempotent.
    /// Prerequisites: DemoDataSeeder.SeedAsync() must be called first.
    /// </summary>
    public async Task SeedAsync()
    {
        _logger.LogInformation("Starting demo user seeding...");

        // Verify prerequisites
        var orgExists = await _context.Organizations.AnyAsync(o => o.Id == DemoDataSeeder.DemoOrganizationId);
        if (!orgExists)
        {
            _logger.LogWarning(
                "Demo organization not found (ID: {OrgId}). Run DemoDataSeeder.SeedAsync() first.",
                DemoDataSeeder.DemoOrganizationId);
            return;
        }

        // Seed users (idempotent — skips existing, creates missing)
        var users = GetDemoUsers();
        var createdCount = 0;

        foreach (var user in users)
        {
            var existing = await _userManager.FindByIdAsync(user.Id);
            if (existing != null)
            {
                continue; // Already seeded
            }

            var result = await _userManager.CreateAsync(user, DemoPassword);

            if (result.Succeeded)
            {
                createdCount++;
                _logger.LogDebug("Created demo user: {Email} ({Role})", user.Email, user.SystemRole);
            }
            else
            {
                _logger.LogError(
                    "Failed to create demo user {Email}: {Errors}",
                    user.Email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        if (createdCount > 0)
        {
            _logger.LogInformation("Created {Count} new demo users", createdCount);
        }
        else
        {
            _logger.LogDebug("All demo users already exist - no new users created");
        }

        // Seed organization memberships for users that need org context
        await SeedOrganizationMembershipsAsync();

        // Seed exercise participants
        await SeedExerciseParticipantsAsync();

        _logger.LogInformation("Demo user seeding complete");
    }

    /// <summary>
    /// Seeds organization membership records for users that need explicit org context.
    /// The ZAP security scanner user needs a membership record so its JWT includes org_role.
    /// </summary>
    private async Task SeedOrganizationMembershipsAsync()
    {
        var zapMembershipExists = await _context.OrganizationMemberships
            .IgnoreQueryFilters()
            .AnyAsync(m => m.UserId == DemoDataSeeder.ZapScannerUserId
                        && m.OrganizationId == DemoDataSeeder.DemoOrganizationId);

        if (zapMembershipExists)
        {
            _logger.LogDebug("ZAP scanner membership already seeded - skipping");
            return;
        }

        var now = DateTime.UtcNow;
        var membership = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = DemoDataSeeder.ZapScannerUserId,
            OrganizationId = DemoDataSeeder.DemoOrganizationId,
            Role = OrgRole.OrgUser,
            Status = MembershipStatus.Active,
            JoinedAt = now,
            InvitedById = DemoDataSeeder.AdminUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.OrganizationMemberships.Add(membership);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created ZAP scanner organization membership");
    }

    /// <summary>
    /// Seeds exercise participant records linking users to exercises with HSEEP roles.
    /// </summary>
    private async Task SeedExerciseParticipantsAsync()
    {
        var hasParticipants = await _context.ExerciseParticipants
            .AnyAsync(p => p.ExerciseId == DemoDataSeeder.HurricaneTtxId);

        if (hasParticipants)
        {
            _logger.LogDebug("Exercise participants already seeded - skipping");
            return;
        }

        var now = DateTime.UtcNow;
        var participants = new List<ExerciseParticipant>();

        // Add participants for all exercises
        participants.AddRange(CreateHurricaneParticipants(now));
        participants.AddRange(CreateCyberIncidentParticipants(now));
        participants.AddRange(CreateEarthquakeParticipants(now));
        participants.AddRange(CreateFloodTrainingParticipants(now));

        _context.ExerciseParticipants.AddRange(participants);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created {Count} exercise participant assignments", participants.Count);
    }

    /// <summary>
    /// Returns all demo users to create.
    /// </summary>
    private List<ApplicationUser> GetDemoUsers()
    {
        var now = DateTime.UtcNow;
        var orgId = DemoDataSeeder.DemoOrganizationId;

        return new List<ApplicationUser>
        {
            // =====================================================================
            // System Administrator
            // =====================================================================
            new ApplicationUser
            {
                Id = DemoDataSeeder.AdminUserId,
                UserName = "admin@metrocounty.gov",
                NormalizedUserName = "ADMIN@METROCOUNTY.GOV",
                Email = "admin@metrocounty.gov",
                NormalizedEmail = "ADMIN@METROCOUNTY.GOV",
                EmailConfirmed = true,
                DisplayName = "Maria Chen",
                SystemRole = SystemRole.Admin,
                Status = UserStatus.Active,
                OrganizationId = orgId,
                CreatedAt = now.AddMonths(-12),
                SecurityStamp = Guid.NewGuid().ToString()
            },

            // =====================================================================
            // Managers (can create/manage exercises)
            // =====================================================================
            new ApplicationUser
            {
                Id = DemoDataSeeder.Director1UserId,
                UserName = "jwashington@metrocounty.gov",
                NormalizedUserName = "JWASHINGTON@METROCOUNTY.GOV",
                Email = "jwashington@metrocounty.gov",
                NormalizedEmail = "JWASHINGTON@METROCOUNTY.GOV",
                EmailConfirmed = true,
                DisplayName = "James Washington",
                SystemRole = SystemRole.Manager,
                Status = UserStatus.Active,
                OrganizationId = orgId,
                CreatedAt = now.AddMonths(-10),
                CreatedById = DemoDataSeeder.AdminUserId,
                SecurityStamp = Guid.NewGuid().ToString()
            },
            new ApplicationUser
            {
                Id = DemoDataSeeder.Director2UserId,
                UserName = "tgarcia@metrocounty.gov",
                NormalizedUserName = "TGARCIA@METROCOUNTY.GOV",
                Email = "tgarcia@metrocounty.gov",
                NormalizedEmail = "TGARCIA@METROCOUNTY.GOV",
                EmailConfirmed = true,
                DisplayName = "Teresa Garcia",
                SystemRole = SystemRole.Manager,
                Status = UserStatus.Active,
                OrganizationId = orgId,
                CreatedAt = now.AddMonths(-8),
                CreatedById = DemoDataSeeder.AdminUserId,
                SecurityStamp = Guid.NewGuid().ToString()
            },

            // =====================================================================
            // Controllers
            // =====================================================================
            new ApplicationUser
            {
                Id = DemoDataSeeder.Controller1UserId,
                UserName = "smartinez@metrocounty.gov",
                NormalizedUserName = "SMARTINEZ@METROCOUNTY.GOV",
                Email = "smartinez@metrocounty.gov",
                NormalizedEmail = "SMARTINEZ@METROCOUNTY.GOV",
                EmailConfirmed = true,
                DisplayName = "Sarah Martinez",
                SystemRole = SystemRole.User,
                Status = UserStatus.Active,
                OrganizationId = orgId,
                CreatedAt = now.AddMonths(-6),
                CreatedById = DemoDataSeeder.AdminUserId,
                SecurityStamp = Guid.NewGuid().ToString()
            },
            new ApplicationUser
            {
                Id = DemoDataSeeder.Controller2UserId,
                UserName = "mbrown@metrocounty.gov",
                NormalizedUserName = "MBROWN@METROCOUNTY.GOV",
                Email = "mbrown@metrocounty.gov",
                NormalizedEmail = "MBROWN@METROCOUNTY.GOV",
                EmailConfirmed = true,
                DisplayName = "Michael Brown",
                SystemRole = SystemRole.User,
                Status = UserStatus.Active,
                OrganizationId = orgId,
                CreatedAt = now.AddMonths(-6),
                CreatedById = DemoDataSeeder.AdminUserId,
                SecurityStamp = Guid.NewGuid().ToString()
            },
            new ApplicationUser
            {
                Id = DemoDataSeeder.Controller3UserId,
                UserName = "kpatel@metrocounty.gov",
                NormalizedUserName = "KPATEL@METROCOUNTY.GOV",
                Email = "kpatel@metrocounty.gov",
                NormalizedEmail = "KPATEL@METROCOUNTY.GOV",
                EmailConfirmed = true,
                DisplayName = "Kiran Patel",
                SystemRole = SystemRole.User,
                Status = UserStatus.Active,
                OrganizationId = orgId,
                CreatedAt = now.AddMonths(-4),
                CreatedById = DemoDataSeeder.AdminUserId,
                SecurityStamp = Guid.NewGuid().ToString()
            },

            // =====================================================================
            // Evaluators
            // =====================================================================
            new ApplicationUser
            {
                Id = DemoDataSeeder.Evaluator1UserId,
                UserName = "ldavis@metrocounty.gov",
                NormalizedUserName = "LDAVIS@METROCOUNTY.GOV",
                Email = "ldavis@metrocounty.gov",
                NormalizedEmail = "LDAVIS@METROCOUNTY.GOV",
                EmailConfirmed = true,
                DisplayName = "Lisa Davis",
                SystemRole = SystemRole.User,
                Status = UserStatus.Active,
                OrganizationId = orgId,
                CreatedAt = now.AddMonths(-6),
                CreatedById = DemoDataSeeder.AdminUserId,
                SecurityStamp = Guid.NewGuid().ToString()
            },
            new ApplicationUser
            {
                Id = DemoDataSeeder.Evaluator2UserId,
                UserName = "awilson@metrocounty.gov",
                NormalizedUserName = "AWILSON@METROCOUNTY.GOV",
                Email = "awilson@metrocounty.gov",
                NormalizedEmail = "AWILSON@METROCOUNTY.GOV",
                EmailConfirmed = true,
                DisplayName = "Angela Wilson",
                SystemRole = SystemRole.User,
                Status = UserStatus.Active,
                OrganizationId = orgId,
                CreatedAt = now.AddMonths(-3),
                CreatedById = DemoDataSeeder.AdminUserId,
                SecurityStamp = Guid.NewGuid().ToString()
            },

            // =====================================================================
            // Observer
            // =====================================================================
            new ApplicationUser
            {
                Id = DemoDataSeeder.ObserverUserId,
                UserName = "rjohnson@metrocounty.gov",
                NormalizedUserName = "RJOHNSON@METROCOUNTY.GOV",
                Email = "rjohnson@metrocounty.gov",
                NormalizedEmail = "RJOHNSON@METROCOUNTY.GOV",
                EmailConfirmed = true,
                DisplayName = "Robert Johnson",
                SystemRole = SystemRole.User,
                Status = UserStatus.Active,
                OrganizationId = orgId,
                CreatedAt = now.AddMonths(-2),
                CreatedById = DemoDataSeeder.AdminUserId,
                SecurityStamp = Guid.NewGuid().ToString()
            },

            // =====================================================================
            // ZAP Security Scanner (DAST authenticated scanning)
            // =====================================================================
            new ApplicationUser
            {
                Id = DemoDataSeeder.ZapScannerUserId,
                UserName = "zap-scanner@cadence-test.com",
                NormalizedUserName = "ZAP-SCANNER@CADENCE-TEST.COM",
                Email = "zap-scanner@cadence-test.com",
                NormalizedEmail = "ZAP-SCANNER@CADENCE-TEST.COM",
                EmailConfirmed = true,
                DisplayName = "ZAP Security Scanner",
                SystemRole = SystemRole.User,
                Status = UserStatus.Active,
                OrganizationId = orgId,
                CurrentOrganizationId = orgId,
                CreatedAt = now.AddMonths(-1),
                CreatedById = DemoDataSeeder.AdminUserId,
                SecurityStamp = Guid.NewGuid().ToString()
            }
        };
    }

    #region Exercise Participants

    /// <summary>
    /// Hurricane TTX participants - full team assignment demonstrating all roles.
    /// </summary>
    private List<ExerciseParticipant> CreateHurricaneParticipants(DateTime now)
    {
        return new List<ExerciseParticipant>
        {
            // Administrator
            CreateParticipant(DemoDataSeeder.HurricaneTtxId, DemoDataSeeder.AdminUserId,
                ExerciseRole.Administrator, now.AddDays(-14), DemoDataSeeder.AdminUserId),
            
            // Exercise Director
            CreateParticipant(DemoDataSeeder.HurricaneTtxId, DemoDataSeeder.Director1UserId,
                ExerciseRole.ExerciseDirector, now.AddDays(-14), DemoDataSeeder.AdminUserId),
            
            // Controllers (3 for larger exercise)
            CreateParticipant(DemoDataSeeder.HurricaneTtxId, DemoDataSeeder.Controller1UserId,
                ExerciseRole.Controller, now.AddDays(-10), DemoDataSeeder.Director1UserId),
            CreateParticipant(DemoDataSeeder.HurricaneTtxId, DemoDataSeeder.Controller2UserId,
                ExerciseRole.Controller, now.AddDays(-10), DemoDataSeeder.Director1UserId),
            CreateParticipant(DemoDataSeeder.HurricaneTtxId, DemoDataSeeder.Controller3UserId,
                ExerciseRole.Controller, now.AddDays(-7), DemoDataSeeder.Director1UserId),
            
            // Evaluators (2 for coverage)
            CreateParticipant(DemoDataSeeder.HurricaneTtxId, DemoDataSeeder.Evaluator1UserId,
                ExerciseRole.Evaluator, now.AddDays(-7), DemoDataSeeder.Director1UserId),
            CreateParticipant(DemoDataSeeder.HurricaneTtxId, DemoDataSeeder.Evaluator2UserId,
                ExerciseRole.Evaluator, now.AddDays(-5), DemoDataSeeder.Director1UserId),
            
            // Observer
            CreateParticipant(DemoDataSeeder.HurricaneTtxId, DemoDataSeeder.ObserverUserId,
                ExerciseRole.Observer, now.AddDays(-3), DemoDataSeeder.Director1UserId)
        };
    }

    /// <summary>
    /// Cyber Incident TTX participants - different director, smaller team.
    /// </summary>
    private List<ExerciseParticipant> CreateCyberIncidentParticipants(DateTime now)
    {
        return new List<ExerciseParticipant>
        {
            // Different director for this exercise
            CreateParticipant(DemoDataSeeder.CyberIncidentTtxId, DemoDataSeeder.Director2UserId,
                ExerciseRole.ExerciseDirector, now.AddDays(-60), DemoDataSeeder.AdminUserId),
            
            // Controllers
            CreateParticipant(DemoDataSeeder.CyberIncidentTtxId, DemoDataSeeder.Controller1UserId,
                ExerciseRole.Controller, now.AddDays(-55), DemoDataSeeder.Director2UserId),
            CreateParticipant(DemoDataSeeder.CyberIncidentTtxId, DemoDataSeeder.Controller2UserId,
                ExerciseRole.Controller, now.AddDays(-55), DemoDataSeeder.Director2UserId),
            
            // Evaluator
            CreateParticipant(DemoDataSeeder.CyberIncidentTtxId, DemoDataSeeder.Evaluator1UserId,
                ExerciseRole.Evaluator, now.AddDays(-50), DemoDataSeeder.Director2UserId)
        };
    }

    /// <summary>
    /// Earthquake FE participants - archived exercise, historical assignments.
    /// </summary>
    private List<ExerciseParticipant> CreateEarthquakeParticipants(DateTime now)
    {
        var baseDate = now.AddMonths(-7);

        return new List<ExerciseParticipant>
        {
            CreateParticipant(DemoDataSeeder.EarthquakeFEId, DemoDataSeeder.Director1UserId,
                ExerciseRole.ExerciseDirector, baseDate, DemoDataSeeder.AdminUserId),
            CreateParticipant(DemoDataSeeder.EarthquakeFEId, DemoDataSeeder.Controller1UserId,
                ExerciseRole.Controller, baseDate.AddDays(7), DemoDataSeeder.Director1UserId),
            CreateParticipant(DemoDataSeeder.EarthquakeFEId, DemoDataSeeder.Controller2UserId,
                ExerciseRole.Controller, baseDate.AddDays(7), DemoDataSeeder.Director1UserId),
            CreateParticipant(DemoDataSeeder.EarthquakeFEId, DemoDataSeeder.Controller3UserId,
                ExerciseRole.Controller, baseDate.AddDays(10), DemoDataSeeder.Director1UserId),
            CreateParticipant(DemoDataSeeder.EarthquakeFEId, DemoDataSeeder.Evaluator1UserId,
                ExerciseRole.Evaluator, baseDate.AddDays(14), DemoDataSeeder.Director1UserId),
            CreateParticipant(DemoDataSeeder.EarthquakeFEId, DemoDataSeeder.Evaluator2UserId,
                ExerciseRole.Evaluator, baseDate.AddDays(14), DemoDataSeeder.Director1UserId)
        };
    }

    /// <summary>
    /// Flood Training participants - minimal team for training exercise.
    /// </summary>
    private List<ExerciseParticipant> CreateFloodTrainingParticipants(DateTime now)
    {
        return new List<ExerciseParticipant>
        {
            // Controller acting as Director for training
            CreateParticipant(DemoDataSeeder.FloodTrainingId, DemoDataSeeder.Controller1UserId,
                ExerciseRole.ExerciseDirector, now.AddDays(-3), DemoDataSeeder.Director1UserId),
            CreateParticipant(DemoDataSeeder.FloodTrainingId, DemoDataSeeder.Controller2UserId,
                ExerciseRole.Controller, now.AddDays(-2), DemoDataSeeder.Controller1UserId)
        };
    }

    /// <summary>
    /// Helper to create ExerciseParticipant with common properties.
    /// </summary>
    private static ExerciseParticipant CreateParticipant(
        Guid exerciseId,
        string userId,
        ExerciseRole role,
        DateTime assignedAt,
        string assignedById)
    {
        return new ExerciseParticipant
        {
            Id = Guid.NewGuid(),
            ExerciseId = exerciseId,
            UserId = userId,
            Role = role,
            AssignedAt = assignedAt,
            AssignedById = assignedById,
            CreatedAt = assignedAt,
            UpdatedAt = assignedAt,
            CreatedBy = SystemConstants.SystemUserIdString,
            ModifiedBy = SystemConstants.SystemUserIdString
        };
    }

    #endregion
}
