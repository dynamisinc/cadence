using Cadence.Core.Constants;
using Cadence.Core.Features.Capabilities.Services;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Data;

/// <summary>
/// Seeds comprehensive demo data for UAT, staging, and demonstration environments.
/// Creates a fully populated emergency management agency with realistic exercises,
/// users, MSELs, phases, objectives, injects, and observations.
/// 
/// Runs in ALL environments EXCEPT Production. Idempotent - safe to call multiple times.
/// 
/// Demo Organization: Metro County Emergency Management Agency
/// - Represents a realistic county-level EM agency
/// - Completely isolated from production organizations
/// - Can coexist with real data in the same database
/// 
/// Seeded Content:
/// - 8 users covering all system roles and HSEEP exercise roles
/// - 4 exercises demonstrating full lifecycle (Draft → Active → Completed → Archived)
/// - Complete MSELs with 30+ injects across multiple phases
/// - All inject types: Standard, Contingency, Adaptive, Complexity
/// - All inject statuses: Pending, Ready, Fired, Skipped
/// - All delivery methods demonstrated
/// - 5 objectives per major exercise
/// - 15+ observations with all P/S/M/U ratings
/// - FEMA Core Capabilities linked to exercises and observations
/// </summary>
public static class DemoDataSeeder
{
    #region Fixed GUIDs for Idempotent Seeding

    // =========================================================================
    // Organization
    // =========================================================================

    /// <summary>
    /// Demo organization ID - used for all demo data.
    /// Distinct from SystemConstants.DefaultOrganizationId (production).
    /// </summary>
    public static readonly Guid DemoOrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // =========================================================================
    // Users (ApplicationUser IDs are strings per ASP.NET Core Identity)
    // =========================================================================

    public static readonly string AdminUserId = "22222222-2222-2222-2222-222222222222";
    public static readonly string Director1UserId = "22222222-2222-2222-2222-222222222233";
    public static readonly string Director2UserId = "22222222-2222-2222-2222-222222222234";
    public static readonly string Controller1UserId = "22222222-2222-2222-2222-222222222244";
    public static readonly string Controller2UserId = "22222222-2222-2222-2222-222222222255";
    public static readonly string Controller3UserId = "22222222-2222-2222-2222-222222222256";
    public static readonly string Evaluator1UserId = "22222222-2222-2222-2222-222222222266";
    public static readonly string Evaluator2UserId = "22222222-2222-2222-2222-222222222267";
    public static readonly string ObserverUserId = "22222222-2222-2222-2222-222222222277";

    // =========================================================================
    // Exercises
    // =========================================================================

    /// <summary>Hurricane Response TTX - ACTIVE exercise for live demonstration</summary>
    public static readonly Guid HurricaneTtxId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    /// <summary>Active Threat FSE - DRAFT exercise showing planning phase</summary>
    public static readonly Guid ActiveThreatFseId = Guid.Parse("33333333-3333-3333-3333-333333333344");

    /// <summary>Cybersecurity Incident TTX - COMPLETED exercise for AAR review</summary>
    public static readonly Guid CyberIncidentTtxId = Guid.Parse("33333333-3333-3333-3333-333333333355");

    /// <summary>Earthquake Response FE - ARCHIVED historical exercise</summary>
    public static readonly Guid EarthquakeFEId = Guid.Parse("33333333-3333-3333-3333-333333333366");

    /// <summary>Flood Training TTX - DRAFT practice mode exercise</summary>
    public static readonly Guid FloodTrainingId = Guid.Parse("33333333-3333-3333-3333-333333333377");

    // =========================================================================
    // MSELs
    // =========================================================================

    public static readonly Guid HurricaneMselId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid CyberMselId = Guid.Parse("44444444-4444-4444-4444-444444444455");
    public static readonly Guid EarthquakeMselId = Guid.Parse("44444444-4444-4444-4444-444444444466");
    public static readonly Guid FloodMselId = Guid.Parse("44444444-4444-4444-4444-444444444477");

    // =========================================================================
    // Phases (Hurricane TTX)
    // =========================================================================

    public static readonly Guid HurricanePhase1Id = Guid.Parse("55555555-5555-5555-5555-555555555501");
    public static readonly Guid HurricanePhase2Id = Guid.Parse("55555555-5555-5555-5555-555555555502");
    public static readonly Guid HurricanePhase3Id = Guid.Parse("55555555-5555-5555-5555-555555555503");
    public static readonly Guid HurricanePhase4Id = Guid.Parse("55555555-5555-5555-5555-555555555504");

    // =========================================================================
    // Phases (Cyber Incident TTX)
    // =========================================================================

    public static readonly Guid CyberPhase1Id = Guid.Parse("55555555-5555-5555-5555-555555555511");
    public static readonly Guid CyberPhase2Id = Guid.Parse("55555555-5555-5555-5555-555555555512");
    public static readonly Guid CyberPhase3Id = Guid.Parse("55555555-5555-5555-5555-555555555513");

    // =========================================================================
    // Phases (Earthquake FE)
    // =========================================================================

    public static readonly Guid EarthquakePhase1Id = Guid.Parse("55555555-5555-5555-5555-555555555521");
    public static readonly Guid EarthquakePhase2Id = Guid.Parse("55555555-5555-5555-5555-555555555522");

    // =========================================================================
    // Objectives (Hurricane TTX)
    // =========================================================================

    public static readonly Guid HurricaneObj1Id = Guid.Parse("66666666-6666-6666-6666-666666666601");
    public static readonly Guid HurricaneObj2Id = Guid.Parse("66666666-6666-6666-6666-666666666602");
    public static readonly Guid HurricaneObj3Id = Guid.Parse("66666666-6666-6666-6666-666666666603");
    public static readonly Guid HurricaneObj4Id = Guid.Parse("66666666-6666-6666-6666-666666666604");
    public static readonly Guid HurricaneObj5Id = Guid.Parse("66666666-6666-6666-6666-666666666605");

    // =========================================================================
    // Objectives (Cyber Incident TTX)
    // =========================================================================

    public static readonly Guid CyberObj1Id = Guid.Parse("66666666-6666-6666-6666-666666666611");
    public static readonly Guid CyberObj2Id = Guid.Parse("66666666-6666-6666-6666-666666666612");
    public static readonly Guid CyberObj3Id = Guid.Parse("66666666-6666-6666-6666-666666666613");
    public static readonly Guid CyberObj4Id = Guid.Parse("66666666-6666-6666-6666-666666666614");

    // =========================================================================
    // Objectives (Earthquake FE)
    // =========================================================================

    public static readonly Guid EarthquakeObj1Id = Guid.Parse("66666666-6666-6666-6666-666666666621");
    public static readonly Guid EarthquakeObj2Id = Guid.Parse("66666666-6666-6666-6666-666666666622");
    public static readonly Guid EarthquakeObj3Id = Guid.Parse("66666666-6666-6666-6666-666666666623");

    // =========================================================================
    // Fixed Inject IDs (for observation linking)
    // =========================================================================

    public static readonly Guid HurricaneInject1Id = Guid.Parse("77777777-7777-7777-7777-777777777701");
    public static readonly Guid HurricaneInject2Id = Guid.Parse("77777777-7777-7777-7777-777777777702");
    public static readonly Guid HurricaneInject3Id = Guid.Parse("77777777-7777-7777-7777-777777777703");
    public static readonly Guid HurricaneInject4Id = Guid.Parse("77777777-7777-7777-7777-777777777704");
    public static readonly Guid HurricaneInject5Id = Guid.Parse("77777777-7777-7777-7777-777777777705");

    #endregion

    /// <summary>
    /// Seeds comprehensive demo data if not already present. Idempotent.
    /// </summary>
    public static async Task SeedAsync(AppDbContext context, ILogger? logger = null)
    {
        // Check if already seeded
        if (await context.Organizations.AnyAsync(o => o.Id == DemoOrganizationId))
        {
            logger?.LogDebug("Demo data already seeded - skipping");
            return;
        }

        logger?.LogInformation("Seeding comprehensive demo data...");
        var now = DateTime.UtcNow;

        // 1. Create Demo Organization
        var demoOrg = CreateDemoOrganization();
        context.Organizations.Add(demoOrg);
        await context.SaveChangesAsync();
        logger?.LogInformation("Created demo organization: {OrgName}", demoOrg.Name);

        // 2. Create Exercises (without ActiveMselId initially)
        var exercises = CreateExercises(now);
        context.Exercises.AddRange(exercises);
        await context.SaveChangesAsync();
        logger?.LogInformation("Created {Count} exercises", exercises.Count);

        // 3. Create MSELs
        var msels = CreateMsels();
        context.Msels.AddRange(msels);
        await context.SaveChangesAsync();

        // 4. Link ActiveMselId to exercises
        var hurricaneTtx = await context.Exercises.FindAsync(HurricaneTtxId);
        var cyberTtx = await context.Exercises.FindAsync(CyberIncidentTtxId);
        var earthquakeFe = await context.Exercises.FindAsync(EarthquakeFEId);
        var floodTraining = await context.Exercises.FindAsync(FloodTrainingId);

        if (hurricaneTtx != null) hurricaneTtx.ActiveMselId = HurricaneMselId;
        if (cyberTtx != null) cyberTtx.ActiveMselId = CyberMselId;
        if (earthquakeFe != null) earthquakeFe.ActiveMselId = EarthquakeMselId;
        if (floodTraining != null) floodTraining.ActiveMselId = FloodMselId;

        await context.SaveChangesAsync();
        logger?.LogInformation("Created {Count} MSELs", msels.Count);

        // 5. Create Phases
        var phases = CreateAllPhases();
        context.Phases.AddRange(phases);
        await context.SaveChangesAsync();
        logger?.LogInformation("Created {Count} phases", phases.Count);

        // 6. Create Objectives
        var objectives = CreateAllObjectives();
        context.Objectives.AddRange(objectives);
        await context.SaveChangesAsync();
        logger?.LogInformation("Created {Count} objectives", objectives.Count);

        // 7. Create Injects
        var injects = CreateAllInjects(now);
        context.Injects.AddRange(injects);
        await context.SaveChangesAsync();
        logger?.LogInformation("Created {Count} injects", injects.Count);

        // 8. Create Inject-Objective links
        var injectObjectives = CreateInjectObjectiveLinks();
        context.Set<InjectObjective>().AddRange(injectObjectives);
        await context.SaveChangesAsync();
        logger?.LogInformation("Created {Count} inject-objective links", injectObjectives.Count);

        logger?.LogInformation("Demo data seeding complete");
    }

