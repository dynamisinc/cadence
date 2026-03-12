using System.Diagnostics.CodeAnalysis;
using Cadence.Core.Constants;
using Cadence.Core.Features.Capabilities.Services;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Data;

/// <summary>
/// Seeds beta testing data for Dynamis internal testers (exercise planners/designers).
/// Creates a realistic agency organization with exercises in various lifecycle states,
/// giving testers a curated environment to explore and build from.
///
/// Runs in ALL environments EXCEPT Production and Testing. Idempotent - safe to call multiple times.
///
/// Beta Organization: Coastal Region Emergency Services Agency
/// - Multi-jurisdiction coastal agency covering 8 municipalities
/// - Active in FIFA World Cup 2026 venue security planning with CISA
/// - Completely isolated from demo and production organizations
///
/// Seeded Content (NO users - testers are invited manually):
/// - 6 exercises demonstrating full lifecycle (Draft → Active → Completed → Archived)
///   - Exercise 1: Mass Casualty Incident TTX (Active, mid-conduct)
///   - Exercise 2: Hazmat Spill Response FE (Completed, ready for AAR)
///   - Exercise 3: FIFA World Cup 2026 Venue Security TTX (Draft, fully planned)
///   - Exercise 4: Community Evacuation Exercise (Draft, empty shell)
///   - Exercise 5: Winter Storm Response TTX (Draft, empty - for Excel import)
///   - Exercise 6: World Cup Preliminary Draw Security (Archived, historical)
/// - Complete MSELs with 65+ injects across multiple phases
/// - All inject types and statuses demonstrated
/// - FEMA Core Capabilities linked to exercises
/// </summary>
[ExcludeFromCodeCoverage]
public static class BetaDataSeeder
{
    #region Fixed GUIDs for Idempotent Seeding

    // =========================================================================
    // Organization
    // =========================================================================

    public static readonly Guid BetaOrganizationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    // =========================================================================
    // Exercises
    // =========================================================================

    /// <summary>Mass Casualty Incident TTX - ACTIVE (mid-conduct showcase)</summary>
    public static readonly Guid MciTtxId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb01");

    /// <summary>Hazmat Spill Response FE - COMPLETED (AAR ready)</summary>
    public static readonly Guid HazmatFeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb02");

    /// <summary>FIFA World Cup 2026 Venue Security TTX - DRAFT (fully planned)</summary>
    public static readonly Guid WorldCupTtxId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb03");

    /// <summary>Community Evacuation Exercise - DRAFT (empty shell)</summary>
    public static readonly Guid EvacuationFseId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb04");

    /// <summary>Winter Storm Response TTX - DRAFT (empty, for Excel import)</summary>
    public static readonly Guid WinterStormTtxId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb05");

    /// <summary>World Cup Preliminary Draw Security - ARCHIVED (historical)</summary>
    public static readonly Guid WcPrelimDrawId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb06");

    // =========================================================================
    // MSELs (only for exercises with data - 4 of 6)
    // =========================================================================

    public static readonly Guid MciMselId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc01");
    public static readonly Guid HazmatMselId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc02");
    public static readonly Guid WorldCupMselId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc03");
    public static readonly Guid WcPrelimMselId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc04");

    // =========================================================================
    // Phases - MCI TTX (Exercise 1)
    // =========================================================================

    public static readonly Guid MciPhase1Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddd0101");
    public static readonly Guid MciPhase2Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddd0102");
    public static readonly Guid MciPhase3Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddd0103");
    public static readonly Guid MciPhase4Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddd0104");

    // =========================================================================
    // Phases - Hazmat FE (Exercise 2)
    // =========================================================================

    public static readonly Guid HazmatPhase1Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddd0201");
    public static readonly Guid HazmatPhase2Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddd0202");
    public static readonly Guid HazmatPhase3Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddd0203");

    // =========================================================================
    // Phases - World Cup TTX (Exercise 3)
    // =========================================================================

    public static readonly Guid WcPhase1Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddd0301");
    public static readonly Guid WcPhase2Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddd0302");
    public static readonly Guid WcPhase3Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddd0303");

    // =========================================================================
    // Phases - WC Preliminary Draw (Exercise 6)
    // =========================================================================

    public static readonly Guid WcPrelimPhase1Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddd0601");
    public static readonly Guid WcPrelimPhase2Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddd0602");

    // =========================================================================
    // Objectives - MCI TTX (Exercise 1)
    // =========================================================================

    public static readonly Guid MciObj1Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0101");
    public static readonly Guid MciObj2Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0102");
    public static readonly Guid MciObj3Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0103");
    public static readonly Guid MciObj4Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0104");
    public static readonly Guid MciObj5Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0105");

    // =========================================================================
    // Objectives - Hazmat FE (Exercise 2)
    // =========================================================================

    public static readonly Guid HazmatObj1Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0201");
    public static readonly Guid HazmatObj2Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0202");
    public static readonly Guid HazmatObj3Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0203");
    public static readonly Guid HazmatObj4Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0204");

    // =========================================================================
    // Objectives - World Cup TTX (Exercise 3)
    // =========================================================================

    public static readonly Guid WcObj1Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0301");
    public static readonly Guid WcObj2Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0302");
    public static readonly Guid WcObj3Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0303");
    public static readonly Guid WcObj4Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0304");
    public static readonly Guid WcObj5Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0305");

    // =========================================================================
    // Objectives - WC Preliminary Draw (Exercise 6)
    // =========================================================================

    public static readonly Guid WcPrelimObj1Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0601");
    public static readonly Guid WcPrelimObj2Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0602");
    public static readonly Guid WcPrelimObj3Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeee0603");

    // =========================================================================
    // Fixed Inject IDs (for inject-objective linking)
    // =========================================================================

    // MCI TTX - Phase 1 fired injects
    public static readonly Guid MciInject1Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0101");
    public static readonly Guid MciInject2Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0102");
    public static readonly Guid MciInject3Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0103");
    public static readonly Guid MciInject4Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0104");
    public static readonly Guid MciInject5Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0105");

    // Hazmat FE - fixed IDs for linking
    public static readonly Guid HazmatInject1Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0201");
    public static readonly Guid HazmatInject2Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0202");
    public static readonly Guid HazmatInject3Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0203");

    // World Cup TTX - fixed IDs for linking
    public static readonly Guid WcInject1Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0301");
    public static readonly Guid WcInject2Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0302");

    // WC Preliminary Draw - fixed IDs for linking
    public static readonly Guid WcPrelimInject1Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff0601");

    // =========================================================================
    // EEG Capability Targets - MCI TTX (Exercise 1)
    // =========================================================================

    public static readonly Guid MciCapTarget_EMS = Guid.Parse("aabbccdd-eeee-0001-0001-000000000001");
    public static readonly Guid MciCapTarget_OpComm = Guid.Parse("aabbccdd-eeee-0001-0001-000000000002");
    public static readonly Guid MciCapTarget_MassCare = Guid.Parse("aabbccdd-eeee-0001-0001-000000000003");
    public static readonly Guid MciCapTarget_Security = Guid.Parse("aabbccdd-eeee-0001-0001-000000000004");
    public static readonly Guid MciCapTarget_SitAssess = Guid.Parse("aabbccdd-eeee-0001-0001-000000000005");

    // =========================================================================
    // EEG Capability Targets - Hazmat FE (Exercise 2)
    // =========================================================================

    public static readonly Guid HazCapTarget_EnvResp = Guid.Parse("aabbccdd-eeee-0001-0002-000000000001");
    public static readonly Guid HazCapTarget_Security = Guid.Parse("aabbccdd-eeee-0001-0002-000000000002");
    public static readonly Guid HazCapTarget_OpComm = Guid.Parse("aabbccdd-eeee-0001-0002-000000000003");
    public static readonly Guid HazCapTarget_EMS = Guid.Parse("aabbccdd-eeee-0001-0002-000000000004");

    // =========================================================================
    // EEG Critical Tasks - MCI TTX (Exercise 1)
    // Pattern: Exercise 01, Target ##, Task ##
    // =========================================================================

    // MCI Target 1 (EMS) tasks
    public static readonly Guid MciTask_EMS_1 = Guid.Parse("aabbccdd-eeee-0002-0001-000100000001");
    public static readonly Guid MciTask_EMS_2 = Guid.Parse("aabbccdd-eeee-0002-0001-000100000002");
    public static readonly Guid MciTask_EMS_3 = Guid.Parse("aabbccdd-eeee-0002-0001-000100000003");

    // MCI Target 2 (OpComm) tasks
    public static readonly Guid MciTask_OpComm_1 = Guid.Parse("aabbccdd-eeee-0002-0001-000200000001");
    public static readonly Guid MciTask_OpComm_2 = Guid.Parse("aabbccdd-eeee-0002-0001-000200000002");

    // MCI Target 3 (MassCare) tasks
    public static readonly Guid MciTask_MassCare_1 = Guid.Parse("aabbccdd-eeee-0002-0001-000300000001");
    public static readonly Guid MciTask_MassCare_2 = Guid.Parse("aabbccdd-eeee-0002-0001-000300000002");

    // MCI Target 4 (Security) tasks
    public static readonly Guid MciTask_Security_1 = Guid.Parse("aabbccdd-eeee-0002-0001-000400000001");
    public static readonly Guid MciTask_Security_2 = Guid.Parse("aabbccdd-eeee-0002-0001-000400000002");

    // MCI Target 5 (SitAssess) tasks
    public static readonly Guid MciTask_SitAssess_1 = Guid.Parse("aabbccdd-eeee-0002-0001-000500000001");
    public static readonly Guid MciTask_SitAssess_2 = Guid.Parse("aabbccdd-eeee-0002-0001-000500000002");

    // =========================================================================
    // EEG Critical Tasks - Hazmat FE (Exercise 2)
    // =========================================================================

    // Hazmat Target 1 (EnvResp) tasks
    public static readonly Guid HazTask_EnvResp_1 = Guid.Parse("aabbccdd-eeee-0002-0002-000100000001");
    public static readonly Guid HazTask_EnvResp_2 = Guid.Parse("aabbccdd-eeee-0002-0002-000100000002");
    public static readonly Guid HazTask_EnvResp_3 = Guid.Parse("aabbccdd-eeee-0002-0002-000100000003");

    // Hazmat Target 2 (Security) tasks
    public static readonly Guid HazTask_Security_1 = Guid.Parse("aabbccdd-eeee-0002-0002-000200000001");
    public static readonly Guid HazTask_Security_2 = Guid.Parse("aabbccdd-eeee-0002-0002-000200000002");

    // Hazmat Target 3 (OpComm) tasks
    public static readonly Guid HazTask_OpComm_1 = Guid.Parse("aabbccdd-eeee-0002-0002-000300000001");
    public static readonly Guid HazTask_OpComm_2 = Guid.Parse("aabbccdd-eeee-0002-0002-000300000002");

    // Hazmat Target 4 (EMS) tasks
    public static readonly Guid HazTask_EMS_1 = Guid.Parse("aabbccdd-eeee-0002-0002-000400000001");
    public static readonly Guid HazTask_EMS_2 = Guid.Parse("aabbccdd-eeee-0002-0002-000400000002");

    // =========================================================================
    // Seed Evaluator Users (for observation/EEG attribution)
    // =========================================================================

    public static readonly string BetaEvaluator1Id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaa001";
    public static readonly string BetaEvaluator2Id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaa002";

    // Org Membership sentinel IDs
    public static readonly Guid BetaMembership1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaab01");
    public static readonly Guid BetaMembership2Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaab02");

    // Exercise Participant sentinel IDs
    public static readonly Guid BetaParticipantEval1Mci = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaac01");
    public static readonly Guid BetaParticipantEval2Mci = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaac02");
    public static readonly Guid BetaParticipantEval1Haz = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaac03");
    public static readonly Guid BetaParticipantEval2Haz = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaac04");

    // =========================================================================
    // Observations - MCI TTX (Exercise 1) — 8 observations
    // =========================================================================

    public static readonly Guid MciObs1Id = Guid.Parse("11111111-1111-1111-0001-000000000001");
    public static readonly Guid MciObs2Id = Guid.Parse("11111111-1111-1111-0001-000000000002");
    public static readonly Guid MciObs3Id = Guid.Parse("11111111-1111-1111-0001-000000000003");
    public static readonly Guid MciObs4Id = Guid.Parse("11111111-1111-1111-0001-000000000004");
    public static readonly Guid MciObs5Id = Guid.Parse("11111111-1111-1111-0001-000000000005");
    public static readonly Guid MciObs6Id = Guid.Parse("11111111-1111-1111-0001-000000000006");
    public static readonly Guid MciObs7Id = Guid.Parse("11111111-1111-1111-0001-000000000007");
    public static readonly Guid MciObs8Id = Guid.Parse("11111111-1111-1111-0001-000000000008");

    // =========================================================================
    // Observations - Hazmat FE (Exercise 2) — 10 observations
    // =========================================================================

    public static readonly Guid HazObs1Id = Guid.Parse("11111111-1111-1111-0002-000000000001");
    public static readonly Guid HazObs2Id = Guid.Parse("11111111-1111-1111-0002-000000000002");
    public static readonly Guid HazObs3Id = Guid.Parse("11111111-1111-1111-0002-000000000003");
    public static readonly Guid HazObs4Id = Guid.Parse("11111111-1111-1111-0002-000000000004");
    public static readonly Guid HazObs5Id = Guid.Parse("11111111-1111-1111-0002-000000000005");
    public static readonly Guid HazObs6Id = Guid.Parse("11111111-1111-1111-0002-000000000006");
    public static readonly Guid HazObs7Id = Guid.Parse("11111111-1111-1111-0002-000000000007");
    public static readonly Guid HazObs8Id = Guid.Parse("11111111-1111-1111-0002-000000000008");
    public static readonly Guid HazObs9Id = Guid.Parse("11111111-1111-1111-0002-000000000009");
    public static readonly Guid HazObs10Id = Guid.Parse("11111111-1111-1111-0002-000000000010");

    // =========================================================================
    // EEG Entries - MCI TTX (Exercise 1) — 7 entries (partial progress)
    // =========================================================================

    public static readonly Guid MciEeg1Id = Guid.Parse("22222222-2222-2222-0001-000000000001");
    public static readonly Guid MciEeg2Id = Guid.Parse("22222222-2222-2222-0001-000000000002");
    public static readonly Guid MciEeg3Id = Guid.Parse("22222222-2222-2222-0001-000000000003");
    public static readonly Guid MciEeg4Id = Guid.Parse("22222222-2222-2222-0001-000000000004");
    public static readonly Guid MciEeg5Id = Guid.Parse("22222222-2222-2222-0001-000000000005");
    public static readonly Guid MciEeg6Id = Guid.Parse("22222222-2222-2222-0001-000000000006");
    public static readonly Guid MciEeg7Id = Guid.Parse("22222222-2222-2222-0001-000000000007");

    // =========================================================================
    // EEG Entries - Hazmat FE (Exercise 2) — 9 entries (complete coverage)
    // =========================================================================

    public static readonly Guid HazEeg1Id = Guid.Parse("22222222-2222-2222-0002-000000000001");
    public static readonly Guid HazEeg2Id = Guid.Parse("22222222-2222-2222-0002-000000000002");
    public static readonly Guid HazEeg3Id = Guid.Parse("22222222-2222-2222-0002-000000000003");
    public static readonly Guid HazEeg4Id = Guid.Parse("22222222-2222-2222-0002-000000000004");
    public static readonly Guid HazEeg5Id = Guid.Parse("22222222-2222-2222-0002-000000000005");
    public static readonly Guid HazEeg6Id = Guid.Parse("22222222-2222-2222-0002-000000000006");
    public static readonly Guid HazEeg7Id = Guid.Parse("22222222-2222-2222-0002-000000000007");
    public static readonly Guid HazEeg8Id = Guid.Parse("22222222-2222-2222-0002-000000000008");
    public static readonly Guid HazEeg9Id = Guid.Parse("22222222-2222-2222-0002-000000000009");

    #endregion

    /// <summary>
    /// Seeds beta testing data incrementally. Idempotent - each step checks for its own data
    /// so partial runs from previous failures can recover and complete successfully.
    /// </summary>
    public static async Task SeedAsync(AppDbContext context, ILogger? logger = null)
    {
        logger?.LogInformation("Seeding beta testing data (incremental)...");
        var now = DateTime.UtcNow;

        // 1. Create Beta Organization
        var orgExists = await context.Organizations.AnyAsync(o => o.Id == BetaOrganizationId);
        if (!orgExists)
        {
            var betaOrg = CreateBetaOrganization();
            context.Organizations.Add(betaOrg);
            await context.SaveChangesAsync();
            logger?.LogInformation("Created beta organization: {OrgName}", betaOrg.Name);
        }
        else
        {
            logger?.LogDebug("Beta organization already exists - skipping");
        }

        // 2. Create Exercises (without ActiveMselId initially)
        if (!await context.Exercises.AnyAsync(e => e.Id == MciTtxId))
        {
            var exercises = CreateExercises(now);
            context.Exercises.AddRange(exercises);
            await context.SaveChangesAsync();
            logger?.LogInformation("Created {Count} beta exercises", exercises.Count);
        }
        else
        {
            logger?.LogDebug("Beta exercises already exist - skipping");
        }

        // 3. Create MSELs
        if (!await context.Msels.IgnoreQueryFilters().AnyAsync(m => m.Id == MciMselId))
        {
            var msels = CreateMsels();
            context.Msels.AddRange(msels);
            await context.SaveChangesAsync();

            // 4. Link ActiveMselId to exercises
            var mciTtx = await context.Exercises.FindAsync(MciTtxId);
            var hazmatFe = await context.Exercises.FindAsync(HazmatFeId);
            var wcTtx = await context.Exercises.FindAsync(WorldCupTtxId);
            var wcPrelim = await context.Exercises.FindAsync(WcPrelimDrawId);

            if (mciTtx != null) mciTtx.ActiveMselId = MciMselId;
            if (hazmatFe != null) hazmatFe.ActiveMselId = HazmatMselId;
            if (wcTtx != null) wcTtx.ActiveMselId = WorldCupMselId;
            if (wcPrelim != null) wcPrelim.ActiveMselId = WcPrelimMselId;

            await context.SaveChangesAsync();
            logger?.LogInformation("Created {Count} MSELs", msels.Count);
        }
        else
        {
            logger?.LogDebug("Beta MSELs already exist - skipping");
        }

        // 5. Create Phases
        if (!await context.Phases.IgnoreQueryFilters().AnyAsync(p => p.Id == MciPhase1Id))
        {
            var phases = CreateAllPhases();
            context.Phases.AddRange(phases);
            await context.SaveChangesAsync();
            logger?.LogInformation("Created {Count} phases", phases.Count);
        }
        else
        {
            logger?.LogDebug("Beta phases already exist - skipping");
        }

        // 6. Create Objectives
        if (!await context.Objectives.IgnoreQueryFilters().AnyAsync(o => o.Id == MciObj1Id))
        {
            var objectives = CreateAllObjectives();
            context.Objectives.AddRange(objectives);
            await context.SaveChangesAsync();
            logger?.LogInformation("Created {Count} objectives", objectives.Count);
        }
        else
        {
            logger?.LogDebug("Beta objectives already exist - skipping");
        }

        // 7. Create Injects
        if (!await context.Injects.IgnoreQueryFilters().AnyAsync(i => i.Id == MciInject1Id))
        {
            var injects = CreateAllInjects(now);
            context.Injects.AddRange(injects);
            await context.SaveChangesAsync();
            logger?.LogInformation("Created {Count} injects", injects.Count);
        }
        else
        {
            logger?.LogDebug("Beta injects already exist - skipping");
        }

        // 8. Create Inject-Objective links
        if (!await context.Set<InjectObjective>().AnyAsync(io => io.InjectId == MciInject1Id))
        {
            var injectObjectives = CreateInjectObjectiveLinks();
            context.Set<InjectObjective>().AddRange(injectObjectives);
            await context.SaveChangesAsync();
            logger?.LogInformation("Created {Count} inject-objective links", injectObjectives.Count);
        }
        else
        {
            logger?.LogDebug("Beta inject-objective links already exist - skipping");
        }

        logger?.LogInformation("Beta data seeding complete");
    }

    /// <summary>
    /// Seeds FEMA Core Capabilities for the beta organization.
    /// </summary>
    public static async Task SeedCapabilitiesAsync(
        AppDbContext context,
        ICapabilityImportService importService,
        ILogger? logger = null)
    {
        var hasCapabilities = await context.Capabilities
            .AnyAsync(c => c.OrganizationId == BetaOrganizationId);

        if (hasCapabilities)
        {
            logger?.LogDebug("Capabilities already seeded for beta organization");
            return;
        }

        var orgExists = await context.Organizations.AnyAsync(o => o.Id == BetaOrganizationId);
        if (!orgExists)
        {
            logger?.LogWarning("Beta organization not found - skipping capability seeding");
            return;
        }

        try
        {
            var result = await importService.ImportLibraryAsync(BetaOrganizationId, "FEMA");
            logger?.LogInformation("Seeded {Count} FEMA Core Capabilities for beta organization", result.Imported);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to seed capabilities for beta organization");
        }
    }

