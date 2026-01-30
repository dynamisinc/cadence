using Cadence.Core.Constants;
using Cadence.Core.Data;
using Cadence.Core.Features.Capabilities.Services;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.WebApi.Extensions;

/// <summary>
/// Extension methods for data seeding.
/// 
/// Two seeding stages:
/// 1. Essential seeding (ALL environments): Default organization required for app function
/// 2. Demo seeding (ALL except Production): Demo org, users, exercises for UAT/demos
/// 
/// Usage in Program.cs:
/// <code>
/// // Stage 1: Essential data - ALL environments
/// await app.SeedEssentialDataAsync();
/// 
/// // Stage 2: Demo data - ALL except Production
/// if (!app.Environment.IsProduction())
/// {
///     await app.SeedDemoDataAsync();
/// }
/// </code>
/// </summary>
public static class DataSeederExtensions
{
    #region Essential Seeding (All Environments)

    /// <summary>
    /// Seeds essential data required for the application to function.
    /// Safe to run in ALL environments including production.
    /// 
    /// Operations:
    /// 1. Apply pending database migrations
    /// 2. Create default organization (required for user registration)
    /// 
    /// Failure behavior: Throws exception - app cannot function without essential data.
    /// </summary>
    public static async Task SeedEssentialDataAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Cadence.Data.EssentialDataSeeder");