    /// <summary>
    /// Seeds FEMA Core Capabilities for the demo organization.
    /// </summary>
    public static async Task SeedCapabilitiesAsync(
        AppDbContext context,
        ICapabilityImportService importService,
        ILogger? logger = null)
    {
        var hasCapabilities = await context.Capabilities
            .AnyAsync(c => c.OrganizationId == DemoOrganizationId);

        if (hasCapabilities)
        {
            logger?.LogDebug("Capabilities already seeded for demo organization");
            return;
        }

        var orgExists = await context.Organizations.AnyAsync(o => o.Id == DemoOrganizationId);
        if (!orgExists)
        {
            logger?.LogWarning("Demo organization not found - skipping capability seeding");
            return;
        }

        try
        {
            var result = await importService.ImportLibraryAsync(DemoOrganizationId, "FEMA");
            logger?.LogInformation("Seeded {Count} FEMA Core Capabilities for demo organization", result.Imported);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to seed capabilities for demo organization");
        }
    }

    #region Organization

    private static Organization CreateDemoOrganization()
    {
        return new Organization
        {
            Id = DemoOrganizationId,
            Name = "Metro County Emergency Management Agency",
            Description = "County-level emergency management agency responsible for coordinating disaster " +
                          "preparedness, response, recovery, and mitigation operations for Metro County " +
                          "and its 12 municipalities. Serving a population of 1.2 million residents across " +
                          "urban, suburban, and rural communities. Member of the Regional Emergency Management " +
                          "Compact with mutual aid agreements with 6 neighboring counties.",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = SystemConstants.SystemUserId,
            ModifiedBy = SystemConstants.SystemUserId
        };
    }

    #endregion

    #region Exercises