    /// <summary>
    /// Seeds EEG data (Capability Targets, Critical Tasks, and Inject-Task links) for the
    /// Active (MCI TTX) and Completed (Hazmat FE) beta exercises. Must run AFTER capabilities
    /// are seeded since targets reference org-level capabilities by name lookup.
    ///
    /// Seeds:
    /// - ExerciseTargetCapability (which capabilities each exercise evaluates)
    /// - CapabilityTarget (exercise-specific measurable performance thresholds)
    /// - CriticalTask (observable actions under each target)
    /// - InjectCriticalTask (links injects to the critical tasks they test)
    ///
    /// Idempotent. Skips if capability targets already exist for these exercises.
    /// </summary>
    public static async Task SeedEegDataAsync(AppDbContext context, ILogger? logger = null)
    {
        // Check if already seeded
        var hasTargets = await context.CapabilityTargets
            .AnyAsync(ct => ct.ExerciseId == MciTtxId || ct.ExerciseId == HazmatFeId);

        if (hasTargets)
        {
            logger?.LogDebug("EEG data already seeded for beta exercises - skipping");
            return;
        }

        // Look up FEMA capabilities by name for the beta organization
        var capabilities = await context.Capabilities
            .Where(c => c.OrganizationId == BetaOrganizationId && c.SourceLibrary == "FEMA")
            .ToListAsync();

        if (capabilities.Count == 0)
        {
            logger?.LogWarning("No FEMA capabilities found for beta org - skipping EEG seeding. " +
                               "Ensure SeedCapabilitiesAsync runs first.");
            return;
        }

        // Build lookup by name
        var capLookup = capabilities.ToDictionary(c => c.Name, c => c.Id);

        // Helper to safely get capability ID
        Guid? GetCapId(string name) => capLookup.TryGetValue(name, out var id) ? id : null;

        var now = DateTime.UtcNow;
        var system = SystemConstants.SystemUserIdString;

        // Required capability IDs
        var emsCapId = GetCapId("Public Health, Healthcare, and Emergency Medical Services");
        var opCommCapId = GetCapId("Operational Communications");
        var massCareCapId = GetCapId("Mass Care Services");
        var securityCapId = GetCapId("On-scene Security, Protection, and Law Enforcement");
        var sitAssessCapId = GetCapId("Situational Assessment");
        var envRespCapId = GetCapId("Environmental Response/Health and Safety");

        if (emsCapId == null || opCommCapId == null || massCareCapId == null ||
            securityCapId == null || sitAssessCapId == null || envRespCapId == null)
        {
            logger?.LogWarning("Missing required FEMA capabilities - skipping EEG seeding. " +
                               "Found {Count} capabilities.", capabilities.Count);
            return;
        }

        logger?.LogInformation("Seeding EEG data for MCI TTX and Hazmat FE exercises...");

        // =====================================================================
        // 1. ExerciseTargetCapability (which capabilities each exercise targets)
        // =====================================================================

        var exerciseTargetCaps = new List<ExerciseTargetCapability>
        {
            // MCI TTX targets 5 capabilities
            new() { ExerciseId = MciTtxId, CapabilityId = emsCapId.Value },
            new() { ExerciseId = MciTtxId, CapabilityId = opCommCapId.Value },
            new() { ExerciseId = MciTtxId, CapabilityId = massCareCapId.Value },
            new() { ExerciseId = MciTtxId, CapabilityId = securityCapId.Value },
            new() { ExerciseId = MciTtxId, CapabilityId = sitAssessCapId.Value },

            // Hazmat FE targets 4 capabilities
            new() { ExerciseId = HazmatFeId, CapabilityId = envRespCapId.Value },
            new() { ExerciseId = HazmatFeId, CapabilityId = securityCapId.Value },
            new() { ExerciseId = HazmatFeId, CapabilityId = opCommCapId.Value },
            new() { ExerciseId = HazmatFeId, CapabilityId = emsCapId.Value },
        };

        context.Set<ExerciseTargetCapability>().AddRange(exerciseTargetCaps);
        await context.SaveChangesAsync();
        logger?.LogInformation("Created {Count} exercise-capability links", exerciseTargetCaps.Count);

        // =====================================================================
        // 2. Capability Targets (exercise-specific performance thresholds)
        // =====================================================================

        var capabilityTargets = new List<CapabilityTarget>
        {
            // -----------------------------------------------------------------
            // MCI TTX - 5 Capability Targets
            // -----------------------------------------------------------------
            new()
            {
                Id = MciCapTarget_EMS,
                ExerciseId = MciTtxId,
                CapabilityId = emsCapId.Value,
                OrganizationId = BetaOrganizationId,
                TargetDescription = "Activate MCI triage system and transport 60+ patients to appropriate receiving facilities within 90 minutes of incident declaration",
                Sources = "Coastal Region MCI Plan v3.2; EMS SOP 12.1; Regional Hospital Surge Protocol",
                SortOrder = 1,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = MciCapTarget_OpComm,
                ExerciseId = MciTtxId,
                CapabilityId = opCommCapId.Value,
                OrganizationId = BetaOrganizationId,
                TargetDescription = "Establish interoperable multi-agency communications within 15 minutes of incident notification",
                Sources = "Regional Interoperability Plan; TICP v2.0; ICS Forms 205/205A",
                SortOrder = 2,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = MciCapTarget_MassCare,
                ExerciseId = MciTtxId,
                CapabilityId = massCareCapId.Value,
                OrganizationId = BetaOrganizationId,
                TargetDescription = "Activate family reunification center and provide crisis support services within 60 minutes of MCI declaration",
                Sources = "Mass Care Annex E; American Red Cross MOA; Family Assistance SOP 8.3",
                SortOrder = 3,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = MciCapTarget_Security,
                ExerciseId = MciTtxId,
                CapabilityId = securityCapId.Value,
                OrganizationId = BetaOrganizationId,
                TargetDescription = "Secure incident perimeter and manage traffic for emergency vehicle access within 20 minutes of first unit arrival",
                Sources = "Law Enforcement MCI Protocol; Traffic Management Plan; ICS-204 Assignment",
                SortOrder = 4,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = MciCapTarget_SitAssess,
                ExerciseId = MciTtxId,
                CapabilityId = sitAssessCapId.Value,
                OrganizationId = BetaOrganizationId,
                TargetDescription = "Provide decision-ready situation reports to unified command at 15-minute intervals throughout the operation",
                Sources = "ICS-209 Incident Summary; EOC SOP 3.1; Situation Unit Leader Checklist",
                SortOrder = 5,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14),
                CreatedBy = system, ModifiedBy = system
            },

            // -----------------------------------------------------------------
            // Hazmat FE - 4 Capability Targets
            // -----------------------------------------------------------------
            new()
            {
                Id = HazCapTarget_EnvResp,
                ExerciseId = HazmatFeId,
                CapabilityId = envRespCapId.Value,
                OrganizationId = BetaOrganizationId,
                TargetDescription = "Establish hot/warm/cold zones and begin environmental air monitoring within 30 minutes of HAZMAT team arrival on scene",
                Sources = "HAZMAT Response Plan; ERG 2024; EPA Region IV MOA; OSHA 1910.120",
                SortOrder = 1,
                CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-30),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = HazCapTarget_Security,
                ExerciseId = HazmatFeId,
                CapabilityId = securityCapId.Value,
                OrganizationId = BetaOrganizationId,
                TargetDescription = "Secure affected area and coordinate evacuation or shelter-in-place within 20 minutes of hazard confirmation",
                Sources = "Shelter-in-Place SOP 6.2; Evacuation Route Plan; Public Alert System Protocol",
                SortOrder = 2,
                CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-30),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = HazCapTarget_OpComm,
                ExerciseId = HazmatFeId,
                CapabilityId = opCommCapId.Value,
                OrganizationId = BetaOrganizationId,
                TargetDescription = "Maintain uninterrupted multi-agency communications throughout the 8-hour HAZMAT operation using primary and backup systems",
                Sources = "HAZMAT Communications Plan; TICP v2.0; Backup Comms SOP 14.1",
                SortOrder = 3,
                CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-30),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = HazCapTarget_EMS,
                ExerciseId = HazmatFeId,
                CapabilityId = emsCapId.Value,
                OrganizationId = BetaOrganizationId,
                TargetDescription = "Establish mass decontamination capability and medical monitoring system for 200+ potential chlorine exposures within 45 minutes",
                Sources = "Mass Decon SOP 9.4; Hospital Surge Protocol; Chlorine Exposure Treatment Guide",
                SortOrder = 4,
                CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-30),
                CreatedBy = system, ModifiedBy = system
            },
        };

        context.CapabilityTargets.AddRange(capabilityTargets);
        await context.SaveChangesAsync();
        logger?.LogInformation("Created {Count} capability targets", capabilityTargets.Count);

        // =====================================================================
        // 3. Critical Tasks (observable actions under each target)
        // =====================================================================

        var criticalTasks = new List<CriticalTask>
        {
            // -----------------------------------------------------------------
            // MCI TTX - Target 1: EMS (3 tasks)
            // -----------------------------------------------------------------
            new()
            {
                Id = MciTask_EMS_1,
                CapabilityTargetId = MciCapTarget_EMS,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Implement START triage at scene within 10 minutes of first responder arrival",
                Standard = "Per MCI Plan v3.2: All patients tagged and triaged using START algorithm; triage officer designated; patient count reported to IC within 10 minutes",
                SortOrder = 1,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = MciTask_EMS_2,
                CapabilityTargetId = MciCapTarget_EMS,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Coordinate patient distribution across 4 hospitals based on real-time surge capacity data",
                Standard = "Per Hospital Surge Protocol: EMS Dispatch confirms bed availability with each hospital; patients distributed based on injury severity and hospital capability; no single facility receives more than 40% of total patients",
                SortOrder = 2,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = MciTask_EMS_3,
                CapabilityTargetId = MciCapTarget_EMS,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Establish patient tracking and family reunification data system",
                Standard = "Per SOP 12.1: All transported patients entered into tracking system with unique ID, receiving hospital, and injury category within 15 minutes of transport",
                SortOrder = 3,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14),
                CreatedBy = system, ModifiedBy = system
            },

            // -----------------------------------------------------------------
            // MCI TTX - Target 2: Operational Communications (2 tasks)
            // -----------------------------------------------------------------
            new()
            {
                Id = MciTask_OpComm_1,
                CapabilityTargetId = MciCapTarget_OpComm,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Activate mutual aid talk groups on regional interoperability channel",
                Standard = "Per TICP v2.0: All responding agencies confirm on assigned interop channel within 15 minutes; communications check completed with all units",
                SortOrder = 1,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = MciTask_OpComm_2,
                CapabilityTargetId = MciCapTarget_OpComm,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Establish unified command post communications including field-to-EOC link",
                Standard = "Per ICS-205: Communications plan distributed; primary and backup frequencies assigned; field-to-EOC radio/phone link verified operational",
                SortOrder = 2,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14),
                CreatedBy = system, ModifiedBy = system
            },

            // -----------------------------------------------------------------
            // MCI TTX - Target 3: Mass Care Services (2 tasks)
            // -----------------------------------------------------------------
            new()
            {
                Id = MciTask_MassCare_1,
                CapabilityTargetId = MciCapTarget_MassCare,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Establish family assistance center at designated location with reception and information areas",
                Standard = "Per Mass Care Annex E: FAC location confirmed; staffing includes social services, chaplain, Red Cross caseworker; information hotline activated; signage posted",
                SortOrder = 1,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = MciTask_MassCare_2,
                CapabilityTargetId = MciCapTarget_MassCare,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Activate crisis counseling and victim advocate services for families and survivors",
                Standard = "Per Family Assistance SOP 8.3: Crisis counselors on-site within 60 minutes; victim advocates available for each family; coordination with hospital social workers established",
                SortOrder = 2,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14),
                CreatedBy = system, ModifiedBy = system
            },

            // -----------------------------------------------------------------
            // MCI TTX - Target 4: Security (2 tasks)
            // -----------------------------------------------------------------
            new()
            {
                Id = MciTask_Security_1,
                CapabilityTargetId = MciCapTarget_Security,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Establish inner and outer perimeters per ICS protocols within 20 minutes of first unit arrival",
                Standard = "Per Law Enforcement MCI Protocol: Inner perimeter excludes civilians from hazard area; outer perimeter controls access; access control points staffed with check-in/check-out logs",
                SortOrder = 1,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = MciTask_Security_2,
                CapabilityTargetId = MciCapTarget_Security,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Implement traffic management plan for emergency vehicle corridors on Highway 17",
                Standard = "Per Traffic Management Plan: Two emergency vehicle corridors established; civilian traffic diverted to alternate routes; DOT message boards activated; staging area access controlled",
                SortOrder = 2,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14),
                CreatedBy = system, ModifiedBy = system
            },

            // -----------------------------------------------------------------
            // MCI TTX - Target 5: Situational Assessment (2 tasks)
            // -----------------------------------------------------------------
            new()
            {
                Id = MciTask_SitAssess_1,
                CapabilityTargetId = MciCapTarget_SitAssess,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Establish situation unit and begin collecting field reports from all operational sectors",
                Standard = "Per EOC SOP 3.1: Situation Unit Leader designated; field reports collected from Fire, EMS, LE, and hospital sectors; information consolidated for briefings",
                SortOrder = 1,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = MciTask_SitAssess_2,
                CapabilityTargetId = MciCapTarget_SitAssess,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Produce initial ICS-209 incident summary within 30 minutes of MCI declaration",
                Standard = "Per ICS-209: Accurate patient count by triage category; resource status; hospital bed availability; road closure status; estimated time to scene clearance",
                SortOrder = 2,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14),
                CreatedBy = system, ModifiedBy = system
            },

            // -----------------------------------------------------------------
            // Hazmat FE - Target 1: Environmental Response (3 tasks)
            // -----------------------------------------------------------------
            new()
            {
                Id = HazTask_EnvResp_1,
                CapabilityTargetId = HazCapTarget_EnvResp,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Conduct initial hazard assessment and positively identify the chemical agent using field instruments",
                Standard = "Per HAZMAT Response Plan: 4-gas monitor and PID deployed; chemical identified using SDS, placards, and field testing; results reported to IC and CHEMTREC notified within 20 minutes",
                SortOrder = 1,
                CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-30),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = HazTask_EnvResp_2,
                CapabilityTargetId = HazCapTarget_EnvResp,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Establish exclusion zones (hot/warm/cold) per ERG guidelines for chlorine release",
                Standard = "Per ERG 2024: Hot zone based on concentration readings; warm zone with decon corridor; cold zone for command and staging; zones marked with barrier tape and signage; perimeter monitored continuously",
                SortOrder = 2,
                CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-30),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = HazTask_EnvResp_3,
                CapabilityTargetId = HazCapTarget_EnvResp,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Deploy continuous air monitoring network and report real-time readings to Incident Commander",
                Standard = "Per OSHA 1910.120: Fixed air monitors at hot zone boundary and downwind residential area; readings recorded every 10 minutes; action levels trigger shelter-in-place or evacuation decisions",
                SortOrder = 3,
                CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-30),
                CreatedBy = system, ModifiedBy = system
            },

            // -----------------------------------------------------------------
            // Hazmat FE - Target 2: Security (2 tasks)
            // -----------------------------------------------------------------
            new()
            {
                Id = HazTask_Security_1,
                CapabilityTargetId = HazCapTarget_Security,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Issue shelter-in-place notification to immediate residential area via reverse 911 and outdoor warning sirens",
                Standard = "Per Shelter-in-Place SOP 6.2: Reverse 911 message sent within 15 minutes of hazard confirmation; outdoor sirens activated; social media alert posted; local media notified",
                SortOrder = 1,
                CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-30),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = HazTask_Security_2,
                CapabilityTargetId = HazCapTarget_Security,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Establish evacuation routes away from plume trajectory based on wind direction data",
                Standard = "Per Evacuation Route Plan: Routes identified upwind and crosswind; school evacuation coordinated with district; special needs transportation arranged; traffic control points staffed",
                SortOrder = 2,
                CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-30),
                CreatedBy = system, ModifiedBy = system
            },

            // -----------------------------------------------------------------
            // Hazmat FE - Target 3: Operational Communications (2 tasks)
            // -----------------------------------------------------------------
            new()
            {
                Id = HazTask_OpComm_1,
                CapabilityTargetId = HazCapTarget_OpComm,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Establish HAZMAT talk group and confirm all responding units on assigned frequency",
                Standard = "Per HAZMAT Comms Plan: Dedicated HAZMAT talk group activated; all HAZMAT, fire, EMS, LE, and EPA units confirmed on frequency; communications check completed within 15 minutes of IC establishment",
                SortOrder = 1,
                CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-30),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = HazTask_OpComm_2,
                CapabilityTargetId = HazCapTarget_OpComm,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Implement backup communications plan using cellular/satellite for extended operations",
                Standard = "Per Backup Comms SOP 14.1: Cellular backup verified; satellite phone available at IC post; radio relay established if terrain issues; communications log maintained throughout incident",
                SortOrder = 2,
                CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-30),
                CreatedBy = system, ModifiedBy = system
            },

            // -----------------------------------------------------------------
            // Hazmat FE - Target 4: EMS (2 tasks)
            // -----------------------------------------------------------------
            new()
            {
                Id = HazTask_EMS_1,
                CapabilityTargetId = HazCapTarget_EMS,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Deploy mass decontamination corridor at warm/cold zone boundary within 45 minutes of arrival",
                Standard = "Per Mass Decon SOP 9.4: Two-stage decon corridor (gross and technical) established; water supply connected; PPE-equipped decon team of 6+ personnel; ambulatory and non-ambulatory lanes operational",
                SortOrder = 1,
                CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-30),
                CreatedBy = system, ModifiedBy = system
            },
            new()
            {
                Id = HazTask_EMS_2,
                CapabilityTargetId = HazCapTarget_EMS,
                OrganizationId = BetaOrganizationId,
                TaskDescription = "Initiate exposure tracking and medical surveillance for all potentially exposed individuals",
                Standard = "Per Chlorine Exposure Guide: Patient registration at decon exit; symptom assessment using CHEMPACK protocols; hospital transport for symptomatic patients; exposure registry maintained for follow-up",
                SortOrder = 2,
                CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-30),
                CreatedBy = system, ModifiedBy = system
            },
        };

        context.CriticalTasks.AddRange(criticalTasks);
        await context.SaveChangesAsync();
        logger?.LogInformation("Created {Count} critical tasks", criticalTasks.Count);

        // =====================================================================
        // 4. InjectCriticalTask links (connecting injects to critical tasks)
        // =====================================================================

        var injectTaskLinks = new List<InjectCriticalTask>
        {
            // MCI TTX - Inject 1 (Multi-Vehicle Accident) tests EMS triage and OpComm activation
            new() { InjectId = MciInject1Id, CriticalTaskId = MciTask_EMS_1, CreatedAt = now.AddDays(-14), CreatedBy = system },
            new() { InjectId = MciInject1Id, CriticalTaskId = MciTask_OpComm_1, CreatedAt = now.AddDays(-14), CreatedBy = system },

            // MCI TTX - Inject 2 (Scene Size-Up) tests EMS triage and Security perimeter
            new() { InjectId = MciInject2Id, CriticalTaskId = MciTask_EMS_1, CreatedAt = now.AddDays(-14), CreatedBy = system },
            new() { InjectId = MciInject2Id, CriticalTaskId = MciTask_Security_1, CreatedAt = now.AddDays(-14), CreatedBy = system },

            // MCI TTX - Inject 3 (MCI Declaration) tests SitAssess and OpComm
            new() { InjectId = MciInject3Id, CriticalTaskId = MciTask_SitAssess_1, CreatedAt = now.AddDays(-14), CreatedBy = system },
            new() { InjectId = MciInject3Id, CriticalTaskId = MciTask_OpComm_2, CreatedAt = now.AddDays(-14), CreatedBy = system },
            new() { InjectId = MciInject3Id, CriticalTaskId = MciTask_SitAssess_2, CreatedAt = now.AddDays(-14), CreatedBy = system },

            // MCI TTX - Inject 4 (Triage Complete) tests EMS patient distribution and MassCare
            new() { InjectId = MciInject4Id, CriticalTaskId = MciTask_EMS_2, CreatedAt = now.AddDays(-14), CreatedBy = system },
            new() { InjectId = MciInject4Id, CriticalTaskId = MciTask_EMS_3, CreatedAt = now.AddDays(-14), CreatedBy = system },
            new() { InjectId = MciInject4Id, CriticalTaskId = MciTask_MassCare_1, CreatedAt = now.AddDays(-14), CreatedBy = system },

            // MCI TTX - Inject 5 (Hospital Surge) tests EMS distribution and MassCare counseling
            new() { InjectId = MciInject5Id, CriticalTaskId = MciTask_EMS_2, CreatedAt = now.AddDays(-14), CreatedBy = system },
            new() { InjectId = MciInject5Id, CriticalTaskId = MciTask_MassCare_2, CreatedAt = now.AddDays(-14), CreatedBy = system },

            // Hazmat FE - Inject 1 (Chemical Odor Reports) tests EnvResp assessment and Security SIP
            new() { InjectId = HazmatInject1Id, CriticalTaskId = HazTask_EnvResp_1, CreatedAt = now.AddDays(-60), CreatedBy = system },
            new() { InjectId = HazmatInject1Id, CriticalTaskId = HazTask_Security_1, CreatedAt = now.AddDays(-60), CreatedBy = system },

            // Hazmat FE - Inject 2 (HAZMAT Team Assessment) tests EnvResp zones and OpComm
            new() { InjectId = HazmatInject2Id, CriticalTaskId = HazTask_EnvResp_2, CreatedAt = now.AddDays(-60), CreatedBy = system },
            new() { InjectId = HazmatInject2Id, CriticalTaskId = HazTask_EnvResp_3, CreatedAt = now.AddDays(-60), CreatedBy = system },
            new() { InjectId = HazmatInject2Id, CriticalTaskId = HazTask_OpComm_1, CreatedAt = now.AddDays(-60), CreatedBy = system },

            // Hazmat FE - Inject 3 (Wind Direction Change) tests Security evacuation and EMS decon
            new() { InjectId = HazmatInject3Id, CriticalTaskId = HazTask_Security_2, CreatedAt = now.AddDays(-60), CreatedBy = system },
            new() { InjectId = HazmatInject3Id, CriticalTaskId = HazTask_EMS_1, CreatedAt = now.AddDays(-60), CreatedBy = system },
            new() { InjectId = HazmatInject3Id, CriticalTaskId = HazTask_OpComm_2, CreatedAt = now.AddDays(-60), CreatedBy = system },
        };

        context.Set<InjectCriticalTask>().AddRange(injectTaskLinks);
        await context.SaveChangesAsync();
        logger?.LogInformation("Created {Count} inject-critical task links", injectTaskLinks.Count);

        logger?.LogInformation("EEG data seeding complete for beta exercises");
    }

    #region Organization

    private static Organization CreateBetaOrganization()
    {
        return new Organization
        {
            Id = BetaOrganizationId,
            Name = "Coastal Region Emergency Services Agency",
            Slug = "coastal-region-esa",
            Description = "Multi-jurisdiction coastal emergency services agency covering 8 municipalities " +
                          "along 120 miles of coastline. Coordinates with CISA, FEMA Region IV, US Coast Guard, " +
                          "and state emergency management. Serves a population of 850,000 with seasonal increases " +
                          "to 1.5 million during tourism season. Active in special events security including " +
                          "FIFA World Cup 2026 venue operations in coordination with CISA's National Special " +
                          "Security Event (NSSE) planning framework.",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = SystemConstants.SystemUserIdString,
            ModifiedBy = SystemConstants.SystemUserIdString
        };
    }

    #endregion

    #region Exercises

    private static List<Exercise> CreateExercises(DateTime now)
    {
        return new List<Exercise>
        {
            // =====================================================================
            // Exercise 1: Mass Casualty Incident TTX - ACTIVE (mid-conduct)
            // =====================================================================
            new Exercise
            {
                Id = MciTtxId,
                Name = "Mass Casualty Incident TTX 2026",
                Description = "Tabletop exercise testing multi-agency response to a mass casualty incident " +
                              "involving a charter bus accident on Highway 17. Scenario involves 60+ patients " +
                              "requiring triage, transport coordination to 4 hospitals, surge capacity activation, " +
                              "and family reunification operations. Participants include fire/EMS, law enforcement, " +
                              "hospital systems, medical examiner, and American Red Cross.",
                ExerciseType = ExerciseType.TTX,
                Status = ExerciseStatus.Active,
                IsPracticeMode = false,
                HasBeenPublished = true,
                ScheduledDate = DateOnly.FromDateTime(now.Date),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(12, 0),
                TimeZoneId = "America/New_York",
                Location = "Coastal Region EOC, Main Operations Floor",
                OrganizationId = BetaOrganizationId,
                DeliveryMode = DeliveryMode.ClockDriven,
                TimelineMode = TimelineMode.Compressed,
                TimeScale = 4.0m,
                ClockState = ExerciseClockState.Running,
                ClockStartedAt = now.AddHours(-1),
                ClockElapsedBeforePause = TimeSpan.Zero,
                ActivatedAt = now.AddHours(-1),
                ActivatedBy = SystemConstants.SystemUserIdString,
                CreatedAt = now.AddDays(-21),
                UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            },

            // =====================================================================
            // Exercise 2: Hazmat Spill Response FE - COMPLETED
            // =====================================================================
            new Exercise
            {
                Id = HazmatFeId,
                Name = "Hazmat Spill Response Functional Exercise",
                Description = "Functional exercise testing HAZMAT team response to an industrial chemical spill " +
                              "at Bayshore Industrial Park. Scenario involves a chlorine gas release from a storage " +
                              "tank affecting nearby residential areas. Validates hot/warm/cold zone operations, " +
                              "shelter-in-place and evacuation decisions, environmental monitoring, and mass " +
                              "decontamination procedures. Joint exercise with county HAZMAT, fire departments, " +
                              "EMS, environmental health, and EPA Region IV.",
                ExerciseType = ExerciseType.FE,
                Status = ExerciseStatus.Completed,
                IsPracticeMode = false,
                HasBeenPublished = true,
                ScheduledDate = DateOnly.FromDateTime(now.AddDays(-30)),
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(16, 0),
                TimeZoneId = "America/New_York",
                Location = "Bayshore Industrial Park & Coastal Region EOC",
                OrganizationId = BetaOrganizationId,
                DeliveryMode = DeliveryMode.ClockDriven,
                TimelineMode = TimelineMode.RealTime,
                ActivatedAt = now.AddDays(-30).AddHours(8),
                ActivatedBy = SystemConstants.SystemUserIdString,
                CompletedAt = now.AddDays(-30).AddHours(16),
                CompletedBy = SystemConstants.SystemUserIdString,
                CreatedAt = now.AddDays(-60),
                UpdatedAt = now.AddDays(-29),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            },

            // =====================================================================
            // Exercise 3: FIFA World Cup 2026 Venue Security TTX - DRAFT (fully planned)
            // =====================================================================
            new Exercise
            {
                Id = WorldCupTtxId,
                Name = "FIFA World Cup 2026 - Venue Security TTX",
                Description = "CISA-coordinated tabletop exercise testing multi-agency venue security operations " +
                              "for FIFA World Cup 2026 Group Stage match at Metro Stadium. Exercise will validate " +
                              "credentialing systems, threat detection procedures, crowd management, VIP security " +
                              "protocols, and interagency communications. Participants include CISA, FBI, USSS, " +
                              "local law enforcement, fire/EMS, stadium operations, and FIFA security. Developed " +
                              "in coordination with CISA's National Special Security Event (NSSE) planning framework.",
                ExerciseType = ExerciseType.TTX,
                Status = ExerciseStatus.Draft,
                IsPracticeMode = false,
                HasBeenPublished = false,
                ScheduledDate = DateOnly.FromDateTime(now.AddDays(21)),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(15, 0),
                TimeZoneId = "America/New_York",
                Location = "Coastal Region Fusion Center, Secure Conference Room",
                OrganizationId = BetaOrganizationId,
                DeliveryMode = DeliveryMode.FacilitatorPaced,
                TimelineMode = TimelineMode.StoryOnly,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-2),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            },

            // =====================================================================
            // Exercise 4: Community Evacuation Exercise - DRAFT (empty shell)
            // =====================================================================
            new Exercise
            {
                Id = EvacuationFseId,
                Name = "Community Evacuation Full-Scale Exercise",
                Description = "Full-scale exercise testing community evacuation procedures for hurricane season. " +
                              "This exercise will validate evacuation route management, special needs transportation, " +
                              "shelter operations, and re-entry procedures across all 8 municipalities. " +
                              "Build your MSEL from scratch using this exercise.",
                ExerciseType = ExerciseType.FSE,
                Status = ExerciseStatus.Draft,
                IsPracticeMode = false,
                HasBeenPublished = false,
                ScheduledDate = DateOnly.FromDateTime(now.AddMonths(3)),
                StartTime = new TimeOnly(6, 0),
                EndTime = new TimeOnly(18, 0),
                TimeZoneId = "America/New_York",
                Location = "Multiple locations - Coastal Region evacuation zones",
                OrganizationId = BetaOrganizationId,
                DeliveryMode = DeliveryMode.ClockDriven,
                TimelineMode = TimelineMode.RealTime,
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-7),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            },

            // =====================================================================
            // Exercise 5: Winter Storm Response TTX - DRAFT (empty, for import)
            // =====================================================================
            new Exercise
            {
                Id = WinterStormTtxId,
                Name = "Winter Storm Response TTX",
                Description = "Tabletop exercise for winter storm response operations. This exercise is " +
                              "designed for testing the MSEL import feature - use the Excel import to " +
                              "populate the MSEL with scenario events from the sample files.",
                ExerciseType = ExerciseType.TTX,
                Status = ExerciseStatus.Draft,
                IsPracticeMode = false,
                HasBeenPublished = false,
                ScheduledDate = DateOnly.FromDateTime(now.AddMonths(1)),
                StartTime = new TimeOnly(13, 0),
                EndTime = new TimeOnly(16, 0),
                TimeZoneId = "America/New_York",
                Location = "Coastal Region EOC, Conference Room B",
                OrganizationId = BetaOrganizationId,
                DeliveryMode = DeliveryMode.FacilitatorPaced,
                TimelineMode = TimelineMode.StoryOnly,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-3),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            },

            // =====================================================================
            // Exercise 6: WC Preliminary Draw Security - ARCHIVED
            // =====================================================================
            new Exercise
            {
                Id = WcPrelimDrawId,
                Name = "World Cup 2026 - Preliminary Draw Security AAR",
                Description = "Functional exercise conducted for the FIFA World Cup 2026 Preliminary Draw " +
                              "ceremony security operations at Coastal Convention Center. Validated venue " +
                              "security, dignitary protection, crowd management, and multi-agency unified " +
                              "command. This exercise informed planning for the main tournament venue " +
                              "security exercises. After Action Review completed.",
                ExerciseType = ExerciseType.FE,
                Status = ExerciseStatus.Archived,
                IsPracticeMode = false,
                HasBeenPublished = true,
                PreviousStatus = ExerciseStatus.Completed,
                ScheduledDate = DateOnly.FromDateTime(now.AddMonths(-4)),
                StartTime = new TimeOnly(7, 0),
                EndTime = new TimeOnly(19, 0),
                TimeZoneId = "America/New_York",
                Location = "Coastal Convention Center",
                OrganizationId = BetaOrganizationId,
                DeliveryMode = DeliveryMode.ClockDriven,
                TimelineMode = TimelineMode.RealTime,
                ActivatedAt = now.AddMonths(-4).AddHours(7),
                ActivatedBy = SystemConstants.SystemUserIdString,
                CompletedAt = now.AddMonths(-4).AddHours(19),
                CompletedBy = SystemConstants.SystemUserIdString,
                ArchivedAt = now.AddMonths(-3),
                ArchivedBy = SystemConstants.SystemUserIdString,
                CreatedAt = now.AddMonths(-6),
                UpdatedAt = now.AddMonths(-3),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
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
                Id = MciMselId,
                Name = "Highway 17 MCI MSEL v1.2",
                Description = "Charter bus accident scenario on Highway 17 with 60+ casualties. " +
                              "Multi-agency response through triage, transport, hospital surge, and family reunification.",
                Version = 2,
                IsActive = true,
                OrganizationId = BetaOrganizationId,
                ExerciseId = MciTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Msel
            {
                Id = HazmatMselId,
                Name = "Bayshore Chemical Release MSEL v1.0",
                Description = "Chlorine gas release from industrial storage tank at Bayshore Chemical facility. " +
                              "Response through detection, containment, decontamination, and environmental recovery.",
                Version = 1,
                IsActive = true,
                OrganizationId = BetaOrganizationId,
                ExerciseId = HazmatFeId,
                CreatedAt = DateTime.UtcNow.AddDays(-55),
                UpdatedAt = DateTime.UtcNow.AddDays(-35),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Msel
            {
                Id = WorldCupMselId,
                Name = "WC26 Group Stage Match Day MSEL v2.0",
                Description = "FIFA World Cup 2026 Group Stage match day security scenario at Metro Stadium. " +
                              "65,000 capacity venue with multi-tier credentialing, CISA/FBI/USSS coordination, " +
                              "and full-spectrum threat response.",
                Version = 2,
                IsActive = true,
                OrganizationId = BetaOrganizationId,
                ExerciseId = WorldCupTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-3),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Msel
            {
                Id = WcPrelimMselId,
                Name = "WC26 Preliminary Draw MSEL v1.0",
                Description = "FIFA World Cup 2026 Preliminary Draw ceremony at Coastal Convention Center. " +
                              "500+ international dignitaries, 15,000 invited guests.",
                Version = 1,
                IsActive = true,
                OrganizationId = BetaOrganizationId,
                ExerciseId = WcPrelimDrawId,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UpdatedAt = DateTime.UtcNow.AddMonths(-5),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            }
        };
    }

    #endregion

    #region Phases

    private static List<Phase> CreateAllPhases()
    {
        var phases = new List<Phase>();

        // MCI TTX Phases
        phases.AddRange(new[]
        {
            new Phase
            {
                Id = MciPhase1Id,
                Name = "Phase 1: Initial Response & Scene Management",
                Description = "0-30 minutes. 911 call, first responder arrival, scene size-up, MCI declaration, " +
                              "and unified command establishment.",
                Sequence = 1,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(9, 30),
                OrganizationId = BetaOrganizationId,
                ExerciseId = MciTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Phase
            {
                Id = MciPhase2Id,
                Name = "Phase 2: Triage & Patient Transport",
                Description = "30-75 minutes. START triage operations, ambulance staging, patient transport " +
                              "coordination, and hospital destination decisions.",
                Sequence = 2,
                StartTime = new TimeOnly(9, 30),
                EndTime = new TimeOnly(10, 15),
                OrganizationId = BetaOrganizationId,
                ExerciseId = MciTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Phase
            {
                Id = MciPhase3Id,
                Name = "Phase 3: Hospital Surge & Medical Operations",
                Description = "75-135 minutes. Hospital surge activation, OR overflow, blood supply management, " +
                              "and medical examiner coordination.",
                Sequence = 3,
                StartTime = new TimeOnly(10, 15),
                EndTime = new TimeOnly(11, 15),
                OrganizationId = BetaOrganizationId,
                ExerciseId = MciTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Phase
            {
                Id = MciPhase4Id,
                Name = "Phase 4: Family Reunification & Demobilization",
                Description = "135-180 minutes. Family Assistance Center activation, public information, " +
                              "scene release, and incident demobilization.",
                Sequence = 4,
                StartTime = new TimeOnly(11, 15),
                EndTime = new TimeOnly(12, 0),
                OrganizationId = BetaOrganizationId,
                ExerciseId = MciTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            }
        });

        // Hazmat FE Phases
        phases.AddRange(new[]
        {
            new Phase
            {
                Id = HazmatPhase1Id,
                Name = "Phase 1: Detection & Initial Response",
                Description = "0-2 hours. Initial reports, HAZMAT team dispatch, chemical identification, " +
                              "and zone establishment.",
                Sequence = 1,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(10, 0),
                OrganizationId = BetaOrganizationId,
                ExerciseId = HazmatFeId,
                CreatedAt = DateTime.UtcNow.AddDays(-55),
                UpdatedAt = DateTime.UtcNow.AddDays(-55),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Phase
            {
                Id = HazmatPhase2Id,
                Name = "Phase 2: Containment & Protective Actions",
                Description = "2-5 hours. Source containment, shelter-in-place/evacuation, air monitoring, " +
                              "and hospital coordination.",
                Sequence = 2,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(13, 0),
                OrganizationId = BetaOrganizationId,
                ExerciseId = HazmatFeId,
                CreatedAt = DateTime.UtcNow.AddDays(-55),
                UpdatedAt = DateTime.UtcNow.AddDays(-55),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Phase
            {
                Id = HazmatPhase3Id,
                Name = "Phase 3: Decontamination & Recovery",
                Description = "5-8 hours. Mass decontamination, environmental sampling, all-clear decision, " +
                              "and re-entry authorization.",
                Sequence = 3,
                StartTime = new TimeOnly(13, 0),
                EndTime = new TimeOnly(16, 0),
                OrganizationId = BetaOrganizationId,
                ExerciseId = HazmatFeId,
                CreatedAt = DateTime.UtcNow.AddDays(-55),
                UpdatedAt = DateTime.UtcNow.AddDays(-55),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            }
        });

        // World Cup TTX Phases
        phases.AddRange(new[]
        {
            new Phase
            {
                Id = WcPhase1Id,
                Name = "Phase 1: Pre-Match Security Operations",
                Description = "T-6 hours to kickoff. Credential system operations, perimeter security, " +
                              "VIP arrivals, counter-UAS operations, and intelligence monitoring.",
                Sequence = 1,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(11, 0),
                OrganizationId = BetaOrganizationId,
                ExerciseId = WorldCupTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Phase
            {
                Id = WcPhase2Id,
                Name = "Phase 2: Match Day Operations",
                Description = "Gates open through halftime. Crowd management, in-venue incidents, " +
                              "medical emergencies, and security threat response.",
                Sequence = 2,
                StartTime = new TimeOnly(11, 0),
                EndTime = new TimeOnly(13, 0),
                OrganizationId = BetaOrganizationId,
                ExerciseId = WorldCupTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Phase
            {
                Id = WcPhase3Id,
                Name = "Phase 3: Incident Response & Post-Match",
                Description = "Second half through venue clear. Major incident response, crowd dispersal, " +
                              "evidence preservation, and after-action hot wash.",
                Sequence = 3,
                StartTime = new TimeOnly(13, 0),
                EndTime = new TimeOnly(15, 0),
                OrganizationId = BetaOrganizationId,
                ExerciseId = WorldCupTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            }
        });

        // WC Preliminary Draw Phases
        phases.AddRange(new[]
        {
            new Phase
            {
                Id = WcPrelimPhase1Id,
                Name = "Phase 1: Arrival & Ceremony Operations",
                Description = "VIP arrivals, credentialing, ceremony security, and dignitary protection " +
                              "for 500+ international dignitaries and 15,000 invited guests.",
                Sequence = 1,
                StartTime = new TimeOnly(7, 0),
                EndTime = new TimeOnly(13, 0),
                OrganizationId = BetaOrganizationId,
                ExerciseId = WcPrelimDrawId,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UpdatedAt = DateTime.UtcNow.AddMonths(-6),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Phase
            {
                Id = WcPrelimPhase2Id,
                Name = "Phase 2: Post-Event & Demobilization",
                Description = "Post-ceremony crowd dispersal, VIP departures, venue security release, " +
                              "and demobilization operations.",
                Sequence = 2,
                StartTime = new TimeOnly(13, 0),
                EndTime = new TimeOnly(19, 0),
                OrganizationId = BetaOrganizationId,
                ExerciseId = WcPrelimDrawId,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                UpdatedAt = DateTime.UtcNow.AddMonths(-6),
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            }
        });

        return phases;
    }

    #endregion

    #region Objectives

    private static List<Objective> CreateAllObjectives()
    {
        var objectives = new List<Objective>();

        // MCI TTX Objectives
        objectives.AddRange(new[]
        {
            new Objective
            {
                Id = MciObj1Id, ObjectiveNumber = "1", Name = "MCI Scene Management",
                Description = "Demonstrate the ability to establish unified command and effective scene control " +
                              "within 15 minutes of first responder arrival, including perimeter security, " +
                              "staging area designation, and resource coordination.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = MciTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14), UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Objective
            {
                Id = MciObj2Id, ObjectiveNumber = "2", Name = "Mass Triage Operations",
                Description = "Demonstrate the ability to implement START triage for 60+ patients within " +
                              "30 minutes and accurately categorize patients by acuity for transport prioritization.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = MciTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14), UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Objective
            {
                Id = MciObj3Id, ObjectiveNumber = "3", Name = "Patient Transport Coordination",
                Description = "Demonstrate the ability to coordinate patient transport to 4 area hospitals " +
                              "based on capability and capacity, managing ambulance staging and air transport " +
                              "for critical patients.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = MciTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14), UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Objective
            {
                Id = MciObj4Id, ObjectiveNumber = "4", Name = "Hospital Surge Activation",
                Description = "Demonstrate the ability to activate hospital surge protocols across the " +
                              "regional hospital network and implement patient tracking across facilities.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = MciTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14), UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Objective
            {
                Id = MciObj5Id, ObjectiveNumber = "5", Name = "Family Assistance & Public Information",
                Description = "Demonstrate the ability to establish a Family Assistance Center and coordinate " +
                              "timely, accurate public messaging across agencies within 2 hours of incident.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = MciTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14), UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            }
        });

        // Hazmat FE Objectives
        objectives.AddRange(new[]
        {
            new Objective
            {
                Id = HazmatObj1Id, ObjectiveNumber = "1", Name = "HAZMAT Identification & Zone Establishment",
                Description = "Demonstrate the ability to identify the chemical agent and establish hot, warm, " +
                              "and cold zones within 45 minutes of HAZMAT team arrival.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = HazmatFeId,
                CreatedAt = DateTime.UtcNow.AddDays(-55), UpdatedAt = DateTime.UtcNow.AddDays(-55),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Objective
            {
                Id = HazmatObj2Id, ObjectiveNumber = "2", Name = "Protective Action Implementation",
                Description = "Demonstrate the ability to execute shelter-in-place and evacuation orders " +
                              "for affected populations within 1 hour of zone establishment.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = HazmatFeId,
                CreatedAt = DateTime.UtcNow.AddDays(-55), UpdatedAt = DateTime.UtcNow.AddDays(-55),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Objective
            {
                Id = HazmatObj3Id, ObjectiveNumber = "3", Name = "Environmental Monitoring",
                Description = "Demonstrate the ability to deploy air monitoring and water sampling within " +
                              "2 hours of incident confirmation and maintain continuous environmental assessment.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = HazmatFeId,
                CreatedAt = DateTime.UtcNow.AddDays(-55), UpdatedAt = DateTime.UtcNow.AddDays(-55),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Objective
            {
                Id = HazmatObj4Id, ObjectiveNumber = "4", Name = "Mass Decontamination Operations",
                Description = "Demonstrate the ability to establish and operate a decontamination corridor " +
                              "capable of processing 200+ affected residents per hour.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = HazmatFeId,
                CreatedAt = DateTime.UtcNow.AddDays(-55), UpdatedAt = DateTime.UtcNow.AddDays(-55),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            }
        });

        // World Cup TTX Objectives
        objectives.AddRange(new[]
        {
            new Objective
            {
                Id = WcObj1Id, ObjectiveNumber = "1", Name = "Credentialing & Access Control",
                Description = "Validate the multi-tier credentialing system across 12 venue entry points " +
                              "including contingency procedures for system failures and fraudulent credentials.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = WorldCupTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14), UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Objective
            {
                Id = WcObj2Id, ObjectiveNumber = "2", Name = "Threat Detection & Response",
                Description = "Demonstrate detection and response capabilities for CBRNE threats, active threat " +
                              "scenarios, and unauthorized UAS within the venue security perimeter.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = WorldCupTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14), UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Objective
            {
                Id = WcObj3Id, ObjectiveNumber = "3", Name = "Crowd Management & Flow",
                Description = "Demonstrate effective management of ingress and egress for 65,000 spectators " +
                              "while maintaining security posture and responding to crowd safety incidents.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = WorldCupTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14), UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Objective
            {
                Id = WcObj4Id, ObjectiveNumber = "4", Name = "Multi-Agency Unified Command",
                Description = "Demonstrate unified command operations between federal (CISA/FBI/USSS), state, " +
                              "and local agencies including clear authority delineation and decision escalation.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = WorldCupTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14), UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Objective
            {
                Id = WcObj5Id, ObjectiveNumber = "5", Name = "Communications & Public Information",
                Description = "Maintain secure interoperable communications between all agencies and coordinate " +
                              "public messaging including emergency notifications through venue PA and digital systems.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = WorldCupTtxId,
                CreatedAt = DateTime.UtcNow.AddDays(-14), UpdatedAt = DateTime.UtcNow.AddDays(-14),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            }
        });

        // WC Preliminary Draw Objectives
        objectives.AddRange(new[]
        {
            new Objective
            {
                Id = WcPrelimObj1Id, ObjectiveNumber = "1", Name = "VIP & Dignitary Security",
                Description = "Coordinate security operations for 500+ international dignitaries including " +
                              "FIFA officials and government leaders across motorcade, venue, and hotel zones.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = WcPrelimDrawId,
                CreatedAt = DateTime.UtcNow.AddMonths(-6), UpdatedAt = DateTime.UtcNow.AddMonths(-6),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Objective
            {
                Id = WcPrelimObj2Id, ObjectiveNumber = "2", Name = "Crowd Management for Ceremonial Events",
                Description = "Manage 15,000 invited guests through credentialing, screening, and seating " +
                              "while maintaining dignified event atmosphere and security posture.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = WcPrelimDrawId,
                CreatedAt = DateTime.UtcNow.AddMonths(-6), UpdatedAt = DateTime.UtcNow.AddMonths(-6),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Objective
            {
                Id = WcPrelimObj3Id, ObjectiveNumber = "3", Name = "Multi-Agency Emergency Response",
                Description = "Coordinate emergency response capabilities between federal, state, and local " +
                              "partners including medical, fire, and law enforcement for venue incidents.",
                OrganizationId = BetaOrganizationId,
                ExerciseId = WcPrelimDrawId,
                CreatedAt = DateTime.UtcNow.AddMonths(-6), UpdatedAt = DateTime.UtcNow.AddMonths(-6),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            }
        });

        return objectives;
    }

    #endregion

    #region Injects

    private static List<Inject> CreateAllInjects(DateTime now)
    {
        var injects = new List<Inject>();

        injects.AddRange(CreateMciInjects(now));
        injects.AddRange(CreateHazmatInjects(now));
        injects.AddRange(CreateWorldCupInjects());
        injects.AddRange(CreateWcPrelimInjects(now));

        return injects;
    }

    // =========================================================================
    // MCI TTX Injects - ACTIVE exercise with mixed statuses
    // =========================================================================

    private static List<Inject> CreateMciInjects(DateTime now)
    {
        var fired1 = now.AddMinutes(-45);
        var fired2 = now.AddMinutes(-38);
        var fired3 = now.AddMinutes(-30);
        var fired4 = now.AddMinutes(-22);
        var fired5 = now.AddMinutes(-15);

        return new List<Inject>
        {
            // === PHASE 1: Initial Response (all Released) ===
            new Inject
            {
                Id = MciInject1Id, InjectNumber = 1,
                Title = "Multi-Vehicle Accident on Highway 17",
                Description = "911 Dispatch receives multiple calls reporting a major accident on Highway 17 " +
                              "involving a charter bus and 3 passenger vehicles near mile marker 42. Callers " +
                              "report the bus overturned with passengers trapped. Multiple injuries visible. " +
                              "Highway blocked in both directions.",
                ScheduledTime = new TimeOnly(9, 0), DeliveryTime = TimeSpan.Zero,
                ScenarioDay = 1, ScenarioTime = new TimeOnly(8, 0),
                Target = "Fire/EMS Dispatch", Source = "911 Communications Center",
                DeliveryMethod = DeliveryMethod.Radio, InjectType = InjectType.Standard,
                Status = InjectStatus.Released, Sequence = 1, Priority = 1,
                TriggerType = TriggerType.Manual, ResponsibleController = "Controller 1",
                LocationName = "Highway 17, MM 42", LocationType = "Field",
                Track = "Fire/EMS",
                ExpectedAction = "1. Dispatch Engine, Ladder, and Battalion Chief\n" +
                                 "2. Request additional ambulances based on caller reports\n" +
                                 "3. Notify law enforcement for traffic control\n" +
                                 "4. Alert hospitals of potential MCI",
                ControllerNotes = "This inject starts the exercise. Allow 3-5 minutes for initial dispatch " +
                                  "discussion before advancing.",
                FiredAt = fired1, FiredByUserId = null,
                MselId = MciMselId, PhaseId = MciPhase1Id,
                CreatedAt = now.AddDays(-14), UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Inject
            {
                Id = MciInject2Id, InjectNumber = 2,
                Title = "First Responder Scene Size-Up",
                Description = "Engine 7 Captain arrives on scene and reports: \"We have a charter bus on its " +
                              "side with approximately 45 passengers, many still inside. Three additional vehicles " +
                              "involved. I'm seeing at least 15-20 patients on the roadway. Multiple entrapments. " +
                              "This is a mass casualty incident. Requesting MCI Level 2 response.\"",
                ScheduledTime = new TimeOnly(9, 5), DeliveryTime = TimeSpan.FromMinutes(5),
                ScenarioDay = 1, ScenarioTime = new TimeOnly(8, 12),
                Target = "Incident Commander", Source = "Engine 7 Captain",
                DeliveryMethod = DeliveryMethod.Radio, InjectType = InjectType.Standard,
                Status = InjectStatus.Released, Sequence = 2, Priority = 1,
                TriggerType = TriggerType.Manual, ResponsibleController = "Controller 1",
                LocationName = "Highway 17, MM 42", LocationType = "Field",
                Track = "Fire/EMS",
                ExpectedAction = "1. Confirm MCI declaration and level\n" +
                                 "2. Request mutual aid per MCI plan\n" +
                                 "3. Establish unified command with PD\n" +
                                 "4. Designate staging area",
                FiredAt = fired2, FiredByUserId = null,
                MselId = MciMselId, PhaseId = MciPhase1Id,
                CreatedAt = now.AddDays(-14), UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Inject
            {
                Id = MciInject3Id, InjectNumber = 3,
                Title = "MCI Declaration & EOC Notification",
                Description = "Incident Commander formally declares MCI Level 2 and requests EOC activation. " +
                              "\"Declaring MCI Level 2. Estimated 60+ patients. Need all available transport units, " +
                              "mutual aid from adjacent jurisdictions, and EOC activation for hospital coordination.\"",
                ScheduledTime = new TimeOnly(9, 10), DeliveryTime = TimeSpan.FromMinutes(10),
                ScenarioDay = 1, ScenarioTime = new TimeOnly(8, 20),
                Target = "EOC / Emergency Management", Source = "Incident Commander",
                DeliveryMethod = DeliveryMethod.Radio, InjectType = InjectType.Standard,
                Status = InjectStatus.Released, Sequence = 3, Priority = 1,
                TriggerType = TriggerType.Manual, ResponsibleController = "Controller 1",
                LocationName = "EOC", LocationType = "Command",
                Track = "Command",
                ExpectedAction = "1. Activate EOC and key staff\n" +
                                 "2. Begin hospital capacity polling\n" +
                                 "3. Activate mutual aid agreements\n" +
                                 "4. Notify County Executive",
                FiredAt = fired3, FiredByUserId = null,
                MselId = MciMselId, PhaseId = MciPhase1Id,
                CreatedAt = now.AddDays(-14), UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Inject
            {
                Id = MciInject4Id, InjectNumber = 4,
                Title = "Media Arrival at Scene",
                Description = "WKST News helicopter is overhead. Two TV news crews are approaching the scene " +
                              "perimeter. A reporter calls the PIO: \"We're live in 10 minutes. Can you confirm " +
                              "reports of a major bus accident with fatalities on Highway 17?\"",
                ScheduledTime = new TimeOnly(9, 15), DeliveryTime = TimeSpan.FromMinutes(15),
                ScenarioDay = 1, ScenarioTime = new TimeOnly(8, 30),
                Target = "Public Information Officer", Source = "WKST News",
                DeliveryMethod = DeliveryMethod.Phone, InjectType = InjectType.Standard,
                Status = InjectStatus.Released, Sequence = 4, Priority = 2,
                TriggerType = TriggerType.Manual, ResponsibleController = "Controller 2",
                LocationName = "Scene Perimeter", LocationType = "Field",
                Track = "Public Information",
                ExpectedAction = "1. Coordinate messaging with IC before responding\n" +
                                 "2. Confirm incident without speculation on casualties\n" +
                                 "3. Establish media staging area away from operations\n" +
                                 "4. Schedule first press briefing",
                FiredAt = fired4, FiredByUserId = null,
                MselId = MciMselId, PhaseId = MciPhase1Id,
                CreatedAt = now.AddDays(-14), UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Inject
            {
                Id = MciInject5Id, InjectNumber = 5,
                Title = "County Executive Inquiry",
                Description = "County Executive's Chief of Staff calls the EOC: \"The County Executive is about " +
                              "to go into a press conference on another matter. She needs a 2-minute briefing on " +
                              "the Highway 17 situation right now. What do we know and what are we doing?\"",
                ScheduledTime = new TimeOnly(9, 20), DeliveryTime = TimeSpan.FromMinutes(20),
                ScenarioDay = 1, ScenarioTime = new TimeOnly(8, 45),
                Target = "EOC Director", Source = "County Executive Office",
                DeliveryMethod = DeliveryMethod.Phone, InjectType = InjectType.Standard,
                Status = InjectStatus.Released, Sequence = 5, Priority = 2,
                TriggerType = TriggerType.Manual, ResponsibleController = "Controller 1",
                LocationName = "EOC", LocationType = "Command",
                Track = "Command",
                ExpectedAction = "1. Provide concise situation summary\n" +
                                 "2. Advise County Exec on what she can/cannot say publicly\n" +
                                 "3. Recommend deferring to PIO on operational details",
                FiredAt = fired5, FiredByUserId = null,
                MselId = MciMselId, PhaseId = MciPhase1Id,
                CreatedAt = now.AddDays(-14), UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },

            // === PHASE 2: Triage & Transport (mixed statuses) ===
            new Inject
            {
                Id = Guid.NewGuid(), InjectNumber = 6,
                Title = "START Triage Results",
                Description = "Triage Officer reports results: 8 Red (Immediate), 15 Yellow (Delayed), " +
                              "28 Green (Minor), 4 Black (Deceased). \"Triage complete. We have 8 criticals " +
                              "needing immediate transport including 2 pediatric patients from the bus.\"",
                ScheduledTime = new TimeOnly(9, 30), DeliveryTime = TimeSpan.FromMinutes(30),
                ScenarioDay = 1, ScenarioTime = new TimeOnly(9, 0),
                Target = "Transport Group Supervisor", Source = "Triage Officer",
                DeliveryMethod = DeliveryMethod.Radio, InjectType = InjectType.Standard,
                Status = InjectStatus.Released, Sequence = 6, Priority = 1,
                TriggerType = TriggerType.Manual, ResponsibleController = "Controller 1",
                LocationName = "Triage Area", LocationType = "Field",
                Track = "Medical",
                ExpectedAction = "1. Prioritize Red patients for immediate transport\n" +
                                 "2. Assign hospital destinations based on capability\n" +
                                 "3. Request pediatric trauma capability confirmation\n" +
                                 "4. Set up Green patient holding area",
                FiredAt = now.AddMinutes(-8), FiredByUserId = null,
                MselId = MciMselId, PhaseId = MciPhase2Id,
                CreatedAt = now.AddDays(-14), UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Inject
            {
                Id = Guid.NewGuid(), InjectNumber = 7,
                Title = "Ambulance Staging Overflow",
                Description = "Staging Area Manager reports: \"I have 12 ambulances queued on Highway 17 " +
                              "shoulder. The staging area is at capacity. Mutual aid units from 3 jurisdictions " +
                              "are arriving and I need a secondary staging location or we'll gridlock.\"",
                ScheduledTime = new TimeOnly(9, 35), DeliveryTime = TimeSpan.FromMinutes(35),
                ScenarioDay = 1, ScenarioTime = new TimeOnly(9, 15),
                Target = "Transport Group Supervisor", Source = "Staging Area Manager",
                DeliveryMethod = DeliveryMethod.Radio, InjectType = InjectType.Standard,
                Status = InjectStatus.Released, Sequence = 7, Priority = 2,
                TriggerType = TriggerType.Manual, ResponsibleController = "Controller 2",
                LocationName = "Staging Area", LocationType = "Field",
                Track = "Fire/EMS",
                ExpectedAction = "1. Designate secondary staging area\n" +
                                 "2. Coordinate with PD for traffic routing\n" +
                                 "3. Implement ambulance rotation plan",
                FiredAt = now.AddMinutes(-3), FiredByUserId = null,
                MselId = MciMselId, PhaseId = MciPhase2Id,
                CreatedAt = now.AddDays(-14), UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Inject
            {
                Id = Guid.NewGuid(), InjectNumber = 8,
                Title = "Helicopter LZ Request for Critical Patients",
                Description = "Medical Branch requests air transport: \"I have 3 critical patients that need " +
                              "Level 1 trauma. Nearest trauma center is 45 minutes by ground. Requesting 2 " +
                              "medevac helicopters and a landing zone designation.\"",
                ScheduledTime = new TimeOnly(9, 40), DeliveryTime = TimeSpan.FromMinutes(40),
                ScenarioDay = 1, ScenarioTime = new TimeOnly(9, 30),
                Target = "Air Operations / IC", Source = "Medical Branch Director",
                DeliveryMethod = DeliveryMethod.Radio, InjectType = InjectType.Standard,
                Status = InjectStatus.Synchronized, ReadyAt = now,
                Sequence = 8, Priority = 1,
                TriggerType = TriggerType.Manual, ResponsibleController = "Controller 1",
                LocationName = "Scene", LocationType = "Field",
                Track = "Medical",
                ExpectedAction = "1. Designate helicopter landing zone\n" +
                                 "2. Request medevac through established channels\n" +
                                 "3. Coordinate LZ security with PD\n" +
                                 "4. Confirm receiving trauma center availability",
                MselId = MciMselId, PhaseId = MciPhase2Id,
                CreatedAt = now.AddDays(-14), UpdatedAt = now,
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Inject
            {
                Id = Guid.NewGuid(), InjectNumber = 9,
                Title = "Walking Wounded Bus Transport",
                Description = "28 Green (Minor) patients need transport to the minor injury clinic at " +
                              "Coastal Community Health Center. Several are elderly tour group members " +
                              "who are confused and distressed but medically stable.",
                ScheduledTime = new TimeOnly(9, 50), DeliveryTime = TimeSpan.FromMinutes(50),
                ScenarioDay = 1, ScenarioTime = new TimeOnly(9, 45),
                Target = "Transport Group", Source = "Treatment Group Supervisor",
                DeliveryMethod = DeliveryMethod.Radio, InjectType = InjectType.Standard,
                Status = InjectStatus.Draft, Sequence = 9, Priority = 3,
                TriggerType = TriggerType.Manual, ResponsibleController = "Controller 2",
                LocationName = "Green Treatment Area", LocationType = "Field",
                Track = "Medical",
                ExpectedAction = "1. Request transit bus for mass transport\n" +
                                 "2. Assign medical monitor for transport\n" +
                                 "3. Notify receiving facility of incoming count\n" +
                                 "4. Begin patient tracking documentation",
                MselId = MciMselId, PhaseId = MciPhase2Id,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-2),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Inject
            {
                Id = Guid.NewGuid(), InjectNumber = 10,
                Title = "Patient Tracking System Activation",
                Description = "Regional Hospital Coordinator requests: \"Hospitals are receiving patients from " +
                              "multiple transport units. We need the patient tracking system activated NOW. " +
                              "Coastal General reports 6 patients arrived but only 3 on the manifest.\"",
                ScheduledTime = new TimeOnly(10, 0), DeliveryTime = TimeSpan.FromMinutes(60),
                ScenarioDay = 1, ScenarioTime = new TimeOnly(10, 0),
                Target = "Medical Branch / EOC", Source = "Regional Hospital Coordinator",
                DeliveryMethod = DeliveryMethod.Phone, InjectType = InjectType.Standard,
                Status = InjectStatus.Draft, Sequence = 10, Priority = 1,
                TriggerType = TriggerType.Manual, ResponsibleController = "Controller 1",
                LocationName = "EOC", LocationType = "Command",
                Track = "Medical",
                ExpectedAction = "1. Activate patient tracking system\n" +
                                 "2. Reconcile patient manifests with hospital counts\n" +
                                 "3. Assign patient tracking coordinator at scene\n" +
                                 "4. Establish hospital liaison communications",
                MselId = MciMselId, PhaseId = MciPhase2Id,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-2),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },

            // === PHASE 3: Hospital Surge (all Draft/Pending) ===
            new Inject
            {
                Id = Guid.NewGuid(), InjectNumber = 11,
                Title = "Coastal General Hospital at Surge Capacity",
                Description = "Coastal General ED reports: \"We've received 14 patients from this incident. " +
                              "Our surge capacity is reached. Diverting all further MCI patients. 2 patients " +
                              "in critical condition heading to OR now.\"",
                ScheduledTime = new TimeOnly(10, 15), DeliveryTime = TimeSpan.FromMinutes(75),
                ScenarioDay = 1, ScenarioTime = new TimeOnly(10, 30),
                Target = "Medical Branch / EOC", Source = "Coastal General Hospital ED",
                DeliveryMethod = DeliveryMethod.Phone, InjectType = InjectType.Standard,
                Status = InjectStatus.Draft, Sequence = 11, Priority = 1,
                TriggerType = TriggerType.Manual, ResponsibleController = "Controller 1",
                LocationName = "EOC", LocationType = "Command", Track = "Medical",
                ExpectedAction = "1. Update hospital status board\n" +
                                 "2. Redirect transport to alternate hospitals\n" +
                                 "3. Notify all transport units of diversion",
                MselId = MciMselId, PhaseId = MciPhase3Id,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-2),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Inject
            {
                Id = Guid.NewGuid(), InjectNumber = 12,
                Title = "Blood Bank Supply Request",
                Description = "Regional blood bank contacts EOC: \"We're running critically low on O-negative. " +
                              "Current supply will be exhausted within 2 hours at current usage rate. Requesting " +
                              "emergency resupply from state blood center.\"",
                ScheduledTime = new TimeOnly(10, 30), DeliveryTime = TimeSpan.FromMinutes(90),
                ScenarioDay = 1, ScenarioTime = new TimeOnly(11, 0),
                Target = "Medical Branch", Source = "Regional Blood Bank Director",
                DeliveryMethod = DeliveryMethod.Phone, InjectType = InjectType.Standard,
                Status = InjectStatus.Draft, Sequence = 12, Priority = 1,
                TriggerType = TriggerType.Manual, ResponsibleController = "Controller 2",
                LocationName = "EOC", LocationType = "Command", Track = "Medical",
                ExpectedAction = "1. Contact state blood center for emergency supply\n" +
                                 "2. Coordinate transport for blood products\n" +
                                 "3. Issue community blood donation appeal",
                MselId = MciMselId, PhaseId = MciPhase3Id,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-2),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Inject
            {
                Id = Guid.NewGuid(), InjectNumber = 13,
                Title = "Medical Examiner Notification",
                Description = "Incident Commander confirms 4 fatalities at scene. Medical Examiner's office " +
                              "needs to be formally notified and coordinated for scene response.",
                ScheduledTime = new TimeOnly(10, 45), DeliveryTime = TimeSpan.FromMinutes(105),
                ScenarioDay = 1, ScenarioTime = new TimeOnly(11, 15),
                Target = "Medical Examiner / Law Enforcement", Source = "Incident Commander",
                DeliveryMethod = DeliveryMethod.Phone, InjectType = InjectType.Standard,
                Status = InjectStatus.Draft, Sequence = 13, Priority = 2,
                TriggerType = TriggerType.Manual, ResponsibleController = "Controller 1",
                LocationName = "Scene", LocationType = "Field", Track = "Law Enforcement",
                ExpectedAction = "1. Formally notify ME office\n" +
                                 "2. Preserve scene for ME investigation\n" +
                                 "3. Coordinate with PD for crash investigation\n" +
                                 "4. Arrange temporary morgue if needed",
                MselId = MciMselId, PhaseId = MciPhase3Id,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-2),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },

            // === PHASE 4: Family Reunification (all Draft) ===
            new Inject
            {
                Id = Guid.NewGuid(), InjectNumber = 14,
                Title = "Family Reunification Center Activation",
                Description = "American Red Cross chapter contacts EOC: \"We have volunteers ready to staff " +
                              "a Family Assistance Center. We're estimating 100+ families will be seeking " +
                              "information about the bus passengers. Where should we set up?\"",
                ScheduledTime = new TimeOnly(11, 15), DeliveryTime = TimeSpan.FromMinutes(135),
                ScenarioDay = 1, ScenarioTime = new TimeOnly(12, 0),
                Target = "Mass Care / EOC", Source = "American Red Cross",
                DeliveryMethod = DeliveryMethod.Phone, InjectType = InjectType.Standard,
                Status = InjectStatus.Draft, Sequence = 14, Priority = 2,
                TriggerType = TriggerType.Manual, ResponsibleController = "Controller 2",
                LocationName = "EOC", LocationType = "Command", Track = "Mass Care",
                ExpectedAction = "1. Designate FAC location near but separate from scene\n" +
                                 "2. Coordinate with PD for FAC security\n" +
                                 "3. Activate crisis counseling resources\n" +
                                 "4. Establish patient information hotline",
                MselId = MciMselId, PhaseId = MciPhase4Id,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-2),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Inject
            {
                Id = Guid.NewGuid(), InjectNumber = 15,
                Title = "Joint Media Briefing Coordination",
                Description = "PIO requests coordination for a joint media briefing. Hospitals, fire, PD, " +
                              "and the County Executive's office all need aligned talking points before " +
                              "the 12:30 PM press conference.",
                ScheduledTime = new TimeOnly(11, 30), DeliveryTime = TimeSpan.FromMinutes(150),
                ScenarioDay = 1, ScenarioTime = new TimeOnly(12, 30),
                Target = "All Agency PIOs", Source = "Lead PIO / JIC",
                DeliveryMethod = DeliveryMethod.Email, InjectType = InjectType.Standard,
                Status = InjectStatus.Draft, Sequence = 15, Priority = 2,
                TriggerType = TriggerType.Manual, ResponsibleController = "Controller 2",
                LocationName = "JIC", LocationType = "Communications", Track = "Public Information",
                ExpectedAction = "1. Draft coordinated press release\n" +
                                 "2. Align hospital messaging on patient counts\n" +
                                 "3. Prepare County Executive talking points\n" +
                                 "4. Designate spokesperson order",
                MselId = MciMselId, PhaseId = MciPhase4Id,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-2),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            },
            new Inject
            {
                Id = Guid.NewGuid(), InjectNumber = 16,
                Title = "Incident Demobilization Planning",
                Description = "IC requests demobilization plan as scene operations wind down. All patients " +
                              "have been transported. ME has completed scene work. Highway needs to reopen.",
                ScheduledTime = new TimeOnly(11, 45), DeliveryTime = TimeSpan.FromMinutes(165),
                ScenarioDay = 1, ScenarioTime = new TimeOnly(14, 0),
                Target = "Planning Section / EOC", Source = "Incident Commander",
                DeliveryMethod = DeliveryMethod.Radio, InjectType = InjectType.Standard,
                Status = InjectStatus.Draft, Sequence = 16, Priority = 3,
                TriggerType = TriggerType.Manual, ResponsibleController = "Controller 1",
                LocationName = "Scene / EOC", LocationType = "Command", Track = "Command",
                ExpectedAction = "1. Develop demobilization checklist\n" +
                                 "2. Coordinate highway reopening with DOT\n" +
                                 "3. Schedule after-action hot wash\n" +
                                 "4. Ensure all units released from staging",
                MselId = MciMselId, PhaseId = MciPhase4Id,
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-2),
                CreatedBy = SystemConstants.SystemUserIdString, ModifiedBy = SystemConstants.SystemUserIdString
            }
        };
    }

    // =========================================================================
    // Hazmat FE Injects - COMPLETED exercise, all Released
    // =========================================================================

    private static List<Inject> CreateHazmatInjects(DateTime now)
    {
        var exerciseDate = now.AddDays(-30);
        var baseTime = exerciseDate.AddHours(8);

        return new List<Inject>
        {
            // === Phase 1: Detection & Initial Response ===
            CreateFiredInject(HazmatInject1Id, 1, "Chemical Odor Reports from Residents",
                "911 receiving multiple calls about a strong chemical smell in the Bayshore neighborhood. " +
                "Residents reporting eye irritation and difficulty breathing. Wind is from the west.",
                new TimeOnly(8, 0), TimeSpan.Zero, "Fire Dispatch / HAZMAT", "911 Dispatch",
                DeliveryMethod.Radio, "HAZMAT", 1, HazmatMselId, HazmatPhase1Id,
                baseTime, exerciseDate.AddDays(-25)),

            CreateFiredInject(HazmatInject2Id, 2, "HAZMAT Team Initial Assessment",
                "HAZMAT 1 identifies chlorine gas leak from a 1-ton cylinder at Bayshore Chemical plant. " +
                "Visible green-yellow gas cloud drifting east toward residential area. Facility evacuated.",
                new TimeOnly(8, 30), TimeSpan.FromMinutes(30), "Incident Commander", "HAZMAT Team Leader",
                DeliveryMethod.Radio, "HAZMAT", 1, HazmatMselId, HazmatPhase1Id,
                baseTime.AddMinutes(30), exerciseDate.AddDays(-25)),

            CreateFiredInject(HazmatInject3Id, 3, "Wind Direction Change Alert",
                "NWS reports wind shifting onshore (east). Gas plume now expanding toward a larger " +
                "residential area including Bayshore Elementary School (450 students, 0.5 miles downwind).",
                new TimeOnly(9, 0), TimeSpan.FromMinutes(60), "IC / EOC", "National Weather Service",
                DeliveryMethod.Email, "HAZMAT", 1, HazmatMselId, HazmatPhase1Id,
                baseTime.AddMinutes(55), exerciseDate.AddDays(-25)),

            CreateFiredInject(Guid.NewGuid(), 4, "School in Affected Zone",
                "Bayshore Elementary principal calls 911: \"We can smell chemicals. Some children are " +
                "coughing. Should we evacuate or shelter in place? Parents are calling demanding answers.\"",
                new TimeOnly(9, 15), TimeSpan.FromMinutes(75), "EOC / Mass Care", "School District",
                DeliveryMethod.Phone, "Schools", 1, HazmatMselId, HazmatPhase1Id,
                baseTime.AddMinutes(70), exerciseDate.AddDays(-25)),

            CreateFiredInject(Guid.NewGuid(), 5, "Facility Owner Provides SDS",
                "Plant manager provides Safety Data Sheet: chlorine gas, cylinder contains 2,000 lbs. " +
                "Estimated 800 lbs released. IDLH: 10 ppm. Facility has no secondary containment.",
                new TimeOnly(9, 30), TimeSpan.FromMinutes(90), "HAZMAT Team / IC", "Bayshore Chemical Plant Manager",
                DeliveryMethod.Verbal, "HAZMAT", 2, HazmatMselId, HazmatPhase1Id,
                baseTime.AddMinutes(85), exerciseDate.AddDays(-25)),

            // === Phase 2: Containment & Protective Actions ===
            CreateFiredInject(Guid.NewGuid(), 6, "Shelter-in-Place Order for Zone 1",
                "IC orders shelter-in-place for 2,500 residents within 1-mile radius of facility. " +
                "Emergency Alert System activation requested. Door-to-door notification in progress.",
                new TimeOnly(10, 0), TimeSpan.FromMinutes(120), "PIO / Emergency Alert System", "Incident Commander",
                DeliveryMethod.Radio, "Public Information", 1, HazmatMselId, HazmatPhase2Id,
                baseTime.AddMinutes(120), exerciseDate.AddDays(-25)),

            CreateFiredInject(Guid.NewGuid(), 7, "Evacuation Order for Zone 2",
                "Wind shift makes shelter-in-place insufficient for 800 residents in Zone 2. " +
                "IC upgrades to evacuation order. Buses needed for residents without transportation.",
                new TimeOnly(10, 30), TimeSpan.FromMinutes(150), "PD / Transportation", "Incident Commander",
                DeliveryMethod.Radio, "Law Enforcement", 1, HazmatMselId, HazmatPhase2Id,
                baseTime.AddMinutes(145), exerciseDate.AddDays(-25)),

            CreateFiredInject(Guid.NewGuid(), 8, "Air Monitoring Results",
                "Environmental Health reports: Chlorine levels exceeding IDLH (10 ppm) at 3 of 5 monitoring " +
                "stations. Readings: Station 1: 15 ppm, Station 2: 22 ppm, Station 3: 8 ppm.",
                new TimeOnly(11, 0), TimeSpan.FromMinutes(180), "HAZMAT / EOC", "Environmental Health",
                DeliveryMethod.Email, "Environmental", 1, HazmatMselId, HazmatPhase2Id,
                baseTime.AddMinutes(175), exerciseDate.AddDays(-25)),

            CreateFiredInject(Guid.NewGuid(), 9, "Hospital Chlorine Exposure Patients",
                "Coastal General ED reports 12 walk-in patients with chlorine exposure symptoms. " +
                "2 in serious condition with pulmonary edema. Requesting decontamination guidance.",
                new TimeOnly(11, 30), TimeSpan.FromMinutes(210), "Medical Branch / HAZMAT", "Coastal General ED",
                DeliveryMethod.Phone, "Medical", 1, HazmatMselId, HazmatPhase2Id,
                baseTime.AddMinutes(205), exerciseDate.AddDays(-25)),

            CreateFiredInject(Guid.NewGuid(), 10, "EPA On-Scene Coordinator En Route",
                "EPA Region IV activating federal response. On-Scene Coordinator ETA 3 hours. " +
                "Requesting facility information package and current air monitoring data.",
                new TimeOnly(12, 0), TimeSpan.FromMinutes(240), "EOC / HAZMAT", "EPA Region IV",
                DeliveryMethod.Email, "Environmental", 2, HazmatMselId, HazmatPhase2Id,
                baseTime.AddMinutes(230), exerciseDate.AddDays(-25)),

            // === Phase 3: Decontamination & Recovery ===
            CreateFiredInject(Guid.NewGuid(), 11, "Leak Contained - Source Secured",
                "HAZMAT Team reports: \"Chlorine cylinder valve has been closed. Leak is contained. " +
                "Gas cloud dissipating. Maintaining hot zone until air monitoring confirms.\"",
                new TimeOnly(13, 0), TimeSpan.FromMinutes(300), "IC / EOC", "HAZMAT Team Leader",
                DeliveryMethod.Radio, "HAZMAT", 1, HazmatMselId, HazmatPhase3Id,
                baseTime.AddMinutes(295), exerciseDate.AddDays(-25)),

            CreateFiredInject(Guid.NewGuid(), 12, "Mass Decon Corridor Active",
                "Mass decontamination corridor operational at Bayshore Park. Processing evacuees from " +
                "Zone 2. 45 residents processed so far, 12 requiring medical evaluation post-decon.",
                new TimeOnly(13, 30), TimeSpan.FromMinutes(330), "Medical Branch", "HAZMAT Decon Team",
                DeliveryMethod.Radio, "HAZMAT", 2, HazmatMselId, HazmatPhase3Id,
                baseTime.AddMinutes(325), exerciseDate.AddDays(-25)),

            CreateFiredInject(Guid.NewGuid(), 13, "Water Sampling Results",
                "Storm drain samples show elevated chlorine levels entering Coastal Creek watershed. " +
                "Environmental Health recommends containment booms at drain outlets.",
                new TimeOnly(14, 0), TimeSpan.FromMinutes(360), "Public Works / EPA", "Environmental Health",
                DeliveryMethod.Email, "Environmental", 2, HazmatMselId, HazmatPhase3Id,
                baseTime.AddMinutes(355), exerciseDate.AddDays(-25)),

            CreateFiredInject(Guid.NewGuid(), 14, "All-Clear Decision Point",
                "Air monitoring shows chlorine levels below PEL (1 ppm) for 30 consecutive minutes " +
                "at all 5 stations. HAZMAT Team recommends downgrading to recovery operations.",
                new TimeOnly(15, 0), TimeSpan.FromMinutes(420), "IC", "HAZMAT / Environmental Health",
                DeliveryMethod.Radio, "HAZMAT", 1, HazmatMselId, HazmatPhase3Id,
                baseTime.AddMinutes(415), exerciseDate.AddDays(-25)),

            CreateFiredInject(Guid.NewGuid(), 15, "Re-entry Authorization",
                "IC authorizes resident re-entry for all zones. PIO issues all-clear through EAS and " +
                "social media. Door-to-door notification teams dispatched to confirm.",
                new TimeOnly(15, 30), TimeSpan.FromMinutes(450), "PIO / PD", "Incident Commander",
                DeliveryMethod.Radio, "Public Information", 2, HazmatMselId, HazmatPhase3Id,
                baseTime.AddMinutes(445), exerciseDate.AddDays(-25))
        };
    }

    /// <summary>Helper to create a Released/fired inject for completed exercises.</summary>
    private static Inject CreateFiredInject(
        Guid id, int number, string title, string description,
        TimeOnly scheduledTime, TimeSpan deliveryTime,
        string target, string source, DeliveryMethod method,
        string track, int priority, Guid mselId, Guid phaseId,
        DateTime firedAt, DateTime createdAt)
    {
        return new Inject
        {
            Id = id, InjectNumber = number,
            Title = title, Description = description,
            ScheduledTime = scheduledTime, DeliveryTime = deliveryTime,
            ScenarioDay = 1, Target = target, Source = source,
            DeliveryMethod = method, InjectType = InjectType.Standard,
            Status = InjectStatus.Released, Sequence = number, Priority = priority,
            TriggerType = TriggerType.Manual,
            Track = track, LocationName = "EOC", LocationType = "Command",
            FiredAt = firedAt, FiredByUserId = null,
            MselId = mselId, PhaseId = phaseId,
            CreatedAt = createdAt, UpdatedAt = firedAt,
            CreatedBy = SystemConstants.SystemUserIdString,
            ModifiedBy = SystemConstants.SystemUserIdString
        };
    }

    // =========================================================================
    // World Cup TTX Injects - DRAFT exercise, all Draft status
    // =========================================================================

    private static List<Inject> CreateWorldCupInjects()
    {
        var created = DateTime.UtcNow.AddDays(-10);

        return new List<Inject>
        {
            // === Phase 1: Pre-Match Security ===
            CreateDraftInject(WcInject1Id, 1, "Credentialing System Failure at Gate 4",
                "Handheld credential scanners offline at Gate 4. 2,000+ ticket holders queued. " +
                "Gate supervisor requests authorization for manual verification procedures. " +
                "Fan frustration escalating with 90 minutes to kickoff.",
                new TimeOnly(9, 0), "VSOC", "Stadium Security Director",
                "Security", 1, WorldCupMselId, WcPhase1Id, created),

            CreateDraftInject(WcInject2Id, 2, "Suspicious Vehicle in Restricted Zone",
                "Unattended cargo van in player arrival zone doesn't match approved vehicle manifest. " +
                "K-9 team alerted. USSS Protective Detail notified with team buses arriving in 20 minutes.",
                new TimeOnly(9, 15), "USSS / Bomb Squad", "Outer Perimeter Security",
                "Law Enforcement", 1, WorldCupMselId, WcPhase1Id, created),

            CreateDraftInject(Guid.NewGuid(), 3, "VIP Motorcade Arrival Coordination",
                "FIFA delegation and senior government officials arriving via separate motorcade routes. " +
                "USSS advance team requests timing coordination to prevent motorcade overlap at venue entrance.",
                new TimeOnly(9, 30), "VSOC / Traffic Management", "USSS Advance Team",
                "Security", 2, WorldCupMselId, WcPhase1Id, created),

            CreateDraftInject(Guid.NewGuid(), 4, "Counter-UAS Detection Alert",
                "C-UAS system detects unauthorized drone approaching from northeast, 0.5 miles from venue. " +
                "FAA notified. Drone appears to be commercial photography type with camera payload.",
                new TimeOnly(9, 45), "VSOC / Law Enforcement Air Support", "CISA C-UAS Operations",
                "Security", 1, WorldCupMselId, WcPhase1Id, created),

            CreateDraftInject(Guid.NewGuid(), 5, "Social Media Threat Intelligence",
                "Joint Intelligence Center identifies social media post with credible threat language " +
                "referencing today's match and a specific venue gate number. Post traced to local IP address. " +
                "FBI cyber team assessing credibility.",
                new TimeOnly(10, 0), "FBI On-Scene Commander / VSOC", "Joint Intelligence Center",
                "Intelligence", 1, WorldCupMselId, WcPhase1Id, created),

            CreateDraftInject(Guid.NewGuid(), 6, "Vendor Credentialing Discrepancy",
                "15 catering staff credentials do not match the approved vendor list submitted to USSS. " +
                "Staff claim they are last-minute replacements. Catering company confirms changes but " +
                "did not submit updated background check paperwork.",
                new TimeOnly(10, 15), "Credentialing Operations", "Gate Security Supervisor",
                "Security", 2, WorldCupMselId, WcPhase1Id, created),

            CreateDraftInject(Guid.NewGuid(), 7, "Organized Protest at Fan Festival",
                "200+ organized protesters assembling in Fan Festival area adjacent to venue. Some carrying " +
                "prohibited items (poles, large banners). Media cameras gathering. Protest permit is for " +
                "a location 2 blocks away.",
                new TimeOnly(10, 30), "PD Event Commander", "Community Liaison Officer",
                "Public Order", 2, WorldCupMselId, WcPhase1Id, created),

            // === Phase 2: Match Day Operations ===
            CreateDraftInject(Guid.NewGuid(), 8, "Dangerous Crowd Compression at North Gate",
                "Crowd management team reports dangerous compression at North Gate as 8,000 fans arrive " +
                "simultaneously 30 minutes before kickoff. Crowd density exceeding safe limits. " +
                "Risk of crush incident if not addressed immediately.",
                new TimeOnly(11, 0), "VSOC / Gate Operations", "Crowd Management Team Leader",
                "Crowd Management", 1, WorldCupMselId, WcPhase2Id, created),

            CreateDraftInject(Guid.NewGuid(), 9, "Medical Emergency - Cardiac Arrest in Stands",
                "Spectator in cardiac arrest in Section 412, upper deck. Venue medical staff have deployed " +
                "AED. Requesting EMS response for transport. Access via upper concourse - elevator needed.",
                new TimeOnly(11, 15), "EMS Branch", "Venue Medical Director",
                "Medical", 1, WorldCupMselId, WcPhase2Id, created),

            CreateDraftInject(Guid.NewGuid(), 10, "Suspicious Package on Concourse B",
                "Unattended backpack found near concession stand on Concourse B. Stadium security has " +
                "cordoned the area. Contents unknown. Located 50 feet from a structural column.",
                new TimeOnly(11, 30), "Bomb Squad / VSOC", "Stadium Security",
                "Security", 1, WorldCupMselId, WcPhase2Id, created),

            CreateDraftInject(Guid.NewGuid(), 11, "Duplicate Credential Detected",
                "Credential technology team detects same VIP media credential scanned at Gate 2 and Gate 7 " +
                "within 3 minutes. Potential credential cloning. Bearer at Gate 7 does not match photo.",
                new TimeOnly(11, 45), "USSS / FBI", "Credential Technology Team",
                "Security", 1, WorldCupMselId, WcPhase2Id, created),

            CreateDraftInject(Guid.NewGuid(), 12, "Separated Children Reports",
                "Fan Services reports 4 children separated from parents in different stadium sections. " +
                "Ages range from 5-10. Language barriers with 2 children (non-English speaking).",
                new TimeOnly(12, 0), "Child Reunification / PD", "Fan Services Manager",
                "Public Safety", 2, WorldCupMselId, WcPhase2Id, created),

            CreateDraftInject(Guid.NewGuid(), 13, "Severe Weather Advisory",
                "NWS issues severe thunderstorm warning for venue area. 45 minutes to impact. " +
                "Possible lightning and hail. Match Commissioner considering lightning delay protocol.",
                new TimeOnly(12, 30), "Match Commissioner / VSOC", "NWS / Stadium Weather Operations",
                "Operations", 1, WorldCupMselId, WcPhase2Id, created),

            // === Phase 3: Incident Response & Post-Match ===
            CreateDraftInject(Guid.NewGuid(), 14, "Active Threat Report - Concourse Level",
                "Security reports individual displaying weapon sighted near Concourse A entrance. " +
                "Lockdown initiated for Concourse A. Tactical response team activating. 15,000 spectators " +
                "in affected sections.",
                new TimeOnly(13, 0), "Tactical Response / All Units", "VSOC",
                "Law Enforcement", 1, WorldCupMselId, WcPhase3Id, created),

            CreateDraftInject(Guid.NewGuid(), 15, "Mass Notification - Shelter in Place",
                "PA system and digital signage activated for shelter-in-place in Sections 100-200. " +
                "Social media monitoring shows fans posting live updates including tactical positions.",
                new TimeOnly(13, 15), "Stadium Operations / Communications", "VSOC",
                "Communications", 1, WorldCupMselId, WcPhase3Id, created),

            CreateDraftInject(Guid.NewGuid(), 16, "Casualty Collection Point Activation",
                "CCP established at Field Level Gate 1 for potential victims. 3 persons with minor injuries " +
                "from crowd movement during lockdown. EMS branch requesting hospital notification.",
                new TimeOnly(13, 30), "Medical Group / Hospital Network", "EMS Branch Director",
                "Medical", 1, WorldCupMselId, WcPhase3Id, created),

            CreateDraftInject(Guid.NewGuid(), 17, "Post-Event Crowd Dispersal Incident",
                "65,000 fans exiting through reduced egress points due to security cordon in Sector 3. " +
                "Transit authority reports platform overcrowding at Metro Stadium station. Crowd density " +
                "unsafe at south exit gates.",
                new TimeOnly(14, 0), "VSOC / Traffic / Transit Authority", "Crowd Management",
                "Crowd Management", 1, WorldCupMselId, WcPhase3Id, created),

            CreateDraftInject(Guid.NewGuid(), 18, "Evidence Preservation & FBI Scene Hold",
                "FBI On-Scene Commander requests scene preservation at 3 locations for investigation: " +
                "Concourse A entrance, Section 112 stairwell, and North parking garage level 2.",
                new TimeOnly(14, 15), "PD / Stadium Security", "FBI On-Scene Commander",
                "Law Enforcement", 1, WorldCupMselId, WcPhase3Id, created),

            CreateDraftInject(Guid.NewGuid(), 19, "International Media Firestorm",
                "International media covering the match is reporting unconfirmed casualty numbers. " +
                "FIFA headquarters in Zurich requesting official statement. State Department inquiring " +
                "about foreign national welfare.",
                new TimeOnly(14, 30), "JIC / State Department Liaison", "FIFA Communications",
                "Public Information", 1, WorldCupMselId, WcPhase3Id, created),

            CreateDraftInject(Guid.NewGuid(), 20, "After-Action Hot Wash",
                "Exercise Director calls immediate hot wash debrief with all section chiefs and federal " +
                "agency representatives. Topics: coordination effectiveness, communication gaps, " +
                "decision-making processes, and improvement priorities.",
                new TimeOnly(14, 45), "All Section Chiefs / Agency Reps", "Exercise Director",
                "Command", 3, WorldCupMselId, WcPhase3Id, created)
        };
    }

    /// <summary>Helper to create a Draft inject for planned exercises.</summary>
    private static Inject CreateDraftInject(
        Guid id, int number, string title, string description,
        TimeOnly scheduledTime, string target, string source,
        string track, int priority, Guid mselId, Guid phaseId, DateTime createdAt)
    {
        return new Inject
        {
            Id = id, InjectNumber = number,
            Title = title, Description = description,
            ScheduledTime = scheduledTime, ScenarioDay = 1,
            Target = target, Source = source,
            InjectType = InjectType.Standard,
            Status = InjectStatus.Draft, Sequence = number, Priority = priority,
            TriggerType = TriggerType.Manual, Track = track,
            MselId = mselId, PhaseId = phaseId,
            CreatedAt = createdAt, UpdatedAt = createdAt,
            CreatedBy = SystemConstants.SystemUserIdString,
            ModifiedBy = SystemConstants.SystemUserIdString
        };
    }

    // =========================================================================
    // WC Preliminary Draw Injects - ARCHIVED exercise, all Released
    // =========================================================================

    private static List<Inject> CreateWcPrelimInjects(DateTime now)
    {
        var exerciseDate = now.AddMonths(-4);
        var baseTime = exerciseDate.AddHours(7);

        return new List<Inject>
        {
            // === Phase 1: Arrival & Ceremony ===
            CreateFiredInject(WcPrelimInject1Id, 1, "VIP Credential Verification Bottleneck",
                "VIP check-in experiencing 30-minute delays due to credential verification system slowdown. " +
                "200+ dignitaries waiting in holding area. Protocol chief requesting expedited processing.",
                new TimeOnly(7, 0), TimeSpan.Zero, "VSOC / Registration", "Registration Desk Supervisor",
                DeliveryMethod.Radio, "Security", 1, WcPrelimMselId, WcPrelimPhase1Id,
                baseTime, exerciseDate.AddMonths(-2)),

            CreateFiredInject(Guid.NewGuid(), 2, "Motorcade Route Change",
                "USSS requests last-minute motorcade route change for principal VIP due to road construction " +
                "discovered on primary route. PD traffic unit needs 15 minutes to reposition.",
                new TimeOnly(7, 30), TimeSpan.FromMinutes(30), "PD Traffic / VSOC", "USSS Advance",
                DeliveryMethod.Radio, "Security", 1, WcPrelimMselId, WcPrelimPhase1Id,
                baseTime.AddMinutes(25), exerciseDate.AddMonths(-2)),

            CreateFiredInject(Guid.NewGuid(), 3, "Medical Standby - Dignitary",
                "Elderly FIFA official experiencing dizziness in holding area. Event medical team responding. " +
                "Dignitary has a known cardiac condition per advance briefing.",
                new TimeOnly(8, 0), TimeSpan.FromMinutes(60), "EMS Branch", "Event Medical Team",
                DeliveryMethod.Radio, "Medical", 2, WcPrelimMselId, WcPrelimPhase1Id,
                baseTime.AddMinutes(55), exerciseDate.AddMonths(-2)),

            CreateFiredInject(Guid.NewGuid(), 4, "Perimeter Breach - Forged Credentials",
                "Individual attempts unauthorized entry through service entrance using forged media credential. " +
                "Detained by perimeter security. Claims to be working for international news agency.",
                new TimeOnly(8, 30), TimeSpan.FromMinutes(90), "PD / USSS", "Perimeter Security",
                DeliveryMethod.Radio, "Law Enforcement", 1, WcPrelimMselId, WcPrelimPhase1Id,
                baseTime.AddMinutes(85), exerciseDate.AddMonths(-2)),

            CreateFiredInject(Guid.NewGuid(), 5, "Power Fluctuation in Main Hall",
                "Brief power interruption in ceremony hall. Backup generators activated within 8 seconds. " +
                "AV system requires manual restart. Ceremony delayed 5 minutes.",
                new TimeOnly(9, 0), TimeSpan.FromMinutes(120), "VSOC / Technical Director", "Venue Operations",
                DeliveryMethod.Radio, "Operations", 2, WcPrelimMselId, WcPrelimPhase1Id,
                baseTime.AddMinutes(115), exerciseDate.AddMonths(-2)),

            CreateFiredInject(Guid.NewGuid(), 6, "Protest Activity at Main Entrance",
                "150 protesters with signs assembling at main guest entrance. Media cameras present. " +
                "Some attempting to block entry. Protest permit is for adjacent park, not venue entrance.",
                new TimeOnly(9, 30), TimeSpan.FromMinutes(150), "PD Event Commander", "Community Liaison",
                DeliveryMethod.Radio, "Public Order", 2, WcPrelimMselId, WcPrelimPhase1Id,
                baseTime.AddMinutes(145), exerciseDate.AddMonths(-2)),

            CreateFiredInject(Guid.NewGuid(), 7, "Suspicious Package at Loading Dock",
                "Package left near loading dock doesn't match delivery manifest. K-9 team requested. " +
                "Loading dock serves kitchen and AV equipment areas adjacent to ceremony hall.",
                new TimeOnly(10, 0), TimeSpan.FromMinutes(180), "Bomb Squad / VSOC", "Loading Dock Security",
                DeliveryMethod.Radio, "Security", 1, WcPrelimMselId, WcPrelimPhase1Id,
                baseTime.AddMinutes(175), exerciseDate.AddMonths(-2)),

            // === Phase 2: Post-Event & Demobilization ===
            CreateFiredInject(Guid.NewGuid(), 8, "Post-Ceremony Crowd Surge at Exit",
                "Dangerous crowd congestion at main exit as 15,000 guests exit simultaneously. " +
                "Crowd management reports crush risk at east doors. Request to open emergency exits.",
                new TimeOnly(13, 0), TimeSpan.FromMinutes(360), "VSOC / PD", "Crowd Management",
                DeliveryMethod.Radio, "Crowd Management", 1, WcPrelimMselId, WcPrelimPhase2Id,
                baseTime.AddMinutes(355), exerciseDate.AddMonths(-2)),

            CreateFiredInject(Guid.NewGuid(), 9, "VIP Medical Emergency at Reception",
                "FIFA official collapses at VIP reception. Executive protection detail calls for EMS. " +
                "Nearest hospital is 8 minutes by ambulance. Media in adjacent room.",
                new TimeOnly(14, 0), TimeSpan.FromMinutes(420), "EMS / Hospital", "Executive Protection",
                DeliveryMethod.Radio, "Medical", 1, WcPrelimMselId, WcPrelimPhase2Id,
                baseTime.AddMinutes(415), exerciseDate.AddMonths(-2)),

            CreateFiredInject(Guid.NewGuid(), 10, "Lost VIP Credential Badges Found",
                "3 VIP credential badges found unattended in restroom facility. Badges belong to " +
                "international delegation members. Potential security implications.",
                new TimeOnly(15, 0), TimeSpan.FromMinutes(480), "Credentialing / USSS", "Venue Security",
                DeliveryMethod.Verbal, "Security", 2, WcPrelimMselId, WcPrelimPhase2Id,
                baseTime.AddMinutes(475), exerciseDate.AddMonths(-2)),

            CreateFiredInject(Guid.NewGuid(), 11, "After-Action Hot Wash",
                "Exercise Director conducts immediate debrief with all agency leads. Key discussion " +
                "topics: credentialing system performance, VIP security coordination, crowd management " +
                "effectiveness, and interagency communications.",
                new TimeOnly(16, 0), TimeSpan.FromMinutes(540), "All Section Chiefs", "Exercise Director",
                DeliveryMethod.Verbal, "Command", 3, WcPrelimMselId, WcPrelimPhase2Id,
                baseTime.AddMinutes(535), exerciseDate.AddMonths(-2)),

            CreateFiredInject(Guid.NewGuid(), 12, "Scene Release & Venue Return",
                "IC authorizes security perimeter release and return of venue to normal operations. " +
                "All units released from assignment. Credential system deactivated.",
                new TimeOnly(17, 0), TimeSpan.FromMinutes(600), "All Units / Venue Management", "Incident Commander",
                DeliveryMethod.Radio, "Command", 3, WcPrelimMselId, WcPrelimPhase2Id,
                baseTime.AddMinutes(595), exerciseDate.AddMonths(-2))
        };
    }

    #endregion

    #region Inject-Objective Links

    private static List<InjectObjective> CreateInjectObjectiveLinks()
    {
        return new List<InjectObjective>
        {
            // MCI TTX links
            new InjectObjective { InjectId = MciInject1Id, ObjectiveId = MciObj1Id },
            new InjectObjective { InjectId = MciInject2Id, ObjectiveId = MciObj1Id },
            new InjectObjective { InjectId = MciInject3Id, ObjectiveId = MciObj1Id },
            new InjectObjective { InjectId = MciInject4Id, ObjectiveId = MciObj5Id },
            new InjectObjective { InjectId = MciInject5Id, ObjectiveId = MciObj5Id },

            // Hazmat FE links
            new InjectObjective { InjectId = HazmatInject1Id, ObjectiveId = HazmatObj1Id },
            new InjectObjective { InjectId = HazmatInject2Id, ObjectiveId = HazmatObj1Id },
            new InjectObjective { InjectId = HazmatInject3Id, ObjectiveId = HazmatObj2Id },

            // World Cup TTX links
            new InjectObjective { InjectId = WcInject1Id, ObjectiveId = WcObj1Id },
            new InjectObjective { InjectId = WcInject2Id, ObjectiveId = WcObj2Id },

            // WC Preliminary Draw links
            new InjectObjective { InjectId = WcPrelimInject1Id, ObjectiveId = WcPrelimObj2Id }
        };
    }

    #endregion

    // =============================================================================
    // OBSERVATIONS & EEG ENTRIES
    // =============================================================================

    #region Seed Users, Observations & EEG Entries

    /// <summary>
    /// Seeds evaluator users, observations, and EEG entries for Active (MCI TTX) and
    /// Completed (Hazmat FE) exercises. Creates dummy evaluator users with org memberships
    /// and exercise participant records, then populates realistic observation and EEG data.
    /// Idempotent - each step checks for its own data.
    /// </summary>
    public static async Task SeedObservationsAndEegAsync(AppDbContext context, ILogger? logger = null)
    {
        var now = DateTime.UtcNow;

        // 1. Create seed evaluator users (needed for FK constraints)
        if (!await context.Users.AnyAsync(u => u.Id == BetaEvaluator1Id))
        {
            var users = CreateBetaSeedUsers(now);
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
            logger?.LogInformation("Created {Count} beta seed evaluator users", users.Count);
        }
        else
        {
            logger?.LogDebug("Beta seed evaluator users already exist - skipping");
        }

        // 2. Create org memberships for evaluators
        if (!await context.OrganizationMemberships.IgnoreQueryFilters().AnyAsync(m => m.Id == BetaMembership1Id))
        {
            var memberships = CreateBetaMemberships(now);
            context.OrganizationMemberships.AddRange(memberships);
            await context.SaveChangesAsync();
            logger?.LogInformation("Created {Count} beta org memberships", memberships.Count);
        }
        else
        {
            logger?.LogDebug("Beta org memberships already exist - skipping");
        }

        // 3. Create exercise participant records
        if (!await context.ExerciseParticipants.IgnoreQueryFilters().AnyAsync(p => p.Id == BetaParticipantEval1Mci))
        {
            var participants = CreateBetaParticipants(now);
            context.ExerciseParticipants.AddRange(participants);
            await context.SaveChangesAsync();
            logger?.LogInformation("Created {Count} beta exercise participants", participants.Count);
        }
        else
        {
            logger?.LogDebug("Beta exercise participants already exist - skipping");
        }

        // 4. Create observations
        if (!await context.Observations.IgnoreQueryFilters().AnyAsync(o => o.Id == MciObs1Id))
        {
            var observations = new List<Observation>();
            observations.AddRange(CreateMciObservations(now));
            observations.AddRange(CreateHazmatObservations(now));
            context.Observations.AddRange(observations);
            await context.SaveChangesAsync();
            logger?.LogInformation("Created {Count} beta observations", observations.Count);
        }
        else
        {
            logger?.LogDebug("Beta observations already exist - skipping");
        }

        // 5. Create EEG entries
        if (!await context.EegEntries.IgnoreQueryFilters().AnyAsync(e => e.Id == MciEeg1Id))
        {
            var entries = new List<EegEntry>();
            entries.AddRange(CreateMciEegEntries(now));
            entries.AddRange(CreateHazmatEegEntries(now));
            context.EegEntries.AddRange(entries);
            await context.SaveChangesAsync();
            logger?.LogInformation("Created {Count} beta EEG entries", entries.Count);
        }
        else
        {
            logger?.LogDebug("Beta EEG entries already exist - skipping");
        }

        // 6. Tag observations with core capabilities (requires capability lookup by name)
        var hasObsCapLinks = await context.ObservationCapabilities
            .AnyAsync(oc => oc.ObservationId == MciObs1Id);
        if (!hasObsCapLinks)
        {
            var capabilities = await context.Capabilities
                .Where(c => c.OrganizationId == BetaOrganizationId)
                .ToListAsync();

            if (capabilities.Count == 0)
            {
                logger?.LogWarning("No capabilities found for beta org - skipping observation capability tagging");
            }
            else
            {
                var capLookup = capabilities.ToDictionary(c => c.Name, c => c.Id);
                var tags = CreateObservationCapabilityTags(capLookup, logger);
                if (tags.Count > 0)
                {
                    context.ObservationCapabilities.AddRange(tags);
                    await context.SaveChangesAsync();
                    logger?.LogInformation("Created {Count} observation-capability tags", tags.Count);
                }
            }
        }
        else
        {
            logger?.LogDebug("Beta observation-capability tags already exist - skipping");
        }
    }

    // =========================================================================
    // Observation-Capability Tags
    // =========================================================================

    private static List<ObservationCapability> CreateObservationCapabilityTags(
        Dictionary<string, Guid> capLookup, ILogger? logger)
    {
        Guid? GetCapId(string name)
        {
            if (capLookup.TryGetValue(name, out var id)) return id;
            logger?.LogDebug("Capability '{Name}' not found - skipping tag", name);
            return null;
        }

        var ems = GetCapId("Public Health, Healthcare, and Emergency Medical Services");
        var opComm = GetCapId("Operational Communications");
        var massCare = GetCapId("Mass Care Services");
        var security = GetCapId("On-scene Security, Protection, and Law Enforcement");
        var sitAssess = GetCapId("Situational Assessment");
        var envResp = GetCapId("Environmental Response/Health and Safety");

        var tags = new List<ObservationCapability>();

        void Tag(Guid obsId, Guid? capId)
        {
            if (capId.HasValue)
                tags.Add(new ObservationCapability { ObservationId = obsId, CapabilityId = capId.Value });
        }

        // --- MCI TTX Observations ---
        // Obs1: 911 dispatch & MCI protocol activation
        Tag(MciObs1Id, opComm);
        Tag(MciObs1Id, ems);
        // Obs2: Scene size-up and START triage
        Tag(MciObs2Id, ems);
        // Obs3: EOC activation delays
        Tag(MciObs3Id, sitAssess);
        Tag(MciObs3Id, opComm);
        // Obs4: Perimeter establishment
        Tag(MciObs4Id, security);
        // Obs5: Radio interoperability failure
        Tag(MciObs5Id, opComm);
        // Obs6 & Obs7: Draft observations — no tags
        // Obs7 (draft but covers hospital surge): Mass Care
        Tag(MciObs7Id, massCare);
        // Obs8: Traffic management
        Tag(MciObs8Id, security);

        // --- Hazmat FE Observations ---
        // Obs1: Chemical ID and CHEMTREC
        Tag(HazObs1Id, envResp);
        // Obs2: Shelter-in-place notification
        Tag(HazObs2Id, security);
        // Obs3: Zone establishment
        Tag(HazObs3Id, envResp);
        // Obs4: HAZMAT talk group
        Tag(HazObs4Id, opComm);
        // Obs5: Air monitoring gaps
        Tag(HazObs5Id, envResp);
        // Obs6: Evacuation route modification
        Tag(HazObs6Id, security);
        // Obs7: Mass decon corridor
        Tag(HazObs7Id, ems);
        Tag(HazObs7Id, envResp);
        // Obs8: Backup communications
        Tag(HazObs8Id, opComm);
        // Obs9: Exposure tracking
        Tag(HazObs9Id, ems);
        // Obs10: CHEMTREC coordination
        Tag(HazObs10Id, envResp);

        return tags;
    }

    // =========================================================================
    // Seed Users
    // =========================================================================

    private static List<ApplicationUser> CreateBetaSeedUsers(DateTime now)
    {
        return new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = BetaEvaluator1Id,
                UserName = "schen@coastal-esa.gov",
                NormalizedUserName = "SCHEN@COASTAL-ESA.GOV",
                Email = "schen@coastal-esa.gov",
                NormalizedEmail = "SCHEN@COASTAL-ESA.GOV",
                EmailConfirmed = true,
                DisplayName = "Sarah Chen",
                SystemRole = SystemRole.User,
                Status = UserStatus.Active,
                OrganizationId = BetaOrganizationId,
                CurrentOrganizationId = BetaOrganizationId,
                SecurityStamp = Guid.NewGuid().ToString(),
                CreatedAt = now.AddDays(-90)
            },
            new ApplicationUser
            {
                Id = BetaEvaluator2Id,
                UserName = "jrodriguez@coastal-esa.gov",
                NormalizedUserName = "JRODRIGUEZ@COASTAL-ESA.GOV",
                Email = "jrodriguez@coastal-esa.gov",
                NormalizedEmail = "JRODRIGUEZ@COASTAL-ESA.GOV",
                EmailConfirmed = true,
                DisplayName = "James Rodriguez",
                SystemRole = SystemRole.User,
                Status = UserStatus.Active,
                OrganizationId = BetaOrganizationId,
                CurrentOrganizationId = BetaOrganizationId,
                SecurityStamp = Guid.NewGuid().ToString(),
                CreatedAt = now.AddDays(-90)
            }
        };
    }

    private static List<OrganizationMembership> CreateBetaMemberships(DateTime now)
    {
        var system = SystemConstants.SystemUserIdString;
        return new List<OrganizationMembership>
        {
            new OrganizationMembership
            {
                Id = BetaMembership1Id,
                UserId = BetaEvaluator1Id,
                OrganizationId = BetaOrganizationId,
                Role = OrgRole.OrgUser,
                Status = MembershipStatus.Active,
                JoinedAt = now.AddDays(-90),
                CreatedAt = now.AddDays(-90),
                UpdatedAt = now.AddDays(-90),
                CreatedBy = system,
                ModifiedBy = system
            },
            new OrganizationMembership
            {
                Id = BetaMembership2Id,
                UserId = BetaEvaluator2Id,
                OrganizationId = BetaOrganizationId,
                Role = OrgRole.OrgUser,
                Status = MembershipStatus.Active,
                JoinedAt = now.AddDays(-90),
                CreatedAt = now.AddDays(-90),
                UpdatedAt = now.AddDays(-90),
                CreatedBy = system,
                ModifiedBy = system
            }
        };
    }

    private static List<ExerciseParticipant> CreateBetaParticipants(DateTime now)
    {
        var system = SystemConstants.SystemUserIdString;
        return new List<ExerciseParticipant>
        {
            // MCI TTX evaluators
            new ExerciseParticipant
            {
                Id = BetaParticipantEval1Mci,
                ExerciseId = MciTtxId,
                UserId = BetaEvaluator1Id,
                Role = ExerciseRole.Evaluator,
                AssignedAt = now.AddDays(-14),
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-14),
                CreatedBy = system,
                ModifiedBy = system
            },
            new ExerciseParticipant
            {
                Id = BetaParticipantEval2Mci,
                ExerciseId = MciTtxId,
                UserId = BetaEvaluator2Id,
                Role = ExerciseRole.Evaluator,
                AssignedAt = now.AddDays(-14),
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-14),
                CreatedBy = system,
                ModifiedBy = system
            },
            // Hazmat FE evaluators
            new ExerciseParticipant
            {
                Id = BetaParticipantEval1Haz,
                ExerciseId = HazmatFeId,
                UserId = BetaEvaluator1Id,
                Role = ExerciseRole.Evaluator,
                AssignedAt = now.AddDays(-60),
                CreatedAt = now.AddDays(-60),
                UpdatedAt = now.AddDays(-60),
                CreatedBy = system,
                ModifiedBy = system
            },
            new ExerciseParticipant
            {
                Id = BetaParticipantEval2Haz,
                ExerciseId = HazmatFeId,
                UserId = BetaEvaluator2Id,
                Role = ExerciseRole.Evaluator,
                AssignedAt = now.AddDays(-60),
                CreatedAt = now.AddDays(-60),
                UpdatedAt = now.AddDays(-60),
                CreatedBy = system,
                ModifiedBy = system
            }
        };
    }

    // =========================================================================
    // Observations - MCI TTX (Active, mid-conduct) — 8 observations
    // Mix of Draft/Complete, various ratings, some inject-linked
    // =========================================================================

    private static List<Observation> CreateMciObservations(DateTime now)
    {
        var eval1 = BetaEvaluator1Id;
        var eval2 = BetaEvaluator2Id;

        return new List<Observation>
        {
            new Observation
            {
                Id = MciObs1Id,
                ExerciseId = MciTtxId,
                OrganizationId = BetaOrganizationId,
                InjectId = MciInject1Id,
                ObjectiveId = MciObj1Id,
                Status = ObservationStatus.Complete,
                Rating = ObservationRating.Performed,
                Content = "911 dispatch received initial calls and activated MCI protocol within 2 minutes. " +
                          "All required notifications (Fire, EMS, PD, Hospital) completed per SOP. " +
                          "Dispatcher correctly identified charter bus involvement and requested " +
                          "additional ambulance resources before first units arrived on scene.",
                Recommendation = null,
                ObservedAt = now.AddMinutes(-43),
                Location = "911 Communications Center",
                CreatedByUserId = eval1,
                CreatedAt = now.AddMinutes(-40), UpdatedAt = now.AddMinutes(-40),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new Observation
            {
                Id = MciObs2Id,
                ExerciseId = MciTtxId,
                OrganizationId = BetaOrganizationId,
                InjectId = MciInject2Id,
                ObjectiveId = MciObj1Id,
                Status = ObservationStatus.Complete,
                Rating = ObservationRating.Satisfactory,
                Content = "First-arriving Battalion Chief conducted scene size-up and established Incident " +
                          "Command within 5 minutes. START triage initiated promptly. However, designation " +
                          "of Triage Officer was delayed approximately 4 minutes while BC coordinated with " +
                          "arriving engine companies. All patients ultimately tagged within 18 minutes.",
                Recommendation = "Consider pre-designating triage officer role in dispatch assignment cards " +
                                 "to eliminate on-scene negotiation delay.",
                ObservedAt = now.AddMinutes(-36),
                Location = "Highway 17, Mile Marker 42",
                CreatedByUserId = eval1,
                CreatedAt = now.AddMinutes(-33), UpdatedAt = now.AddMinutes(-33),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new Observation
            {
                Id = MciObs3Id,
                ExerciseId = MciTtxId,
                OrganizationId = BetaOrganizationId,
                InjectId = MciInject3Id,
                ObjectiveId = MciObj3Id,
                Status = ObservationStatus.Complete,
                Rating = ObservationRating.Marginal,
                Content = "EOC activation was delayed 11 minutes after MCI declaration. Emergency Management " +
                          "Director reported the automated notification system sent alerts to only 4 of 7 " +
                          "required staff members. Two department heads were reached by personal cell phone " +
                          "by the Director. WebEOC board was not updated until 22 minutes after declaration.",
                Recommendation = "Test and validate the automated EOC notification system monthly. Establish " +
                                 "backup notification procedure using group text message as secondary channel.",
                ObservedAt = now.AddMinutes(-28),
                Location = "County EOC",
                CreatedByUserId = eval2,
                CreatedAt = now.AddMinutes(-25), UpdatedAt = now.AddMinutes(-25),
                CreatedBy = eval2, ModifiedBy = eval2
            },
            new Observation
            {
                Id = MciObs4Id,
                ExerciseId = MciTtxId,
                OrganizationId = BetaOrganizationId,
                InjectId = MciInject2Id,
                ObjectiveId = MciObj4Id,
                Status = ObservationStatus.Complete,
                Rating = ObservationRating.Performed,
                Content = "Law enforcement established inner and outer perimeters within 8 minutes of " +
                          "first PD arrival. Traffic diversion points set at Highway 17/Route 9 and " +
                          "Highway 17/County Road 12. Emergency vehicle corridor maintained throughout. " +
                          "No unauthorized entry into the scene observed.",
                Recommendation = null,
                ObservedAt = now.AddMinutes(-35),
                Location = "Highway 17 Perimeter",
                CreatedByUserId = eval1,
                CreatedAt = now.AddMinutes(-32), UpdatedAt = now.AddMinutes(-32),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new Observation
            {
                Id = MciObs5Id,
                ExerciseId = MciTtxId,
                OrganizationId = BetaOrganizationId,
                InjectId = null,
                ObjectiveId = MciObj2Id,
                Status = ObservationStatus.Complete,
                Rating = ObservationRating.Unsatisfactory,
                Content = "Significant interoperability failure: Fire and EMS units operated on separate " +
                          "talk groups for 12 minutes during the critical initial response period. Mutual " +
                          "aid units from Riverside FD were unable to contact County EMS Dispatch on the " +
                          "assigned tactical channel. IC was unaware of the communication gap until a " +
                          "runner physically relayed a patient count discrepancy.",
                Recommendation = "Conduct quarterly interoperability radio drills between Fire, EMS, and " +
                                 "mutual aid agencies. Program shared tactical channels into all portable " +
                                 "radios. Include communication check as mandatory step in MCI checklist.",
                ObservedAt = now.AddMinutes(-30),
                Location = "Radio Communications",
                CreatedByUserId = eval2,
                CreatedAt = now.AddMinutes(-27), UpdatedAt = now.AddMinutes(-27),
                CreatedBy = eval2, ModifiedBy = eval2
            },
            new Observation
            {
                Id = MciObs6Id,
                ExerciseId = MciTtxId,
                OrganizationId = BetaOrganizationId,
                InjectId = MciInject4Id,
                ObjectiveId = null,
                Status = ObservationStatus.Draft,
                Rating = null,
                Content = "PIO arrived at media staging area. Initial statement being prepared.",
                Recommendation = null,
                ObservedAt = now.AddMinutes(-20),
                Location = "Media Staging Area",
                CreatedByUserId = eval1,
                CreatedAt = now.AddMinutes(-18), UpdatedAt = now.AddMinutes(-18),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new Observation
            {
                Id = MciObs7Id,
                ExerciseId = MciTtxId,
                OrganizationId = BetaOrganizationId,
                InjectId = MciInject5Id,
                ObjectiveId = null,
                Status = ObservationStatus.Draft,
                Rating = null,
                Content = "Hospital surge protocol activated at Regional Medical Center and Community General.",
                Recommendation = null,
                ObservedAt = now.AddMinutes(-13),
                Location = "Regional Medical Center",
                CreatedByUserId = eval2,
                CreatedAt = now.AddMinutes(-10), UpdatedAt = now.AddMinutes(-10),
                CreatedBy = eval2, ModifiedBy = eval2
            },
            new Observation
            {
                Id = MciObs8Id,
                ExerciseId = MciTtxId,
                OrganizationId = BetaOrganizationId,
                InjectId = null,
                ObjectiveId = MciObj1Id,
                Status = ObservationStatus.Complete,
                Rating = ObservationRating.Satisfactory,
                Content = "Traffic management plan activated per county emergency traffic plan. DOT message " +
                          "boards updated within 15 minutes. However, social media alerts were not posted " +
                          "until 25 minutes after highway closure, resulting in continued traffic approaching " +
                          "the scene from the eastbound direction.",
                Recommendation = "Include social media notification as a parallel step in the traffic " +
                                 "management plan activation checklist, not a sequential step.",
                ObservedAt = now.AddMinutes(-32),
                Location = "Highway 17 Traffic Control",
                CreatedByUserId = eval1,
                CreatedAt = now.AddMinutes(-29), UpdatedAt = now.AddMinutes(-29),
                CreatedBy = eval1, ModifiedBy = eval1
            }
        };
    }

    // =========================================================================
    // Observations - Hazmat FE (Completed, AAR-ready) — 10 observations
    // All Complete with ratings and recommendations
    // =========================================================================

    private static List<Observation> CreateHazmatObservations(DateTime now)
    {
        var eval1 = BetaEvaluator1Id;
        var eval2 = BetaEvaluator2Id;
        var baseDate = now.AddDays(-30); // Exercise ran 30 days ago

        return new List<Observation>
        {
            new Observation
            {
                Id = HazObs1Id,
                ExerciseId = HazmatFeId,
                OrganizationId = BetaOrganizationId,
                InjectId = HazmatInject1Id,
                ObjectiveId = HazmatObj1Id,
                Status = ObservationStatus.Complete,
                Rating = ObservationRating.Performed,
                Content = "HAZMAT team deployed 4-gas monitor and photoionization detector within 6 minutes " +
                          "of arrival. Chemical positively identified as chlorine via colorimetric tubes. " +
                          "CHEMTREC contacted at 0918 and confirmed product identification. SDS obtained " +
                          "from facility within 12 minutes of request.",
                Recommendation = "Excellent performance. Consider sharing this response timeline as a " +
                                 "benchmark for future HAZMAT training scenarios.",
                ObservedAt = baseDate.AddHours(9).AddMinutes(15),
                Location = "Bayshore Chemical Main Gate",
                CreatedByUserId = eval1,
                CreatedAt = baseDate.AddHours(9).AddMinutes(25), UpdatedAt = baseDate.AddHours(9).AddMinutes(25),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new Observation
            {
                Id = HazObs2Id,
                ExerciseId = HazmatFeId,
                OrganizationId = BetaOrganizationId,
                InjectId = HazmatInject1Id,
                ObjectiveId = HazmatObj2Id,
                Status = ObservationStatus.Complete,
                Rating = ObservationRating.Satisfactory,
                Content = "Shelter-in-place notification sent via reverse 911 to affected zone within 14 " +
                          "minutes. Social media alert posted 3 minutes later. However, notification text " +
                          "used generic language ('chemical incident') rather than specific protective actions " +
                          "('close windows, turn off HVAC systems'). School district notification was timely.",
                Recommendation = "Update reverse 911 message templates to include specific protective actions " +
                                 "for chemical releases. Pre-stage shelter-in-place templates with fill-in-the-blank " +
                                 "fields for chemical name and affected zones.",
                ObservedAt = baseDate.AddHours(9).AddMinutes(30),
                Location = "EOC Communications",
                CreatedByUserId = eval2,
                CreatedAt = baseDate.AddHours(9).AddMinutes(40), UpdatedAt = baseDate.AddHours(9).AddMinutes(40),
                CreatedBy = eval2, ModifiedBy = eval2
            },
            new Observation
            {
                Id = HazObs3Id,
                ExerciseId = HazmatFeId,
                OrganizationId = BetaOrganizationId,
                InjectId = HazmatInject2Id,
                ObjectiveId = HazmatObj1Id,
                Status = ObservationStatus.Complete,
                Rating = ObservationRating.Marginal,
                Content = "Hot/warm/cold zone establishment was delayed. Initial hot zone was set at 150 feet " +
                          "based on ERG but wind conditions required 300+ feet. Warm zone boundary was not " +
                          "clearly marked with barrier tape until 25 minutes after zone declaration. Two " +
                          "non-HAZMAT firefighters briefly entered the warm zone without proper PPE before " +
                          "being redirected by the Safety Officer.",
                Recommendation = "Conduct zone establishment drills with emphasis on wind-adjusted distances. " +
                                 "Pre-stage zone marking kits on HAZMAT apparatus. Add mandatory PPE checkpoint " +
                                 "at warm zone entry point to all HAZMAT SOPs.",
                ObservedAt = baseDate.AddHours(10).AddMinutes(0),
                Location = "Hot Zone Boundary",
                CreatedByUserId = eval1,
                CreatedAt = baseDate.AddHours(10).AddMinutes(15), UpdatedAt = baseDate.AddHours(10).AddMinutes(15),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new Observation
            {
                Id = HazObs4Id,
                ExerciseId = HazmatFeId,
                OrganizationId = BetaOrganizationId,
                InjectId = HazmatInject2Id,
                ObjectiveId = HazmatObj3Id,
                Status = ObservationStatus.Complete,
                Rating = ObservationRating.Performed,
                Content = "HAZMAT talk group established within 8 minutes. All responding units confirmed on " +
                          "frequency. Command channel separated from tactical channel per SOP. Mutual aid " +
                          "units from Metro Fire patched in via regional interoperability channel. No " +
                          "communication gaps observed during the assessment phase.",
                Recommendation = "Strong performance. Document the channel plan used as a template for " +
                                 "future HAZMAT responses involving mutual aid.",
                ObservedAt = baseDate.AddHours(10).AddMinutes(10),
                Location = "Incident Command Post",
                CreatedByUserId = eval2,
                CreatedAt = baseDate.AddHours(10).AddMinutes(20), UpdatedAt = baseDate.AddHours(10).AddMinutes(20),
                CreatedBy = eval2, ModifiedBy = eval2
            },
            new Observation
            {
                Id = HazObs5Id,
                ExerciseId = HazmatFeId,
                OrganizationId = BetaOrganizationId,
                InjectId = HazmatInject2Id,
                ObjectiveId = HazmatObj1Id,
                Status = ObservationStatus.Complete,
                Rating = ObservationRating.Unsatisfactory,
                Content = "Air monitoring network had significant coverage gaps. Only 3 of 6 planned downwind " +
                          "monitoring points were established during the first hour. Wind shift at 1045 was " +
                          "not detected by field monitors for approximately 18 minutes. Residential area in " +
                          "the new downwind path was not alerted until 30 minutes after wind direction changed. " +
                          "Contributing factor: one air monitor had depleted batteries.",
                Recommendation = "Implement mandatory battery check protocol for all monitoring equipment at " +
                                 "start of each shift. Establish minimum staffing for 6-point monitoring network " +
                                 "in HAZMAT response SOP. Consider procurement of fixed continuous air monitors " +
                                 "for facility perimeter.",
                ObservedAt = baseDate.AddHours(11).AddMinutes(15),
                Location = "Downwind Monitoring Station 3",
                CreatedByUserId = eval1,
                CreatedAt = baseDate.AddHours(11).AddMinutes(30), UpdatedAt = baseDate.AddHours(11).AddMinutes(30),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new Observation
            {
                Id = HazObs6Id,
                ExerciseId = HazmatFeId,
                OrganizationId = BetaOrganizationId,
                InjectId = HazmatInject3Id,
                ObjectiveId = HazmatObj2Id,
                Status = ObservationStatus.Complete,
                Rating = ObservationRating.Satisfactory,
                Content = "Evacuation routes were modified within 10 minutes of wind shift notification. " +
                          "School bus fleet was repositioned to new staging area. Evacuees were routed to " +
                          "alternate shelter at Community Center. However, two evacuation route signs at " +
                          "Oak Street and 4th Avenue were not updated, causing brief confusion for 3 vehicles.",
                Recommendation = "Add a dedicated route signage team to the evacuation SOP. Use digital " +
                                 "variable message signs at key intersections when available.",
                ObservedAt = baseDate.AddHours(11).AddMinutes(45),
                Location = "Evacuation Route Alpha",
                CreatedByUserId = eval2,
                CreatedAt = baseDate.AddHours(12).AddMinutes(0), UpdatedAt = baseDate.AddHours(12).AddMinutes(0),
                CreatedBy = eval2, ModifiedBy = eval2
            },
            new Observation
            {
                Id = HazObs7Id,
                ExerciseId = HazmatFeId,
                OrganizationId = BetaOrganizationId,
                InjectId = HazmatInject3Id,
                ObjectiveId = HazmatObj4Id,
                Status = ObservationStatus.Complete,
                Rating = ObservationRating.Performed,
                Content = "Mass decontamination corridor established in 38 minutes using two-stage " +
                          "gross/technical decon setup. All decon team members in proper Level B PPE. " +
                          "Water supply established from hydrant at Station 4. Privacy screens deployed " +
                          "for technical decon. Patient throughput approximately 15 per hour, within " +
                          "expected parameters for this scenario.",
                Recommendation = "Excellent execution. Recommend using this exercise as the basis for " +
                                 "annual decon proficiency evaluation.",
                ObservedAt = baseDate.AddHours(12).AddMinutes(30),
                Location = "Decontamination Corridor",
                CreatedByUserId = eval1,
                CreatedAt = baseDate.AddHours(12).AddMinutes(45), UpdatedAt = baseDate.AddHours(12).AddMinutes(45),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new Observation
            {
                Id = HazObs8Id,
                ExerciseId = HazmatFeId,
                OrganizationId = BetaOrganizationId,
                InjectId = HazmatInject3Id,
                ObjectiveId = HazmatObj3Id,
                Status = ObservationStatus.Complete,
                Rating = ObservationRating.Marginal,
                Content = "Backup communications were activated when primary repeater experienced interference. " +
                          "However, satellite phone designated as secondary backup had a depleted battery and " +
                          "was not operational. Cellular phone was used as tertiary backup successfully. " +
                          "Field units reported intermittent coverage gaps during the 12-minute primary " +
                          "repeater outage.",
                Recommendation = "Add satellite phone battery check to daily apparatus checkout. Establish " +
                                 "a communications equipment maintenance log with weekly verification. " +
                                 "Consider adding a portable repeater to the HAZMAT cache.",
                ObservedAt = baseDate.AddHours(13).AddMinutes(0),
                Location = "Backup Communications Station",
                CreatedByUserId = eval2,
                CreatedAt = baseDate.AddHours(13).AddMinutes(15), UpdatedAt = baseDate.AddHours(13).AddMinutes(15),
                CreatedBy = eval2, ModifiedBy = eval2
            },
            new Observation
            {
                Id = HazObs9Id,
                ExerciseId = HazmatFeId,
                OrganizationId = BetaOrganizationId,
                InjectId = HazmatInject3Id,
                ObjectiveId = HazmatObj4Id,
                Status = ObservationStatus.Complete,
                Rating = ObservationRating.Satisfactory,
                Content = "Exposure tracking and patient registration system was functional throughout. " +
                          "All patients entering decon were logged with name, exposure duration, and symptoms. " +
                          "Hospital receiving data was transmitted electronically. Minor delay noted in " +
                          "registration of non-ambulatory patients who required assistance from decon team " +
                          "members, creating a brief bottleneck at the registration point.",
                Recommendation = "Designate a dedicated registration assistant for non-ambulatory patients. " +
                                 "Consider tablet-based registration to replace paper forms in wet environment.",
                ObservedAt = baseDate.AddHours(13).AddMinutes(30),
                Location = "Medical Staging Area",
                CreatedByUserId = eval1,
                CreatedAt = baseDate.AddHours(13).AddMinutes(45), UpdatedAt = baseDate.AddHours(13).AddMinutes(45),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new Observation
            {
                Id = HazObs10Id,
                ExerciseId = HazmatFeId,
                OrganizationId = BetaOrganizationId,
                InjectId = HazmatInject1Id,
                ObjectiveId = HazmatObj1Id,
                Status = ObservationStatus.Complete,
                Rating = ObservationRating.Performed,
                Content = "Overall CHEMTREC coordination and information flow was excellent. Initial call " +
                          "placed within 10 minutes of product identification. CHEMTREC provided detailed " +
                          "response guidance including recommended isolation distances and PPE requirements. " +
                          "Facility emergency coordinator was on-scene and cooperative throughout, providing " +
                          "SDS and process information promptly.",
                Recommendation = "Document the CHEMTREC coordination timeline as a best practice example. " +
                                 "Consider adding facility emergency coordinator contact information to " +
                                 "pre-incident plan database.",
                ObservedAt = baseDate.AddHours(14).AddMinutes(0),
                Location = "HAZMAT Staging Area",
                CreatedByUserId = eval2,
                CreatedAt = baseDate.AddHours(14).AddMinutes(15), UpdatedAt = baseDate.AddHours(14).AddMinutes(15),
                CreatedBy = eval2, ModifiedBy = eval2
            }
        };
    }

    // =========================================================================
    // EEG Entries - MCI TTX (Active, partial progress) — 7 of 11 tasks
    // Shows mid-conduct evaluation with some tasks not yet assessed
    // =========================================================================

    private static List<EegEntry> CreateMciEegEntries(DateTime now)
    {
        var eval1 = BetaEvaluator1Id;
        var eval2 = BetaEvaluator2Id;

        return new List<EegEntry>
        {
            new EegEntry
            {
                Id = MciEeg1Id,
                CriticalTaskId = MciTask_EMS_1,
                OrganizationId = BetaOrganizationId,
                EvaluatorId = eval1,
                TriggeringInjectId = MciInject2Id,
                Rating = PerformanceRating.Performed,
                ObservationText = "START triage was implemented within 10 minutes of first-arriving unit. " +
                                  "All 47 patients were assessed and tagged with appropriate color codes. " +
                                  "Triage officer maintained accurate patient count board. No re-triage " +
                                  "discrepancies identified during secondary assessment.",
                ObservedAt = now.AddMinutes(-35),
                RecordedAt = now.AddMinutes(-32),
                CreatedAt = now.AddMinutes(-32), UpdatedAt = now.AddMinutes(-32),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new EegEntry
            {
                Id = MciEeg2Id,
                CriticalTaskId = MciTask_EMS_2,
                OrganizationId = BetaOrganizationId,
                EvaluatorId = eval1,
                TriggeringInjectId = MciInject4Id,
                Rating = PerformanceRating.SomeChallenges,
                ObservationText = "Patient distribution to hospitals was initiated per MCI plan. However, " +
                                  "hospital capacity data from Regional Medical Center was delayed 8 minutes " +
                                  "due to phone system issues. This resulted in 5 patients being transported " +
                                  "to Regional before surge capacity was confirmed, temporarily overloading " +
                                  "their trauma bay. Community General received appropriate patient load.",
                ObservedAt = now.AddMinutes(-20),
                RecordedAt = now.AddMinutes(-17),
                CreatedAt = now.AddMinutes(-17), UpdatedAt = now.AddMinutes(-17),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new EegEntry
            {
                Id = MciEeg3Id,
                CriticalTaskId = MciTask_OpComm_1,
                OrganizationId = BetaOrganizationId,
                EvaluatorId = eval2,
                TriggeringInjectId = MciInject1Id,
                Rating = PerformanceRating.Performed,
                ObservationText = "Mutual aid talk groups were activated within 12 minutes of MCI declaration. " +
                                  "All responding agencies confirmed on the assigned interoperability channel. " +
                                  "Dispatch maintained clear radio discipline throughout. Channel patching " +
                                  "between county and municipal systems completed without interruption.",
                ObservedAt = now.AddMinutes(-40),
                RecordedAt = now.AddMinutes(-37),
                CreatedAt = now.AddMinutes(-37), UpdatedAt = now.AddMinutes(-37),
                CreatedBy = eval2, ModifiedBy = eval2
            },
            new EegEntry
            {
                Id = MciEeg4Id,
                CriticalTaskId = MciTask_OpComm_2,
                OrganizationId = BetaOrganizationId,
                EvaluatorId = eval2,
                TriggeringInjectId = MciInject3Id,
                Rating = PerformanceRating.MajorChallenges,
                ObservationText = "Field-to-EOC communication link took 25 minutes to establish after EOC " +
                                  "activation. The designated liaison frequency was not programmed in 2 of " +
                                  "4 field command radios. Backup frequency assignment was not communicated " +
                                  "to field units. IC relied on cell phone for EOC updates during the gap. " +
                                  "Unified Command briefing schedule was delayed as a result.",
                ObservedAt = now.AddMinutes(-26),
                RecordedAt = now.AddMinutes(-23),
                CreatedAt = now.AddMinutes(-23), UpdatedAt = now.AddMinutes(-23),
                CreatedBy = eval2, ModifiedBy = eval2
            },
            new EegEntry
            {
                Id = MciEeg5Id,
                CriticalTaskId = MciTask_Security_1,
                OrganizationId = BetaOrganizationId,
                EvaluatorId = eval1,
                TriggeringInjectId = MciInject2Id,
                Rating = PerformanceRating.Performed,
                ObservationText = "Inner and outer perimeters established within 8 minutes of first PD " +
                                  "arrival. Traffic diversion set at two intersections per the emergency " +
                                  "traffic plan. Scene access control maintained throughout with credential " +
                                  "checking at the outer perimeter. Emergency vehicle corridor remained clear.",
                ObservedAt = now.AddMinutes(-34),
                RecordedAt = now.AddMinutes(-31),
                CreatedAt = now.AddMinutes(-31), UpdatedAt = now.AddMinutes(-31),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new EegEntry
            {
                Id = MciEeg6Id,
                CriticalTaskId = MciTask_Security_2,
                OrganizationId = BetaOrganizationId,
                EvaluatorId = eval1,
                TriggeringInjectId = null,
                Rating = PerformanceRating.SomeChallenges,
                ObservationText = "Emergency traffic corridors were established and DOT notified. However, " +
                                  "electronic message boards on Highway 17 westbound were not updated for " +
                                  "22 minutes due to a coordination delay between PD dispatch and DOT " +
                                  "operations center. Eastbound boards updated within 10 minutes. " +
                                  "Manual traffic control by officers was effective throughout.",
                ObservedAt = now.AddMinutes(-28),
                RecordedAt = now.AddMinutes(-25),
                CreatedAt = now.AddMinutes(-25), UpdatedAt = now.AddMinutes(-25),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new EegEntry
            {
                Id = MciEeg7Id,
                CriticalTaskId = MciTask_SitAssess_1,
                OrganizationId = BetaOrganizationId,
                EvaluatorId = eval2,
                TriggeringInjectId = MciInject3Id,
                Rating = PerformanceRating.Performed,
                ObservationText = "Situation Unit activated promptly after EOC staffing. Field situation " +
                                  "reports were received every 15 minutes per SOP. Situation status board " +
                                  "maintained with current casualty figures, resource status, and hospital " +
                                  "capacity. Initial situation report to County Executive completed within " +
                                  "20 minutes of EOC activation.",
                ObservedAt = now.AddMinutes(-22),
                RecordedAt = now.AddMinutes(-19),
                CreatedAt = now.AddMinutes(-19), UpdatedAt = now.AddMinutes(-19),
                CreatedBy = eval2, ModifiedBy = eval2
            }
        };
    }

    // =========================================================================
    // EEG Entries - Hazmat FE (Completed, full coverage) — all 9 tasks
    // Complete evaluation with all critical tasks assessed
    // =========================================================================

    private static List<EegEntry> CreateHazmatEegEntries(DateTime now)
    {
        var eval1 = BetaEvaluator1Id;
        var eval2 = BetaEvaluator2Id;
        var baseDate = now.AddDays(-30);

        return new List<EegEntry>
        {
            new EegEntry
            {
                Id = HazEeg1Id,
                CriticalTaskId = HazTask_EnvResp_1,
                OrganizationId = BetaOrganizationId,
                EvaluatorId = eval1,
                TriggeringInjectId = HazmatInject1Id,
                Rating = PerformanceRating.Performed,
                ObservationText = "Hazardous material positively identified as chlorine gas within 18 minutes " +
                                  "of HAZMAT team arrival using 4-gas meter and colorimetric detection tubes. " +
                                  "CHEMTREC contacted immediately; product verification confirmed. Facility " +
                                  "SDS obtained and cross-referenced. Response guidance disseminated to all " +
                                  "units within 25 minutes of identification.",
                ObservedAt = baseDate.AddHours(9).AddMinutes(20),
                RecordedAt = baseDate.AddHours(9).AddMinutes(28),
                CreatedAt = baseDate.AddHours(9).AddMinutes(28), UpdatedAt = baseDate.AddHours(9).AddMinutes(28),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new EegEntry
            {
                Id = HazEeg2Id,
                CriticalTaskId = HazTask_EnvResp_2,
                OrganizationId = BetaOrganizationId,
                EvaluatorId = eval1,
                TriggeringInjectId = HazmatInject2Id,
                Rating = PerformanceRating.SomeChallenges,
                ObservationText = "Hot, warm, and cold zones established per ERG guidelines. However, initial " +
                                  "warm zone was set too narrow at 150 feet given wind speed of 12 mph. Safety " +
                                  "Officer identified the issue and expanded to 300 feet after 10 minutes. " +
                                  "Zone boundary marking with tape and cones completed after expansion. Two " +
                                  "brief unauthorized entries into warm zone corrected by Safety Officer.",
                ObservedAt = baseDate.AddHours(10).AddMinutes(5),
                RecordedAt = baseDate.AddHours(10).AddMinutes(12),
                CreatedAt = baseDate.AddHours(10).AddMinutes(12), UpdatedAt = baseDate.AddHours(10).AddMinutes(12),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new EegEntry
            {
                Id = HazEeg3Id,
                CriticalTaskId = HazTask_EnvResp_3,
                OrganizationId = BetaOrganizationId,
                EvaluatorId = eval1,
                TriggeringInjectId = HazmatInject2Id,
                Rating = PerformanceRating.MajorChallenges,
                ObservationText = "Air monitoring network established at only 3 of 6 planned downwind positions " +
                                  "during the first hour. One monitor had depleted batteries and was non-functional. " +
                                  "Staff shortage prevented covering the two furthest monitoring points. Wind " +
                                  "direction shift at 1045 was not detected by field monitors for 18 minutes, " +
                                  "resulting in a 30-minute notification delay to newly affected residential area.",
                ObservedAt = baseDate.AddHours(11).AddMinutes(20),
                RecordedAt = baseDate.AddHours(11).AddMinutes(28),
                CreatedAt = baseDate.AddHours(11).AddMinutes(28), UpdatedAt = baseDate.AddHours(11).AddMinutes(28),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new EegEntry
            {
                Id = HazEeg4Id,
                CriticalTaskId = HazTask_Security_1,
                OrganizationId = BetaOrganizationId,
                EvaluatorId = eval2,
                TriggeringInjectId = HazmatInject1Id,
                Rating = PerformanceRating.Performed,
                ObservationText = "Shelter-in-place notification executed via reverse 911 within 12 minutes of " +
                                  "initial hazard confirmation. Social media alert posted 3 minutes later on " +
                                  "county emergency channels. School district notified and confirmed lockdown " +
                                  "at all 4 schools in the affected zone within 8 minutes. Media advisory " +
                                  "issued to local stations with shelter-in-place guidance.",
                ObservedAt = baseDate.AddHours(9).AddMinutes(35),
                RecordedAt = baseDate.AddHours(9).AddMinutes(42),
                CreatedAt = baseDate.AddHours(9).AddMinutes(42), UpdatedAt = baseDate.AddHours(9).AddMinutes(42),
                CreatedBy = eval2, ModifiedBy = eval2
            },
            new EegEntry
            {
                Id = HazEeg5Id,
                CriticalTaskId = HazTask_Security_2,
                OrganizationId = BetaOrganizationId,
                EvaluatorId = eval2,
                TriggeringInjectId = HazmatInject3Id,
                Rating = PerformanceRating.SomeChallenges,
                ObservationText = "Evacuation routes identified and activated per county evacuation plan. Bus " +
                                  "staging area established at Park & Ride lot. However, coordination with " +
                                  "Lincoln Elementary was delayed 20 minutes because the school's emergency " +
                                  "contact number was outdated in the county database. School was reached via " +
                                  "the principal's personal cell. All students evacuated safely once contact " +
                                  "was established.",
                ObservedAt = baseDate.AddHours(12).AddMinutes(0),
                RecordedAt = baseDate.AddHours(12).AddMinutes(8),
                CreatedAt = baseDate.AddHours(12).AddMinutes(8), UpdatedAt = baseDate.AddHours(12).AddMinutes(8),
                CreatedBy = eval2, ModifiedBy = eval2
            },
            new EegEntry
            {
                Id = HazEeg6Id,
                CriticalTaskId = HazTask_OpComm_1,
                OrganizationId = BetaOrganizationId,
                EvaluatorId = eval2,
                TriggeringInjectId = HazmatInject2Id,
                Rating = PerformanceRating.Performed,
                ObservationText = "HAZMAT tactical talk group activated and all units confirmed on frequency " +
                                  "within 13 minutes. Command channel properly separated from tactical. " +
                                  "Mutual aid units from Metro Fire patched via regional interoperability " +
                                  "channel. HAZMAT Branch Director maintained clear radio discipline. " +
                                  "No cross-talk or communication gaps observed during the assessment phase.",
                ObservedAt = baseDate.AddHours(10).AddMinutes(15),
                RecordedAt = baseDate.AddHours(10).AddMinutes(22),
                CreatedAt = baseDate.AddHours(10).AddMinutes(22), UpdatedAt = baseDate.AddHours(10).AddMinutes(22),
                CreatedBy = eval2, ModifiedBy = eval2
            },
            new EegEntry
            {
                Id = HazEeg7Id,
                CriticalTaskId = HazTask_OpComm_2,
                OrganizationId = BetaOrganizationId,
                EvaluatorId = eval2,
                TriggeringInjectId = HazmatInject3Id,
                Rating = PerformanceRating.SomeChallenges,
                ObservationText = "When primary repeater experienced interference, backup communications " +
                                  "activation was attempted. Satellite phone (designated backup) had a " +
                                  "depleted battery and could not be powered on. Team pivoted to cellular " +
                                  "phones as tertiary backup within 4 minutes. Communication was maintained " +
                                  "but with reduced reliability during the 12-minute primary outage period.",
                ObservedAt = baseDate.AddHours(13).AddMinutes(5),
                RecordedAt = baseDate.AddHours(13).AddMinutes(12),
                CreatedAt = baseDate.AddHours(13).AddMinutes(12), UpdatedAt = baseDate.AddHours(13).AddMinutes(12),
                CreatedBy = eval2, ModifiedBy = eval2
            },
            new EegEntry
            {
                Id = HazEeg8Id,
                CriticalTaskId = HazTask_EMS_1,
                OrganizationId = BetaOrganizationId,
                EvaluatorId = eval1,
                TriggeringInjectId = HazmatInject3Id,
                Rating = PerformanceRating.Performed,
                ObservationText = "Mass decontamination corridor was operational within 42 minutes of the " +
                                  "decision to decon, well within the 60-minute standard. Two-stage " +
                                  "gross/technical decon setup executed per HAZMAT SOP. All decon team " +
                                  "members wore appropriate Level B PPE. Water supply from Station 4 " +
                                  "hydrant was adequate. Patient throughput of 15 per hour achieved.",
                ObservedAt = baseDate.AddHours(12).AddMinutes(45),
                RecordedAt = baseDate.AddHours(12).AddMinutes(52),
                CreatedAt = baseDate.AddHours(12).AddMinutes(52), UpdatedAt = baseDate.AddHours(12).AddMinutes(52),
                CreatedBy = eval1, ModifiedBy = eval1
            },
            new EegEntry
            {
                Id = HazEeg9Id,
                CriticalTaskId = HazTask_EMS_2,
                OrganizationId = BetaOrganizationId,
                EvaluatorId = eval1,
                TriggeringInjectId = HazmatInject3Id,
                Rating = PerformanceRating.SomeChallenges,
                ObservationText = "Patient exposure tracking and registration system was functional. All " +
                                  "patients passing through decon were logged with name, estimated exposure " +
                                  "duration, and presenting symptoms. Hospital notification included exposure " +
                                  "data. However, registration of non-ambulatory patients created a bottleneck " +
                                  "as decon team members had to assist with data collection while maintaining " +
                                  "decon throughput. Average registration time was 4 minutes for ambulatory " +
                                  "vs 9 minutes for non-ambulatory patients.",
                ObservedAt = baseDate.AddHours(13).AddMinutes(35),
                RecordedAt = baseDate.AddHours(13).AddMinutes(42),
                CreatedAt = baseDate.AddHours(13).AddMinutes(42), UpdatedAt = baseDate.AddHours(13).AddMinutes(42),
                CreatedBy = eval1, ModifiedBy = eval1
            }
        };
    }

    #endregion
}