        try
        {
            var context = services.GetRequiredService<AppDbContext>();

            logger.LogInformation("Applying pending database migrations...");
            await context.Database.MigrateAsync();

            await EssentialDataSeeder.SeedAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during essential data seeding");
            throw; // Essential seeding failure should prevent app startup
        }
    }

    #endregion

    #region Demo Seeding (All Except Production)

    /// <summary>
    /// Seeds comprehensive demo data for UAT, staging, and demonstrations.
    /// Runs in ALL environments EXCEPT Production.
    /// 
    /// Prerequisites: SeedEssentialDataAsync() should be called first.
    /// 
    /// Seeds:
    /// - Demo organization (Metro County Emergency Management Agency)
    /// - 9 demo users with proper password hashing
    /// - 5 exercises demonstrating full lifecycle (Draft → Active → Completed → Archived)
    /// - Complete MSELs with 35+ injects across multiple phases
    /// - 12 objectives across exercises
    /// - FEMA Core Capabilities
    /// - 12+ observations with all P/S/M/U ratings
    /// 
    /// All operations are idempotent. Demo data is isolated from production data.
    /// Failure behavior: Logs error and continues - app can function without demo data.
    /// </summary>
    /// <param name="app">The web application host</param>
    /// <param name="seedObservations">Whether to seed sample observations (default: true)</param>
    public static async Task SeedDemoDataAsync(this IHost app, bool seedObservations = true)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<DemoUserSeeder>>();

        try
        {
            logger.LogInformation("=== Starting Demo Data Seeding ===");

            var context = services.GetRequiredService<AppDbContext>();

            // 1. Seed base data (demo organization, exercises, MSELs, phases, objectives, injects)
            logger.LogInformation("Seeding demo organization, exercises, MSELs, objectives, injects...");
            await DemoDataSeeder.SeedAsync(context, logger);

            // 2. Seed users (requires UserManager for password hashing)
            logger.LogInformation("Seeding demo users...");
            var userSeeder = services.GetRequiredService<DemoUserSeeder>();
            await userSeeder.SeedAsync();

            // 3. Seed capabilities (FEMA Core Capabilities for demo org)
            var importService = services.GetService<ICapabilityImportService>();
            if (importService != null)
            {
                logger.LogInformation("Seeding FEMA Core Capabilities for demo organization...");
                await DemoDataSeeder.SeedCapabilitiesAsync(context, importService, logger);
            }
            else
            {
                logger.LogDebug("ICapabilityImportService not registered - skipping capability seeding");
            }

            // 4. Seed observations (demonstrates evaluator workflow)
            if (seedObservations)
            {
                logger.LogInformation("Seeding sample observations...");
                await SeedObservationsAsync(context, logger);
            }

            logger.LogInformation("=== Demo Data Seeding Complete ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding demo data");
            // Don't re-throw - demo seeding failure shouldn't prevent app startup
        }
    }

    /// <summary>
    /// Registers the DemoUserSeeder with the DI container.
    /// Call this in Program.cs for non-Production environments.
    /// </summary>
    public static IServiceCollection AddDemoSeeder(this IServiceCollection services)
    {
        services.AddScoped<DemoUserSeeder>();
        return services;
    }

    #endregion

    #region Observations Seeding

    /// <summary>
    /// Seeds comprehensive sample observations demonstrating:
    /// - All P/S/M/U ratings
    /// - Observations linked to injects
    /// - Observations linked to objectives
    /// - General observations without links
    /// - Observations from different evaluators
    /// - Different locations and timestamps
    /// </summary>
    private static async Task SeedObservationsAsync(AppDbContext context, ILogger logger)
    {
        // Check if already seeded
        var hasObservations = await context.Observations
            .AnyAsync(o => o.ExerciseId == DemoDataSeeder.HurricaneTtxId);

        if (hasObservations)
        {
            logger.LogDebug("Observations already seeded - skipping");
            return;
        }

        var now = DateTime.UtcNow;
        var observations = new List<Observation>();

        // Hurricane TTX Observations (Active Exercise)
        observations.AddRange(CreateHurricaneObservations(now));

        // Cyber Incident TTX Observations (Completed Exercise)
        observations.AddRange(CreateCyberObservations(now));

        // Earthquake FE Observations (Archived Exercise)
        observations.AddRange(CreateEarthquakeObservations(now));

        context.Observations.AddRange(observations);
        await context.SaveChangesAsync();

        logger.LogInformation("Created {Count} sample observations across exercises", observations.Count);
    }

    private static List<Observation> CreateHurricaneObservations(DateTime now)
    {
        return new List<Observation>
        {
            // Observation 1: Performed - EOC Activation
            new Observation
            {
                Id = Guid.NewGuid(),
                ExerciseId = DemoDataSeeder.HurricaneTtxId,
                InjectId = DemoDataSeeder.HurricaneInject1Id,
                ObjectiveId = DemoDataSeeder.HurricaneObj1Id,
                Content = "EOC activation was completed within the 2-hour target window. All key personnel " +
                          "were notified promptly via the emergency notification system. Conference bridge " +
                          "was established within 15 minutes of initial notification. ESF representatives " +
                          "arrived and assumed positions efficiently.",
                Rating = ObservationRating.Performed,
                Recommendation = "Continue current procedures. Consider documenting the activation checklist " +
                                 "as a training resource for new EOC staff.",
                ObservedAt = now.AddMinutes(-40),
                Location = "Emergency Operations Center - Main Floor",
                CreatedByUserId = DemoDataSeeder.Evaluator1UserId,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
                CreatedAt = now.AddMinutes(-40),
                UpdatedAt = now.AddMinutes(-40)
            },

            // Observation 2: Satisfactory - PIO Coordination
            new Observation
            {
                Id = Guid.NewGuid(),
                ExerciseId = DemoDataSeeder.HurricaneTtxId,
                InjectId = DemoDataSeeder.HurricaneInject2Id,
                ObjectiveId = DemoDataSeeder.HurricaneObj2Id,
                Content = "PIO coordinated response with EOC Director before engaging media. Messaging was " +
                          "consistent with operational status. Minor delay (~10 minutes) in preparing talking " +
                          "points due to template not being readily accessible on the shared drive.",
                Rating = ObservationRating.Satisfactory,
                Recommendation = "Pre-position public information templates in easily accessible location. " +
                                 "Consider creating scenario-specific message templates (hurricane, tornado, etc.) " +
                                 "that can be quickly customized.",
                ObservedAt = now.AddMinutes(-30),
                Location = "JIC/PIO Workspace",
                CreatedByUserId = DemoDataSeeder.Evaluator1UserId,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
                CreatedAt = now.AddMinutes(-30),
                UpdatedAt = now.AddMinutes(-30)
            },

            // Observation 3: Satisfactory - Information Flow
            new Observation
            {
                Id = Guid.NewGuid(),
                ExerciseId = DemoDataSeeder.HurricaneTtxId,
                InjectId = null, // General observation
                ObjectiveId = DemoDataSeeder.HurricaneObj1Id,
                Content = "WebEOC status boards were updated consistently throughout the exercise. Information " +
                          "flow between ESFs was good. Noted minor confusion about which board to use for " +
                          "shelter status (ESF-6 vs. Mass Care). Some ESF representatives unfamiliar with " +
                          "new WebEOC update process.",
                Rating = ObservationRating.Satisfactory,
                Recommendation = "Clarify WebEOC board assignments in the EOC Operations Manual. Include " +
                                 "WebEOC refresher training in next EOC orientation session.",
                ObservedAt = now.AddMinutes(-25),
                Location = "EOC Operations Floor",
                CreatedByUserId = DemoDataSeeder.Evaluator2UserId,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
                CreatedAt = now.AddMinutes(-25),
                UpdatedAt = now.AddMinutes(-25)
            },

            // Observation 4: Marginal - School District Coordination
            new Observation
            {
                Id = Guid.NewGuid(),
                ExerciseId = DemoDataSeeder.HurricaneTtxId,
                InjectId = DemoDataSeeder.HurricaneInject3Id,
                ObjectiveId = DemoDataSeeder.HurricaneObj4Id,
                Content = "Response to school district inquiry was delayed. EOC Director was engaged in " +
                          "other coordination activities and Schools ESF representative was not immediately " +
                          "available at assigned position. Callback took 25 minutes, exceeding the 15-minute " +
                          "target for stakeholder response.",
                Rating = ObservationRating.Marginal,
                Recommendation = "Ensure Schools ESF position is staffed continuously when school-related " +
                                 "issues are anticipated. Consider establishing direct line between EOC and " +
                                 "School District EOC for faster coordination.",
                ObservedAt = now.AddMinutes(-20),
                Location = "EOC - Schools/Mass Care Section",
                CreatedByUserId = DemoDataSeeder.Evaluator1UserId,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
                CreatedAt = now.AddMinutes(-20),
                UpdatedAt = now.AddMinutes(-20)
            },

            // Observation 5: Performed - Hospital Coordination
            new Observation
            {
                Id = Guid.NewGuid(),
                ExerciseId = DemoDataSeeder.HurricaneTtxId,
                InjectId = DemoDataSeeder.HurricaneInject4Id,
                ObjectiveId = DemoDataSeeder.HurricaneObj5Id,
                Content = "Medical branch demonstrated excellent coordination with hospital system. Patient " +
                          "evacuation triggers were clearly communicated. Fuel resupply contingencies were " +
                          "identified proactively. ESF-8 representative had strong relationships with hospital " +
                          "contacts that facilitated rapid information exchange.",
                Rating = ObservationRating.Performed,
                Recommendation = "Document the hospital coordination checklist used as a best practice. " +
                                 "Consider sharing approach with neighboring counties for regional consistency.",
                ObservedAt = now.AddMinutes(-15),
                Location = "EOC - Medical Branch",
                CreatedByUserId = DemoDataSeeder.Evaluator2UserId,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
                CreatedAt = now.AddMinutes(-15),
                UpdatedAt = now.AddMinutes(-15)
            },

            // Observation 6: Satisfactory - State Coordination
            new Observation
            {
                Id = Guid.NewGuid(),
                ExerciseId = DemoDataSeeder.HurricaneTtxId,
                InjectId = DemoDataSeeder.HurricaneInject5Id,
                ObjectiveId = DemoDataSeeder.HurricaneObj1Id,
                Content = "County status brief for state coordination call was prepared within timeframe. " +
                          "Resource needs were compiled accurately. Minor issue: some mutual aid request " +
                          "forms were outdated (2019 version vs. current 2023 version).",
                Rating = ObservationRating.Satisfactory,
                Recommendation = "Update all mutual aid forms to current versions. Establish quarterly " +
                                 "review process for EOC form library.",
                ObservedAt = now.AddMinutes(-5),
                Location = "EOC - Command Section",
                CreatedByUserId = DemoDataSeeder.Evaluator1UserId,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
                CreatedAt = now.AddMinutes(-5),
                UpdatedAt = now.AddMinutes(-5)
            }
        };
    }

    private static List<Observation> CreateCyberObservations(DateTime now)
    {
        var exerciseDate = now.AddDays(-45);

        return new List<Observation>
        {
            // Cyber Observation 1: Marginal - Detection Time
            new Observation
            {
                Id = Guid.NewGuid(),
                ExerciseId = DemoDataSeeder.CyberIncidentTtxId,
                InjectId = null,
                ObjectiveId = DemoDataSeeder.CyberObj1Id,
                Content = "Initial detection of ransomware took 45 minutes from first user report to IT " +
                          "security confirmation. Help desk initially treated as routine performance issue. " +
                          "Escalation procedures were not followed until third similar report.",
                Rating = ObservationRating.Marginal,
                Recommendation = "Develop cyber incident recognition training for Help Desk staff. Create " +
                                 "clear escalation triggers for suspicious activity patterns.",
                ObservedAt = exerciseDate.AddMinutes(30),
                Location = "IT Operations Center",
                CreatedByUserId = DemoDataSeeder.Evaluator1UserId,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
                CreatedAt = exerciseDate.AddMinutes(30),
                UpdatedAt = exerciseDate.AddMinutes(30)
            },

            // Cyber Observation 2: Performed - Business Continuity
            new Observation
            {
                Id = Guid.NewGuid(),
                ExerciseId = DemoDataSeeder.CyberIncidentTtxId,
                InjectId = null,
                ObjectiveId = DemoDataSeeder.CyberObj2Id,
                Content = "911 dispatch successfully transitioned to manual backup procedures within 20 minutes " +
                          "of CAD system going offline. Dispatchers were well-trained on paper-based procedures. " +
                          "Radio communications remained operational throughout.",
                Rating = ObservationRating.Performed,
                Recommendation = "Continue quarterly backup procedure drills. Consider cross-training between " +
                                 "dispatch shifts on manual procedures.",
                ObservedAt = exerciseDate.AddMinutes(90),
                Location = "911 Communications Center",
                CreatedByUserId = DemoDataSeeder.Evaluator1UserId,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
                CreatedAt = exerciseDate.AddMinutes(90),
                UpdatedAt = exerciseDate.AddMinutes(90)
            },

            // Cyber Observation 3: Unsatisfactory - Public Communication
            new Observation
            {
                Id = Guid.NewGuid(),
                ExerciseId = DemoDataSeeder.CyberIncidentTtxId,
                InjectId = null,
                ObjectiveId = DemoDataSeeder.CyberObj3Id,
                Content = "Public communication about service disruption was not issued until 3 hours after " +
                          "incident confirmation. No pre-approved cyber incident messaging templates existed. " +
                          "Legal review delayed initial statement. Social media inquiries went unanswered.",
                Rating = ObservationRating.Unsatisfactory,
                Recommendation = "Develop pre-approved cyber incident communication templates with legal review " +
                                 "completed in advance. Establish social media monitoring and response protocols " +
                                 "for IT incidents.",
                ObservedAt = exerciseDate.AddMinutes(150),
                Location = "PIO Office",
                CreatedByUserId = DemoDataSeeder.Evaluator1UserId,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
                CreatedAt = exerciseDate.AddMinutes(150),
                UpdatedAt = exerciseDate.AddMinutes(150)
            }
        };
    }

    private static List<Observation> CreateEarthquakeObservations(DateTime now)
    {
        var exerciseDate = now.AddMonths(-6);

        return new List<Observation>
        {
            // Earthquake Observation 1: Satisfactory - Damage Assessment
            new Observation
            {
                Id = Guid.NewGuid(),
                ExerciseId = DemoDataSeeder.EarthquakeFEId,
                InjectId = null,
                ObjectiveId = DemoDataSeeder.EarthquakeObj1Id,
                Content = "Windshield damage assessment teams deployed within 3 hours of exercise start. " +
                          "Teams covered assigned sectors systematically. Some teams lacked updated assessment " +
                          "forms and had to improvise documentation.",
                Rating = ObservationRating.Satisfactory,
                Recommendation = "Pre-stage damage assessment kits with current forms in designated vehicles. " +
                                 "Consider digital damage assessment tools for faster reporting.",
                ObservedAt = exerciseDate.AddHours(4),
                Location = "Field Operations",
                CreatedByUserId = DemoDataSeeder.Evaluator1UserId,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
                CreatedAt = exerciseDate.AddHours(4),
                UpdatedAt = exerciseDate.AddHours(4)
            },

            // Earthquake Observation 2: Performed - Mutual Aid
            new Observation
            {
                Id = Guid.NewGuid(),
                ExerciseId = DemoDataSeeder.EarthquakeFEId,
                InjectId = null,
                ObjectiveId = DemoDataSeeder.EarthquakeObj2Id,
                Content = "EMAC request was initiated within 90 minutes of local emergency declaration. " +
                          "Resource typing was accurate. State EOC liaison facilitated rapid processing. " +
                          "Regional mutual aid partners were contacted through established protocols.",
                Rating = ObservationRating.Performed,
                Recommendation = "Document the successful EMAC request process as a case study for training. " +
                                 "Maintain current contact information for state and regional partners.",
                ObservedAt = exerciseDate.AddHours(5),
                Location = "EOC - Logistics Section",
                CreatedByUserId = DemoDataSeeder.Evaluator2UserId,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
                CreatedAt = exerciseDate.AddHours(5),
                UpdatedAt = exerciseDate.AddHours(5)
            },

            // Earthquake Observation 3: Satisfactory - Search & Rescue
            new Observation
            {
                Id = Guid.NewGuid(),
                ExerciseId = DemoDataSeeder.EarthquakeFEId,
                InjectId = null,
                ObjectiveId = DemoDataSeeder.EarthquakeObj3Id,
                Content = "US&R coordination was established effectively. Sector assignments were clear. " +
                          "Integration with incoming state US&R team took longer than expected due to " +
                          "unfamiliarity with ICS organizational structure.",
                Rating = ObservationRating.Satisfactory,
                Recommendation = "Conduct joint training with state US&R team annually. Review ICS integration " +
                                 "procedures with all potential mutual aid resources.",
                ObservedAt = exerciseDate.AddHours(7),
                Location = "Incident Command Post - Alpha",
                CreatedByUserId = DemoDataSeeder.Evaluator1UserId,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
                CreatedAt = exerciseDate.AddHours(7),
                UpdatedAt = exerciseDate.AddHours(7)
            }
        };
    }

    #endregion
}