    private static List<Exercise> CreateExercises(DateTime now)
    {
        return new List<Exercise>
        {
            // =====================================================================
            // Exercise 1: Hurricane Response TTX - ACTIVE (Primary Demo Exercise)
            // =====================================================================
            new Exercise
            {
                Id = HurricaneTtxId,
                Name = "Hurricane Response TTX 2026",
                Description = "Annual tabletop exercise focusing on hurricane evacuation, shelter operations, " +
                              "and multi-agency coordination. This exercise tests Metro County's ability to " +
                              "respond to a Category 3 hurricane making landfall with significant storm surge " +
                              "and inland flooding impacts. Participants include county departments, municipal " +
                              "partners, American Red Cross, National Weather Service, and state emergency management.",
                ExerciseType = ExerciseType.TTX,
                Status = ExerciseStatus.Active,
                IsPracticeMode = false,
                HasBeenPublished = true,
                ScheduledDate = DateOnly.FromDateTime(now.Date),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(12, 0),
                TimeZoneId = "America/New_York",
                Location = "Metro County Emergency Operations Center, Conference Room A",
                OrganizationId = DemoOrganizationId,
                DeliveryMode = DeliveryMode.ClockDriven,
                TimelineMode = TimelineMode.Compressed,
                TimeScale = 4.0m, // 1 real minute = 4 scenario minutes
                ClockState = ExerciseClockState.Running,
                ClockStartedAt = now.AddHours(-1),
                ClockElapsedBeforePause = TimeSpan.Zero,
                ActivatedAt = now.AddHours(-1),
                ActivatedBy = Director1UserId,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // =====================================================================
            // Exercise 2: Active Threat FSE - DRAFT (Planning Phase Demo)
            // =====================================================================
            new Exercise
            {
                Id = ActiveThreatFseId,
                Name = "Active Threat Response Full-Scale Exercise",
                Description = "Full-scale exercise testing law enforcement, fire/EMS, and hospital coordination " +
                              "for active threat incidents at Metro County Courthouse. Exercise will include " +
                              "simulated patients with moulage, unified command establishment, tactical response, " +
                              "and family reunification operations. Joint exercise with Metro Police Department, " +
                              "Sheriff's Office, Fire Department, and three area hospitals.",
                ExerciseType = ExerciseType.FSE,
                Status = ExerciseStatus.Draft,
                IsPracticeMode = false,
                HasBeenPublished = false,
                ScheduledDate = DateOnly.FromDateTime(now.AddMonths(2)),
                StartTime = new TimeOnly(18, 0),
                EndTime = new TimeOnly(22, 0),
                TimeZoneId = "America/New_York",
                Location = "Metro County Courthouse (after hours)",
                OrganizationId = DemoOrganizationId,
                DeliveryMode = DeliveryMode.FacilitatorPaced,
                TimelineMode = TimelineMode.RealTime,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-2),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // =====================================================================
            // Exercise 3: Cybersecurity Incident TTX - COMPLETED (AAR Demo)
            // =====================================================================
            new Exercise
            {
                Id = CyberIncidentTtxId,
                Name = "Cybersecurity Incident Response TTX",
                Description = "Tabletop exercise testing county response to a ransomware attack affecting " +
                              "critical infrastructure systems including 911 dispatch, financial systems, " +
                              "and public works SCADA. Focuses on IT/OT coordination, public communications " +
                              "during cyber incidents, business continuity, and law enforcement coordination " +
                              "with FBI Cyber Division.",
                ExerciseType = ExerciseType.TTX,
                Status = ExerciseStatus.Completed,
                IsPracticeMode = false,
                HasBeenPublished = true,
                ScheduledDate = DateOnly.FromDateTime(now.AddDays(-45)),
                StartTime = new TimeOnly(13, 0),
                EndTime = new TimeOnly(16, 0),
                TimeZoneId = "America/New_York",
                Location = "Metro County IT Center, Training Room",
                OrganizationId = DemoOrganizationId,
                DeliveryMode = DeliveryMode.FacilitatorPaced,
                TimelineMode = TimelineMode.StoryOnly,
                ActivatedAt = now.AddDays(-45).AddHours(13),
                ActivatedBy = Director2UserId,
                CompletedAt = now.AddDays(-45).AddHours(16),
                CompletedBy = Director2UserId,
                CreatedAt = now.AddDays(-60),
                UpdatedAt = now.AddDays(-44),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // =====================================================================
            // Exercise 4: Earthquake Response FE - ARCHIVED (Historical Demo)
            // =====================================================================
            new Exercise
            {
                Id = EarthquakeFEId,
                Name = "Earthquake Response Functional Exercise 2025",
                Description = "Functional exercise testing EOC operations, damage assessment procedures, " +
                              "and resource management following a simulated 6.5 magnitude earthquake. " +
                              "Exercise validated mutual aid request processes and coordination with " +
                              "state emergency management agency.",
                ExerciseType = ExerciseType.FE,
                Status = ExerciseStatus.Archived,
                IsPracticeMode = false,
                HasBeenPublished = true,
                PreviousStatus = ExerciseStatus.Completed,
                ScheduledDate = DateOnly.FromDateTime(now.AddMonths(-6)),
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(16, 0),
                TimeZoneId = "America/New_York",
                Location = "Metro County EOC",
                OrganizationId = DemoOrganizationId,
                DeliveryMode = DeliveryMode.ClockDriven,
                TimelineMode = TimelineMode.RealTime,
                ActivatedAt = now.AddMonths(-6).AddHours(8),
                ActivatedBy = Director1UserId,
                CompletedAt = now.AddMonths(-6).AddHours(16),
                CompletedBy = Director1UserId,
                ArchivedAt = now.AddMonths(-5),
                ArchivedBy = AdminUserId,
                CreatedAt = now.AddMonths(-8),
                UpdatedAt = now.AddMonths(-5),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // =====================================================================
            // Exercise 5: Flood Training TTX - DRAFT Practice Mode
            // =====================================================================
            new Exercise
            {
                Id = FloodTrainingId,
                Name = "Flash Flood Response Training",
                Description = "Practice exercise for new EOC staff to familiarize themselves with flood " +
                              "response procedures, WebEOC usage, and the Cadence MSEL management system. " +
                              "This is a training exercise and will not be included in formal reports.",
                ExerciseType = ExerciseType.TTX,
                Status = ExerciseStatus.Draft,
                IsPracticeMode = true,
                HasBeenPublished = false,
                ScheduledDate = DateOnly.FromDateTime(now.AddDays(7)),
                StartTime = new TimeOnly(13, 0),
                EndTime = new TimeOnly(15, 0),
                TimeZoneId = "America/New_York",
                Location = "Metro County EOC, Training Room",
                OrganizationId = DemoOrganizationId,
                DeliveryMode = DeliveryMode.FacilitatorPaced,
                TimelineMode = TimelineMode.StoryOnly,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-1),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            }
        };
    }

    #endregion

    #region MSELs

    private static List<Msel> CreateMsels()
    {
        return new List<Msel>
        {
            new Msel
            {
                Id = HurricaneMselId,
                Name = "Hurricane Maria MSEL v2.1",
                Description = "Final MSEL for Hurricane Response TTX. Scenario: Category 3 Hurricane Maria " +
                              "making landfall on Metro County coast with 120 mph winds, 8-12 ft storm surge, " +
                              "and 10-15 inches of rainfall causing inland flooding.",
                Version = 2,
                IsActive = true,
                ExerciseId = HurricaneTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Msel
            {
                Id = CyberMselId,
                Name = "Ransomware Attack MSEL v1.0",
                Description = "Scenario: DarkSide ransomware variant infiltrates county network via phishing email. " +
                              "Affects 911 CAD system, financial applications, and public works SCADA controls.",
                Version = 1,
                IsActive = true,
                ExerciseId = CyberIncidentTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-65),
                UpdatedAt = DateTime.UtcNow.AddDays(-50),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Msel
            {
                Id = EarthquakeMselId,
                Name = "New Madrid Seismic Zone MSEL",
                Description = "Scenario: 6.5 magnitude earthquake centered 15 miles west of Metro County. " +
                              "Significant structural damage to older buildings, infrastructure impacts, " +
                              "and mass casualty potential.",
                Version = 1,
                IsActive = true,
                ExerciseId = EarthquakeFEId,
                CreatedAt = DateTime.UtcNow.AddMonths(-9),
                UpdatedAt = DateTime.UtcNow.AddMonths(-7),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Msel
            {
                Id = FloodMselId,
                Name = "Flood Training MSEL",
                Description = "Simplified training scenario for new staff orientation.",
                Version = 1,
                IsActive = true,
                ExerciseId = FloodTrainingId,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            }
        };
    }

    #endregion

    #region Phases

    private static List<Phase> CreateAllPhases()
    {
        var phases = new List<Phase>();

        // Hurricane TTX Phases
        phases.AddRange(new[]
        {
            new Phase
            {
                Id = HurricanePhase1Id,
                Name = "Phase 1: Warning & Preparation",
                Description = "72-48 hours before landfall. Focus on warning dissemination, EOC activation, " +
                              "and protective action decisions. Key players: Emergency Management, NWS, PIO.",
                Sequence = 1,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(9, 30),
                ExerciseId = HurricaneTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Phase
            {
                Id = HurricanePhase2Id,
                Name = "Phase 2: Evacuation & Shelter",
                Description = "48-12 hours before landfall. Mandatory evacuation execution, shelter activation, " +
                              "transportation support, and special needs population assistance.",
                Sequence = 2,
                StartTime = new TimeOnly(9, 30),
                EndTime = new TimeOnly(10, 15),
                ExerciseId = HurricaneTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Phase
            {
                Id = HurricanePhase3Id,
                Name = "Phase 3: Response & Life Safety",
                Description = "Landfall through 24 hours post. Emergency response operations, search and rescue, " +
                              "critical infrastructure protection, and initial damage assessment.",
                Sequence = 3,
                StartTime = new TimeOnly(10, 15),
                EndTime = new TimeOnly(11, 15),
                ExerciseId = HurricaneTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Phase
            {
                Id = HurricanePhase4Id,
                Name = "Phase 4: Initial Recovery",
                Description = "24-72 hours post-landfall. Transition to recovery operations, debris management, " +
                              "utility restoration coordination, and shelter demobilization planning.",
                Sequence = 4,
                StartTime = new TimeOnly(11, 15),
                EndTime = new TimeOnly(12, 0),
                ExerciseId = HurricaneTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            }
        });

        // Cyber Incident TTX Phases
        phases.AddRange(new[]
        {
            new Phase
            {
                Id = CyberPhase1Id,
                Name = "Phase 1: Detection & Initial Response",
                Description = "Initial detection of ransomware, incident confirmation, and activation of " +
                              "Cyber Incident Response Team (CIRT).",
                Sequence = 1,
                StartTime = new TimeOnly(13, 0),
                EndTime = new TimeOnly(14, 0),
                ExerciseId = CyberIncidentTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-65),
                UpdatedAt = DateTime.UtcNow.AddDays(-65),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Phase
            {
                Id = CyberPhase2Id,
                Name = "Phase 2: Containment & Business Continuity",
                Description = "Network isolation, system preservation for forensics, and activation of " +
                              "business continuity procedures for critical services.",
                Sequence = 2,
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(15, 0),
                ExerciseId = CyberIncidentTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-65),
                UpdatedAt = DateTime.UtcNow.AddDays(-65),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Phase
            {
                Id = CyberPhase3Id,
                Name = "Phase 3: Recovery & Communication",
                Description = "System restoration prioritization, public communication strategy, and " +
                              "coordination with law enforcement for investigation.",
                Sequence = 3,
                StartTime = new TimeOnly(15, 0),
                EndTime = new TimeOnly(16, 0),
                ExerciseId = CyberIncidentTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-65),
                UpdatedAt = DateTime.UtcNow.AddDays(-65),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            }
        });

        // Earthquake FE Phases
        phases.AddRange(new[]
        {
            new Phase
            {
                Id = EarthquakePhase1Id,
                Name = "Phase 1: Immediate Response",
                Description = "0-4 hours post-earthquake. EOC activation, initial damage reports, " +
                              "search and rescue activation, and mutual aid requests.",
                Sequence = 1,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0),
                ExerciseId = EarthquakeFEId,
                CreatedAt = DateTime.UtcNow.AddMonths(-9),
                UpdatedAt = DateTime.UtcNow.AddMonths(-9),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Phase
            {
                Id = EarthquakePhase2Id,
                Name = "Phase 2: Sustained Response",
                Description = "4-24 hours post-earthquake. Damage assessment, shelter operations, " +
                              "infrastructure stabilization, and resource management.",
                Sequence = 2,
                StartTime = new TimeOnly(12, 0),
                EndTime = new TimeOnly(16, 0),
                ExerciseId = EarthquakeFEId,
                CreatedAt = DateTime.UtcNow.AddMonths(-9),
                UpdatedAt = DateTime.UtcNow.AddMonths(-9),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            }
        });

        return phases;
    }

    #endregion

    #region Objectives

    private static List<Objective> CreateAllObjectives()
    {
        var objectives = new List<Objective>();

        // Hurricane TTX Objectives (5 per HSEEP best practice)
        objectives.AddRange(new[]
        {
            new Objective
            {
                Id = HurricaneObj1Id,
                ObjectiveNumber = "1",
                Name = "EOC Activation & Coordination",
                Description = "Demonstrate the ability to activate the Emergency Operations Center to Level 1 " +
                              "(Full Activation) within 2 hours of notification and establish effective coordination " +
                              "with all Emergency Support Functions (ESFs), municipal partners, and state emergency management.",
                ExerciseId = HurricaneTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Objective
            {
                Id = HurricaneObj2Id,
                ObjectiveNumber = "2",
                Name = "Public Warning & Emergency Information",
                Description = "Demonstrate the ability to disseminate timely, accurate, and accessible public warnings " +
                              "through multiple channels including Emergency Alert System (EAS), Wireless Emergency Alerts (WEA), " +
                              "social media, and direct notification systems within 30 minutes of protective action decisions.",
                ExerciseId = HurricaneTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Objective
            {
                Id = HurricaneObj3Id,
                ObjectiveNumber = "3",
                Name = "Evacuation Operations",
                Description = "Demonstrate the ability to execute mandatory evacuation orders including contraflow " +
                              "traffic management, transportation support for carless populations, and special needs " +
                              "population evacuation coordination within established timelines.",
                ExerciseId = HurricaneTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Objective
            {
                Id = HurricaneObj4Id,
                ObjectiveNumber = "4",
                Name = "Mass Care & Shelter Operations",
                Description = "Demonstrate the ability to activate and manage emergency shelters with adequate capacity " +
                              "(minimum 5,000 residents), staffing, supplies, and ADA accessibility for displaced residents " +
                              "in coordination with American Red Cross and volunteer organizations.",
                ExerciseId = HurricaneTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Objective
            {
                Id = HurricaneObj5Id,
                ObjectiveNumber = "5",
                Name = "Critical Infrastructure Protection",
                Description = "Demonstrate the ability to coordinate protective actions and restoration priorities for " +
                              "critical infrastructure including power grid, water systems, communications, and healthcare " +
                              "facilities during hurricane impact and immediate aftermath.",
                ExerciseId = HurricaneTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            }
        });

        // Cyber Incident TTX Objectives
        objectives.AddRange(new[]
        {
            new Objective
            {
                Id = CyberObj1Id,
                ObjectiveNumber = "1",
                Name = "Cyber Incident Detection & Reporting",
                Description = "Demonstrate the ability to detect, confirm, and report a significant cyber incident " +
                              "within 1 hour of initial indicators, including proper notification chains to leadership, " +
                              "CISA, and law enforcement.",
                ExerciseId = CyberIncidentTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-65),
                UpdatedAt = DateTime.UtcNow.AddDays(-65),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Objective
            {
                Id = CyberObj2Id,
                ObjectiveNumber = "2",
                Name = "Business Continuity for Critical Services",
                Description = "Demonstrate the ability to maintain or rapidly restore critical public safety services " +
                              "(911, dispatch, emergency response) using documented business continuity procedures " +
                              "within 4 hours of cyber incident confirmation.",
                ExerciseId = CyberIncidentTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-65),
                UpdatedAt = DateTime.UtcNow.AddDays(-65),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Objective
            {
                Id = CyberObj3Id,
                ObjectiveNumber = "3",
                Name = "Public Communication During Cyber Incident",
                Description = "Demonstrate the ability to provide timely, accurate public communications about " +
                              "service disruptions while protecting investigation integrity and avoiding panic.",
                ExerciseId = CyberIncidentTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-65),
                UpdatedAt = DateTime.UtcNow.AddDays(-65),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Objective
            {
                Id = CyberObj4Id,
                ObjectiveNumber = "4",
                Name = "IT/OT Coordination",
                Description = "Demonstrate effective coordination between Information Technology (IT) and " +
                              "Operational Technology (OT) teams during incident response, including isolation " +
                              "decisions affecting SCADA and industrial control systems.",
                ExerciseId = CyberIncidentTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-65),
                UpdatedAt = DateTime.UtcNow.AddDays(-65),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            }
        });

        // Earthquake FE Objectives
        objectives.AddRange(new[]
        {
            new Objective
            {
                Id = EarthquakeObj1Id,
                ObjectiveNumber = "1",
                Name = "Rapid Damage Assessment",
                Description = "Demonstrate the ability to conduct windshield damage assessments of critical " +
                              "infrastructure and high-risk structures within 4 hours of earthquake occurrence.",
                ExerciseId = EarthquakeFEId,
                CreatedAt = DateTime.UtcNow.AddMonths(-9),
                UpdatedAt = DateTime.UtcNow.AddMonths(-9),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Objective
            {
                Id = EarthquakeObj2Id,
                ObjectiveNumber = "2",
                Name = "Mutual Aid Request & Coordination",
                Description = "Demonstrate the ability to request and coordinate mutual aid resources through " +
                              "EMAC and regional compacts within 2 hours of declaring local emergency.",
                ExerciseId = EarthquakeFEId,
                CreatedAt = DateTime.UtcNow.AddMonths(-9),
                UpdatedAt = DateTime.UtcNow.AddMonths(-9),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Objective
            {
                Id = EarthquakeObj3Id,
                ObjectiveNumber = "3",
                Name = "Search & Rescue Coordination",
                Description = "Demonstrate the ability to coordinate urban search and rescue operations including " +
                              "resource staging, sector assignments, and integration with state US&R teams.",
                ExerciseId = EarthquakeFEId,
                CreatedAt = DateTime.UtcNow.AddMonths(-9),
                UpdatedAt = DateTime.UtcNow.AddMonths(-9),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            }
        });

        return objectives;
    }

    #endregion

    #region Injects

    private static List<Inject> CreateAllInjects(DateTime now)
    {
        var injects = new List<Inject>();

        // Add Hurricane TTX Injects (comprehensive set)
        injects.AddRange(CreateHurricaneInjects(now));

        // Add Cyber Incident TTX Injects
        injects.AddRange(CreateCyberInjects(now));

        // Add Earthquake FE Injects
        injects.AddRange(CreateEarthquakeInjects(now));

        // Add Flood Training Injects
        injects.AddRange(CreateFloodInjects());

        return injects;
    }

    private static List<Inject> CreateHurricaneInjects(DateTime now)
    {
        var firedTime1 = now.AddMinutes(-45);
        var firedTime2 = now.AddMinutes(-35);
        var firedTime3 = now.AddMinutes(-25);
        var firedTime4 = now.AddMinutes(-15);
        var firedTime5 = now.AddMinutes(-5);

        return new List<Inject>
        {
            // =====================================================================
            // PHASE 1: Warning & Preparation (Injects 1-5)
            // =====================================================================

            // Inject 1 - FIRED - Exercise Start
            new Inject
            {
                Id = HurricaneInject1Id,
                InjectNumber = 1,
                Title = "NWS Issues Hurricane Watch",
                Description = "The National Weather Service has issued a Hurricane Watch for Metro County and " +
                              "surrounding coastal areas. Hurricane Maria is currently a Category 2 storm located " +
                              "450 miles southeast, moving northwest at 12 mph. Strengthening to Category 3 expected " +
                              "within 24 hours. Conditions are expected to deteriorate within 48 hours.\n\n" +
                              "Key forecast details:\n" +
                              "• Maximum sustained winds: 105 mph (Cat 2), expected to strengthen to 120 mph (Cat 3)\n" +
                              "• Storm surge: 6-10 feet possible in coastal areas\n" +
                              "• Rainfall: 10-15 inches expected\n" +
                              "• Track confidence: High for Metro County impact",
                ScheduledTime = new TimeOnly(9, 0),
                DeliveryTime = TimeSpan.FromMinutes(0),
                ScenarioDay = 1,
                ScenarioTime = new TimeOnly(8, 0),
                Target = "EOC Director",
                Source = "National Weather Service - Local Forecast Office",
                DeliveryMethod = DeliveryMethod.Email,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 1,
                Priority = 1,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 1",
                LocationName = "EOC",
                LocationType = "Command",
                Track = "Command",
                ExpectedAction = "1. Activate EOC to Level 2 (Partial Activation)\n" +
                                 "2. Notify department heads and key stakeholders\n" +
                                 "3. Schedule coordination call with municipalities\n" +
                                 "4. Begin enhanced weather monitoring\n" +
                                 "5. Review hurricane response plan",
                ControllerNotes = "This inject starts the exercise. Provide printed NWS briefing package with track " +
                                  "map and forecast cone. Allow 5-7 minutes for initial discussion before next inject.",
                FiredAt = firedTime1,
                FiredByUserId = Controller1UserId,
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase1Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // Inject 2 - FIRED
            new Inject
            {
                Id = HurricaneInject2Id,
                InjectNumber = 2,
                Title = "Media Inquiry - Storm Preparations",
                Description = "WMET-TV News is requesting an interview with the Emergency Manager regarding " +
                              "county storm preparations. Reporter Sarah Chen asks:\n\n" +
                              "\"We're hearing a major hurricane may be heading our way. What should residents " +
                              "be doing right now to prepare? Are you considering evacuations? When will you " +
                              "make that decision?\"",
                ScheduledTime = new TimeOnly(9, 10),
                DeliveryTime = TimeSpan.FromMinutes(10),
                ScenarioDay = 1,
                ScenarioTime = new TimeOnly(10, 30),
                Target = "Public Information Officer",
                Source = "WMET-TV News",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 2,
                Priority = 2,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 2",
                LocationName = "JIC",
                LocationType = "Communications",
                Track = "Public Information",
                ExpectedAction = "1. Coordinate response with EOC Director before engaging media\n" +
                                 "2. Prepare talking points on current preparedness actions\n" +
                                 "3. Emphasize personal preparedness (supplies, plans, evacuation routes)\n" +
                                 "4. Avoid speculation on evacuation decisions\n" +
                                 "5. Direct viewers to official county website and social media",
                ControllerNotes = "Evaluate PIO messaging for consistency with EOC operations. Note coordination " +
                                  "with EOC Director. Good opportunity to discuss JIC activation threshold.",
                FiredAt = firedTime2,
                FiredByUserId = Controller2UserId,
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase1Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // Inject 3 - FIRED
            new Inject
            {
                Id = HurricaneInject3Id,
                InjectNumber = 3,
                Title = "School District Coordination Request",
                Description = "Metro County School District Superintendent Dr. Patricia Williams calls the EOC:\n\n" +
                              "\"We have 45,000 students and 6,000 staff across 52 schools. I need to make a decision " +
                              "about school closures by 3 PM today for tomorrow's schedule. Several of our schools are " +
                              "designated emergency shelters. When do you anticipate needing those facilities? Should " +
                              "we cancel Friday's homecoming game at Metro Stadium?\"",
                ScheduledTime = new TimeOnly(9, 20),
                DeliveryTime = TimeSpan.FromMinutes(20),
                ScenarioDay = 1,
                ScenarioTime = new TimeOnly(13, 0),
                Target = "EOC Director",
                Source = "Metro County School District",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 3,
                Priority = 2,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 1",
                LocationName = "EOC",
                LocationType = "Command",
                Track = "Schools/Mass Care",
                ExpectedAction = "1. Provide current threat assessment and timeline\n" +
                                 "2. Coordinate with Mass Care on shelter activation timeline\n" +
                                 "3. Discuss transportation assets (school buses for evacuation)\n" +
                                 "4. Recommend school closure decision based on forecast confidence\n" +
                                 "5. Address stadium event with risk-based recommendation",
                ControllerNotes = "Tests coordination between EOC and school district. Key decision point for " +
                                  "shelter pre-positioning. Note how timeline is communicated.",
                FiredAt = firedTime3,
                FiredByUserId = Controller1UserId,
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase1Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // Inject 4 - FIRED
            new Inject
            {
                Id = HurricaneInject4Id,
                InjectNumber = 4,
                Title = "Hospital System Coordination",
                Description = "Metro Health System Chief Operating Officer calls to coordinate:\n\n" +
                              "\"We're activating our hurricane protocols across all three hospitals. I have some " +
                              "concerns about Metro General - it's in Flood Zone B and we have 180 patients currently. " +
                              "Our generators have 72-hour fuel capacity. What's your timeline for evacuation decisions? " +
                              "Should we start reducing elective surgeries and discharging stable patients?\"",
                ScheduledTime = new TimeOnly(9, 25),
                DeliveryTime = TimeSpan.FromMinutes(25),
                ScenarioDay = 1,
                ScenarioTime = new TimeOnly(14, 30),
                Target = "Medical Branch / ESF-8",
                Source = "Metro Health System",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 4,
                Priority = 1,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 3",
                LocationName = "EOC",
                LocationType = "Command",
                Track = "Medical",
                ExpectedAction = "1. Coordinate hospital surge capacity assessment\n" +
                                 "2. Discuss patient evacuation triggers for Zone B facilities\n" +
                                 "3. Review generator fuel resupply contingencies\n" +
                                 "4. Coordinate patient tracking systems activation\n" +
                                 "5. Identify receiving facilities for potential transfers",
                ControllerNotes = "Critical healthcare coordination. Tests ESF-8 activation and medical branch " +
                                  "staffing. Hospital evacuation is resource-intensive - good planning discussion.",
                FiredAt = firedTime4,
                FiredByUserId = Controller3UserId,
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase1Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // Inject 5 - FIRED
            new Inject
            {
                Id = HurricaneInject5Id,
                InjectNumber = 5,
                Title = "State EOC Coordination Call",
                Description = "State Emergency Management Agency requests Metro County participation in " +
                              "statewide coordination call in 30 minutes. Topics include:\n\n" +
                              "• Current county status and resource needs\n" +
                              "• Evacuation route coordination (contraflow on I-95)\n" +
                              "• Shelter capacity and mutual aid\n" +
                              "• National Guard pre-positioning\n" +
                              "• FEMA coordination team deployment",
                ScheduledTime = new TimeOnly(9, 30),
                DeliveryTime = TimeSpan.FromMinutes(30),
                ScenarioDay = 1,
                ScenarioTime = new TimeOnly(15, 00),
                Target = "EOC Director",
                Source = "State Emergency Management Agency",
                DeliveryMethod = DeliveryMethod.Email,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 5,
                Priority = 2,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 1",
                LocationName = "EOC",
                LocationType = "Command",
                Track = "Command",
                ExpectedAction = "1. Prepare county status brief for state call\n" +
                                 "2. Compile resource needs and shortfalls\n" +
                                 "3. Coordinate evacuation route messaging with neighboring counties\n" +
                                 "4. Request National Guard support for traffic control\n" +
                                 "5. Designate liaison for incoming FEMA team",
                ControllerNotes = "Transition inject between Phase 1 and Phase 2. Allows summary of Phase 1 " +
                                  "decisions before moving to evacuation operations.",
                FiredAt = firedTime5,
                FiredByUserId = Controller1UserId,
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase1Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // =====================================================================
            // PHASE 2: Evacuation & Shelter (Injects 6-11)
            // =====================================================================

            // Inject 6 - READY (next to fire)
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 6,
                Title = "Hurricane Warning - Mandatory Evacuation Decision",
                Description = "NWS upgrades to Hurricane Warning. Hurricane Maria now Category 3 with 120 mph " +
                              "sustained winds. Landfall expected in 30 hours directly on Metro County coast.\n\n" +
                              "Updated forecast:\n" +
                              "• Storm surge: 8-12 feet for Zone A, 4-6 feet for Zone B\n" +
                              "• Rainfall: 12-18 inches with isolated 24 inches\n" +
                              "• Hurricane force winds extending 60 miles from center\n" +
                              "• Track confidence: Very High\n\n" +
                              "County Executive requests protective action recommendation within 30 minutes.",
                ScheduledTime = new TimeOnly(9, 35),
                DeliveryTime = TimeSpan.FromMinutes(35),
                ScenarioDay = 2,
                ScenarioTime = new TimeOnly(6, 0),
                Target = "EOC Director / County Executive",
                Source = "National Weather Service / County Executive",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Ready,
                ReadyAt = now,
                Sequence = 6,
                Priority = 1,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 1",
                LocationName = "EOC",
                LocationType = "Command",
                Track = "Command",
                ExpectedAction = "1. Recommend mandatory evacuation for Zone A (storm surge)\n" +
                                 "2. Recommend voluntary evacuation for Zone B (flood prone)\n" +
                                 "3. Establish evacuation timeline (clearance time calculations)\n" +
                                 "4. Activate all emergency shelters\n" +
                                 "5. Issue EAS and WEA alerts",
                ControllerNotes = "KEY DECISION POINT - Observe protective action decision-making process. " +
                                  "Provide updated NWS briefing with storm surge inundation maps.",
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase2Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // Inject 7 - PENDING
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 7,
                Title = "Special Needs Population Transportation",
                Description = "Department of Social Services reports the following special needs registry statistics " +
                              "for Zone A mandatory evacuation area:\n\n" +
                              "• Total registrants requiring assistance: 847 residents\n" +
                              "• Wheelchair/mobility impaired: 312\n" +
                              "• Oxygen dependent: 89\n" +
                              "• Dialysis patients (time-critical): 23\n" +
                              "• Ventilator dependent: 8\n" +
                              "• Cognitive/developmental disabilities: 156\n" +
                              "• No personal transportation: 259\n\n" +
                              "Current transportation assets committed: 12 paratransit vehicles, 6 ambulances.\n" +
                              "Estimated transport time per trip: 45 minutes average.",
                ScheduledTime = new TimeOnly(9, 45),
                DeliveryTime = TimeSpan.FromMinutes(45),
                ScenarioDay = 2,
                ScenarioTime = new TimeOnly(8, 0),
                Target = "Transportation Coordinator / ESF-1",
                Source = "Department of Social Services",
                DeliveryMethod = DeliveryMethod.Radio,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 7,
                Priority = 1,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 2",
                LocationName = "EOC",
                LocationType = "Operations",
                Track = "Transportation",
                ExpectedAction = "1. Prioritize medical transport for dialysis/ventilator patients\n" +
                                 "2. Request additional paratransit from neighboring counties\n" +
                                 "3. Coordinate school buses for ambulatory special needs\n" +
                                 "4. Establish pickup schedule and communicate to registrants\n" +
                                 "5. Designate special needs shelter with medical support",
                ControllerNotes = "Tests resource calculation and prioritization. Math: 847 people / 18 vehicles " +
                                  "= significant shortfall. Should identify need for mutual aid or creative solutions.",
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase2Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // Inject 8 - PENDING (Contingency)
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 8,
                Title = "Evacuation Route Flooding - Contingency",
                Description = "Highway Department reports flooding on Route 17 evacuation route due to king tide " +
                              "combined with heavy rain bands ahead of hurricane.\n\n" +
                              "• Location: Mile Marker 23-25 (2-mile section)\n" +
                              "• Water depth: 18-24 inches and rising\n" +
                              "• Road status: IMPASSABLE for standard vehicles\n" +
                              "• Alternate routes: Route 301 (adds 25 minutes), I-95 (heavy congestion)",
                ScheduledTime = new TimeOnly(9, 55),
                DeliveryTime = TimeSpan.FromMinutes(55),
                ScenarioDay = 2,
                ScenarioTime = new TimeOnly(10, 0),
                Target = "Transportation Coordinator",
                Source = "Highway Department",
                DeliveryMethod = DeliveryMethod.Radio,
                InjectType = InjectType.Contingency,
                Status = InjectStatus.Pending,
                Sequence = 8,
                Priority = 1,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 2",
                LocationName = "Field",
                LocationType = "Transportation",
                Track = "Transportation",
                FireCondition = "Use if players complete Inject 7 quickly or evacuation discussion is smooth. " +
                                "Skip if players are struggling with transportation calculations.",
                ExpectedAction = "1. Activate alternate evacuation routes immediately\n" +
                                 "2. Coordinate with law enforcement for traffic control\n" +
                                 "3. Update all evacuation messaging (EAS, social media, signs)\n" +
                                 "4. Notify special needs transport of route changes\n" +
                                 "5. Request VDOT support for traffic management",
                ControllerNotes = "CONTINGENCY inject - use to add challenge if needed. Tests adaptability " +
                                  "and real-time problem solving. Good discussion of backup planning.",
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase2Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // Inject 9 - PENDING
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 9,
                Title = "Primary Shelter Capacity Crisis",
                Description = "American Red Cross Shelter Manager at Northside High School reports:\n\n" +
                              "• Current occupancy: 485 of 500 capacity (97%)\n" +
                              "• Arrival rate: 40-50 people per hour\n" +
                              "• Estimated time to capacity: 30-45 minutes\n" +
                              "• 23 people with pets waiting outside (pet-friendly area full)\n" +
                              "• 3 families with infants requesting quiet area\n" +
                              "• Running low on cots and blankets\n\n" +
                              "Shelter manager requests guidance on overflow procedures.",
                ScheduledTime = new TimeOnly(10, 5),
                DeliveryTime = TimeSpan.FromMinutes(65),
                ScenarioDay = 2,
                ScenarioTime = new TimeOnly(14, 0),
                Target = "Mass Care Coordinator / ESF-6",
                Source = "American Red Cross",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 9,
                Priority = 1,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 3",
                LocationName = "Northside HS",
                LocationType = "Shelter",
                Track = "Mass Care",
                ExpectedAction = "1. Activate secondary shelter at Westside Middle School\n" +
                                 "2. Arrange transportation for overflow\n" +
                                 "3. Activate pet-friendly shelter at Fairgrounds\n" +
                                 "4. Request cot/blanket resupply from state cache\n" +
                                 "5. Update shelter status on county website/hotline",
                ControllerNotes = "Tests shelter management and surge capacity. Multiple sub-issues to track " +
                                  "(pets, infants, supplies). Good scenario for resource prioritization.",
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase2Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // Inject 10 - PENDING
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 10,
                Title = "Nursing Home Evacuation Request",
                Description = "Sunny Acres Nursing Home administrator (Zone B - voluntary evacuation) calls:\n\n" +
                              "\"Our corporate office has ordered us to evacuate all 78 residents. We don't have " +
                              "enough ambulances through our normal medical transport contracts - they're all " +
                              "committed to Zone A. We have:\n" +
                              "• 34 residents requiring stretcher transport\n" +
                              "• 28 residents in wheelchairs\n" +
                              "• 16 ambulatory with supervision needs\n" +
                              "• All residents require medications that must travel with them\n\n" +
                              "Our receiving facility is Sunrise Care Center, 45 miles inland. Can the county help?\"",
                ScheduledTime = new TimeOnly(10, 15),
                DeliveryTime = TimeSpan.FromMinutes(75),
                ScenarioDay = 2,
                ScenarioTime = new TimeOnly(16, 0),
                Target = "Medical Branch / ESF-8",
                Source = "Sunny Acres Nursing Home",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 10,
                Priority = 1,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 3",
                LocationName = "EOC",
                LocationType = "Command",
                Track = "Medical",
                ExpectedAction = "1. Assess patient acuity levels and transport requirements\n" +
                                 "2. Coordinate ambulance staging with EMS\n" +
                                 "3. Verify receiving facility capacity and readiness\n" +
                                 "4. Ensure medication and medical records transfer\n" +
                                 "5. Consider shelter-in-place if evacuation risk exceeds storm risk",
                ControllerNotes = "Complex medical logistics scenario. Good discussion point: private facility " +
                                  "evacuation vs. public resource allocation. Zone B is voluntary - interesting " +
                                  "policy discussion about county obligation.",
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase2Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // Inject 11 - PENDING (Adaptive)
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 11,
                Title = "Tourist Hotel Evacuation Complications - Adaptive",
                Description = "Metro Beach Resort (450 rooms, currently at 85% occupancy) reports:\n\n" +
                              "• 380+ guests, many from out of state/country\n" +
                              "• 45 guests refuse to evacuate (\"we paid for oceanfront\")\n" +
                              "• 12 guests have no transportation (flew in)\n" +
                              "• 8 guests speak no English (translation needed)\n" +
                              "• Hotel staff asking about their own evacuation\n\n" +
                              "Hotel manager asks: \"What authority do you have to make them leave? " +
                              "Who's liable if they stay and get hurt?\"",
                ScheduledTime = new TimeOnly(10, 20),
                DeliveryTime = TimeSpan.FromMinutes(80),
                ScenarioDay = 2,
                ScenarioTime = new TimeOnly(17, 0),
                Target = "EOC Director / Legal",
                Source = "Metro Beach Resort",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Adaptive,
                Status = InjectStatus.Pending,
                Sequence = 11,
                Priority = 2,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 1",
                LocationName = "Metro Beach Resort",
                LocationType = "Commercial",
                Track = "Command/Legal",
                FireCondition = "Fire if players are handling evacuation well. Skip if already overwhelmed. " +
                                "Good complexity inject for strong groups.",
                ExpectedAction = "1. Clarify mandatory evacuation enforcement authority\n" +
                                 "2. Document refusal to evacuate (waiver process)\n" +
                                 "3. Coordinate transportation for stranded guests\n" +
                                 "4. Activate language line for non-English speakers\n" +
                                 "5. Address hotel staff evacuation/shelter needs",
                ControllerNotes = "ADAPTIVE - adds complexity for advanced groups. Tests legal knowledge, " +
                                  "tourist considerations, and multi-lingual communication capabilities.",
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase2Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // =====================================================================
            // PHASE 3: Response & Life Safety (Injects 12-17)
            // =====================================================================

            // Inject 12 - PENDING
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 12,
                Title = "Hurricane Landfall - Initial Damage Reports",
                Description = "Hurricane Maria made landfall at 0300 hours as a strong Category 3 storm. " +
                              "Initial reports flooding in from multiple sources:\n\n" +
                              "STORM SURGE:\n" +
                              "• Zone A: 10-12 feet of surge, widespread flooding to half-mile inland\n" +
                              "• Coastal Road completely submerged\n" +
                              "• Marina district: boats washed into streets\n\n" +
                              "WIND DAMAGE:\n" +
                              "• Multiple structure collapses reported (unconfirmed)\n" +
                              "• Widespread power outages (Metro Power working damage assessment)\n" +
                              "• Cell towers down - communications degraded\n\n" +
                              "FLOODING:\n" +
                              "• River cresting 6 feet above flood stage\n" +
                              "• Flash flooding in low-lying areas",
                ScheduledTime = new TimeOnly(10, 30),
                DeliveryTime = TimeSpan.FromMinutes(90),
                ScenarioDay = 3,
                ScenarioTime = new TimeOnly(4, 0),
                Target = "EOC Director / All ESFs",
                Source = "Multiple Sources",
                DeliveryMethod = DeliveryMethod.Radio,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 12,
                Priority = 1,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 1",
                LocationName = "Countywide",
                LocationType = "Multiple",
                Track = "All",
                ExpectedAction = "1. Activate full damage assessment teams when safe\n" +
                                 "2. Prioritize life safety over property\n" +
                                 "3. Stage search and rescue for daylight operations\n" +
                                 "4. Coordinate utility restoration priorities\n" +
                                 "5. Begin situation report compilation for state",
                ControllerNotes = "PHASE TRANSITION - Major inject establishing post-landfall conditions. " +
                                  "Pause for operational period briefing before continuing.",
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase3Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // Inject 13 - PENDING
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 13,
                Title = "Water Rescue Operations - Multiple Calls",
                Description = "Metro Fire Department reports significant rescue operations needed:\n\n" +
                              "911 BACKLOG: 47 water rescue calls pending\n\n" +
                              "PRIORITY CALLS:\n" +
                              "• 123 Coastal Drive: Family of 4 on roof, water at 2nd floor\n" +
                              "• Sunrise Apartments: 8 residents trapped, elderly building\n" +
                              "• Oak Street Bridge: Vehicle in floodwater, occupants unknown\n" +
                              "• Marina Village: Multiple distress calls, boats involved\n\n" +
                              "AVAILABLE RESOURCES:\n" +
                              "• 2 swift water rescue teams operational\n" +
                              "• 4 high-water vehicles\n" +
                              "• Fire boats unable to deploy (debris)\n\n" +
                              "Fire Chief requests additional water rescue resources and prioritization guidance.",
                ScheduledTime = new TimeOnly(10, 40),
                DeliveryTime = TimeSpan.FromMinutes(100),
                ScenarioDay = 3,
                ScenarioTime = new TimeOnly(5, 30),
                Target = "Fire/Rescue Branch",
                Source = "Metro Fire Department",
                DeliveryMethod = DeliveryMethod.Radio,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 13,
                Priority = 1,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 2",
                LocationName = "Countywide",
                LocationType = "Field",
                Track = "Fire/Rescue",
                ExpectedAction = "1. Establish rescue prioritization (life threat, access)\n" +
                                 "2. Request mutual aid swift water teams\n" +
                                 "3. Consider Coast Guard/National Guard helicopter support\n" +
                                 "4. Coordinate civilian boat volunteers safely\n" +
                                 "5. Establish rescue coordination point",
                ControllerNotes = "CRITICAL LIFE SAFETY - Mass rescue scenario. Tests incident prioritization " +
                                  "and resource management under pressure. Multiple right answers exist.",
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase3Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // Inject 14 - PENDING (Complexity)
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 14,
                Title = "Hospital Generator Failure - Complexity",
                Description = "Metro General Hospital (Zone B) reports CRITICAL situation:\n\n" +
                              "• Main commercial power: OFFLINE\n" +
                              "• Primary generator: FAILED (mechanical issue)\n" +
                              "• Backup generator: OPERATIONAL but only 4 hours fuel remaining\n" +
                              "• Fuel truck unable to reach hospital (flooded roads)\n\n" +
                              "PATIENT STATUS:\n" +
                              "• ICU: 28 patients, 12 on ventilators\n" +
                              "• Surgery: 3 procedures in progress (cannot stop)\n" +
                              "• ER: 45 patients, continuing to receive storm injuries\n" +
                              "• Total census: 210 patients\n\n" +
                              "Hospital Administrator: \"We need fuel within 3 hours or we start losing patients.\"",
                ScheduledTime = new TimeOnly(10, 50),
                DeliveryTime = TimeSpan.FromMinutes(110),
                ScenarioDay = 3,
                ScenarioTime = new TimeOnly(7, 0),
                Target = "Medical Branch / Infrastructure",
                Source = "Metro General Hospital",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Complexity,
                Status = InjectStatus.Pending,
                Sequence = 14,
                Priority = 1,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 3",
                LocationName = "Metro General Hospital",
                LocationType = "Healthcare",
                Track = "Medical/Infrastructure",
                FireCondition = "Fire if players are managing well. Creates cascading decisions. " +
                                "Skip if team is overwhelmed with rescue operations.",
                ExpectedAction = "1. Coordinate emergency fuel delivery (high-clearance vehicle)\n" +
                                 "2. Identify portable generator resources\n" +
                                 "3. Prepare patient evacuation plan as backup\n" +
                                 "4. Coordinate helicopter fuel delivery if ground impossible\n" +
                                 "5. Pre-position ambulances for potential evacuation",
                ControllerNotes = "COMPLEXITY inject - major challenge for strong groups. Multiple solution " +
                                  "paths exist. Tests creativity and inter-agency coordination.",
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase3Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // Inject 15 - SKIPPED (Example of skipped inject)
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 15,
                Title = "Hazardous Materials Release",
                Description = "Reports of chemical release at Metro Industrial Park due to storm damage. " +
                              "Unknown substance leaking from damaged storage tanks. Facility in flood zone " +
                              "with potential for waterway contamination.",
                ScheduledTime = new TimeOnly(10, 55),
                DeliveryTime = TimeSpan.FromMinutes(115),
                ScenarioDay = 3,
                ScenarioTime = new TimeOnly(8, 0),
                Target = "HazMat Coordinator / Fire",
                Source = "Metro Fire Department",
                DeliveryMethod = DeliveryMethod.Radio,
                InjectType = InjectType.Complexity,
                Status = InjectStatus.Skipped,
                Sequence = 15,
                Priority = 2,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 2",
                LocationName = "Industrial Park",
                LocationType = "Industrial",
                Track = "Fire/HazMat",
                FireCondition = "Exercise Director discretion. Use only for advanced groups completing " +
                                "other objectives easily.",
                ExpectedAction = "Dispatch HazMat team, establish isolation perimeter, coordinate EPA.",
                ControllerNotes = "SKIPPED - Preserved for future exercise. Time constraints in current scenario.",
                SkippedAt = now.AddMinutes(-10),
                SkippedByUserId = Director1UserId,
                SkipReason = "Time constraints - exercise needed to focus on life safety priorities. " +
                             "HazMat scenario saved for dedicated future exercise.",
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase3Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // Inject 16 - PENDING
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 16,
                Title = "Widespread Power Outage - Utility Coordination",
                Description = "Metro Power Company provides damage assessment update:\n\n" +
                              "OUTAGE STATUS:\n" +
                              "• Customers without power: 89,000 (62% of county)\n" +
                              "• Transmission lines damaged: 12 major, 47 distribution\n" +
                              "• Substations offline: 4 of 15\n" +
                              "• Poles down: 340+ reported\n\n" +
                              "RESTORATION ESTIMATE:\n" +
                              "• Critical facilities: 24-48 hours\n" +
                              "• Urban core: 3-5 days\n" +
                              "• Hardest hit coastal areas: 7-10 days\n\n" +
                              "Utility requests county priorities for restoration sequence.",
                ScheduledTime = new TimeOnly(11, 5),
                DeliveryTime = TimeSpan.FromMinutes(125),
                ScenarioDay = 3,
                ScenarioTime = new TimeOnly(10, 0),
                Target = "Infrastructure Branch / ESF-12",
                Source = "Metro Power Company",
                DeliveryMethod = DeliveryMethod.Email,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 16,
                Priority = 2,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 1",
                LocationName = "EOC",
                LocationType = "Command",
                Track = "Infrastructure",
                ExpectedAction = "1. Provide critical facility prioritization list\n" +
                                 "2. Coordinate generator allocation for extended outage\n" +
                                 "3. Establish cooling centers for vulnerable populations\n" +
                                 "4. Update public on restoration timeline\n" +
                                 "5. Coordinate with water utility (treatment plant power)",
                ControllerNotes = "Tests infrastructure coordination and long-term planning during " +
                                  "response phase. Good transition discussion toward recovery.",
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase3Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // Inject 17 - PENDING
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 17,
                Title = "Governor's Press Conference Request",
                Description = "Governor's office announces joint press conference in 45 minutes at State EOC. " +
                              "Governor and FEMA Region Administrator will speak. Requesting Metro County update:\n\n" +
                              "REQUIRED INFORMATION:\n" +
                              "• Confirmed fatalities/injuries (if any)\n" +
                              "• Damage assessment summary (preliminary)\n" +
                              "• Current shelter population\n" +
                              "• Rescue operations status\n" +
                              "• Critical infrastructure status\n" +
                              "• Specific resource needs for federal assistance",
                ScheduledTime = new TimeOnly(11, 15),
                DeliveryTime = TimeSpan.FromMinutes(135),
                ScenarioDay = 3,
                ScenarioTime = new TimeOnly(12, 0),
                Target = "EOC Director / PIO",
                Source = "Governor's Office",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 17,
                Priority = 2,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 1",
                LocationName = "EOC",
                LocationType = "Command",
                Track = "Command/PIO",
                ExpectedAction = "1. Compile comprehensive situation report\n" +
                                 "2. Prepare specific resource request documentation\n" +
                                 "3. Coordinate talking points with PIO\n" +
                                 "4. Assign liaison to State EOC\n" +
                                 "5. Ensure consistent messaging across all channels",
                ControllerNotes = "Capstone inject for Phase 3. Tests information management, external " +
                                  "coordination, and executive communication skills.",
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase3Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // =====================================================================
            // PHASE 4: Initial Recovery (Injects 18-20)
            // =====================================================================

            // Inject 18 - PENDING
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 18,
                Title = "Debris Management Coordination",
                Description = "Public Works Director requests coordination meeting for debris operations:\n\n" +
                              "INITIAL ESTIMATES:\n" +
                              "• Vegetative debris: 850,000 cubic yards\n" +
                              "• Construction debris: 125,000 cubic yards\n" +
                              "• White goods (appliances): 15,000+ units\n" +
                              "• Household hazardous waste: Unknown quantity\n\n" +
                              "QUESTIONS FOR EOC:\n" +
                              "• Debris staging site approval (environmental concerns)\n" +
                              "• Private property debris removal authorization\n" +
                              "• Contract activation for debris hauling\n" +
                              "• FEMA debris monitoring requirements",
                ScheduledTime = new TimeOnly(11, 25),
                DeliveryTime = TimeSpan.FromMinutes(145),
                ScenarioDay = 4,
                ScenarioTime = new TimeOnly(8, 0),
                Target = "Infrastructure / Public Works",
                Source = "Public Works Department",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 18,
                Priority = 3,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 2",
                LocationName = "EOC",
                LocationType = "Command",
                Track = "Public Works",
                ExpectedAction = "1. Authorize pre-designated debris staging sites\n" +
                                 "2. Issue emergency authorization for right-of-way debris\n" +
                                 "3. Activate pre-positioned debris removal contracts\n" +
                                 "4. Coordinate FEMA debris monitoring requirements\n" +
                                 "5. Plan environmental compliance for staging sites",
                ControllerNotes = "Recovery phase inject. Tests transition planning and FEMA coordination " +
                                  "for Public Assistance eligibility.",
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase4Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // Inject 19 - PENDING
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 19,
                Title = "Shelter Demobilization Planning",
                Description = "American Red Cross Mass Care Lead reports current shelter status:\n\n" +
                              "SHELTER POPULATION:\n" +
                              "• Total sheltered: 2,847 residents (down from peak of 4,200)\n" +
                              "• Long-term housing need: ~1,200 residents (homes destroyed/unlivable)\n" +
                              "• Estimated shelter duration: 2-4 weeks for many\n\n" +
                              "CHALLENGES:\n" +
                              "• School facilities needed for classes resuming Monday\n" +
                              "• Volunteer fatigue - need fresh shelter staff\n" +
                              "• Supply chain running low (food, hygiene items)\n" +
                              "• 47 residents with medical needs requiring ongoing care",
                ScheduledTime = new TimeOnly(11, 35),
                DeliveryTime = TimeSpan.FromMinutes(155),
                ScenarioDay = 4,
                ScenarioTime = new TimeOnly(14, 0),
                Target = "Mass Care / Human Services",
                Source = "American Red Cross",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 19,
                Priority = 2,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Controller 3",
                LocationName = "Multiple Shelters",
                LocationType = "Shelter",
                Track = "Mass Care",
                ExpectedAction = "1. Identify non-school facilities for extended sheltering\n" +
                                 "2. Coordinate FEMA Transitional Sheltering Assistance\n" +
                                 "3. Request voluntary organizations for supply replenishment\n" +
                                 "4. Plan medical needs shelter continuation\n" +
                                 "5. Begin housing solutions coordination (hotels, rentals)",
                ControllerNotes = "Tests mass care sustainability and transition to recovery. Good " +
                                  "discussion of long-term sheltering vs. housing solutions.",
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase4Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },

            // Inject 20 - PENDING (ENDEX)
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 20,
                Title = "ENDEX - Exercise Termination",
                Description = "The Exercise Director announces termination of the exercise. All play stops.\n\n" +
                              "ENDEX ANNOUNCEMENT:\n" +
                              "\"This concludes Hurricane Response Tabletop Exercise 2026. All exercise play " +
                              "has ended. Please remove any exercise materials from the EOC and return the " +
                              "room to normal configuration.\n\n" +
                              "A hot wash will begin in 15 minutes in the main EOC conference room. Please " +
                              "take a short break, grab refreshments, and return for our initial discussion.\n\n" +
                              "Thank you for your participation today.\"",
                ScheduledTime = new TimeOnly(11, 55),
                DeliveryTime = TimeSpan.FromMinutes(175),
                ScenarioDay = 4,
                ScenarioTime = new TimeOnly(18, 0),
                Target = "All Players",
                Source = "Exercise Director",
                DeliveryMethod = DeliveryMethod.Verbal,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 20,
                Priority = 1,
                TriggerType = TriggerType.Manual,
                ResponsibleController = "Lead Controller",
                LocationName = "EOC",
                LocationType = "Command",
                Track = "All",
                ExpectedAction = "1. Cease all exercise play\n" +
                                 "2. Transition to hot wash discussion\n" +
                                 "3. Complete player feedback forms\n" +
                                 "4. Identify immediate improvement priorities",
                ControllerNotes = "Read ENDEX statement clearly. Ensure all players heard. Direct to " +
                                  "hot wash location. Collect any exercise materials.",
                MselId = HurricaneMselId,
                PhaseId = HurricanePhase4Id,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            }
        };
    }

    private static List<Inject> CreateCyberInjects(DateTime now)
    {
        // Cyber TTX is COMPLETED - all injects should be Fired
        var baseTime = now.AddDays(-45).AddHours(13);

        return new List<Inject>
        {
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 1,
                Title = "IT Help Desk Reports Unusual Activity",
                Description = "IT Help Desk receives multiple calls about slow computers and strange pop-up messages " +
                              "from Finance Department workstations.",
                ScheduledTime = new TimeOnly(13, 0),
                Target = "IT Security Team",
                Source = "IT Help Desk",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 1,
                FiredAt = baseTime,
                FiredByUserId = Controller1UserId,
                MselId = CyberMselId,
                PhaseId = CyberPhase1Id,
                CreatedAt = now.AddDays(-65),
                UpdatedAt = now.AddDays(-45),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 2,
                Title = "Ransomware Confirmation",
                Description = "Security team confirms DarkSide ransomware variant. Ransom note demands $2.5M in Bitcoin.",
                ScheduledTime = new TimeOnly(13, 20),
                Target = "IT Director / CIRT",
                Source = "IT Security",
                DeliveryMethod = DeliveryMethod.Email,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 2,
                FiredAt = baseTime.AddMinutes(20),
                FiredByUserId = Controller1UserId,
                MselId = CyberMselId,
                PhaseId = CyberPhase1Id,
                CreatedAt = now.AddDays(-65),
                UpdatedAt = now.AddDays(-45),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 3,
                Title = "911 CAD System Impacted",
                Description = "Computer-Aided Dispatch system is offline. 911 operators revert to manual dispatch procedures.",
                ScheduledTime = new TimeOnly(13, 45),
                Target = "Public Safety / 911 Center",
                Source = "911 Supervisor",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 3,
                FiredAt = baseTime.AddMinutes(45),
                FiredByUserId = Controller2UserId,
                MselId = CyberMselId,
                PhaseId = CyberPhase2Id,
                CreatedAt = now.AddDays(-65),
                UpdatedAt = now.AddDays(-45),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 4,
                Title = "FBI Cyber Division Contact",
                Description = "FBI Cyber Division reaches out offering investigation assistance and intelligence sharing.",
                ScheduledTime = new TimeOnly(14, 30),
                Target = "County Administrator / IT",
                Source = "FBI",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 4,
                FiredAt = baseTime.AddMinutes(90),
                FiredByUserId = Controller1UserId,
                MselId = CyberMselId,
                PhaseId = CyberPhase2Id,
                CreatedAt = now.AddDays(-65),
                UpdatedAt = now.AddDays(-45),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 5,
                Title = "Media Learns of Incident",
                Description = "Local TV station calls asking about \"computer problems\" affecting county services.",
                ScheduledTime = new TimeOnly(15, 0),
                Target = "PIO",
                Source = "WMET-TV",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 5,
                FiredAt = baseTime.AddMinutes(120),
                FiredByUserId = Controller2UserId,
                MselId = CyberMselId,
                PhaseId = CyberPhase3Id,
                CreatedAt = now.AddDays(-65),
                UpdatedAt = now.AddDays(-45),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 6,
                Title = "ENDEX - Cyber Exercise Complete",
                Description = "Exercise Director terminates exercise. Hot wash to follow.",
                ScheduledTime = new TimeOnly(15, 55),
                Target = "All Players",
                Source = "Exercise Director",
                DeliveryMethod = DeliveryMethod.Verbal,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 6,
                FiredAt = baseTime.AddMinutes(175),
                FiredByUserId = Director2UserId,
                MselId = CyberMselId,
                PhaseId = CyberPhase3Id,
                CreatedAt = now.AddDays(-65),
                UpdatedAt = now.AddDays(-45),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            }
        };
    }

    private static List<Inject> CreateEarthquakeInjects(DateTime now)
    {
        // Earthquake FE is ARCHIVED - all injects should be Fired
        var baseTime = now.AddMonths(-6).AddHours(8);

        return new List<Inject>
        {
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 1,
                Title = "Earthquake Strikes - Initial Reports",
                Description = "6.5 magnitude earthquake. Multiple structure damage reports across county.",
                ScheduledTime = new TimeOnly(8, 0),
                Target = "EOC Director",
                Source = "USGS / 911",
                DeliveryMethod = DeliveryMethod.Radio,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 1,
                FiredAt = baseTime,
                FiredByUserId = Controller1UserId,
                MselId = EarthquakeMselId,
                PhaseId = EarthquakePhase1Id,
                CreatedAt = now.AddMonths(-9),
                UpdatedAt = now.AddMonths(-6),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 2,
                Title = "Bridge Collapse Reported",
                Description = "Main Street Bridge over Metro River has partially collapsed. Unknown if vehicles involved.",
                ScheduledTime = new TimeOnly(8, 30),
                Target = "Fire/Rescue",
                Source = "Police Unit",
                DeliveryMethod = DeliveryMethod.Radio,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 2,
                FiredAt = baseTime.AddMinutes(30),
                FiredByUserId = Controller2UserId,
                MselId = EarthquakeMselId,
                PhaseId = EarthquakePhase1Id,
                CreatedAt = now.AddMonths(-9),
                UpdatedAt = now.AddMonths(-6),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 3,
                Title = "Hospital Reports Mass Casualties",
                Description = "Metro General ER reports receiving 45 patients in first hour. Requesting trauma support.",
                ScheduledTime = new TimeOnly(9, 15),
                Target = "Medical Branch",
                Source = "Metro General Hospital",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 3,
                FiredAt = baseTime.AddMinutes(75),
                FiredByUserId = Controller3UserId,
                MselId = EarthquakeMselId,
                PhaseId = EarthquakePhase1Id,
                CreatedAt = now.AddMonths(-9),
                UpdatedAt = now.AddMonths(-6),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 4,
                Title = "Gas Leak in Downtown",
                Description = "Major natural gas leak from ruptured main. 4-block area being evacuated.",
                ScheduledTime = new TimeOnly(10, 0),
                Target = "Fire/HazMat",
                Source = "Fire Department",
                DeliveryMethod = DeliveryMethod.Radio,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 4,
                FiredAt = baseTime.AddMinutes(120),
                FiredByUserId = Controller2UserId,
                MselId = EarthquakeMselId,
                PhaseId = EarthquakePhase1Id,
                CreatedAt = now.AddMonths(-9),
                UpdatedAt = now.AddMonths(-6),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 5,
                Title = "State EOC Requests Damage Report",
                Description = "State requesting preliminary damage assessment for potential disaster declaration.",
                ScheduledTime = new TimeOnly(12, 30),
                Target = "EOC Director",
                Source = "State EOC",
                DeliveryMethod = DeliveryMethod.Email,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 5,
                FiredAt = baseTime.AddMinutes(270),
                FiredByUserId = Controller1UserId,
                MselId = EarthquakeMselId,
                PhaseId = EarthquakePhase2Id,
                CreatedAt = now.AddMonths(-9),
                UpdatedAt = now.AddMonths(-6),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 6,
                Title = "ENDEX - Earthquake Exercise Complete",
                Description = "Exercise Director terminates exercise.",
                ScheduledTime = new TimeOnly(15, 55),
                Target = "All Players",
                Source = "Exercise Director",
                DeliveryMethod = DeliveryMethod.Verbal,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 6,
                FiredAt = baseTime.AddHours(8),
                FiredByUserId = Director1UserId,
                MselId = EarthquakeMselId,
                PhaseId = EarthquakePhase2Id,
                CreatedAt = now.AddMonths(-9),
                UpdatedAt = now.AddMonths(-6),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            }
        };
    }

    private static List<Inject> CreateFloodInjects()
    {
        return new List<Inject>
        {
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 1,
                Title = "Flash Flood Watch Issued",
                Description = "NWS issues Flash Flood Watch. Heavy rainfall expected over next 6 hours.",
                ScheduledTime = new TimeOnly(13, 0),
                ScenarioDay = 1,
                ScenarioTime = new TimeOnly(6, 0),
                Target = "EOC Director",
                Source = "National Weather Service",
                DeliveryMethod = DeliveryMethod.Email,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 1,
                ExpectedAction = "Monitor weather updates. Consider EOC activation level.",
                ControllerNotes = "Training inject - walk through basic notification process.",
                MselId = FloodMselId,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 2,
                Title = "Road Flooding Reports",
                Description = "Highway department reports multiple roads with water over roadway.",
                ScheduledTime = new TimeOnly(13, 30),
                Target = "Transportation",
                Source = "Highway Department",
                DeliveryMethod = DeliveryMethod.Radio,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 2,
                ExpectedAction = "Coordinate road closures. Update status boards.",
                ControllerNotes = "Training inject - demonstrate coordination process.",
                MselId = FloodMselId,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            },
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 3,
                Title = "End Training Exercise",
                Description = "Training exercise complete. Debrief to follow.",
                ScheduledTime = new TimeOnly(14, 45),
                Target = "All Participants",
                Source = "Exercise Controller",
                DeliveryMethod = DeliveryMethod.Verbal,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 3,
                ExpectedAction = "Complete feedback forms.",
                ControllerNotes = "End training session.",
                MselId = FloodMselId,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3),
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId
            }
        };
    }

    #endregion

    #region Inject-Objective Links

    private static List<InjectObjective> CreateInjectObjectiveLinks()
    {
        return new List<InjectObjective>
        {
            // Hurricane Inject 1 -> Objective 1 (EOC Activation)
            new InjectObjective { InjectId = HurricaneInject1Id, ObjectiveId = HurricaneObj1Id },
            
            // Hurricane Inject 2 -> Objective 2 (Public Warning)
            new InjectObjective { InjectId = HurricaneInject2Id, ObjectiveId = HurricaneObj2Id },
            
            // Hurricane Inject 3 -> Objectives 1 & 4 (EOC Coordination, Mass Care)
            new InjectObjective { InjectId = HurricaneInject3Id, ObjectiveId = HurricaneObj1Id },
            new InjectObjective { InjectId = HurricaneInject3Id, ObjectiveId = HurricaneObj4Id },
            
            // Hurricane Inject 4 -> Objective 5 (Critical Infrastructure)
            new InjectObjective { InjectId = HurricaneInject4Id, ObjectiveId = HurricaneObj5Id },
            
            // Hurricane Inject 5 -> Objective 1 (EOC Coordination)
            new InjectObjective { InjectId = HurricaneInject5Id, ObjectiveId = HurricaneObj1Id }
        };
    }

    #endregion
}
